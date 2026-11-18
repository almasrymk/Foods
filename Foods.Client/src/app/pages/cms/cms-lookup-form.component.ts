import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CmsService, CountryRow, CuisineRow, NamedRow, UnitRow } from '../../core/cms.service';
import { findLookup } from './lookup-kinds';
import { IconComponent } from '../../core/icon.component';
import { PhotoComponent } from '../../core/photo.component';
import { CmsColorPickerComponent } from './cms-color-picker.component';
import { CmsIconPickerComponent } from './cms-icon-picker.component';
import { CmsTone } from './cms-palette';

@Component({
  selector: 'app-cms-lookup-form',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    IconComponent,
    PhotoComponent,
    CmsIconPickerComponent,
    CmsColorPickerComponent
  ],
  template: `
    <a class="back" [routerLink]="listLink">الرجوع للقائمة</a>
    <h1><app-icon [name]="id ? 'edit' : 'add'"></app-icon> {{ id ? 'تعديل' : 'إضافة' }} {{ title }}</h1>
    <p class="error" *ngIf="error">{{ error }}</p>
    <form class="panel editor" (ngSubmit)="save()">
      <div class="fields">
        <label>الاسم<input [(ngModel)]="name" name="name" required></label>
        <label *ngIf="dialect">اللهجة<input [(ngModel)]="dialectName" name="dialect" placeholder="مصري، شامي، خليجي"></label>
        <label *ngIf="dialect">العملة<input [(ngModel)]="currency" name="currency" placeholder="ج.م، ر.س، د.م">
          <small>الأسعار في البلد دي بتظهر بالعملة دي</small>
        </label>
        <label *ngIf="unit">الاختصار<input [(ngModel)]="nickName" name="nick"></label>
        <label *ngIf="cuisine || sorted">الترتيب<input [(ngModel)]="sort" name="sort" type="number" min="0" step="1">
          <small>الأصغر يظهر الأول</small>
        </label>
        <label *ngIf="cuisine">بلد المطبخ
          <select [(ngModel)]="countryId" name="country">
            <option [ngValue]="null">من الاسم (تلقائي)</option>
            <option *ngFor="let c of countries" [ngValue]="c.id">{{ c.name }}</option>
          </select>
          <small>عملاء البلد دي يشوفوا المطبخ ده الأول، والوجبة تفتح عليه</small>
        </label>
      </div>
      <section *ngIf="dialect">
        <h2>البلاد المجاورة</h2>
        <p class="muted">القوائم بتبدأ بالبلد اللي العميل فيها، وبعدها البلاد المجاورة دي، وبعدها الباقي.</p>
        <div class="checks">
          <label *ngFor="let c of neighborChoices"><input type="checkbox" [checked]="neighbors.has(c.id)" (change)="toggleNeighbor(c.id)"> {{ c.name }}</label>
        </div>
      </section>
      <section *ngIf="cuisine">
        <h2>الصورة</h2>
        <div class="file-row">
          <app-photo [path]="imagePath" icon="category" alt=""></app-photo>
          <label class="file-btn">اختيار صورة<input type="file" accept="image/*" (change)="onImage($event)"></label>
          <button type="button" class="ghost" *ngIf="imagePath" (click)="imagePath = ''">إزالة الصورة</button>
        </div>
      </section>
      <section>
        <h2>الأيقونة</h2>
        <app-cms-icon-picker
          [value]="iconPath"
          (valueChange)="onIconKey($event)"
          (upload)="onIconUpload($event)">
        </app-cms-icon-picker>
      </section>
      <section>
        <h2>اللون</h2>
        <app-cms-color-picker
          [color]="color"
          [textColor]="textColor"
          (pickTone)="pick($event)">
        </app-cms-color-picker>
      </section>
      <button type="submit" class="save">حفظ</button>
    </form>
  `
})
export class CmsLookupFormComponent implements OnInit {
  title = '';
  unit = false;
  dialect = false;
  cuisine = false;
  sorted = false;
  name = '';
  dialectName = '';
  nickName = '';
  sort = 0;
  countryId: string | null = null;
  countries: NamedRow[] = [];
  currency = '';
  neighbors = new Set<string>();

