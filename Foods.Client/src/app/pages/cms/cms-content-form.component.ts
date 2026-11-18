import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CmsService, ContentCountryRow, ContentRow, CountryRow, NamedRow, UnitRow } from '../../core/cms.service';
import { mediaUrl } from '../../core/environment';
import { IconComponent } from '../../core/icon.component';
import { PhotoComponent } from '../../core/photo.component';
import { Select2Component } from '../../core/select2.component';
import { CmsAlbumComponent } from './cms-album.component';

@Component({
  selector: 'app-cms-content-form',
  imports: [CommonModule, FormsModule, RouterLink, IconComponent, PhotoComponent, Select2Component, CmsAlbumComponent],
  template: `
    <a class="back" routerLink="/cms/contents">الرجوع للقائمة</a>
    <h1><app-icon [name]="form.id ? 'edit' : 'add'"></app-icon> {{ form.id ? 'تعديل مكوّن' : 'إضافة مكوّن' }}</h1>
    <p class="error" *ngIf="error">{{ error }}</p>
    <form class="panel editor" (ngSubmit)="save()">
      <div class="fields">
        <label>الاسم<input [(ngModel)]="form.name" name="name" required></label>
        <label>أقل كمية<input type="number" step="0.01" [(ngModel)]="form.miniQuantity" name="mini"></label>
        <label>التصنيف
          <app-select2 name="category" [(ngModel)]="form.contentCategoryId" [options]="categories" placeholder="اختار التصنيف"></app-select2>
        </label>
        <label>المجموعة
          <app-select2 name="group" [(ngModel)]="form.contentGroupId" [options]="groups" placeholder="اختار المجموعة"></app-select2>
        </label>
        <label>الوحدة
          <app-select2 name="unit" [(ngModel)]="form.unitId" [options]="units" placeholder="اختار الوحدة"></app-select2>
        </label>
        <label>مكان الشراء
          <app-select2 name="place" [(ngModel)]="form.purchasePlaceId" [options]="places" placeholder="بدون" [allowEmpty]="true" emptyLabel="بدون"></app-select2>
        </label>
        <label>المطبخ
          <app-select2 name="cuisine" [(ngModel)]="form.cuisineId" [options]="cuisines" placeholder="بدون مطبخ" [allowEmpty]="true" emptyLabel="بدون مطبخ"></app-select2>
        </label>
        <label>الترتيب<input type="number" min="0" step="1" [(ngModel)]="form.sort" name="sort">
          <small>الأصغر يظهر الأول</small>
        </label>
      </div>
      <section class="purchase">
        <h2>أقل وحدة شراء</h2>
        <p class="muted">بتحدد المكون بيتشرى إزاي، والعداد في "طلباتي" بيزيد وينقص على أساسها.</p>
        <div class="fields">
          <label>طريقة الشراء
            <select [(ngModel)]="form.purchaseMode" name="pmode" (ngModelChange)="onMode($event)">
              <option [ngValue]="0">عادي (بالجرام أو العدد زي ما هو)</option>
              <option [ngValue]="1">عبوة (زجاجة، علبة، كيس...)</option>
              <option [ngValue]="2">كسور الكيلو (1/8، 1/4، 1/2 كيلو...)</option>
              <option [ngValue]="3">بالفلوس (بعشرين جنيه...)</option>
              <option [ngValue]="4">بالفلوس لحد 1/8 كيلو</option>
            </select>
          </label>
          <ng-container *ngIf="form.purchaseMode === 1">
            <label>اسم العبوة<input [(ngModel)]="form.packageName" name="pname" placeholder="زجاجة"></label>
            <label>العبوة فيها كام (بوحدة الوصفة، مثلاً 1000 ملليتر)<input type="number" min="0" step="1" [(ngModel)]="form.purchaseStep" name="pstep"></label>
          </ng-container>
          <ng-container *ngIf="form.purchaseMode === 2">
            <label>أقل كمية
              <select [(ngModel)]="form.purchaseStep" name="pfrac">
                <option [ngValue]="125">1/8 كيلو</option>
                <option [ngValue]="250">1/4 كيلو</option>
                <option [ngValue]="333.333">1/3 كيلو</option>
                <option [ngValue]="500">1/2 كيلو</option>
                <option [ngValue]="1000">1 كيلو</option>
              </select>
            </label>
          </ng-container>
          <ng-container *ngIf="form.purchaseMode === 3">
            <label>أقل مبلغ (جنيه)<input type="number" min="0" step="1" [(ngModel)]="form.purchaseStep" name="pmoney"></label>
            <label>العداد يزود كام جنيه<input type="number" min="0" step="1" list="money-incs" [(ngModel)]="form.purchaseIncrement" name="pinc"></label>
          </ng-container>
          <ng-container *ngIf="form.purchaseMode === 4">
            <label>سعر الـ 1/8 كيلو (جنيه)<input type="number" min="0" step="1" [(ngModel)]="form.purchaseUnitPrice" name="pprice">
              <small>لما المبلغ يوصل لسعر الـ 1/8 كيلو يتحسب بكسور الكيلو</small>
            </label>
            <label>أقل مبلغ (جنيه)<input type="number" min="0" step="1" [(ngModel)]="form.purchaseMinAmount" name="pminamount"></label>
            <label>الزيادة كل مرة (جنيه)<input type="number" min="0" step="1" list="money-incs" [(ngModel)]="form.purchaseIncrement" name="pinc4"></label>
          </ng-container>
          <datalist id="money-incs"><option value="1"></option><option value="5"></option><option value="10"></option></datalist>
        </div>
      </section>
      <section class="purchase">
        <h2>السعر</h2>
        <p class="muted">سعر المكون بيتحسب منه سعر الأكلة والطلب للعميل اللي مشغّل الأسعار. العميل يقدر يغيّره لنفسه.</p>
        <div class="fields">
          <label>السعر (جنيه)<input type="number" min="0" step="0.5" [(ngModel)]="form.price" name="price"></label>
          <label>لكل كمية<input type="number" min="0" step="1" [(ngModel)]="form.priceQuantity" name="pqty"></label>
          <label>الوحدة
            <app-select2 name="punit" [(ngModel)]="form.priceUnitId" [options]="units" placeholder="وحدة المكون" [allowEmpty]="true" emptyLabel="وحدة المكون"></app-select2>
          </label>
        </div>
        <p class="muted" *ngIf="(form.price || 0) > 0">{{ form.price }} جنيه لكل {{ form.priceQuantity || 1 }} {{ priceUnitName() }}</p>
        <p class="muted">ده السعر العام بالجنيه. البلاد اللي عملتها غير الجنيه بتاخد سعرها من "المكون في كل بلد" تحت.</p>
      </section>
      <section>
        <h2>المكون في كل بلد</h2>
        <p class="muted">اكتب اسم المكون ووصفه بلهجة كل بلد، وسعره بعملتها. العميل بيشوف الاسم بلهجة بلده، والسعر بتاع البلد اللي هو فيها.</p>
        <div class="country-list" *ngIf="countryRows.length">
          <div class="country-card" *ngFor="let row of countryRows; let i = index">
            <div class="country-head">
              <div class="country-select">
                <app-select2 [name]="'cc' + i" [(ngModel)]="row.countryId" [options]="countryChoices" placeholder="البلد"></app-select2>
              </div>
              <button type="button" class="country-remove" (click)="countryRows.splice(i, 1)"><app-icon name="delete"></app-icon> حذف</button>
            </div>
            <div class="country-fields">
              <label>الاسم<input [(ngModel)]="row.name" [name]="'ccn' + i" placeholder="الاسم باللهجة"></label>
              <label>السعر ({{ currencyOf(row.countryId) }})<input type="number" min="0" step="0.5" [(ngModel)]="row.price" [name]="'ccp' + i"></label>
              <label>لكل كمية<input type="number" min="0" step="1" [(ngModel)]="row.priceQuantity" [name]="'ccq' + i"></label>
              <label>الوحدة
                <app-select2 [name]="'ccu' + i" [(ngModel)]="row.priceUnitId" [options]="units" placeholder="وحدة المكون" [allowEmpty]="true" emptyLabel="وحدة المكون"></app-select2>
              </label>
              <label class="wide">الوصف<textarea rows="3" [(ngModel)]="row.description" [name]="'ccd' + i" placeholder="وصف المكون باللهجة"></textarea></label>
            </div>
          </div>
        </div>
        <button type="button" class="add" (click)="addCountry()"><app-icon name="add"></app-icon> بلد</button>
      </section>
      <section>
        <h2>الصورة</h2>
        <div class="file-row">
          <app-photo [path]="form.imagePath" icon="contents" alt=""></app-photo>
          <label class="file-btn">اختيار صورة<input type="file" accept="image/*" (change)="onFile($event)"></label>
        </div>
      </section>
      <section>
        <h2>ألبوم الصور</h2>
        <app-cms-album kind="content" [ownerId]="form.id"></app-cms-album>
      </section>
      <button type="submit" class="save">حفظ</button>
    </form>
  `,
  styles: [`
    .country-list { display: flex; flex-direction: column; gap: 14px; width: 100%; align-self: stretch; margin-bottom: 10px; }
    .country-card { display: flex; flex-direction: column; gap: 14px; width: 100%; box-sizing: border-box; background: #fff; border-radius: 18px; padding: 16px 18px; box-shadow: inset 0 0 0 1px var(--line); }
    .country-head { display: flex; align-items: center; gap: 14px; flex-wrap: wrap; }
    .country-select { flex: 1 1 260px; min-width: 0; max-width: 420px; }
    .country-remove { margin-inline-start: auto; width: auto; padding: 6px 12px; border-radius: 12px; background: transparent; color: #c0392b; box-shadow: inset 0 0 0 1px #e8b4ad; font-size: .92rem; display: inline-flex; align-items: center; gap: 6px; }
    .country-fields { display: grid; grid-template-columns: 1fr 1fr; gap: 12px 14px; }
    .country-fields label { min-width: 0; }
    .country-fields .wide { grid-column: 1 / -1; }
    @media (max-width: 700px) { .country-fields { grid-template-columns: 1fr; } .country-card { padding: 14px; } }
  `]
})
export class CmsContentFormComponent implements OnInit {
  categories: NamedRow[] = [];
  groups: NamedRow[] = [];
  units: UnitRow[] = [];
  places: NamedRow[] = [];
  cuisines: NamedRow[] = [];
  countries: CountryRow[] = [];
  countryRows: ContentCountryRow[] = [];

