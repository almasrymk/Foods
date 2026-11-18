import { Component, OnDestroy, OnInit } from '@angular/core';
import { ApiService } from '../../../core/api.service';
import { ContentPriceRow } from '../../../core/models';
import { mediaUrl } from '../../../core/environment';
import { egp, qtyText, setCurrency } from '../../../core/money';
import { FaScreenHeaderComponent } from '../shared/screen-header.component';
import { FaTabBarComponent } from '../shared/tab-bar.component';
import { FaIconComponent } from '../shared/icon.component';
import { FaPriceSheetComponent } from '../shared/price-sheet.component';
import { FaSearchBarComponent, SearchFilterSection } from '../shared/search-bar.component';

/** الإعدادات → أسعار المكونات: every ingredient with its price; tap one to change it for this customer. */
@Component({
  selector: 'app-prices-screen',
  imports: [FaScreenHeaderComponent, FaTabBarComponent, FaIconComponent, FaPriceSheetComponent, FaSearchBarComponent],
  template: `
    <div class="fa-screen pr" dir="rtl">
      <fa-screen-header title="أسعار المكونات" titleIcon="tag" subtitle="سعر الأكلة والطلب بيتحسب من أسعار مكوناتها" [height]="210" [titleY]="157" [backY]="120" />

      <div class="on-card">
        <fa-icon name="coins" [weight]="1.8" />
        <span class="txt"><b>التعامل بالأسعار</b><small>يظهر سعر الأكلات والطلبات</small></span>
        <button type="button" class="sw" role="switch" [class.on]="enabled" [attr.aria-checked]="enabled" aria-label="التعامل بالأسعار" (click)="toggle()"><i></i></button>
      </div>

      <!-- same search as the dishes: text, voice and the advanced-search panel -->
      <fa-search-bar class="search-bar" placeholder="ابحث عن مكون ..." [value]="text"
                     [filters]="filters" [picked]="picked" (pickedChange)="picked = $event"
                     (valueChange)="search($event)" (searched)="search($event, true)" />

      <div class="fa-chips tabs">
        <button type="button" class="fa-chip" [class.on]="!mineOnly" (click)="mineOnly = false">الكل</button>
        <button type="button" class="fa-chip" [class.on]="mineOnly" (click)="mineOnly = true">أسعاري</button>
      </div>

      <div class="list">
        @for (r of shown; track r.contentId) {
          <button type="button" class="row" [class.mine]="r.isMine" (click)="editing = r.contentId">
            <img [src]="img(r.imagePath)" alt="">
            <span class="txt">
              <b>{{ r.name }}</b>
              <span class="p" [class.none]="r.price == null">{{ label(r) }}</span>
              @if (r.isMine) { <small>{{ r.adminPrice ? 'سعرك انت · التطبيق: ' + adminLabel(r) : 'سعرك انت' }}</small> }
            </span>
            <fa-icon name="pencil" [filled]="true" />
          </button>
        }
        @if (loaded && !shown.length) { <p class="empty">{{ mineOnly ? 'لسه محطتش أسعار ليك' : 'مفيش مكونات' }}</p> }
        @if (!loaded) { <p class="empty">جاري التحميل...</p> }
        @if (error) { <p class="err">{{ error }}</p> }
      </div>
      <fa-price-sheet [contentId]="editing" (closed)="onClosed($event)" />
      <fa-tab-bar />
    </div>
  `,
  styles: [`
    .pr { padding-bottom: calc(260 * var(--s)); }
    .on-card {
      margin: calc(40 * var(--s)) calc(36 * var(--s)) 0; padding: calc(16 * var(--s)) calc(24 * var(--s));
      display: flex; align-items: center; gap: calc(18 * var(--s));
      border-radius: calc(22 * var(--s)); background: #FFF4E6; border: calc(1.5 * var(--s)) solid #F9CF9F;
    }
    .on-card > fa-icon { font-size: calc(40 * var(--s)); color: #F37202; }
    .txt { flex: 1; display: flex; flex-direction: column; align-items: flex-start; text-align: right; min-width: 0; }
    .on-card b { font-size: calc(28 * var(--s)); font-weight: 800; color: #2E1E12; }
    .on-card small { font-size: calc(21 * var(--s)); color: #6E625A; }
    .sw { position: relative; flex: none; width: calc(78 * var(--s)); height: calc(42 * var(--s)); border-radius: calc(21 * var(--s)); border: 0; padding: 0; background: #E4D9CC; cursor: pointer; }
    .sw i { position: absolute; top: calc(4 * var(--s)); left: calc(4 * var(--s)); width: calc(34 * var(--s)); height: calc(34 * var(--s)); border-radius: 50%; background: #fff; transition: left .2s; }
    .sw.on { background: linear-gradient(90deg, #FF8A12, #F36202); }
    .sw.on i { left: calc(40 * var(--s)); }
    .search-bar { margin-top: calc(24 * var(--s)); }
    .tabs { margin: calc(20 * var(--s)) calc(36 * var(--s)) 0; display: flex; gap: calc(14 * var(--s)); }
    .list { margin: calc(22 * var(--s)) calc(36 * var(--s)) 0; display: flex; flex-direction: column; gap: calc(14 * var(--s)); }
    .row {
      display: flex; align-items: center; gap: calc(20 * var(--s)); padding: calc(16 * var(--s)) calc(20 * var(--s));
      border-radius: calc(22 * var(--s)); background: #FFFCF8; border: calc(1.5 * var(--s)) solid #F2E6D8; font: inherit; cursor: pointer;
      box-shadow: 0 calc(4 * var(--s)) calc(14 * var(--s)) rgba(120, 72, 32, .07);
    }
    .row.mine { border-color: #F9CF9F; }
    .row img { width: calc(78 * var(--s)); height: calc(78 * var(--s)); border-radius: calc(14 * var(--s)); object-fit: contain; background: #fff; }
    .row b { font-size: calc(28 * var(--s)); font-weight: 800; color: #2E1E12; }
    .row .p { font-size: calc(23 * var(--s)); font-weight: 700; color: #2F6A16; }
    .row .p.none { color: #9A8B7E; }
    .row small { font-size: calc(19 * var(--s)); color: #C45A00; font-weight: 500; }
    .row > fa-icon { font-size: calc(30 * var(--s)); color: #F37202; }
    .empty { text-align: center; color: #8A7A6E; font-size: calc(26 * var(--s)); font-weight: 700; padding: calc(60 * var(--s)) 0; margin: 0; }
    .err { text-align: center; color: #B71C1C; font-weight: 700; }
  `]
})
export class PricesScreenComponent implements OnInit, OnDestroy {
  items: ContentPriceRow[] = [];
  enabled = false;
  loaded = false;
  error = '';
  mineOnly = false;
  editing: string | null = null;
  text = '';
  private timer = 0;
  private token = 0;

