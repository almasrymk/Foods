import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, forkJoin, of } from 'rxjs';
import { catchError, finalize, map, switchMap, tap } from 'rxjs/operators';
import { ApiService } from './api.service';
import { OrderDetail, SelectedItem, ShoppingLine } from './models';

const ORDER_ID_KEY = 'foods.draft.orderId';
const DRAFT_NAME = 'طلباتي';

/**
 * Optimistic selection + debounced server save.
 * Guards against boot races and infinite save/retry loops.
 */
@Injectable({ providedIn: 'root' })
export class DraftService {
  orderId: string | null = null;
  name = DRAFT_NAME;
  date = new Date().toISOString().slice(0, 10);
  notes = '';
  items: SelectedItem[] = [];
  lines: ShoppingLine[] = [];

  private readonly changed$ = new BehaviorSubject<number>(0);
  readonly selectionCount$ = this.changed$.asObservable();

  private readonly busy$ = new BehaviorSubject<boolean>(false);
  readonly syncing$ = this.busy$.asObservable();

  private readonly readySubject = new BehaviorSubject<boolean>(false);
  readonly ready$ = this.readySubject.asObservable();

  private booted = false;
  private booting = false;
  private syncing = false;
  private pending: SelectedItem[] | null = null;
  private pushTimer = 0;
  private linesDirty = false;
  private localRev = 0;
  private lastSavedKey = '';
  private failCount = 0;
  private bgRefresh = false;
  /** Fingerprint of the meals the current `lines` were made for. */
  private linesFor = '';

  constructor(private api: ApiService) {
    try {
      this.orderId = sessionStorage.getItem(ORDER_ID_KEY);
    } catch {
      this.orderId = null;
    }
  }

  get selectedCount(): number {
    return this.items.filter(item => item.repeatCount > 0).length;
  }

  get isSyncing(): boolean {
    return this.busy$.value;
  }

  get isReady(): boolean {
    return this.readySubject.value;
  }

  count(itemId: string): number {
    const key = this.key(itemId);
    return this.items.find(entry => this.key(entry.itemId) === key)?.repeatCount || 0;
  }

  has(itemId: string): boolean {
    return this.count(itemId) > 0;
  }

  bootFromServer(): void {
    if (this.booted || this.booting) return;
    this.booting = true;
    this.reloadFromServer(true);
  }

  refreshFromServer(): void {
    if (this.syncing || this.booting || this.pushTimer || this.pending) return;
    this.reloadFromServer(false);
  }

  setQty(item: Pick<SelectedItem, 'itemId' | 'name' | 'imagePath' | 'peopleCount'> & { countryId?: string | null }, qty: number): void {
    const nextQty = Math.max(0, Math.floor(qty));
    const key = this.key(item.itemId);
    if (!key) return;

    let next = this.dedupe(this.items);
    if (nextQty <= 0) {
      next = next.filter(entry => this.key(entry.itemId) !== key);
    } else {
      const existing = next.find(entry => this.key(entry.itemId) === key);
      if (existing) {
        existing.repeatCount = nextQty;
        existing.peopleCount = item.peopleCount ?? existing.peopleCount;
        if (item.countryId !== undefined) existing.countryId = item.countryId;
        existing.name = item.name || existing.name;
        existing.imagePath = item.imagePath ?? existing.imagePath;
      } else {
        next.push({
          itemId: item.itemId,
          name: item.name || '',
          imagePath: item.imagePath,
          repeatCount: nextQty,
          peopleCount: item.peopleCount ?? null,
          countryId: item.countryId ?? null
        });
      }
      next = this.dedupe(next);
    }

    if (this.fingerprint(next) === this.fingerprint(this.items)) return;

    this.applyItems(next);
    this.queuePush(next);
  }

  remove(itemId: string): void {
    const key = this.key(itemId);
    const next = this.dedupe(this.items).filter(entry => this.key(entry.itemId) !== key);
    if (this.fingerprint(next) === this.fingerprint(this.items)) return;
    this.applyItems(next);
    this.queuePush(next);
  }

  /** A dish's ingredients changed (optional pick or part): let the server recalculate طلباتي. */
  recipeChanged(itemId: string): void {
    if (!this.count(itemId)) return;
    this.lines = [];
    this.linesFor = '';
    this.linesDirty = false;
    this.lastSavedKey = '';
    this.queuePush(this.dedupe(this.items), true);
  }

