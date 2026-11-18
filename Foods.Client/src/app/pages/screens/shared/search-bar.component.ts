import { Component, EventEmitter, HostListener, Input, NgZone, OnDestroy, Output, inject } from '@angular/core';
import { ApiService, SearchExtra } from '../../../core/api.service';
import { MealCard, MealItemFilter } from '../../../core/models';

/**
 * Shared search bar: text search, voice search (Web Speech API, ar-EG) and an
 * advanced-search dropdown (category + ingredient). Sizes use --s (941px mockup).
 */
/** One section of a screen's own advanced search. */
export interface SearchFilterSection {
  key: string;
  title: string;
  options: { id: string; name?: string | null }[];
}

@Component({
  selector: 'fa-search-bar',
  standalone: true,
  template: `
    <div class="sb-row">
      <form class="sb-pill" (submit)="$event.preventDefault(); submit()">
        <button type="submit" class="sb-go" aria-label="بحث">
          <svg viewBox="0 0 24 24"><path d="M15.5 14h-.79l-.28-.27A6.47 6.47 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/></svg>
        </button>
        <input type="search" autocomplete="off"
               [placeholder]="listening ? 'اتكلم دلوقتي...' : placeholder"
               [value]="value" (input)="onInput($any($event.target).value)">
        @if (value) {
          <button type="button" class="sb-clear" aria-label="مسح" (click)="onInput('')">&times;</button>
        }
        <button type="button" class="sb-mic" [class.on]="listening" aria-label="بحث صوتي" (click)="toggleVoice()">
          <svg viewBox="0 0 24 24"><path d="M12 14c1.66 0 3-1.34 3-3V5c0-1.66-1.34-3-3-3S9 3.34 9 5v6c0 1.66 1.34 3 3 3zm-1.2-9.1c0-.66.54-1.2 1.2-1.2s1.2.54 1.2 1.2l-.01 6.2c0 .66-.53 1.2-1.19 1.2s-1.2-.54-1.2-1.2V4.9zm6.5 6.1c0 3-2.54 5.1-5.3 5.1S6.7 14 6.7 11H5c0 3.41 2.72 6.23 6 6.72V21h2v-3.28c3.28-.48 6-3.3 6-6.72h-1.7z"/></svg>
        </button>
      </form>
      @if (showAdvanced) {
        <button type="button" class="sb-tune" [class.on]="open || active || hasExtra" aria-label="بحث متقدم" (click)="toggleAdvanced()">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 6h9M17 6h3M4 12h3M11 12h9M4 18h11M19 18h1M15 4v4M9 10v4M17 16v4"/></svg>
          @if (hasExtra) { <span class="sb-dot"></span> }
        </button>
      }
    </div>
    @if (note) { <div class="sb-note">{{ note }}</div> }
    @if (panel && open && filters) {
      <!-- the screen's own sections (e.g. ingredient prices): one pick each -->
      <div class="sb-panel">
        @for (f of filters; track f.key) {
          <div class="sb-title">{{ f.title }}</div>
          <div class="sb-chips">
            <button type="button" class="sb-chip" [class.sel]="!picked[f.key]" (click)="pick(f.key, null)">الكل</button>
            @for (o of f.options; track o.id) {
              <button type="button" class="sb-chip" [class.sel]="picked[f.key] === o.id" (click)="pick(f.key, o.id)">{{ o.name }}</button>
            }
          </div>
        }
        @if (!filters.length) { <div class="sb-label">مفيش اختيارات للبحث المتقدم دلوقتي</div> }
        <div class="sb-actions">
          <button type="button" class="sb-done" (click)="done()">بحث</button>
          <button type="button" class="sb-reset" (click)="clearPicked()">مسح</button>
        </div>
      </div>
    } @else if (panel && open) {
      <div class="sb-panel">
        <div class="sb-title">الوجبة</div>
        <div class="sb-chips">
          <button type="button" class="sb-chip" [class.sel]="!mealId" (click)="mealId = null; emitExtra()">الكل</button>
          @for (m of meals; track m.id) {
            <button type="button" class="sb-chip" [class.sel]="mealId === m.id" (click)="mealId = m.id; emitExtra()">{{ m.name }}</button>
          }
        </div>
        <div class="sb-title">المطبخ</div>
        <div class="sb-chips">
          <button type="button" class="sb-chip" [class.sel]="!kitchenId" (click)="kitchenId = null; emitExtra()">الكل</button>
          @for (c of kitchens; track c.id ?? c.name) {
            <button type="button" class="sb-chip" [class.sel]="kitchenId === c.id" (click)="kitchenId = c.id ?? null; emitExtra()">{{ c.name }}</button>
          }
        </div>
        <div class="sb-title">المجموعة</div>
        <div class="sb-chips">
          <button type="button" class="sb-chip" [class.sel]="!groupId" (click)="groupId = null; emitExtra()">الكل</button>
          @for (c of groups; track c.id ?? c.name) {
            <button type="button" class="sb-chip" [class.sel]="groupId === c.id" (click)="groupId = c.id ?? null; emitExtra()">{{ c.name }}</button>
          }
        </div>
        <div class="sb-title">التصنيف <small class="sb-hint">(تقدر تختار أكتر من واحد)</small></div>
        <div class="sb-chips">
          <button type="button" class="sb-chip" [class.sel]="!categoryId" (click)="setCategory(null)">الكل</button>
          @for (c of categories; track c.id ?? c.name) {
            <button type="button" class="sb-chip" [class.sel]="hasCategory(c.id)" (click)="setCategory(c.id ?? null)">{{ c.name }}</button>
          }
        </div>
        <label class="sb-label">فيها مكوّن (ممكن أكتر من واحد بينهم فاصلة)
          <input type="text" class="sb-ing" placeholder="مثلاً: فراخ، رز" autocomplete="off"
                 [value]="ingredient" (input)="onIngredient($any($event.target).value)">
        </label>
        <div class="sb-actions">
          <button type="button" class="sb-done" (click)="done()">بحث</button>
          <button type="button" class="sb-reset" (click)="clearExtra()">مسح</button>
        </div>
      </div>
    }
  `,
  styles: [`
    :host { --s: calc(var(--fa-w, 460px) / 941); display: block; margin: 0 calc(33 * var(--s)) 0 calc(45 * var(--s)); position: relative; z-index: 20; font-family: inherit; }
    button { border: 0; padding: 0; background: transparent; cursor: pointer; font-family: inherit; box-shadow: none; }
    .sb-row { display: flex; gap: calc(17 * var(--s)); align-items: center; }
    .sb-pill { flex: 1 1 auto; min-width: 0; display: flex !important; flex-direction: row !important; flex-wrap: nowrap !important; align-items: center; gap: 0 !important; margin: 0; position: relative; overflow: hidden; height: calc(94 * var(--s)); padding: 0 calc(6 * var(--s)) 0 calc(26 * var(--s)); border-radius: calc(47 * var(--s)); background: #FFFCF9; box-shadow: 0 calc(6 * var(--s)) calc(20 * var(--s)) rgba(120, 72, 30, .12); box-sizing: border-box; }
    .sb-go { flex: none; position: static; margin: 0; width: calc(82 * var(--s)); height: calc(82 * var(--s)); border-radius: 50%; background: linear-gradient(135deg, #FF9A2E, #F36202) !important; color: #fff; display: grid; place-items: center; }
    .sb-go svg { width: calc(48 * var(--s)); height: calc(48 * var(--s)); fill: currentColor; }
    .sb-pill input { display: block; flex: 1 1 auto; width: auto !important; min-width: 0; height: 100%; position: static; margin: 0 calc(20 * var(--s)) 0 0; padding: 0; border: 0; border-radius: 0; background: none; outline: none; box-shadow: none; -webkit-appearance: none; appearance: none; font-family: inherit; font-size: calc(27 * var(--s)); font-weight: 500; color: #3B2A1E; }
    .sb-pill input::placeholder { color: #8C8079; }
    .sb-pill input::-webkit-search-cancel-button { -webkit-appearance: none; appearance: none; display: none; }
    .sb-clear { flex: none; position: static; display: grid; place-items: center; margin: 0; color: #A08D7E; font-size: calc(40 * var(--s)); line-height: 1; padding: 0 calc(10 * var(--s)); }
    .sb-mic { flex: none; position: static; margin: 0; width: calc(56 * var(--s)); height: calc(56 * var(--s)); border-radius: 50%; color: #4A3426; display: grid; place-items: center; transition: background .2s, color .2s; }
    .sb-mic svg { width: calc(46 * var(--s)); height: calc(46 * var(--s)); fill: currentColor; }
    .sb-mic.on { background: #E5262A !important; color: #fff; animation: sb-pulse 1.2s ease-out infinite; }
    @keyframes sb-pulse { 0% { box-shadow: 0 0 0 0 rgba(229, 38, 42, .55); } 100% { box-shadow: 0 0 0 calc(18 * var(--s)) rgba(229, 38, 42, 0); } }
    .sb-tune { position: relative; flex: none; width: calc(94 * var(--s)); height: calc(90 * var(--s)); border-radius: calc(30 * var(--s)); color: #fff; display: grid; place-items: center; background: radial-gradient(circle at 50% 30%, #5FA22E, #2F7514) !important; box-shadow: 0 calc(6 * var(--s)) calc(18 * var(--s)) rgba(47, 117, 20, .35); }
    .sb-tune.on { background: linear-gradient(135deg, #FF9A2E, #F36202) !important; box-shadow: 0 calc(6 * var(--s)) calc(18 * var(--s)) rgba(243, 98, 2, .35); }
    .sb-tune svg { width: calc(46 * var(--s)); height: calc(46 * var(--s)); }
    .sb-dot { position: absolute; top: calc(14 * var(--s)); right: calc(14 * var(--s)); width: calc(14 * var(--s)); height: calc(14 * var(--s)); border-radius: 50%; background: #fff; }
    .sb-note { font-size: calc(21 * var(--s)); color: #B3541E; margin-top: calc(8 * var(--s)); padding: 0 calc(20 * var(--s)); }
    .sb-panel { position: absolute; top: calc(100% + 10 * var(--s)); left: 0; right: 0; max-height: 62vh; overflow-y: auto; overscroll-behavior: contain; background: #fff; border-radius: calc(30 * var(--s)); padding: calc(24 * var(--s)); box-shadow: 0 calc(18 * var(--s)) calc(50 * var(--s)) rgba(80, 45, 15, .22); box-sizing: border-box; }
    .sb-title { font-size: calc(26 * var(--s)); font-weight: 700; color: #3B2A1E; margin-bottom: calc(14 * var(--s)); }
    .sb-hint { font-size: calc(19 * var(--s)); font-weight: 600; color: #8A6E58; }
    .sb-chips { display: flex; flex-wrap: wrap; gap: calc(12 * var(--s)); margin-bottom: calc(22 * var(--s)); }
    .sb-chip { height: calc(58 * var(--s)); padding: 0 calc(24 * var(--s)); display: inline-flex; align-items: center; justify-content: center; line-height: 1; border-radius: calc(30 * var(--s)); background: #FBF1E4; color: #6B4A2E; font-size: calc(23 * var(--s)); font-weight: 600; }
    .sb-chip.sel { background: linear-gradient(135deg, #FF9A2E, #F36202); color: #fff; }
    .sb-label { display: block; font-size: calc(23 * var(--s)); color: #6B4A2E; font-weight: 600; }
    .sb-ing { display: block; width: 100%; box-sizing: border-box; margin-top: calc(10 * var(--s)); height: calc(70 * var(--s)); padding: 0 calc(22 * var(--s)); border: calc(2 * var(--s)) solid #F1E3D3; border-radius: calc(22 * var(--s)); background: #FFFCF9; outline: none; font-family: inherit; font-size: calc(24 * var(--s)); color: #3B2A1E; }
    .sb-ing::placeholder { color: #8C8079; }
    .sb-actions { display: flex; gap: calc(14 * var(--s)); margin-top: calc(22 * var(--s)); }
    .sb-done, .sb-reset { display: inline-flex; align-items: center; justify-content: center; line-height: 1; height: calc(70 * var(--s)); border-radius: calc(35 * var(--s)); font-size: calc(25 * var(--s)); font-weight: 700; }
    .sb-done { flex: 1; background: linear-gradient(135deg, #FF9A2E, #F36202); color: #fff; }
    .sb-reset { padding: 0 calc(36 * var(--s)); background: #FBF1E4; color: #6B4A2E; }
    /* Tajawal sits high in its line box: trim to the glyphs so labels are truly centred. */
    .sb-chip, .sb-done, .sb-reset { text-box: trim-both cap alphabetic; }
  `]
})
export class FaSearchBarComponent implements OnDestroy {
  private readonly api = inject(ApiService);
  private readonly zone = inject(NgZone);

