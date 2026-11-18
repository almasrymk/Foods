import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { ApiService } from './core/api.service';
import { setCurrency } from './core/money';
import { DraftService } from './core/draft.service';
import { IconComponent } from './core/icon.component';
import { FaSelectedSheetComponent } from './pages/screens/shared/selected-sheet.component';
import { FaTopBarComponent } from './pages/screens/shared/top-bar.component';
import { FaTabBarComponent } from './pages/screens/shared/tab-bar.component';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, IconComponent, FaSelectedSheetComponent, FaTopBarComponent, FaTabBarComponent],
  template: `
    <div class="app" [class.cms]="isCms()" [class.guest]="isGuest()" [class.menu-open]="menuOpen" [class.customer-home]="isHome()" [class.home-screen]="isHomeScreen()">
      <ng-container *ngIf="isCms(); else customer">
        <button type="button" class="cms-backdrop" *ngIf="menuOpen && !isGuest()" (click)="menuOpen = false" aria-label="إغلاق القائمة"></button>
        <aside class="side" *ngIf="!isGuest()">
          <div class="brand"><img class="logo" src="logo.png?v=2" alt="Foods"> لوحة الإدارة</div>
          <a routerLink="/cms/lookups" routerLinkActive="active"><app-icon name="lookups"></app-icon> البيانات المساعدة</a>
          <a routerLink="/cms/meals" routerLinkActive="active"><app-icon name="meals"></app-icon> الوجبات</a>
          <a routerLink="/cms/items" routerLinkActive="active"><app-icon name="items"></app-icon> الأكلات</a>
          <a routerLink="/cms/contents" routerLinkActive="active"><app-icon name="contents"></app-icon> المكونات</a>
          <a routerLink="/cms/users" routerLinkActive="active"><app-icon name="users"></app-icon> المستخدمين</a>
          <a routerLink="/home"><app-icon name="app"></app-icon> تطبيق العميل</a>
          <button type="button" class="ghost" (click)="auth.logout()"><app-icon name="logout"></app-icon> خروج</button>
        </aside>
        <main class="cms-main">
          <button type="button" class="cms-menu" *ngIf="!isGuest()" (click)="menuOpen = !menuOpen">
            <span class="bars" aria-hidden="true"></span>
            القائمة
          </button>
          <router-outlet></router-outlet>
        </main>
      </ng-container>
      <ng-template #customer>
        <!-- Screens without their own mockup header (family, builder) get the shared top bar and bottom nav. -->
        @if (auth.isLoggedIn() && !isGuest() && !isHome()) {
          <fa-top-bar class="app-top-bar" />
        }
        <a *ngIf="auth.isAdmin() && !isGuest()" class="admin-fab" routerLink="/cms/meals"
           title="الإدارة" aria-label="الإدارة">
          <!-- Same icon family and weight as the notifications bell next to it. -->
          <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M19 5v2h-4V5h4M9 5v6H5V5h4m10 8v6h-4v-6h4M9 17v2H5v-2h4M21 3h-8v6h8V3zM11 3H3v10h8V3zm10 8h-8v10h8V11zm-10 4H3v6h8v-6z"/></svg>
          <span>الإدارة</span>
        </a>
        @if (!isGuest() && !isSettings() && !isPurchaseOrder()) {
          <fa-selected-sheet />
        }
        <main class="page">
          <router-outlet></router-outlet>
        </main>
        @if (auth.isLoggedIn() && !isGuest() && !isHome()) {
          <fa-tab-bar />
        }
      </ng-template>
    </div>
  `
})
export class AppComponent {
  menuOpen = false;

  private currencyLoaded = false;

  constructor(public auth: AuthService, public draft: DraftService, private router: Router, private api: ApiService) {
    this.router.events.subscribe(event => {
      if (!(event instanceof NavigationEnd)) return;
      this.menuOpen = false;
      // The currency of the country the customer is in (prices are shown in it).
      if (!this.currencyLoaded && this.auth.isLoggedIn() && !this.isGuest() && !this.isCms()) {
        this.currencyLoaded = true;
        this.api.priceCountry().subscribe({ next: c => setCurrency(c.currency), error: () => this.currencyLoaded = false });
      }
    });
  }

  isCms(): boolean {
    return this.router.url.startsWith('/cms');
  }

  isGuest(): boolean {
    const path = this.router.url.split('?')[0];
    return path === '/login' || path === '/register';
  }

  isSettings(): boolean {
    return this.router.url.split('?')[0] === '/settings';
  }

  isPurchaseOrder(): boolean {
    return /^\/orders\/[^/]+$/.test(this.router.url.split('?')[0]);
  }

  /** Customer screens that draw their own full-page mockup layout (no top bar, no page padding). */
  private static readonly SCREENS = [
    /^\/home$/, /^\/meals(\/[^/]+)?$/, /^\/cuisines$/, /^\/items\/[^/]+$/, /^\/favorites$/,
    /^\/orders$/, /^\/orders\/[^/]+$/, /^\/ingredients$/, /^\/settings$/, /^\/notifications$/, /^\/prices$/
  ];

  isHome(): boolean {
    return this.isHomeScreen();
  }

  isHomeScreen(): boolean {
    const path = this.router.url.split('?')[0];
    return AppComponent.SCREENS.some(re => re.test(path));
  }
}
