import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-hub-nav',
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="hub-nav">
      <a routerLink="/home" routerLinkActive="active">
        <svg viewBox="0 0 24 24"><path d="M4 10.5 12 4l8 6.5V20a1 1 0 0 1-1 1h-5.2v-6.2H10.2V21H5a1 1 0 0 1-1-1z"></path></svg>
        الرئيسية
      </a>
      <a routerLink="/favorites" routerLinkActive="active">
        <svg viewBox="0 0 24 24"><path d="M12 20s-7-4.4-7-9a4 4 0 0 1 7-2 4 4 0 0 1 7 2c0 4.6-7 9-7 9z"></path></svg>
        المفضلة
      </a>
      <a class="cart" routerLink="/builder" routerLinkActive="active">
        <span>
          <svg viewBox="0 0 24 24"><path d="M5 8h14l-1.3 11.2a1.5 1.5 0 0 1-1.5 1.3H7.8a1.5 1.5 0 0 1-1.5-1.3z"></path><path d="M8.5 8V6.8a3.5 3.5 0 0 1 7 0V8"></path></svg>
        </span>
        طلباتي
      </a>
      <a routerLink="/orders" routerLinkActive="active">
        <svg viewBox="0 0 24 24"><path d="M8 7h11"></path><path d="M8 12h11"></path><path d="M8 17h11"></path><path d="M5 7h.01M5 12h.01M5 17h.01"></path></svg>
        قائمة الطلبات
      </a>
      <a routerLink="/family" routerLinkActive="active">
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="3"></circle><path d="M12 3v2M12 19v2M3 12h2M19 12h2M5.6 5.6l1.4 1.4M17 17l1.4 1.4M18.4 5.6 17 7M7 17l-1.4 1.4"></path></svg>
        الإعدادات
      </a>
    </nav>
  `
})
export class HubNavComponent {}