  @Input() placeholder = 'ابحث عن أكلة، وصفة، مكون ...';
  @Input() value = '';
  @Input() showAdvanced = true;
  @Input() panel = true;
  @Input() active = false;

  @Output() valueChange = new EventEmitter<string>();
  /** Explicit search: Enter / search button, voice result, or "تم" in the advanced panel. */
  @Output() searched = new EventEmitter<string>();
  @Output() extraChange = new EventEmitter<SearchExtra>();
  @Output() advanced = new EventEmitter<void>();

  open = false;
  listening = false;
  note = '';
  categories: MealItemFilter[] = [];
  kitchens: MealItemFilter[] = [];
  groups: MealItemFilter[] = [];
  meals: MealCard[] = [];
  @Input() categoryId: string | null = null;
  @Input() ingredient = '';
  @Input() kitchenId: string | null = null;
  @Input() groupId: string | null = null;
  @Input() mealId: string | null = null;
  /** A screen's own advanced-search sections (in place of the dish ones); picked holds key → id. */
  @Input() filters: SearchFilterSection[] | null = null;
  @Input() picked: Record<string, string> = {};
  @Output() pickedChange = new EventEmitter<Record<string, string>>();

  private categoriesLoaded = false;
  private recognition: any = null;
  private noteTimer: any = null;
  private ingredientTimer: any = null;