  get neighborChoices(): NamedRow[] {
    return this.countries.filter(c => c.id !== this.id);
  }

  toggleNeighbor(id: string): void {
    if (this.neighbors.has(id)) this.neighbors.delete(id); else this.neighbors.add(id);
    this.neighbors = new Set(this.neighbors);
  }
  iconPath = '';
  imagePath = '';
  color = '';
  textColor = '';
  id = '';
  error = '';
  listLink = '/cms/lookups';
  private controller = '';

  constructor(private cms: CmsService, private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    const kind = findLookup(this.route.snapshot.paramMap.get('kind') || '');
    if (!kind) { this.error = 'القائمة غير موجودة'; return; }
    this.title = kind.title;
    this.unit = !!kind.unit;
    this.dialect = !!kind.dialect;
    this.cuisine = !!kind.cuisine;
    this.sorted = !!kind.sorted;
    this.controller = kind.key;
    this.listLink = `/cms/lookups/${kind.key}`;
    if (this.cuisine || this.dialect) this.cms.list<NamedRow>('Country').subscribe(rows => this.countries = rows);
    this.id = this.route.snapshot.paramMap.get('id') || '';
    if (!this.id) return;
    this.cms.get<NamedRow & UnitRow & CountryRow & CuisineRow>(this.controller, this.id).subscribe({
      next: row => {
        this.name = row.name || '';
        this.nickName = row.nickName || '';
        this.dialectName = row.dialect || '';
        this.currency = (row as CountryRow).currency || '';
        this.neighbors = new Set(((row as CountryRow).neighbors || '').split(',').map(e => e.trim()).filter(Boolean));
        this.sort = row.sort ?? 0;
        this.countryId = (row as CuisineRow).countryId || null;
        this.iconPath = row.iconPath || '';
        this.imagePath = row.imagePath || '';
        this.color = row.color || '';
        this.textColor = row.textColor || '';
      },
      error: (err: Error) => this.error = err.message
    });
  }

  onIconKey(key: string): void {
    this.iconPath = key;
  }

  onImage(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.cms.upload(file).subscribe({
      next: path => this.imagePath = path,
      error: (err: Error) => this.error = err.message
    });
  }

  onIconUpload(file: File): void {
    this.cms.upload(file).subscribe({
      next: path => this.iconPath = path,
      error: (err: Error) => this.error = err.message
    });
  }

  pick(tone: CmsTone): void {
    this.color = tone.color;
    this.textColor = tone.textColor;
  }

  save(): void {
    const style = {
      iconPath: this.iconPath || null,
      color: this.color || null,
      textColor: this.textColor || null
    };
    const body = this.unit
      ? { id: this.id, name: this.name, nickName: this.nickName || null, basicQuantity: 1, beDivided: true, ...style }
      : this.dialect
        ? { id: this.id, name: this.name, dialect: this.dialectName || null, currency: this.currency.trim() || null, neighbors: [...this.neighbors].join(',') || null, ...style }
        : this.cuisine
          ? { id: this.id, name: this.name, ...style, imagePath: this.imagePath || null, sort: Number(this.sort) || 0, countryId: this.countryId || null }
          : this.sorted
            ? { id: this.id, name: this.name, ...style, sort: Number(this.sort) || 0 }
            : { id: this.id, name: this.name, ...style };
    const done = () => this.router.navigateByUrl(this.listLink);
    const fail = (err: Error) => this.error = err.message;
    if (this.id) this.cms.update(this.controller, body).subscribe({ next: done, error: fail });
    else this.cms.create(this.controller, body).subscribe({ next: done, error: fail });
  }
}