  constructor(private api: ApiService) {}

  filters: SearchFilterSection[] = [];
  picked: Record<string, string> = {};

  get shown(): ContentPriceRow[] {
    const picked = Object.entries(this.picked);
    return this.items.filter(r => (!this.mineOnly || r.isMine)
      && picked.every(([key, id]) => (r as any)[key + 'Id'] === id));
  }

  ngOnInit(): void { this.load(); }
  ngOnDestroy(): void { window.clearTimeout(this.timer); }

  img(path?: string | null): string { return mediaUrl(path) || 'assets/screens/ingredients/onion.png'; }

  label(r: ContentPriceRow): string {
    return r.price == null ? 'مفيش سعر' : `${egp(r.price)} لكل ${qtyText(r.quantity)} ${r.priceUnitName || ''}`.trim();
  }

  adminLabel(r: ContentPriceRow): string {
    return `${egp(r.adminPrice)} لكل ${qtyText(r.adminQuantity)} ${r.adminUnitName || ''}`.trim();
  }

  search(v: string, now = false): void {
    const text = (v || '').trim();
    if (text === this.text && !now) return;
    this.text = text;
    window.clearTimeout(this.timer);
    this.timer = window.setTimeout(() => this.load(), now ? 0 : 350);
  }

  load(): void {
    const token = ++this.token;
    this.api.prices(this.text).subscribe({
      next: data => {
        if (token !== this.token) return;
        this.items = data.items;
        this.filters = data.filters || [];
        this.enabled = data.enabled;
        if (data.currency) setCurrency(data.currency);
        this.loaded = true;
        this.error = '';
      },
      error: (e: Error) => { this.loaded = true; this.error = e.message; }
    });
  }

  toggle(): void {
    const before = this.enabled;
    this.enabled = !before;
    this.api.setPricesEnabled(!before).subscribe({ error: (e: Error) => { this.enabled = before; this.error = e.message; } });
  }

  onClosed(changed: boolean): void {
    this.editing = null;
    if (changed) this.load();
  }
}