  /** True when `lines` belong to the meals selected now (so screens should show them, not recalculate). */
  get linesMatchItems(): boolean {
    return this.lines.length > 0 && this.linesFor === this.fingerprint(this.items);
  }

  setLines(lines: ShoppingLine[], sync = true): void {
    this.lines = lines.map(line => ({ ...line }));
    this.linesFor = this.fingerprint(this.items);
    this.linesDirty = !!sync;
    if (!sync) return;
    this.queuePush(this.dedupe(this.items), true);
  }

  loadOrder(id: string, name: string, date: string, notes: string, items: SelectedItem[], lines: ShoppingLine[]): void {
    this.applyServer({
      id,
      name: name || DRAFT_NAME,
      date,
      notes,
      items: items.map(item => ({
        itemId: item.itemId,
        name: item.name,
        imagePath: item.imagePath,
        repeatCount: item.repeatCount,
        peopleCount: item.peopleCount,
        countryId: item.countryId ?? null
      })),
      lines
    });
  }

  clear(): void {
    if (!this.items.length) return;
    this.applyItems([]);
    this.queuePush([]);
  }

  flushSync(): void {
    window.clearTimeout(this.pushTimer);
    this.pushTimer = 0;
    this.queuePush(this.dedupe(this.items), true);
  }

  /**
   * حفظ الطلب: create a confirmed order in a separate record (Status=2),
   * then clear the temporary draft (حفظ مؤقت) so selection starts fresh.
   */
  confirmOrder(): Observable<string> {
    const items = this.dedupe(this.items);
    if (!items.length) {
      return new Observable(sub => {
        sub.error(new Error('اختر أكلة واحدة على الأقل'));
      });
    }

    window.clearTimeout(this.pushTimer);
    this.pushTimer = 0;
    this.pending = null;

    const draftId = this.orderId;
    const lines = this.lines.filter(line => line.quantity > 0).map(line => ({ ...line }));
    const day = (this.date || new Date().toISOString().slice(0, 10)).slice(0, 10);
    const date = `${day}T00:00:00`;
    const name = `طلب شراء ${day}`;

    this.setBusy(true);
    return this.api.saveOrder(null, name, date, this.notes || '', items, lines, true).pipe(
      switchMap(confirmedId => {
        if (!draftId || draftId === confirmedId) return of(confirmedId);
        // Remove the temporary working order after the confirmed copy is stored.
        return this.api.saveOrder(draftId, DRAFT_NAME, date, '', [], []).pipe(
          map(() => confirmedId),
          catchError(() => of(confirmedId))
        );
      }),
      tap(() => this.resetLocal()),
      finalize(() => this.setBusy(false))
    );
  }

  /** Clear selected meals locally (and soft-delete temp draft on server if any). */
  discardWorkingSelection(): void {
    const draftId = this.orderId;
    const date = `${(this.date || new Date().toISOString().slice(0, 10)).slice(0, 10)}T00:00:00`;
    this.resetLocal();
    if (!draftId) return;
    this.api.saveOrder(draftId, DRAFT_NAME, date, '', [], []).subscribe({
      error: () => undefined
    });
  }

  private resetLocal(): void {
    window.clearTimeout(this.pushTimer);
    this.pushTimer = 0;
    this.pending = null;
    this.syncing = false;
    this.linesDirty = false;
    this.failCount = 0;
    this.lastSavedKey = '';
    this.orderId = null;
    this.storeOrderId(null);
    this.name = DRAFT_NAME;
    this.date = new Date().toISOString().slice(0, 10);
    this.notes = '';
    this.lines = [];
    this.linesFor = '';
    this.applyItems([]);
  }

