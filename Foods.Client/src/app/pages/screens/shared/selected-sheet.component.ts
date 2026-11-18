import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { DraftService } from '../../../core/draft.service';
import { mediaUrl } from '../../../core/environment';
import { FaIconComponent } from './icon.component';

interface SheetMeal {
  id: string;
  name: string;
  image: string;
  imagePath: string | null;
  qty: number;
}

/** Bottom sheet: الأكلات المختارة — qty +/- / remove (synced to server), then متابعة → طلباتي. */
@Component({
  selector: 'fa-selected-sheet',
  imports: [RouterLink, FaIconComponent],
  template: `
    @if (ready && (meals.length > 0 || busy)) {
      @if (expanded) {
        <button type="button" class="backdrop" (click)="collapse()" aria-label="إغلاق الأكلات المختارة"></button>
      }
      <div class="sheet" dir="rtl" [class.open]="expanded" [class.busy]="busy">
        @if (!expanded) {
          <div class="bar">
            <button type="button" class="open-btn" (click)="expand()">
              <span class="ok"><fa-icon name="check" [weight]="3" /></span>
              <b class="count">{{ countLabel }}</b>
            </button>
            <i class="sep" aria-hidden="true"></i>
            @if (onIngredients) {
              <button type="button" class="cta go" (click)="onSaveOrder()" [disabled]="busy || saving">
                {{ saving ? 'جاري الحفظ...' : 'حفظ الطلب' }}
                <fa-icon name="basket" [weight]="2" />
              </button>
            } @else {
              <a class="cta go" routerLink="/ingredients" (click)="onContinue($event)" [class.disabled]="busy">
                متابعة
                <fa-icon name="arrowLeft" [weight]="2.6" />
              </a>
            }
          </div>
        } @else {
          <div class="panel">
            <div class="head">
              <button type="button" class="close" (click)="collapse()" [disabled]="busy" aria-label="إغلاق">
                <fa-icon name="x" [weight]="2.6" />
              </button>
              <h3>الأكلات المختارة ({{ count }})</h3>
              <i class="head-spacer" aria-hidden="true"></i>
            </div>

            @if (busy) {
              <div class="status" role="status" aria-live="polite">
                <span class="spin" aria-hidden="true"></span>
                <b>{{ statusText }}</b>
              </div>
            }

            <div class="rows" [class.locked]="busy">
              @for (meal of meals; track meal.id) {
                <div class="row" [class.removing]="meal.id === removingId">
                  <button type="button" class="trash" (click)="remove(meal)" [disabled]="busy" aria-label="حذف">
                    <fa-icon name="trash" [weight]="2" />
                  </button>
                  <b class="title">{{ meal.name }}</b>
                  <div class="hm-qty">
                    <button type="button" class="hm-round hm-plus" (click)="step(meal, 1)" [disabled]="busy"
                            [attr.aria-label]="'إضافة ' + meal.name">
                      <svg viewBox="0 0 24 24"><path d="M18 13h-5v5c0 .55-.45 1-1 1s-1-.45-1-1v-5H6c-.55 0-1-.45-1-1s.45-1 1-1h5V6c0-.55.45-1 1-1s1 .45 1 1v5h5c.55 0 1 .45 1 1s-.45 1-1 1z"/></svg>
                    </button>
                    @if (meal.qty > 0) {
                      <b>{{ meal.qty }}</b>
                      <button type="button" class="hm-round hm-minus" (click)="step(meal, -1)" [disabled]="busy"
                              [attr.aria-label]="'نقص ' + meal.name">
                        <svg viewBox="0 0 24 24"><path d="M18 13H6c-.55 0-1-.45-1-1s.45-1 1-1h12c.55 0 1 .45 1 1s-.45 1-1 1z"/></svg>
                      </button>
                    }
                  </div>
                </div>
              } @empty {
                <div class="empty">جاري تحديث القائمة...</div>
              }
            </div>
            @if (onIngredients) {
              <button type="button" class="cta order" (click)="onSaveOrder()" [disabled]="busy || saving">
                {{ saving ? 'جاري الحفظ...' : 'حفظ الطلب' }}
              </button>
            } @else {
              <a class="cta order" routerLink="/ingredients" (click)="onContinue($event)" [class.disabled]="busy">متابعة</a>
            }
          </div>
        }
      </div>
    }
  `,
  styles: [`
    :host { display: contents; }

    .backdrop {
      position: fixed;
      inset: 0;
      z-index: 27;
      margin: 0;
      padding: 0;
      border: 0;
      background: transparent;
      cursor: default;
      -webkit-tap-highlight-color: transparent;
    }

    .sheet {
      --s: calc(var(--fa-w) / 941);
      position: fixed;
      z-index: 28;
      left: 50%;
      transform: translateX(-50%);
      bottom: calc(178 * var(--s) + env(safe-area-inset-bottom));
      width: min(calc(920 * var(--s)), calc(100vw - 24px));
      font-family: 'Tajawal', 'Cairo', sans-serif;
      color: #2E1E12;
      box-sizing: border-box;
    }
    .sheet *, .sheet *::before, .sheet *::after { box-sizing: border-box; }
    /* Solid page-coloured backing from just above the bar down to the screen bottom, so the bar and
       the tab bar under it read as one block and no scrolled content shows through the gap. */
    .sheet:not(.open)::before {
      content: ""; position: absolute; z-index: -1; pointer-events: none;
      left: 50%; transform: translateX(-50%); width: 100vw;
      /* stops exactly at the top edge of the tab bar (22 + 126 tab units ≈ 149 page units) so it never covers it */
      top: calc(-14 * var(--s)); bottom: calc(-28.9 * var(--s));
      background: linear-gradient(180deg, rgba(253, 248, 243, 0), #FDF8F3 calc(14 * var(--s)));
    }

    .bar {
      width: 100%;
      min-height: calc(100 * var(--s));
      display: flex;
      flex-direction: row;
      align-items: center;
      gap: calc(12 * var(--s));
      padding: calc(12 * var(--s)) calc(18 * var(--s));
      border-radius: calc(50 * var(--s));
      background: #FFFCF9;
      box-shadow: 0 calc(6 * var(--s)) calc(22 * var(--s)) rgba(120, 72, 30, .12);
    }

    .open-btn {
      flex: 1;
      min-width: 0;
      display: flex;
      align-items: center;
      justify-content: flex-start;
      gap: calc(14 * var(--s));
      margin: 0;
      padding: 0;
      border: 0;
      background: transparent !important;
      color: inherit !important;
      cursor: pointer;
      text-align: right;
      font: inherit;
    }
    .open-btn:disabled { opacity: .7; cursor: wait; }

    .ok {
      width: calc(52 * var(--s));
      height: calc(52 * var(--s));
      border-radius: 50%;
      display: grid;
      place-items: center;
      background: #E4F0D2;
      color: #2F7D1E;
      font-size: calc(28 * var(--s));
      flex: none;
    }
    .ok fa-icon, .ok svg { width: 1em; height: 1em; display: block; }

    .count {
      font-size: calc(30 * var(--s));
      font-weight: 800;
      color: #2F7D1E;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .sep {
      width: calc(2 * var(--s));
      height: calc(48 * var(--s));
      background: #E9DCCB;
      flex: none;
      border-radius: 1px;
    }

    .cta {
      display: inline-flex;
      flex-direction: row;
      align-items: center;
      justify-content: center;
      gap: calc(12 * var(--s));
      height: calc(72 * var(--s));
      border-radius: calc(36 * var(--s));
      background: linear-gradient(90deg, #FF8A12, #F35E02);
      color: #fff !important;
      box-shadow: 0 calc(6 * var(--s)) calc(14 * var(--s)) rgba(243, 94, 2, .3);
      font-size: calc(28 * var(--s));
      font-weight: 800;
      text-decoration: none;
      line-height: 1;
    }
    .cta.disabled, .cta:disabled {
      pointer-events: none;
      opacity: .55;
      cursor: wait;
    }
    button.cta {
      border: 0;
      cursor: pointer;
      font-family: inherit;
    }
    .cta fa-icon, .cta svg { width: calc(28 * var(--s)); height: calc(28 * var(--s)); display: block; }
    .go {
      width: calc(260 * var(--s));
      flex: none; white-space: nowrap;
      padding: 0 calc(18 * var(--s));
    }
    .order {
      width: 100%;
      flex: none;
      margin-top: calc(4 * var(--s));
    }

    .panel {
      background: #FFFCF9;
      border-radius: calc(36 * var(--s)) calc(36 * var(--s)) calc(42 * var(--s)) calc(42 * var(--s));
      box-shadow: 0 calc(-8 * var(--s)) calc(28 * var(--s)) rgba(120, 72, 30, .16);
      padding: calc(20 * var(--s)) calc(22 * var(--s)) calc(22 * var(--s));
      max-height: calc(620 * var(--s));
      display: flex;
      flex-direction: column;
      gap: calc(14 * var(--s));
    }

    .head {
      display: flex;
      align-items: center;
      gap: calc(12 * var(--s));
    }
    .close, .head-spacer {
      width: calc(48 * var(--s));
      height: calc(48 * var(--s));
      flex: none;
    }
    .close {
      border: 0;
      border-radius: 50%;
      background: #F5EEE4 !important;
      color: #2E1E12 !important;
      display: grid;
      place-items: center;
      cursor: pointer;
      padding: 0;
      font-size: calc(24 * var(--s));
    }
    .close:disabled { opacity: .5; cursor: wait; }
    .close fa-icon, .close svg { width: 1em; height: 1em; display: block; }
    .head h3 {
      margin: 0;
      flex: 1;
      text-align: center;
      font-size: calc(30 * var(--s));
      font-weight: 800;
      color: #2E1E12;
    }

    .status {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: calc(12 * var(--s));
      min-height: calc(44 * var(--s));
      padding: calc(8 * var(--s)) calc(14 * var(--s));
      border-radius: calc(18 * var(--s));
      background: #FFF1E0;
      color: #B85A08;
      font-size: calc(24 * var(--s));
    }
    .status b { font-weight: 800; }
    .spin {
      width: calc(22 * var(--s));
      height: calc(22 * var(--s));
      border-radius: 50%;
      border: 2px solid rgba(184, 90, 8, .25);
      border-top-color: #F37202;
      animation: fa-spin .7s linear infinite;
      flex: none;
    }
    @keyframes fa-spin { to { transform: rotate(360deg); } }

    .rows {
      overflow: auto;
      display: flex;
      flex-direction: column;
      gap: calc(12 * var(--s));
      max-height: calc(360 * var(--s));
      padding-left: calc(2 * var(--s));
    }
    .rows.locked { opacity: .72; }

    .empty {
      text-align: center;
      padding: calc(24 * var(--s));
      color: #8A6A4A;
      font-size: calc(24 * var(--s));
      font-weight: 700;
    }

    .row {
      display: flex;
      align-items: center;
      gap: calc(12 * var(--s));
      min-height: calc(82 * var(--s));
      padding: calc(10 * var(--s)) calc(12 * var(--s));
      border-radius: calc(22 * var(--s));
      background: #FFF8F1;
      border: 1px solid #F0E4D6;
      transition: opacity .15s ease;
    }
    .row.removing {
      opacity: .45;
      outline: 1px dashed #E59A9A;
      background: #FFF5F5;
    }

    .trash {
      width: calc(46 * var(--s));
      height: calc(46 * var(--s));
      border: 0;
      border-radius: 50%;
      background: #FDE8E8 !important;
      color: #C0392B !important;
      display: grid;
      place-items: center;
      cursor: pointer;
      padding: 0;
      flex: none;
      font-size: calc(24 * var(--s));
    }
    .trash:disabled, .hm-round:disabled {
      opacity: .45;
      cursor: wait;
    }
    .trash fa-icon, .trash svg { width: 1em; height: 1em; display: block; }

    .title {
      flex: 1;
      min-width: 0;
      font-size: calc(26 * var(--s));
      font-weight: 800;
      color: #2E1E12;
      text-align: right;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    /* Same +/- look as home suggested meals */
    .hm-qty {
      display: flex;
      flex-direction: row;
      justify-content: center;
      align-items: center;
      gap: calc(18 * var(--s));
      flex: none;
      min-width: calc(150 * var(--s));
    }
    .hm-qty b {
      min-width: calc(28 * var(--s));
      text-align: center;
      font-size: calc(28 * var(--s));
      font-weight: 700;
      color: #2E1E12;
      line-height: 1;
    }
    .hm-round {
      width: calc(48 * var(--s));
      height: calc(48 * var(--s));
      border: 0;
      padding: 0;
      border-radius: 50%;
      display: grid;
      place-items: center;
      cursor: pointer;
      flex: none;
    }
    .hm-round svg {
      width: calc(34 * var(--s));
      height: calc(34 * var(--s));
      fill: currentColor;
      display: block;
    }
    .hm-round.hm-plus {
      color: #fff;
      background: linear-gradient(135deg, #FF9A1F, #F37202) !important;
      box-shadow: 0 2px 6px rgba(0, 0, 0, .15);
    }
    .hm-round.hm-minus {
      color: #C0392B;
      background: #fff !important;
      box-shadow: 0 2px 6px rgba(0, 0, 0, .10);
    }
  `]
})
export class FaSelectedSheetComponent implements OnInit, OnDestroy {
  @Output() continue = new EventEmitter<void>();
  meals: SheetMeal[] = [];
  count = 0;
  expanded = false;
  busy = false;
  ready = false;
  saving = false;
  onIngredients = false;
  removingId: string | null = null;
  private mode: 'idle' | 'delete' | 'save' = 'idle';
  private sub?: Subscription;
  private syncSub?: Subscription;
  private readySub?: Subscription;
  private navSub?: Subscription;