  get countryChoices(): NamedRow[] {
    return this.countries.map(c => ({ id: c.id, name: c.currency ? `${c.name} · ${c.currency}` : c.name }));
  }

  currencyOf(countryId: string): string {
    return this.countries.find(c => c.id === countryId)?.currency || 'ج.م';
  }

  addCountry(): void {
    const used = new Set(this.countryRows.map(r => r.countryId));
    const next = this.countries.find(c => !used.has(c.id));
    this.countryRows.push({ countryId: next?.id || '', name: this.form.name || '', description: '', price: 0, priceQuantity: this.form.priceQuantity || 1, priceUnitId: this.form.priceUnitId || null });
  }
  form: ContentRow = { id: '', name: '', contentCategoryId: '', contentGroupId: '', unitId: '', miniQuantity: 1, imagePath: '', purchasePlaceId: null, purchaseMode: 0, packageName: '', purchaseStep: 0, purchaseIncrement: 0, purchaseUnitPrice: 0, purchaseMinAmount: 0, price: 0, priceQuantity: 1000, priceUnitId: null };
  error = '';
  image = mediaUrl;

  constructor(private cms: CmsService, private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    this.cms.list<NamedRow>('ContentCategory').subscribe(rows => this.categories = rows);
    this.cms.list<NamedRow>('ContentGroup').subscribe(rows => this.groups = rows);
    this.cms.list<UnitRow>('Unit').subscribe(rows => this.units = rows);
    this.cms.list<NamedRow>('PurchasePlace').subscribe(rows => this.places = rows);
    this.cms.list<NamedRow>('Cuisine').subscribe(rows => this.cuisines = rows);
    this.cms.list<CountryRow>('Country').subscribe(rows => this.countries = rows);
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.cms.contentCountries(id).subscribe(rows => this.countryRows = rows.map(r => ({ ...r, name: r.name || '', description: r.description || '', priceUnitId: r.priceUnitId || null })));
    this.cms.get<ContentRow>('Content', id).subscribe({
      next: row => this.form = { ...row, purchaseMode: row.purchaseMode || 0, purchaseStep: row.purchaseStep || 0, purchaseIncrement: row.purchaseIncrement || 0, purchaseUnitPrice: row.purchaseUnitPrice || 0, purchaseMinAmount: row.purchaseMinAmount || 0, price: row.price || 0, priceQuantity: row.priceQuantity || 1, priceUnitId: row.priceUnitId || null, cuisineId: row.cuisineId || null, sort: row.sort ?? 0 },
      error: (err: Error) => this.error = err.message
    });
  }

