import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CmsService, ItemRow, LinkRow, MealRow, NamedRow } from '../../core/cms.service';
import { mediaUrl } from '../../core/environment';
import { CmsConfirmComponent } from './cms-confirm.component';
import { IconComponent } from '../../core/icon.component';
import { LoadingComponent } from '../../core/loading.component';
import { PhotoComponent } from '../../core/photo.component';
import { CmsPagerComponent } from './cms-pager.component';
import { Select2Component } from '../../core/select2.component';

@Component({
  selector: 'app-cms-items',
  imports: [CommonModule, FormsModule, RouterLink, CmsConfirmComponent, IconComponent, LoadingComponent, PhotoComponent, CmsPagerComponent, Select2Component],
  template: `
    <div class="list-head">
      <h1><app-icon name="items"></app-icon> الأكلات</h1>
      <a class="primary" routerLink="new"><app-icon name="add"></app-icon> إضافة</a>
    </div>
    <div class="filters">
      <input class="cms-search" [(ngModel)]="text" (ngModelChange)="page = 0" name="text" placeholder="بحث بالاسم أو الملخص">
      <app-select2 name="meal" [(ngModel)]="mealId" (ngModelChange)="page = 0" [options]="meals" placeholder="كل الوجبات" [allowEmpty]="true" emptyLabel="كل الوجبات"></app-select2>
      <app-select2 name="category" [(ngModel)]="categoryId" (ngModelChange)="page = 0" [options]="categories" placeholder="كل التصنيفات" [allowEmpty]="true" emptyLabel="كل التصنيفات"></app-select2>
      <app-select2 name="cuisine" [(ngModel)]="cuisineId" (ngModelChange)="page = 0" [options]="cuisines" placeholder="كل المطابخ" [allowEmpty]="true" emptyLabel="كل المطابخ"></app-select2>
      <app-select2 name="group" [(ngModel)]="groupId" (ngModelChange)="page = 0" [options]="groups" placeholder="كل المجموعات" [allowEmpty]="true" emptyLabel="كل المجموعات"></app-select2>
    </div>
    <p class="error" *ngIf="error">{{ error }}</p>
    <app-loading *ngIf="loading"></app-loading>
    <div class="table-scroll" *ngIf="!loading && shown().length">
    <table class="data-table">
      <thead>
        <tr>
          <th>الصورة</th>
          <th>الاسم</th>
          <th>التصنيف</th>
          <th>المطبخ</th>
          <th>المجموعة</th>
          <th>الوجبات</th>
          <th class="actions">إجراءات</th>
        </tr>
      </thead>
      <tbody>
      <tr *ngFor="let item of pageRows()">
        <td><app-photo [path]="item.imagePath" icon="items" [alt]="item.name || ''"></app-photo></td>
        <td class="name-col">
          <div>{{ item.name }}</div>
          <small *ngIf="item.summary">{{ item.summary }}</small>
          <small class="hidden-badge" *ngIf="item.showToCustomer === false">مخفية عن العميل (صنف بس)</small>
        </td>
        <td>{{ nameOf(categories, item.itemCategoryId) }}</td>
        <td>{{ nameOf(cuisines, item.cuisineId) }}</td>
        <td>{{ nameOf(groups, item.itemGroupId) }}</td>
        <td>{{ mealNames(item.id) }}</td>
        <td class="actions">
          <div class="row">
            <a class="text-link" [routerLink]="item.id"><app-icon name="edit"></app-icon> تعديل</a>
            <button type="button" class="ghost" (click)="pending = item"><app-icon name="delete"></app-icon> حذف</button>
          </div>
        </td>
      </tr>
      </tbody>
    </table>
    </div>
    <app-cms-pager [total]="shown().length" [page]="page" [size]="pageSize" (pageChange)="page = $event"></app-cms-pager>
    <p class="muted" *ngIf="!loading && !error && items.length === 0">لا توجد أكلات.</p>
    <p class="muted" *ngIf="!loading && !error && items.length && !shown().length">لا توجد نتائج.</p>
    <app-cms-confirm [name]="pending?.name || ''" (confirm)="remove()" (cancel)="pending = null"></app-cms-confirm>
  `,
  styles: [`
    .hidden-badge { display: inline-block; margin-top: 4px; color: #8a6d00; background: #fff4cc; border-radius: 8px; padding: 2px 8px; }
  `]
})
export class CmsItemsComponent implements OnInit {
  items: ItemRow[] = [];
  meals: MealRow[] = [];
  categories: NamedRow[] = [];
  cuisines: NamedRow[] = [];
  groups: NamedRow[] = [];
  links: LinkRow[] = [];
  text = '';
  page = 0;
  readonly pageSize = 10;
  mealId = '';
  categoryId = '';
  cuisineId = '';
  groupId = '';
  pending: ItemRow | null = null;
  error = '';
  loading = true;
  image = mediaUrl;
  private mealsByItem = new Map<string, string[]>();

