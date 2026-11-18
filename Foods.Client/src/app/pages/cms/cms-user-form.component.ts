import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CmsService, CountryRow, NamedRow, UserRow } from '../../core/cms.service';
import { IconComponent } from '../../core/icon.component';
import { Select2Component } from '../../core/select2.component';

@Component({
  selector: 'app-cms-user-form',
  imports: [CommonModule, FormsModule, RouterLink, IconComponent, Select2Component],
  template: `
    <a class="back" routerLink="/cms/users">الرجوع للقائمة</a>
    <h1><app-icon [name]="id ? 'edit' : 'add'"></app-icon> {{ id ? 'تعديل مستخدم' : 'إضافة مستخدم' }}</h1>
    <p class="error" *ngIf="error">{{ error }}</p>
    <form class="panel editor" (ngSubmit)="save()">
      <div class="fields">
        <label>الاسم<input [(ngModel)]="name" name="name" required></label>
        <label>اسم الدخول<input [(ngModel)]="userName" name="userName" required></label>
        <label>الدور
          <app-select2 name="role" [(ngModel)]="role" [options]="roles" placeholder="اختار الدور" [required]="true"></app-select2>
        </label>
        <label>البلد
          <app-select2 name="country" [(ngModel)]="countryId" [options]="countryOptions" placeholder="اختار البلد" [allowEmpty]="role !== 'Customer'" emptyLabel="بدون" [required]="role === 'Customer'"></app-select2>
        </label>
        <label>كلمة المرور
          <input [(ngModel)]="password" name="password" type="password" [required]="!id" [placeholder]="id ? 'اتركها فارغة لو مش هتغيرها' : ''">
        </label>
      </div>
      <button type="submit" class="save">حفظ</button>
    </form>
  `
})
export class CmsUserFormComponent implements OnInit {
  id = '';
  name = '';
  userName = '';
  password = '';
  role = 'Customer';
  countryId = '';
  countries: CountryRow[] = [];
  roles: NamedRow[] = [
    { id: 'Admin', name: 'أدمن' },
    { id: 'Customer', name: 'عميل' }
  ];
  error = '';

  get countryOptions(): NamedRow[] {
    return this.countries.map(country => ({
      id: country.id,
      name: country.dialect ? `${country.name} · ${country.dialect}` : country.name
    }));
  }

  constructor(private cms: CmsService, private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    this.cms.list<CountryRow>('Country').subscribe({
      next: rows => this.countries = rows,
      error: (err: Error) => this.error = err.message
    });
    const id = this.route.snapshot.paramMap.get('id') || '';
    if (!id || id === 'new') return;
    this.id = id;
    this.cms.get<UserRow>('User', id).subscribe({
      next: user => {
        this.name = user.name || '';
        this.userName = user.userName || '';
        this.role = user.role || 'Customer';
        this.countryId = user.countryId || '';
      },
      error: (err: Error) => this.error = err.message
    });
  }

  save(): void {
    const body = {
      id: this.id || undefined,
      name: this.name,
      userName: this.userName,
      password: this.password || null,
      role: this.role,
      countryId: this.countryId || null
    };
    const done = () => this.router.navigateByUrl('/cms/users');
    const fail = (err: Error) => this.error = err.message;
    if (this.id) this.cms.update('User', body).subscribe({ next: done, error: fail });
    else this.cms.create('User', body).subscribe({ next: done, error: fail });
  }
}
