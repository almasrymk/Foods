import { Component, Input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { FaIconComponent } from './icon.component';

/**
 * The app's single floating bottom navigation: the home page design (841px mockup units).
 * Used by the home page and every mockup screen.
 */
@Component({
  selector: 'fa-tab-bar',
  imports: [RouterLink, RouterLinkActive, FaIconComponent],
  template: `
    <nav class="tb">
      <a routerLink="/home" routerLinkActive="active" #home="routerLinkActive" [class.forced]="force === 'home'">
        @if (home.isActive || force === 'home') {
          <svg viewBox="0 0 24 24"><path d="M10 19v-5h4v5c0 .55.45 1 1 1h3c.55 0 1-.45 1-1v-7h1.7c.46 0 .68-.57.33-.87L12.67 3.6c-.38-.34-.96-.34-1.34 0l-8.36 7.53c-.34.3-.13.87.33.87H5v7c0 .55.45 1 1 1h3c.55 0 1-.45 1-1z"/></svg>
        } @else {
          <svg viewBox="0 0 24 24"><path d="m12 5.69 5 4.5V18h-2v-6H9v6H7v-7.81l5-4.5M12 3 2 12h3v8h6v-6h2v6h6v-8h3L12 3z"/></svg>
        }
        <span>الرئيسية</span>
      </a>
      <a routerLink="/favorites" routerLinkActive="active" #fav="routerLinkActive">
        @if (fav.isActive) {
          <svg viewBox="0 0 24 24"><path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/></svg>
        } @else {
          <svg viewBox="0 0 24 24"><path d="M16.5 3c-1.74 0-3.41.81-4.5 2.09C10.91 3.81 9.24 3 7.5 3 4.42 3 2 5.42 2 8.5c0 3.78 3.4 6.86 8.55 11.54L12 21.35l1.45-1.32C18.6 15.36 22 12.28 22 8.5 22 5.42 19.58 3 16.5 3zm-4.4 15.55-.1.1-.1-.1C7.14 14.24 4 11.39 4 8.5 4 6.5 5.5 5 7.5 5c1.54 0 3.04.99 3.57 2.36h1.87C13.46 5.99 14.96 5 16.5 5c2 0 3.5 1.5 3.5 3.5 0 2.89-3.14 5.74-7.9 10.05z"/></svg>
        }
        <span>أكلاتي المفضلة</span>
      </a>
      <i class="tb-gap"></i>
      <a routerLink="/orders" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" #ord="routerLinkActive">
        @if (ord.isActive) {
          <svg viewBox="0 0 24 24" fill-rule="evenodd"><path d="M19.5 3.5 18 2l-1.5 1.5L15 2l-1.5 1.5L12 2l-1.5 1.5L9 2 7.5 3.5 6 2v14H3v3c0 1.66 1.34 3 3 3h12c1.66 0 3-1.34 3-3V2l-1.5 1.5zM9 7h6v2H9zm7 0h2v2h-2zm-7 3h6v2H9zm7 0h2v2h-2zM5 18h10v2H6c-.55 0-1-.45-1-1z"/></svg>
        } @else {
          <svg viewBox="0 0 24 24"><path d="M19.5 3.5 18 2l-1.5 1.5L15 2l-1.5 1.5L12 2l-1.5 1.5L9 2 7.5 3.5 6 2v14H3v3c0 1.66 1.34 3 3 3h12c1.66 0 3-1.34 3-3V2l-1.5 1.5zM15 20H6c-.55 0-1-.45-1-1v-1h10v2zm4-1c0 .55-.45 1-1 1s-1-.45-1-1v-3H8V5h11v14z"/><path d="M9 7h6v2H9zM16 7h2v2h-2zM9 10h6v2H9zM16 10h2v2h-2z"/></svg>
        }
        <span>طلبات قديمة</span>
      </a>
      <a routerLink="/settings" routerLinkActive="active" #set="routerLinkActive">
        @if (set.isActive) {
          <svg viewBox="0 0 24 24"><path d="M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58a.49.49 0 0 0 .12-.61l-1.92-3.32a.49.49 0 0 0-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54a.48.48 0 0 0-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96a.49.49 0 0 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58a.49.49 0 0 0-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32a.49.49 0 0 0-.12-.61l-2.01-1.58zM12 15.6a3.6 3.6 0 1 1 0-7.2 3.6 3.6 0 0 1 0 7.2z"/></svg>
        } @else {
          <svg viewBox="0 0 24 24"><path d="M19.43 12.98c.04-.32.07-.64.07-.98 0-.34-.03-.66-.07-.98l2.11-1.65c.19-.15.24-.42.12-.64l-2-3.46a.5.5 0 0 0-.61-.22l-2.49 1c-.52-.4-1.08-.73-1.69-.98l-.38-2.65A.49.49 0 0 0 14 2h-4c-.25 0-.46.18-.49.42l-.38 2.65c-.61.25-1.17.59-1.69.98l-2.49-1a.57.57 0 0 0-.18-.03c-.17 0-.34.09-.43.25l-2 3.46c-.13.22-.07.49.12.64l2.11 1.65c-.04.32-.07.65-.07.98 0 .33.03.66.07.98l-2.11 1.65c-.19.15-.24.42-.12.64l2 3.46a.5.5 0 0 0 .61.22l2.49-1c.52.4 1.08.73 1.69.98l.38 2.65c.03.24.24.42.49.42h4c.25 0 .46-.18.49-.42l.38-2.65c.61-.25 1.17-.59 1.69-.98l2.49 1c.06.02.12.03.18.03.17 0 .34-.09.43-.25l2-3.46c.12-.22.07-.49-.12-.64l-2.11-1.65zm-1.98-1.71c.04.31.05.52.05.73 0 .21-.02.43-.05.73l-.14 1.13.89.7 1.08.84-.7 1.21-1.27-.51-1.04-.42-.9.68c-.43.32-.84.56-1.25.73l-1.06.43-.16 1.13-.2 1.35h-1.4l-.19-1.35-.16-1.13-1.06-.43c-.43-.18-.83-.41-1.23-.71l-.91-.7-1.06.43-1.27.51-.7-1.21 1.08-.84.89-.7-.14-1.13c-.03-.31-.05-.54-.05-.74s.02-.43.05-.73l.14-1.13-.89-.7-1.08-.84.7-1.21 1.27.51 1.04.42.9-.68c.43-.32.84-.56 1.25-.73l1.06-.43.16-1.13.2-1.35h1.39l.19 1.35.16 1.13 1.06.43c.43.18.83.41 1.23.71l.91.7 1.06-.43 1.27-.51.7 1.21-1.07.85-.89.7.14 1.13zM12 8c-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4-1.79-4-4-4zm0 6c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2z"/></svg>
        }
        <span>الإعدادات</span>
      </a>
      <a class="tb-basket" routerLink="/ingredients" routerLinkActive="active" aria-label="طلباتي">
        <fa-icon name="basket" [weight]="1.9" />
        <span>طلباتي</span>
      </a>
    </nav>
  `,
  styles: [`
    :host {
      display: block;
      /* the bar is drawn at 90% of the page scale */
      --u: calc(var(--fa-w) * 0.9 / 841);
      --tb-orange: #F37202;
      --tb-orange-light: #FF9A1F;
      --tb-icon: #5B4636;
      --tb-bg: #FFFAF3;
      font-family: 'Tajawal', 'Cairo', sans-serif;
    }
    .tb {
      position: fixed; z-index: 30; left: 50%; transform: translateX(-50%);
      bottom: calc(22 * var(--u) + env(safe-area-inset-bottom));
      /* full-width column on phones/tablets: keep a small gap to the screen edges */
      width: min(var(--fa-w), calc(100vw - var(--sbw, 0px) - 16px)); height: calc(126 * var(--u));
      display: flex; border-radius: calc(63 * var(--u)); background: var(--tb-bg);
      box-shadow: 0 calc(6 * var(--u)) calc(24 * var(--u)) rgba(138, 90, 43, .14);
    }
    /* strip under the floating bar so page content never shows through the gap below it */
    :host::before {
      content: ""; position: fixed; z-index: 29; left: 0; right: 0; bottom: 0; pointer-events: none;
      height: calc((22 + 126) * var(--u) + env(safe-area-inset-bottom));
      background: linear-gradient(180deg, rgba(253, 248, 243, 0), #FDF8F3 22%);
    }
    .tb > a:not(.tb-basket) {
      flex: 1 1 0; min-width: 0; display: flex; flex-direction: column; align-items: center; justify-content: center;
      gap: calc(6 * var(--u)); border-radius: calc(40 * var(--u));
      color: var(--tb-icon); text-decoration: none; font-size: calc(25 * var(--u)); font-weight: 700; white-space: nowrap;
    }
    .tb > a svg { display: block; width: calc(50 * var(--u)); height: calc(50 * var(--u)); fill: currentColor; flex: none; }
    .tb > a.active:not(.tb-basket), .tb > a.forced { color: var(--tb-orange); font-weight: 800; }
    .tb-gap { flex: 0 0 calc(170 * var(--u)); }
    .tb > a.tb-basket {
      position: absolute; left: 50%; top: calc(-16 * var(--u)); transform: translateX(-50%);
      width: calc(138 * var(--u)); height: calc(138 * var(--u));
      display: flex; flex-direction: column; align-items: center; justify-content: center;
      border-radius: 50%; border: calc(4 * var(--u)) solid var(--tb-bg);
      background: linear-gradient(180deg, var(--tb-orange-light), var(--tb-orange));
      box-shadow: 0 calc(6 * var(--u)) calc(16 * var(--u)) rgba(243, 114, 2, .35);
      color: #fff; text-decoration: none;
    }
    .tb > a.tb-basket fa-icon { font-size: calc(64 * var(--u)); margin-bottom: calc(2 * var(--u)); }
    .tb > a.tb-basket.active { box-shadow: 0 0 0 calc(5 * var(--u)) rgba(243, 114, 2, .22), 0 calc(6 * var(--u)) calc(16 * var(--u)) rgba(243, 114, 2, .35); }
    .tb-basket span { font-size: calc(26 * var(--u)); font-weight: 700; line-height: 1.1; }
  `]
})
export class FaTabBarComponent {
  /** Highlight Home on screens reached from it (the /meals flow). */
  @Input() force = '';
}
