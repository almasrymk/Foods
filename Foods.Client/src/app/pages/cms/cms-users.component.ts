import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CmsService, NamedRow, UserRow } from '../../core/cms.service';
import { Select2Component } from '../../core/select2.component';
import { CmsConfirmComponent } from './cms-confirm.component';
import { CmsPagerComponent } from './cms-pager.component';
import { IconComponent } from '../../core/icon.component';
import { LoadingComponent } from '../../core/loading.component';

@Component({
  selector: 'app-cms-users',
  imports: [CommonModule, FormsModule, RouterLink, CmsConfirmComponent, CmsPagerComponent, IconComponent, LoadingComponent, Select2Component],
  template: `
    <div class="list-head">
      <h1><app-icon name="users"></app-icon> المستخدمين</h1>
      <a class="primary" routerLink="new"><app-icon name="add"></app-icon> إضافة</a>
    </div>
    <div class="filters">
      <input class="cms-search" [(ngModel)]="text" (ngModelChange)="page = 0" name="text" placeholder="بحث بالاسم أو اسم الدخول">
      <app-select2 name="role" [(ngModel)]="role" (ngModelChange)="page = 0" [options]="roles" placeholder="كل الأدوار" [allowEmpty]="true" emptyLabel="كل الأدوار"></app-select2>
    </div>
    <p class="error" *ngIf="error">{{ error }}</p>
    <app-loading *ngIf="loading"></app-loading>
    <div class="table-scroll" *ngIf="!loading && shown().length">
      <table class="data-table">
        <thead>
          <tr>
            <th>الاسم</th>
            <th>اسم الدخول</th>
            <th>البلد</th>
            <th>الدور</th>
            <th class="actions">إجراءات</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let user of pageRows()">
            <td>{{ user.name }}</td>
            <td>{{ user.userName }}</td>
            <td>{{ user.countryName || '—' }}<small *ngIf="user.dialect"> · {{ user.dialect }}</small></td>
            <td>{{ roleName(user.role) }}</td>
            <td class="actions">
              <div class="row">
                <a class="text-link" [routerLink]="user.id"><app-icon name="edit"></app-icon> تعديل</a>
                <button type="button" class="ghost" (click)="pending = user"><app-icon name="delete"></app-icon> حذف</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <p class="muted" *ngIf="!loading && !error && users.length === 0">لا يوجد مستخدمين.</p>
    <app-cms-pager [total]="shown().length" [page]="page" [size]="pageSize" (pageChange)="page = $event"></app-cms-pager>
    <p class="muted" *ngIf="!loading && !error && users.length && !shown().length">لا توجد نتائج.</p>
    <app-cms-confirm [name]="pending?.name || ''" (confirm)="remove()" (cancel)="pending = null"></app-cms-confirm>
  `
})
export class CmsUsersComponent implements OnInit {
  users: UserRow[] = [];
  text = '';
  role = '';
  roles: NamedRow[] = [
    { id: 'Admin', name: 'أدمن' },
    { id: 'Customer', name: 'عميل' }
  ];
  page = 0;
  readonly pageSize = 10;
  pending: UserRow | null = null;
  error = '';
  loading = true;

  constructor(private cms: CmsService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.cms.list<UserRow>('User').subscribe({
      next: users => { this.users = users; this.loading = false; },
      error: (err: Error) => { this.error = err.message; this.loading = false; }
    });
  }

  pageRows(): UserRow[] {
    const start = this.page * this.pageSize;
    return this.shown().slice(start, start + this.pageSize);
  }

  shown(): UserRow[] {
    const query = this.text.trim().toLowerCase();
    return this.users.filter(user => {
      const roleOk = !this.role || user.role === this.role;
      const textOk = !query || `${user.name || ''} ${user.userName || ''}`.toLowerCase().includes(query);
      return roleOk && textOk;
    });
  }

  roleName(role?: string | null): string {
    if (role === 'Admin') return 'أدمن';
    if (role === 'Customer') return 'عميل';
    return role || '';
  }

  remove(): void {
    if (!this.pending) return;
    this.cms.remove('User', this.pending.id).subscribe({
      next: () => { this.pending = null; this.load(); },
      error: (err: Error) => { this.pending = null; this.error = err.message; }
    });
  }
}