  constructor(private draft: DraftService, private router: Router) {}

  get countLabel(): string {
    if (this.count === 1) return 'تم اختيار أكلة واحدة';
    if (this.count === 2) return 'تم اختيار أكلتين';
    if (this.count >= 3 && this.count <= 10) return `تم اختيار ${this.count} أكلات`;
    return `تم اختيار ${this.count} أكلة`;
  }

  get statusText(): string {
    if (this.saving) return 'جاري حفظ الطلب...';
    if (this.mode === 'delete') return 'جاري الحذف من السيرفر...';
    if (this.mode === 'save') return 'جاري الحفظ المؤقت...';
    return 'جاري التحديث...';
  }

  ngOnInit(): void {
    this.draft.bootFromServer();
    this.ready = this.draft.isReady;
    this.syncRoute();
    this.refresh();
    this.readySub = this.draft.ready$.subscribe(ready => {
      this.ready = ready;
      this.refresh();
    });
    this.sub = this.draft.selectionCount$.subscribe(() => this.refresh());
    this.syncSub = this.draft.syncing$.subscribe(busy => {
      const wasBusy = this.busy;
      this.busy = busy;
      // Never auto-open details while selecting — only update the outer count bar.
      if (!busy && wasBusy) {
        this.removingId = null;
        this.mode = 'idle';
        this.refresh();
        if (this.count === 0) this.expanded = false;
      }
    });
    // Tab/menu navigation: collapse details back to the closed bar (do not hide).
    this.navSub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => {
        this.expanded = false;
        this.syncRoute();
      });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
    this.syncSub?.unsubscribe();
    this.readySub?.unsubscribe();
    this.navSub?.unsubscribe();
  }

  expand(): void {
    this.expanded = true;
  }

  collapse(): void {
    if (this.busy || this.saving) return;
    this.expanded = false;
  }

  onContinue(event: Event): void {
    if (this.busy || this.saving) {
      event.preventDefault();
      event.stopPropagation();
      return;
    }
    this.continue.emit();
  }

  onSaveOrder(): void {
    if (this.busy || this.saving || !this.ready || this.count === 0) return;
    this.saving = true;
    this.mode = 'save';
    this.draft.confirmOrder().subscribe({
      next: id => {
        this.saving = false;
        this.expanded = false;
        if (!id) {
          this.mode = 'idle';
          return;
        }
        this.router.navigateByUrl(`/orders/${id}`);
      },
      error: () => {
        this.saving = false;
        this.mode = 'idle';
      }
    });
  }

  private syncRoute(): void {
    const path = this.router.url.split('?')[0];
    this.onIngredients = path === '/ingredients';
  }

  step(meal: SheetMeal, delta: number): void {
    if (!this.ready) return;
    const qty = Math.max(0, meal.qty + delta);
    if (qty === 0) {
      this.remove(meal);
      return;
    }
    this.mode = 'save';
    // Optimistic qty; server save is debounced in DraftService.
    meal.qty = qty;
    this.draft.setQty({
      itemId: meal.id,
      name: meal.name,
      imagePath: meal.imagePath,
      peopleCount: null
    }, qty);
  }

  remove(meal: SheetMeal): void {
    if (this.busy || !this.ready) return;
    this.mode = 'delete';
    this.removingId = meal.id;
    this.draft.remove(meal.id);
  }

  private refresh(): void {
    if (!this.ready) {
      this.meals = [];
      this.count = 0;
      return;
    }
    const wasExpanded = this.expanded;
    const seen = new Set<string>();
    const next: SheetMeal[] = [];
    for (const item of this.draft.items) {
      const id = (item.itemId || '').trim();
      if (!id || item.repeatCount <= 0) continue;
      const key = id.toLowerCase();
      if (seen.has(key)) continue;
      seen.add(key);
      next.push({
        id,
        name: item.name,
        image: mediaUrl(item.imagePath) || 'assets/screens/foods/peppers.png',
        imagePath: item.imagePath || null,
        qty: item.repeatCount
      });
    }

    // While deleting inside an open panel, keep the removed row visible until sync finishes.
    if (this.busy && this.expanded && this.removingId) {
      const target = next.find(m => m.id === this.removingId) || this.meals.find(m => m.id === this.removingId);
      if (target && !next.some(m => m.id === this.removingId)) {
        next.unshift({ ...target });
      }
    }

    this.meals = next;
    this.count = this.draft.selectedCount;
    // Stay collapsed while picking from lists; only keep details open if user already opened them.
    this.expanded = this.count > 0 && wasExpanded;
  }
}
