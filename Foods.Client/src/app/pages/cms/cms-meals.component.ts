import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CmsService, MealRow } from '../../core/cms.service';
import { mediaUrl } from '../../core/environment';
import { CmsConfirmComponent } from './cms-confirm.component';
import { CmsPagerComponent } from './cms-pager.component';
import { IconComponent } from '../../core/icon.component';
import { LoadingComponent } from '../../core/loading.component';
import { PhotoComponent } from '../../core/photo.component';
import { FaIconComponent } from '../screens/shared/icon.component';
import { isCmsImagePath } from './cms-palette';

@Component({
  selector: 'app-cms-meals',
  imports: [CommonModule, FormsModule, RouterLink, CmsConfirmComponent, CmsPagerComponent, IconComponent, LoadingComponent, PhotoComponent, FaIconComponent],
  template: `
    <div class="list-head">
      <h1><app-icon name="meals"></app-icon> الوجبات</h1>
      <a class="primary" routerLink="new"><app-icon name="add"></app-icon> إضافة</a>
    </div>
    <input class="cms-search" [(ngModel)]="text" (ngModelChange)="page = 0" name="text" placeholder="بحث بالاسم أو الوصف">
    <p class="error" *ngIf="error">{{ error }}</p>
    <app-loading *ngIf="loading"></app-loading>
    <div class="table-scroll" *ngIf="!loading && shown().length">
    <table class="data-table">
      <thead>
        <tr>
          <th>الترتيب</th>
          <th>الصورة</th>
          <th>الأيقونة</th>
          <th>اللون</th>
          <th>الاسم</th>
          <th class="actions">إجراءات</th>
        </tr>
      </thead>
      <tbody>
      <tr *ngFor="let meal of pageRows()">
        <td>{{ meal.sort ?? 0 }}</td>
        <td><app-photo [path]="meal.imagePath" icon="meals" [alt]="meal.name || ''"></app-photo></td>
        <td>
          <app-photo *ngIf="isImage(meal.iconPath)" [path]="meal.iconPath" icon="meals" [alt]="(meal.name || '') + ' icon'"></app-photo>
          <span class="icon-key" *ngIf="!isImage(meal.iconPath) && meal.iconPath">
            <fa-icon [name]="meal.iconPath!" [weight]="1.8"></fa-icon>
            {{ meal.iconPath }}
          </span>
          <span *ngIf="!meal.iconPath">—</span>
        </td>
        <td>
          <span class="color-dot" *ngIf="meal.color" [style.background]="meal.color" [title]="meal.color"></span>
          <span *ngIf="!meal.color">—</span>
        </td>
        <td>
          <div>{{ meal.name }}</div>
          <small *ngIf="meal.description">{{ meal.description }}</small>
        </td>
        <td class="actions">
          <div class="row">
            <a class="text-link" [routerLink]="meal.id"><app-icon name="edit"></app-icon> تعديل</a>
            <button type="button" class="ghost" (click)="pending = meal"><app-icon name="delete"></app-icon> حذف</button>
          </div>
        </td>
      </tr>
      </tbody>
    </table>
    </div>
    <p class="muted" *ngIf="!loading && !error && meals.length === 0">لا توجد وجبات.</p>
    <app-cms-pager [total]="shown().length" [page]="page" [size]="pageSize" (pageChange)="page = $event"></app-cms-pager>
    <p class="muted" *ngIf="!loading && !error && meals.length && !shown().length">لا توجد نتائج.</p>
    <app-cms-confirm [name]="pending?.name || ''" (confirm)="remove()" (cancel)="pending = null"></app-cms-confirm>
  `,
  styles: [`
    .color-dot {
      width: 28px;
      height: 28px;
      border-radius: 8px;
      display: inline-grid;
      place-items: center;
      font-weight: 800;
      font-size: 12px;
      box-shadow: inset 0 0 0 1px rgba(0,0,0,.08);
    }
    .icon-key {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      color: #5B4636;
      font-weight: 700;
    }
    .icon-key fa-icon { width: 20px; height: 20px; font-size: 20px; }
  `]
})
export class CmsMealsComponent implements OnInit {
  meals: MealRow[] = [];
  text = '';
  page = 0;
  isImage = isCmsImagePath;
  readonly pageSize = 10;
  pending: MealRow | null = null;
  error = '';
  loading = true;
  image = mediaUrl;

  constructor(private cms: CmsService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.cms.list<MealRow>('Meal').subscribe({
      next: meals => { this.meals = meals; this.loading = false; },
      error: (err: Error) => { this.error = err.message; this.loading = false; }
    });
  }

  pageRows(): MealRow[] {
    const start = this.page * this.pageSize;
    return this.shown().slice(start, start + this.pageSize);
  }

  shown(): MealRow[] {
    const query = this.text.trim().toLowerCase();
    if (!query) return this.meals;
    return this.meals.filter(meal => `${meal.name || ''} ${meal.description || ''}`.toLowerCase().includes(query));
  }

  remove(): void {
    if (!this.pending) return;
    this.cms.remove('Meal', this.pending.id).subscribe({
      next: () => { this.pending = null; this.load(); },
      error: (err: Error) => { this.pending = null; this.error = err.message; }
    });
  }
}