  /**
   * إعادة الطلب: add an old order's meals and ingredients to the current selection.
   * Meals are not duplicated (a meal already chosen keeps one row).
   * Ingredients are merged into one row each and their quantities are added together.
   * An ingredient the user removed is brought back with its full quantity
   * (current + old order) only when the old order has it too.
   */
  mergeOrder(orderId: string): Observable<void> {
    const currentItems = this.dedupe(this.items);
    const currentLines = this.linesMatchItems ? this.lines.map(line => ({ ...line })) : [];
    return forkJoin({
      old: this.api.order(orderId),
      calc: currentItems.length ? this.api.calculate(currentItems) : of([] as ShoppingLine[])
    }).pipe(
      map(({ old, calc }) => {
        const key = (line: Pick<ShoppingLine, 'contentId' | 'unitId'>) => `${line.contentId}:${line.unitId}`;
        const active = new Map<string, ShoppingLine>();
        // No saved edits yet: everything calculated for the current meals is active.
        for (const line of (currentLines.length ? currentLines : calc)) {
          if (line.quantity > 0) active.set(key(line), { ...line });
        }
        const full = new Map(calc.map(line => [key(line), line]));
        const merged = new Map<string, ShoppingLine>(active);

        for (const line of old.lines || []) {
          if (!(line.quantity > 0)) continue;
          const k = key(line);
          const mine = merged.get(k);
          if (mine) {
            mine.quantity = this.round(mine.quantity + line.quantity);
            continue;
          }
          // Not active now: either removed by the user (bring back with all its quantity) or new.
          const removed = full.get(k);
          merged.set(k, {
            ...line,
            quantity: this.round((removed?.quantity || 0) + line.quantity)
          });
        }

        const items = this.dedupe([
          ...currentItems,
          ...(old.items || []).map(item => ({
            itemId: item.itemId,
            name: item.name || '',
            imagePath: item.imagePath,
            repeatCount: item.repeatCount || 1,
            peopleCount: item.peopleCount ?? null,
            countryId: item.countryId ?? null
          }))
        ]);
        // Keep the meals the user already chose as they are (no double count).
        for (const mine of currentItems) {
          const row = items.find(item => this.key(item.itemId) === this.key(mine.itemId));
          if (row) { row.repeatCount = mine.repeatCount; row.peopleCount = mine.peopleCount ?? row.peopleCount; }
        }

        this.applyItems(items);
        this.lines = [...merged.values()];
        this.linesFor = this.fingerprint(this.items);
        this.linesDirty = true;
        window.clearTimeout(this.pushTimer);
        this.pushTimer = 0;
        this.queuePush(this.items, true);
      })
    );
  }

  private round(value: number): number {
    return Math.round(value * 100) / 100;
  }

  private queuePush(items: SelectedItem[], immediate = false): void {
    const payload = this.dedupe(items);
    const key = this.fingerprint(payload);

    // Already saved and no ingredient edits pending — do not hit the API again.
    if (!this.linesDirty && key === this.lastSavedKey && (this.orderId || !payload.length)) {
      this.pending = null;
      return;
    }

    this.pending = payload;
    window.clearTimeout(this.pushTimer);
    this.pushTimer = 0;

    if (immediate) {
      const next = this.pending;
      this.pending = null;
      this.pushToServer(next);
      return;
    }

    this.pushTimer = window.setTimeout(() => {
      this.pushTimer = 0;
      const next = this.pending ?? this.dedupe(this.items);
      this.pending = null;
      this.pushToServer(next);
    }, 280);
  }

  private reloadFromServer(markBoot: boolean): void {
    const rev = this.localRev;
    const cachedId = this.orderId;
    if (cachedId) {
      this.api.order(cachedId).subscribe({
        next: detail => this.acceptServerLoad(detail, rev, markBoot),
        error: () => {
          // Stale cached id — fall back to name lookup once.
          this.orderId = null;
          this.storeOrderId(null);
          this.lastSavedKey = '';
          this.loadDraftByName(rev, markBoot);
        }
      });
      return;
    }
    this.loadDraftByName(rev, markBoot);
  }

  private loadDraftByName(rev: number, markBoot: boolean): void {
    this.api.orders().subscribe({
      next: orders => {
        const draft = orders.find(order => (order.name || '').trim() === DRAFT_NAME);
        if (!draft) {
          if (this.localRev !== rev && this.items.length) {
            this.finishBoot(markBoot);
            this.queuePush(this.dedupe(this.items));
            return;
          }
          this.orderId = null;
          this.storeOrderId(null);
          this.lastSavedKey = '';
          if (this.localRev === rev) {
            this.items = [];
            this.lines = [];
            this.emitCount();
          }
          this.finishBoot(markBoot);
          return;
        }
        this.api.order(draft.id).subscribe({
          next: detail => this.acceptServerLoad(detail, rev, markBoot),
          error: () => this.finishBoot(markBoot)
        });
      },
      error: () => this.finishBoot(markBoot)
    });
  }

