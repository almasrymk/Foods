import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/api.service';
import { ContentPriceRow } from '../../../core/models';
import { mediaUrl } from '../../../core/environment';
import { currency, egp, qtyText } from '../../../core/money';

/** Change one ingredient's price for this customer only (أسعار المكونات / تفاصيل الأكلة). */
@Component({
  selector: 'fa-price-sheet',
  imports: [FormsModule],
  template: `
    @if (contentId) {
      <div class="ps-bg" (click)="close(false)"></div>
      <form class="ps" dir="rtl" role="dialog" aria-label="سعر المكون" (ngSubmit)="save()">
        @if (!row) {
          <p class="ps-wait">{{ error || 'جاري التحميل...' }}</p>
        } @else {
          <div class="ps-head">
            <img [src]="img(row.imagePath)" alt="">
            <div>
              <h3>سعر {{ row.name }}</h3>
              <small>{{ adminText }}</small>
            </div>
          </div>
          <label>السعر ({{ currencyName }})<input name="price" type="number" min="0" step="any" inputmode="decimal" [(ngModel)]="price" placeholder="مثلاً 120" dir="ltr"></label>
          <div class="ps-row">
            <label class="q">لكل<input name="qty" type="number" min="0" step="any" inputmode="decimal" [(ngModel)]="quantity" dir="ltr"></label>
            <label class="u">الوحدة
              <select name="unit" [(ngModel)]="unitId">
                @for (u of row.units; track u.id) { <option [ngValue]="u.id">{{ u.name }}</option> }
              </select>
            </label>
          </div>
          @if (error) { <p class="ps-err">{{ error }}</p> }
          <button type="submit" class="ps-save" [disabled]="busy">حفظ السعر</button>
          @if (row.isMine) {
            <button type="button" class="ps-reset" [disabled]="busy" (click)="reset()">{{ row.adminPrice ? 'رجّع سعر التطبيق' : 'امسح سعري' }}</button>
          }
          <button type="button" class="ps-cancel" (click)="close(false)">إلغاء</button>
        }
      </form>
    }
  `,
  styles: [`
    .ps-bg { position: fixed; inset: 0; z-index: 300; background: rgba(46, 30, 18, .42); }
    .ps {
      position: fixed; z-index: 301; left: 50%; bottom: 0; transform: translateX(-50%);
      width: min(560px, 100vw); box-sizing: border-box; padding: 22px 22px 26px;
      border-radius: 22px 22px 0 0; background: #FFFCF8; font-family: Tajawal, sans-serif;
      display: flex; flex-direction: column; gap: 12px; max-height: 90vh; overflow-y: auto;
    }
    .ps-wait { text-align: center; color: #8A7A6E; font-weight: 700; padding: 30px 0; margin: 0; }
    .ps-head { display: flex; gap: 14px; align-items: center; }
    .ps-head img { width: 64px; height: 64px; border-radius: 14px; object-fit: contain; background: #fff; }
    .ps-head h3 { margin: 0; font-size: 20px; font-weight: 800; color: #2E1E12; }
    .ps-head small { color: #6E625A; font-size: 14px; font-weight: 500; }
    label { display: flex; flex-direction: column; gap: 6px; font-size: 15px; font-weight: 800; color: #2E1E12; }
    input, select { height: 46px; padding: 0 14px; border-radius: 14px; border: 1.5px solid #E8D8C5; background: #fff; font: inherit; font-size: 17px; text-align: right; }
    input:focus, select:focus { outline: none; border-color: #F37202; }
    .ps-row { display: flex; gap: 12px; }
    .ps-row .q { width: 40%; }
    .ps-row .u { flex: 1; }
    .ps-err { margin: 0; color: #B71C1C; font-weight: 700; font-size: 14px; }
    .ps-save { height: 50px; border: 0; border-radius: 25px; color: #fff; font: inherit; font-size: 17px; font-weight: 800; background: linear-gradient(90deg, #F35E02, #FF8A12); cursor: pointer; }
    .ps-reset, .ps-cancel { height: 40px; border: 0; background: none; font: inherit; font-size: 15px; font-weight: 800; color: #C45A00; cursor: pointer; }
    .ps-cancel { color: #6E625A; }
    button:disabled { opacity: .6; }
  `]
})
export class FaPriceSheetComponent implements OnChanges {
  get currencyName(): string { return currency(); }
  /** The ingredient to edit; null hides the sheet. */
  @Input() contentId: string | null = null;
  /** true when the price changed. */
  @Output() closed = new EventEmitter<boolean>();

  row: ContentPriceRow | null = null;
  price: number | null = null;
  quantity = 1;
  unitId: string | null = null;
  busy = false;
  error = '';

  constructor(private api: ApiService) {}

  get adminText(): string {
    const r = this.row;
    if (!r?.adminPrice) return 'التطبيق مش حاطط سعر للمكون ده';
    return `سعر التطبيق: ${egp(r.adminPrice)} لكل ${qtyText(r.adminQuantity)} ${r.adminUnitName || ''}`;
  }

  img(path?: string | null): string {
    return mediaUrl(path) || 'assets/screens/ingredients/onion.png';
  }

  ngOnChanges(): void {
    // The page's own fixed bars (summary / tab bar) sit in another stacking layer: hide them while the sheet is open.
    document.body.classList.toggle('ps-open', !!this.contentId);
    this.row = null;
    this.error = '';
    this.busy = false;
    if (!this.contentId) return;
    this.api.prices('', this.contentId).subscribe({
      next: data => {
        const row = data.items[0] || null;
        this.row = row;
        if (!row) { this.error = 'المكون مش موجود'; return; }
        this.price = row.price ?? null;
        this.quantity = row.quantity || 1;
        this.unitId = row.priceUnitId || row.unitId;
      },
      error: (e: Error) => this.error = e.message
    });
  }

  save(): void {
    if (!this.contentId || this.busy) return;
    const price = Number(this.price || 0);
    if (price <= 0) { this.error = 'اكتب السعر'; return; }
    this.busy = true;
    this.api.saveMyPrice(this.contentId, price, Number(this.quantity || 1), this.unitId).subscribe({
      next: () => this.close(true),
      error: (e: Error) => { this.busy = false; this.error = e.message; }
    });
  }

  reset(): void {
    if (!this.contentId || this.busy) return;
    this.busy = true;
    this.api.removeMyPrice(this.contentId).subscribe({
      next: () => this.close(true),
      error: (e: Error) => { this.busy = false; this.error = e.message; }
    });
  }

  close(changed: boolean): void {
    document.body.classList.remove('ps-open');
    this.closed.emit(changed);
  }
}
