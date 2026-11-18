import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { lookupKinds } from './lookup-kinds';
import { IconComponent } from '../../core/icon.component';

@Component({
  selector: 'app-cms-lookups',
  imports: [CommonModule, RouterLink, IconComponent],
  template: `
    <div class="lookup-head">
      <h1><app-icon name="lookups"></app-icon> البيانات المساعدة</h1>
      <p class="muted">اختار القائمة، وبعدين أضف أو عدّل من شاشتها.</p>
    </div>
    <div class="tiles lookup-tiles">
      <a class="tile" *ngFor="let kind of kinds" [routerLink]="kind.key">
        <app-icon [name]="kind.icon"></app-icon>
        <b>{{ kind.title }}</b>
      </a>
    </div>
  `
})
export class CmsLookupsComponent {
  kinds = lookupKinds;
}