  private acceptServerLoad(detail: OrderDetail, rev: number, markBoot: boolean): void {
    if (this.localRev !== rev) {
      this.finishBoot(markBoot);
      const serverKey = this.fingerprint((detail.items || []).map(item => ({
        itemId: item.itemId,
        name: item.name || '',
        imagePath: item.imagePath,
        repeatCount: item.repeatCount || 1,
        peopleCount: item.peopleCount ?? null,
        countryId: item.countryId ?? null
      })));
      const localKey = this.fingerprint(this.items);
      if (localKey !== serverKey) this.queuePush(this.dedupe(this.items));
      return;
    }
    this.applyServer(detail);
    this.finishBoot(markBoot);
  }

  private finishBoot(markBoot: boolean): void {
    if (markBoot) {
      this.booting = false;
      this.booted = true;
    }
    if (!this.readySubject.value) this.readySubject.next(true);
  }

  private pushToServer(items: SelectedItem[] | null): void {
    const payload = this.dedupe(items || []);
    const key = this.fingerprint(payload);

    if (!this.linesDirty && key === this.lastSavedKey && (this.orderId || !payload.length)) {
      this.pending = null;
      this.syncing = false;
      this.setBusy(false);
      return;
    }

    if (this.syncing) {
      this.pending = payload;
      return;
    }

    this.syncing = true;
    this.setBusy(true);
    const linesPayload = this.linesDirty ? this.lines.map(line => ({ ...line })) : [];
    const revAtStart = this.localRev;

    const finishOk = (savedId: string | null) => {
      this.failCount = 0;
      if (savedId) {
        this.orderId = savedId;
        this.storeOrderId(savedId);
      }
      if (this.takePendingIfNewer(key)) return;

      if (!payload.length) {
        if (this.localRev === revAtStart) {
          this.orderId = null;
          this.storeOrderId(null);
          this.lastSavedKey = '';
          this.items = [];
          this.lines = [];
          this.linesDirty = false;
          this.emitCount();
        }
        this.endSync();
        return;
      }

      this.lastSavedKey = key;
      this.linesDirty = false;
      if (this.localRev === revAtStart) {
        // Already shown optimistically — avoid extra emits that retrigger screens.
        this.items = payload.map(item => ({ ...item }));
      }
      this.endSync();
      this.refreshLinesInBackground(savedId!);
    };

    const fail = (clearOrderId = false) => {
      if (clearOrderId) {
        this.orderId = null;
        this.storeOrderId(null);
        this.lastSavedKey = '';
      }
      this.syncing = false;
      this.failCount += 1;
      if (this.failCount > 2) {
        this.failCount = 0;
        this.pending = null;
        this.setBusy(false);
        return;
      }
      const delay = 400 * this.failCount;
      window.setTimeout(() => {
        this.pushToServer(this.dedupe(this.pending ?? this.items));
      }, delay);
    };

    const save = (id: string | null) => {
      this.api.saveOrder(
        id,
        this.name || DRAFT_NAME,
        this.date,
        this.notes || '',
        payload,
        linesPayload
      ).subscribe({
        next: savedId => finishOk(savedId),
        error: err => {
          const status = err?.status ?? err?.statusCode;
          fail(status === 404 || !id);
        }
      });
    };

    if (!payload.length) {
      if (!this.orderId) {
        this.lastSavedKey = '';
        this.endSync();
        return;
      }
      this.api.saveOrder(this.orderId, this.name || DRAFT_NAME, this.date, this.notes || '', [], []).subscribe({
        next: () => finishOk(null),
        error: err => {
          const status = err?.status ?? err?.statusCode;
          fail(status === 404);
        }
      });
      return;
    }

    if (this.orderId) {
      save(this.orderId);
      return;
    }

    this.api.orders().subscribe({
      next: orders => {
        const draft = orders.find(order => (order.name || '').trim() === DRAFT_NAME);
        save(draft?.id || null);
      },
      error: () => save(null)
    });
  }