  get hasExtra(): boolean {
    if (this.filters) return Object.keys(this.picked || {}).length > 0;
    return !!this.categoryId || !!this.ingredient.trim() || !!this.kitchenId || !!this.groupId || !!this.mealId;
  }

  onInput(text: string): void {
    this.value = text;
    this.valueChange.emit(text);
  }

  submit(): void {
    this.valueChange.emit(this.value);
    this.searched.emit(this.value);
  }

  done(): void {
    this.open = false;
    if (this.ingredientTimer) {
      clearTimeout(this.ingredientTimer);
      this.ingredientTimer = null;
      this.emitExtra();
    }
    this.searched.emit(this.value);
  }

  toggleAdvanced(): void {
    if (!this.panel) {
      this.advanced.emit();
      return;
    }
    this.open = !this.open;
    if (this.open && !this.categoriesLoaded && !this.filters) {
      this.categoriesLoaded = true;
      this.api.categories().subscribe({
        next: list => (this.categories = list || []),
        error: () => (this.categoriesLoaded = false)
      });
      this.api.cuisines().subscribe({ next: list => (this.kitchens = (list || []).filter(c => !!c.id)) });
      this.api.groups().subscribe({ next: list => (this.groups = list || []) });
      this.api.meals().subscribe({ next: list => (this.meals = list || []) });
    }
  }

