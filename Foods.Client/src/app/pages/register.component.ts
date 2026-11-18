import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { ApiService } from '../core/api.service';
import { FamilyMemberInput } from '../core/models';
import { NamedRow } from '../core/cms.service';
import { Select2Component } from '../core/select2.component';

@Component({
  selector: 'app-register',
  imports: [CommonModule, FormsModule, RouterLink, Select2Component],
  template: `
    <section class="auth">
      <img class="logo big" src="logo.png?v=2" alt="Foods">
      <h1>حساب جديد</h1>
      <form class="auth-card" (ngSubmit)="submit()">
        <label>الاسم<input [(ngModel)]="name" name="name" required></label>
        <label>اسم المستخدم<input [(ngModel)]="userName" name="userName" required></label>
        <label>البلد
          <app-select2 name="country" [(ngModel)]="countryId" [options]="countryOptions" placeholder="اختار بلدك" [required]="true"></app-select2>
        </label>
        <label>كلمة المرور<input [(ngModel)]="password" name="password" type="password" required></label>
        <h2>أفراد الأسرة</h2>
        <div class="row" *ngFor="let member of family; let i = index">
          <input [(ngModel)]="member.name" [name]="'n' + i" placeholder="الاسم">
          <input [(ngModel)]="member.age" [name]="'a' + i" type="number" min="0" max="120" placeholder="العمر">
          <button type="button" (click)="family.splice(i, 1)">حذف</button>
        </div>
        <button type="button" (click)="family.push({ name: '', age: 30 })">إضافة فرد</button>
        <p class="error" *ngIf="error">{{ error }}</p>
        <button type="submit">تسجيل</button>
      </form>
      <a routerLink="/login">عندي حساب</a>
    </section>
  `
})
export class RegisterComponent implements OnInit {
  name = '';
  userName = '';
  password = '';
  countryId = '';
  countries: { id: string; name?: string | null; dialect?: string | null }[] = [];
  error = '';
  family: FamilyMemberInput[] = [{ name: '', age: 30 }];

  constructor(private auth: AuthService, private api: ApiService, private router: Router) {}

  get countryOptions(): NamedRow[] {
    return this.countries.map(country => ({
      id: country.id,
      name: country.dialect ? `${country.name} · ${country.dialect}` : country.name
    }));
  }

  ngOnInit(): void {
    this.api.countries().subscribe({
      next: rows => this.countries = rows,
      error: (err: Error) => this.error = err.message
    });
  }

  submit(): void {
    this.error = '';
    this.auth.register(this.name, this.userName, this.password, this.countryId, this.family).subscribe({
      next: () => this.router.navigateByUrl('/home'),
      error: (err: Error) => this.error = err.message
    });
  }
}