  constructor(private cms: CmsService) {}

  ngOnInit(): void { this.load(); }

  shown(): ItemRow[] {
    const query = this.text.trim().toLowerCase();
    return this.items.filter(item => {
      const nameOk = !query || `${item.name || ''} ${item.summary || ''}`.toLowerCase().includes(query);
      const mealOk = !this.mealId || this.links.some(link => link.itemId === item.id && link.mealId === this.mealId);
      const categoryOk = !this.categoryId || item.itemCategoryId === this.categoryId;
      const cuisineOk = !this.cuisineId || item.cuisineId === this.cuisineId;
      const groupOk = !this.groupId || item.itemGroupId === this.groupId;
      return nameOk && mealOk && categoryOk && cuisineOk && groupOk;
    });
  }

  nameOf(rows: NamedRow[], id?: string | null): string {
    return rows.find(row => row.id === id)?.name || '—';
  }

  pageRows(): ItemRow[] {
    const start = this.page * this.pageSize;
    return this.shown().slice(start, start + this.pageSize);
  }

  mealNames(itemId: string): string {
    return this.mealsByItem.get(itemId)?.join('، ') || '—';
  }

  load(): void {
    this.loading = true;
    this.cms.list<ItemRow>('Item').subscribe({
      next: items => { this.items = items; this.loading = false; },
      error: (err: Error) => { this.error = err.message; this.loading = false; }
    });
    this.cms.list<MealRow>('Meal').subscribe({
      next: meals => { this.meals = meals; this.buildMeals(); },
      error: (err: Error) => this.error = err.message
    });
    this.cms.list<NamedRow>('ItemCategory').subscribe({
      next: rows => this.categories = rows,
      error: (err: Error) => this.error = err.message
    });
    this.cms.list<NamedRow>('Cuisine').subscribe({
      next: rows => this.cuisines = rows,
      error: (err: Error) => this.error = err.message
    });
    this.cms.list<NamedRow>('ItemGroup').subscribe({
      next: rows => this.groups = rows,
      error: (err: Error) => this.error = err.message
    });
    this.cms.list<LinkRow>('ItemMeal').subscribe({
      next: links => { this.links = links; this.buildMeals(); },
      error: (err: Error) => this.error = err.message
    });
  }

  remove(): void {
    if (!this.pending) return;
    this.cms.remove('Item', this.pending.id).subscribe({
      next: () => { this.pending = null; this.load(); },
      error: (err: Error) => { this.pending = null; this.error = err.message; }
    });
  }

  private buildMeals(): void {
    const names = new Map(this.meals.map(meal => [meal.id, meal.name || '']));
    const grouped = new Map<string, string[]>();
    for (const link of this.links) {
      const name = names.get(link.mealId || '');
      if (!name) continue;
      const list = grouped.get(link.itemId) || [];
      list.push(name);
      grouped.set(link.itemId, list);
    }
    this.mealsByItem = grouped;
  }
}