  /** The picked categories (categoryId holds them joined by commas). */
  private get pickedCategories(): string[] {
    return (this.categoryId || '').split(',').map(e => e.trim()).filter(Boolean);
  }

  hasCategory(id: string | null | undefined): boolean {
    return !!id && this.pickedCategories.includes(id);
  }

  /** "الكل" clears the categories; any other chip is added or removed (more than one can be picked). */
  setCategory(id: string | null): void {
    if (!id) {
      this.categoryId = null;
    } else {
      const now = this.pickedCategories;
      const next = now.includes(id) ? now.filter(e => e !== id) : [...now, id];
      this.categoryId = next.length ? next.join(',') : null;
    }
    this.emitExtra();
  }

  onIngredient(text: string): void {
    this.ingredient = text;
    clearTimeout(this.ingredientTimer);
    this.ingredientTimer = setTimeout(() => { this.ingredientTimer = null; this.emitExtra(); }, 350);
  }

  pick(key: string, id: string | null): void {
    const next = { ...(this.picked || {}) };
    if (id) next[key] = id; else delete next[key];
    this.picked = next;
    this.pickedChange.emit(next);
  }

  clearPicked(): void {
    this.picked = {};
    this.pickedChange.emit({});
  }

  clearExtra(): void {
    clearTimeout(this.ingredientTimer);
    this.categoryId = null;
    this.ingredient = '';
    this.kitchenId = this.groupId = this.mealId = null;
    this.emitExtra();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (!this.open) return;
    const target = event.target as Element | null;
    if (!target?.closest?.('fa-search-bar')) this.open = false;
  }