  /** Sensible defaults when the purchase mode changes. */
  onMode(mode: number): void {
    if (mode === 1) { this.form.packageName = this.form.packageName || 'زجاجة'; this.form.purchaseStep = this.form.purchaseStep || 1000; }
    if (mode === 2) this.form.purchaseStep = [125, 250, 333.333, 500, 1000].includes(Number(this.form.purchaseStep)) ? Number(this.form.purchaseStep) : 125;
    if (mode === 3) { this.form.purchaseStep = this.form.purchaseStep && this.form.purchaseStep < 1000 ? this.form.purchaseStep : 20; this.form.purchaseIncrement = this.form.purchaseIncrement || 5; }
    if (mode === 4) {
      this.form.purchaseStep = 125;
      const inc = Number(this.form.purchaseIncrement || 0);
      this.form.purchaseIncrement = inc > 0 && inc <= 10 ? inc : 5;
      this.form.purchaseUnitPrice = this.form.purchaseUnitPrice || 40;
      this.form.purchaseMinAmount = this.form.purchaseMinAmount || 15;
    }
  }

  priceUnitName(): string {
    const id = this.form.priceUnitId || this.form.unitId;
    return this.units.find(u => u.id === id)?.name || "";
  }

  onFile(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.cms.upload(file).subscribe({
      next: path => this.form.imagePath = path,
      error: (err: Error) => this.error = err.message
    });
  }

