import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="auth">
      <img class="logo big" src="logo.png?v=2" alt="Foods">
      <h1>مرحبا بك</h1>
      <form class="auth-card" (ngSubmit)="submit()">
        <p class="lead">لن رحلتك في بيتك</p>
        <input [(ngModel)]="userName" name="userName" placeholder="اسم المستخدم" required>
        <input [(ngModel)]="password" name="password" type="password" placeholder="كلمة المرور" required>
        <p class="error" *ngIf="error">{{ error }}</p>
        <button type="submit">دخول</button>
      </form>
      <a routerLink="/register">ليس لديك حساب؟ سجّل الآن</a>
    </section>
  `
})
export class LoginComponent {
  userName = '';
  password = '';
  error = '';

  constructor(private auth: AuthService, private router: Router) {}

  submit(): void {
    this.error = '';
    this.auth.login(this.userName, this.password).subscribe({
      next: () => this.router.navigateByUrl('/home'),
      error: (err: Error) => this.error = err.message
    });
  }
}