  toggleVoice(): void {
    if (this.listening) {
      this.recognition?.stop();
      return;
    }
    const w = window as any;
    const Ctor = w.SpeechRecognition || w.webkitSpeechRecognition;
    if (!Ctor) {
      this.showNote('البحث بالصوت مش مدعوم في المتصفح ده، جرّب Chrome أو Edge.');
      return;
    }
    const rec = new Ctor();
    rec.lang = 'ar-EG';
    rec.interimResults = true;
    rec.continuous = false;
    // Speaking ended: close the mic and search once (some phones never fire "end", or fire it late).
    let done = false;
    const finish = () => {
      if (done) return;
      done = true;
      this.listening = false;
      if (this.recognition === rec) this.recognition = null;
      if (this.value.trim()) this.searched.emit(this.value);
    };
    rec.onresult = (e: any) => this.zone.run(() => {
      let text = '';
      let final = false;
      for (let i = 0; i < e.results.length; i++) {
        text += e.results[i][0].transcript;
        if (e.results[i].isFinal) final = true;
      }
      text = text.trim().replace(/[.,!?؟،؛:;]+$/u, '').trim();
      this.onInput(text);
      if (final) {
        try { rec.stop(); } catch { /* already stopped */ }
        finish();
      }
    });
    rec.onerror = (e: any) => this.zone.run(() => {
      const err = e?.error;
      if (err === 'not-allowed') this.showNote('اسمح للمتصفح يستخدم المايك عشان البحث بالصوت');
      else if (err === 'no-speech') this.showNote('مسمعتش حاجة، جرّب تاني');
      else if (err !== 'aborted') this.showNote('حصلت مشكلة في البحث بالصوت');
    });
    rec.onend = () => this.zone.run(finish);
    this.recognition = rec;
    try {
      rec.start();
      this.listening = true;
      this.note = '';
    } catch {
      this.recognition = null;
      this.showNote('حصلت مشكلة في البحث بالصوت');
    }
  }

  ngOnDestroy(): void {
    clearTimeout(this.noteTimer);
    clearTimeout(this.ingredientTimer);
    if (this.recognition) {
      this.recognition.onresult = this.recognition.onerror = this.recognition.onend = null;
      this.recognition.abort();
      this.recognition = null;
    }
  }

  emitExtra(): void {
    const ing = this.ingredient.trim();
    this.extraChange.emit({
      categoryId: this.categoryId || null,
      ingredient: ing || null,
      kitchenId: this.kitchenId || null,
      groupId: this.groupId || null,
      mealId: this.mealId || null
    });
  }

  private showNote(text: string): void {
    this.note = text;
    clearTimeout(this.noteTimer);
    this.noteTimer = setTimeout(() => this.zone.run(() => (this.note = '')), 4000);
  }
}