  save(): void {
    const body = {
      id: this.form.id,
      name: this.form.name,
      contentCategoryId: this.form.contentCategoryId,
      contentGroupId: this.form.contentGroupId,
      unitId: this.form.unitId,
      miniQuantity: Number(this.form.miniQuantity),
      imagePath: this.form.imagePath || null,
      purchasePlaceId: this.form.purchasePlaceId || null,
      purchaseMode: Number(this.form.purchaseMode || 0),
      packageName: (this.form.packageName || '').trim() || null,
      purchaseStep: Number(this.form.purchaseStep || 0),
      purchaseIncrement: Number(this.form.purchaseIncrement || 0),
      purchaseUnitPrice: Number(this.form.purchaseUnitPrice || 0),
      purchaseMinAmount: Number(this.form.purchaseMinAmount || 0),
      price: Number(this.form.price || 0),
      priceQuantity: Number(this.form.priceQuantity || 1),
      priceUnitId: this.form.priceUnitId || null,
      cuisineId: this.form.cuisineId || null,
      sort: Number(this.form.sort) || 0
    };
    const picked = this.countryRows.filter(r => r.countryId);
    if (new Set(picked.map(r => r.countryId)).size !== picked.length) { this.error = 'البلد متكررة على المكون'; return; }
    this.error = '';
    const fail = (err: Error) => this.error = err.message;
    const rows = picked.map(r => ({ countryId: r.countryId, name: (r.name || '').trim() || null, description: (r.description || '').trim() || null, price: Number(r.price || 0), priceQuantity: Number(r.priceQuantity || 1), priceUnitId: r.priceUnitId || null }));
    const done = (id: string) => this.cms.saveContentCountries(id, rows).subscribe({ next: () => this.router.navigateByUrl('/cms/contents'), error: fail });
    if (this.form.id) this.cms.update('Content', body).subscribe({ next: () => done(this.form.id), error: fail });
    else this.cms.create('Content', body).subscribe({ next: id => done(id), error: fail });
  }
}
