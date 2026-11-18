import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-icon',
  imports: [CommonModule],
  template: `
    <svg class="ico" viewBox="0 0 24 24" aria-hidden="true">
      <ng-container [ngSwitch]="name">
        <ng-container *ngSwitchCase="'lookups'">
          <rect x="4" y="3" width="16" height="18" rx="2"></rect>
          <path d="M8 8h8M8 12h8M8 16h5"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'meals'">
          <path d="M4 16h16"></path>
          <path d="M6 16a6 6 0 0 1 12 0"></path>
          <path d="M12 6v2"></path>
          <circle cx="12" cy="5" r="1"></circle>
        </ng-container>
        <ng-container *ngSwitchCase="'items'">
          <ellipse cx="12" cy="13" rx="8" ry="3.2"></ellipse>
          <path d="M4 13c0 3.2 3.6 6 8 6s8-2.8 8-6"></path>
          <path d="M8 12c.4-2.2 1.8-3.5 4-3.5s3.6 1.3 4 3.5"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'contents'">
          <path d="M7 3h7l5 5v12a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z"></path>
          <path d="M14 3v5h5"></path>
          <path d="M9 13h6M9 16h6M9 19h3"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'app'">
          <rect x="7" y="2" width="10" height="20" rx="2"></rect>
          <path d="M11 18h2"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'favorites'">
          <path d="M12 20s-7-4.2-7-9a4 4 0 0 1 7-2 4 4 0 0 1 7 2c0 4.8-7 9-7 9z"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'orders'">
          <path d="M7 3h10v18l-2.5-1.5L12 21l-2.5-1.5L7 21z"></path>
          <path d="M10 8h4M10 12h4"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'builder'">
          <path d="M6 8h15l-1.5 9H8z"></path>
          <path d="M6 8L5 4H2"></path>
          <circle cx="9" cy="20" r="1"></circle>
          <circle cx="17" cy="20" r="1"></circle>
        </ng-container>
        <ng-container *ngSwitchCase="'family'">
          <circle cx="8" cy="8" r="2.5"></circle>
          <circle cx="16" cy="9" r="2"></circle>
          <path d="M3.5 19c.6-3 2.4-4.5 4.5-4.5s3.9 1.5 4.5 4.5"></path>
          <path d="M13 19c.4-2.2 1.6-3.4 3.2-3.4 1.7 0 2.9 1.2 3.3 3.4"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'users'">
          <circle cx="9" cy="8" r="2.4"></circle>
          <path d="M4.5 18c.5-2.6 2.2-4 4.5-4s4 1.4 4.5 4"></path>
          <circle cx="16.5" cy="9" r="1.8"></circle>
          <path d="M14.2 18c.3-1.8 1.3-2.8 2.6-2.8 1.4 0 2.4 1 2.7 2.8"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'admin'">
          <!-- لوحة تحكم / إدارة -->
          <rect x="3" y="3" width="8" height="8" rx="1.5"></rect>
          <rect x="13" y="3" width="8" height="5" rx="1.5"></rect>
          <rect x="13" y="10" width="8" height="11" rx="1.5"></rect>
          <rect x="3" y="13" width="8" height="8" rx="1.5"></rect>
          <circle cx="17" cy="15.5" r="1.6"></circle>
          <path d="M17 12.8v.9M17 17.3v.9M14.8 15.5h.9M18.3 15.5h.9"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'logout'">
          <path d="M10 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h4"></path>
          <path d="M14 16l4-4-4-4M9 12h9"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'category'">
          <path d="M4 12V5h7l9 9-7 7z"></path>
          <circle cx="8.5" cy="8.5" r="1"></circle>
        </ng-container>
        <ng-container *ngSwitchCase="'group'">
          <rect x="3" y="4" width="8" height="7" rx="1"></rect>
          <rect x="13" y="4" width="8" height="7" rx="1"></rect>
          <rect x="8" y="13" width="8" height="7" rx="1"></rect>
        </ng-container>
        <ng-container *ngSwitchCase="'place'">
          <path d="M4 10l8-6 8 6"></path>
          <path d="M6 10v9h12v-9"></path>
          <path d="M10 19v-5h4v5"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'unit'">
          <path d="M4 19l8-14 8 14"></path>
          <path d="M8 19h8M9.5 14h5"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'add'">
          <circle cx="12" cy="12" r="9"></circle>
          <path d="M12 6.5v11M6.5 12h11"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'edit'">
          <path d="M4 20h4l10-10-4-4L4 16z"></path>
          <path d="M12 8l4 4"></path>
        </ng-container>
        <ng-container *ngSwitchCase="'delete'">
          <path d="M5 7h14"></path>
          <path d="M9 7V5h6v2"></path>
          <path d="M7 7l1 13h8l1-13"></path>
          <path d="M10 11v6M14 11v6"></path>
        </ng-container>
      </ng-container>
    </svg>
  `
})
export class IconComponent {
  @Input() name = '';
}