  /** If a newer edit arrived while saving, push it once (skip if identical). */
  private takePendingIfNewer(savedKey: string): boolean {
    if (!this.pending) return false;
    const queued = this.pending;
    this.pending = null;
    const queuedKey = this.fingerprint(queued);
    if (queuedKey === savedKey && !this.linesDirty) {
      this.lastSavedKey = savedKey;
      this.endSync();
      return true;
    }
    this.syncing = false;
    this.pushToServer(queued);
    return true;
  }

  private refreshLinesInBackground(savedId: string): void {
    if (this.bgRefresh) return;
    this.bgRefresh = true;
    const rev = this.localRev;
    this.api.order(savedId).subscribe({
      next: detail => {
        this.bgRefresh = false;
        if (this.localRev !== rev || this.syncing || this.pending || this.pushTimer) return;
        this.lines = (detail.lines || []).map(line => ({ ...line }));
        this.linesFor = this.fingerprint(this.items);
      },
      error: () => {
        this.bgRefresh = false;
      }
    });
  }

  private endSync(): void {
    this.syncing = false;
    if (this.pending) {
      const queued = this.pending;
      this.pending = null;
      const queuedKey = this.fingerprint(queued);
      if (queuedKey === this.lastSavedKey && !this.linesDirty) {
        this.setBusy(false);
        return;
      }
      this.pushToServer(queued);
      return;
    }
    this.setBusy(false);
  }

  private applyItems(items: SelectedItem[]): void {
    this.localRev += 1;
    this.items = this.dedupe(items);
    this.emitCount();
  }

  private applyServer(detail: OrderDetail): void {
    this.orderId = detail.id;
    this.storeOrderId(detail.id);
    this.name = detail.name || DRAFT_NAME;
    this.date = (detail.date || this.date).toString().slice(0, 10);
    this.notes = detail.notes || '';
    this.items = this.dedupe((detail.items || []).map(item => ({
      itemId: item.itemId,
      name: item.name || '',
      imagePath: item.imagePath,
      repeatCount: item.repeatCount || 1,
      peopleCount: item.peopleCount ?? null,
      countryId: item.countryId ?? null
    })));
    this.lines = (detail.lines || []).map(line => ({ ...line }));
    this.linesFor = this.fingerprint(this.items);
    this.linesDirty = false;
    this.lastSavedKey = this.fingerprint(this.items);
    this.emitCount();
  }

  private emitCount(): void {
    this.changed$.next(this.selectedCount);
  }

  private fingerprint(items: SelectedItem[]): string {
    return this.dedupe(items)
      .map(item => `${this.key(item.itemId)}:${item.repeatCount}:${item.countryId || ''}`)
      .sort()
      .join('|');
  }

  private storeOrderId(id: string | null): void {
    try {
      if (id) sessionStorage.setItem(ORDER_ID_KEY, id);
      else sessionStorage.removeItem(ORDER_ID_KEY);
    } catch { /* ignore */ }
  }

  private setBusy(value: boolean): void {
    if (this.busy$.value !== value) this.busy$.next(value);
  }

  private dedupe(items: SelectedItem[]): SelectedItem[] {
    const map = new Map<string, SelectedItem>();
    for (const item of items || []) {
      const id = (item?.itemId || '').trim();
      if (!id || !(item.repeatCount > 0)) continue;
      const key = this.key(id);
      const prev = map.get(key);
      if (!prev) {
        map.set(key, {
          itemId: id,
          name: item.name || '',
          imagePath: item.imagePath,
          repeatCount: item.repeatCount,
          peopleCount: item.peopleCount ?? null,
          countryId: item.countryId ?? null
        });
        continue;
      }
      prev.repeatCount = Math.max(prev.repeatCount, item.repeatCount);
      if (!prev.name && item.name) prev.name = item.name;
      if (!prev.imagePath && item.imagePath) prev.imagePath = item.imagePath;
      if (!prev.countryId && item.countryId) prev.countryId = item.countryId;
      if (prev.peopleCount == null && item.peopleCount != null) prev.peopleCount = item.peopleCount;
    }
    return [...map.values()];
  }

  private key(id: string): string {
    return (id || '').trim().toLowerCase();
  }
}
