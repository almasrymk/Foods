import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiService } from '../core/api.service';
import { DraftService } from '../core/draft.service';
import { MealCard } from '../core/models';
import { PhotoComponent } from '../core/photo.component';
import { HubNavComponent } from '../core/hub-nav.component';

interface MealTile {
  key: string;
  label: string;
  tone: string;
  icon: string;
  wide: boolean;
  mealId: string | null;
}

@Component({
  selector: 'app-meals',
  imports: [CommonModule, RouterLink, PhotoComponent, HubNavComponent],
  template: `
    <section class="hub plan" data-screen="meal-plan">
      <header class="hub-top">
        <a class="hub-bell" routerLink="/orders" aria-label="التنبيهات">
          <svg viewBox="0 0 24 24"><path d="M6 9a6 6 0 1 1 12 0c0 5 2 6 2 6H4s2-1 2-6z"></path><path d="M10 18a2 2 0 0 0 4 0"></path></svg>
          <b *ngIf="draft.items.length">{{ draft.items.length }}</b>
        </a>
        <img class="hub-logo" src="logo.png?v=2" alt="Foods">
        <a class="hub-avatar" routerLink="/family" aria-label="الحساب">
          <svg viewBox="0 0 24 24"><circle cx="12" cy="8" r="3.2"></circle><path d="M5 19c1.4-3 3.8-4.5 7-4.5S17.6 16 19 19"></path></svg>
        </a>
      </header>

      <div class="pick-hero">
        <div class="pick-step">
          <span>الخطوة 1 من 3</span>
          <i aria-hidden="true"><b></b><b></b><b class="dot"></b></i>
        </div>
        <span class="pick-cal" aria-hidden="true">
          <svg viewBox="0 0 92 78">
            <rect x="18" y="16" width="58" height="50" rx="8" fill="#fff"></rect>
            <path d="M18 30h58" stroke="#d7efe0" stroke-width="2"></path>
            <rect x="30" y="10" width="5" height="12" rx="2" fill="#1f8a4c"></rect>
            <rect x="58" y="10" width="5" height="12" rx="2" fill="#1f8a4c"></rect>
            <rect x="28" y="38" width="10" height="8" rx="2" fill="#2f9e57"></rect>
            <rect x="42" y="38" width="10" height="8" rx="2" fill="#b7e4c7"></rect>
            <rect x="56" y="38" width="10" height="8" rx="2" fill="#2f9e57"></rect>
            <rect x="28" y="50" width="10" height="8" rx="2" fill="#d8f3e2"></rect>
            <rect x="42" y="50" width="10" height="8" rx="2" fill="#2f9e57"></rect>
            <circle cx="16" cy="58" r="9" fill="#e23b3b"></circle>
            <circle cx="78" cy="62" r="8" fill="#f2c200"></circle>
          </svg>
        </span>
      </div>

      <h1 class="pick-title">
        اختر نوع الوجبة
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M8 3v8M5 3v6a3 3 0 0 0 6 0V3"></path><path d="M16 3v18"></path><path d="M16 8h3a2 2 0 0 0 0-4h-3"></path></svg>
      </h1>
      <p class="pick-sub">ابدأ بتحديد الوجبات التي تريد إضافتها إلى خطة الشهر</p>
      <p class="error" *ngIf="error">{{ error }}</p>

      <div class="pick-grid">
        <button type="button" class="pick-card" *ngFor="let tile of tiles" [class]="tile.tone" [class.on]="selected === tile.key" [disabled]="!tile.mealId && tile.key !== 'snacks' && tile.key !== 'healthy'" (click)="choose(tile)">
          <svg *ngIf="tile.icon === 'sun'" viewBox="0 0 24 24"><circle cx="12" cy="12" r="3.2"></circle><path d="M12 3v2M12 19v2M3 12h2M19 12h2M5.6 5.6l1.4 1.4M17 17l1.4 1.4M18.4 5.6 17 7M7 17l-1.4 1.4"></path></svg>
          <svg *ngIf="tile.icon === 'cloche'" viewBox="0 0 24 24"><path d="M4 16h16"></path><path d="M6 16a6 6 0 0 1 12 0"></path><path d="M12 6v2"></path></svg>
          <svg *ngIf="tile.icon === 'moon'" viewBox="0 0 24 24"><path d="M15 4a8 8 0 1 0 5 13 7 7 0 1 1-5-13z"></path></svg>
          <svg *ngIf="tile.icon === 'cup'" viewBox="0 0 24 24"><path d="M7 8h8v5a4 4 0 0 1-4 4H11a4 4 0 0 1-4-4z"></path><path d="M15 9h2a2 2 0 0 1 0 4h-2"></path><path d="M8 20h8"></path></svg>
          <svg *ngIf="tile.icon === 'leaf'" viewBox="0 0 24 24"><path d="M5 19C8 8 16 5 20 4c-1 6-4 12-15 15z"></path><path d="M9 15c2-2 4-3 7-4"></path></svg>
          <b>{{ tile.label }}</b>
          <span class="art">
            <app-photo [path]="covers[tile.key]" icon="meals" [alt]="tile.label"></app-photo>
          </span>
        </button>
        <button type="button" class="pick-wide" *ngIf="sweet as tile" [class.on]="selected === tile.key" [disabled]="!tile.mealId" (click)="choose(tile)">
          <span class="copy">
            <svg viewBox="0 0 24 24"><path d="M4 14h16v3a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2z"></path><path d="M7 14c1-4 3-6 5-6s4 2 5 6"></path><path d="M12 4v4"></path></svg>
            <b>{{ tile.label }}</b>
          </span>
          <span class="art">
            <app-photo [path]="covers[tile.key]" icon="meals" [alt]="tile.label"></app-photo>
          </span>
        </button>
      </div>

      <div class="pick-hint">
        <p>اختر نوع الوجبة ثم حدد الأكلات وعدد مرات التكرار خلال الشهر.</p>
        <span aria-hidden="true">
          <svg viewBox="0 0 24 24"><rect x="4" y="5" width="16" height="15" rx="2"></rect><path d="M8 3v4M16 3v4M4 10h16"></path><path d="M9 14l2 2 4-4"></path></svg>
        </span>
      </div>

      <button type="button" class="pick-next" [disabled]="!selected" (click)="next()">
        التالي
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M14 6l-6 6 6 6"></path></svg>
      </button>

      <app-hub-nav></app-hub-nav>
    </section>
  `
})
export class MealsComponent implements OnInit {
  meals: MealCard[] = [];
  covers: Record<string, string> = {};
  selected = '';
  error = '';

  constructor(private api: ApiService, public draft: DraftService, private router: Router) {}

  ngOnInit(): void {
    this.api.meals().subscribe({
      next: meals => {
        this.meals = [...meals].sort((a, b) => (a.sort ?? 0) - (b.sort ?? 0));
        const linked = meals.filter(meal => this.keyFor(meal.name));
        if (!linked.length) return;
        const covers: Record<string, string> = {};
        for (const meal of linked) {
          const key = this.keyFor(meal.name);
          const path = meal.imagePath || meal.iconPath;
          if (key && path) covers[key] = path;
        }
        this.covers = covers;
        const missing = linked.filter(meal => {
          const key = this.keyFor(meal.name);
          return key && !covers[key];
        });
        if (!missing.length) return;
        forkJoin(missing.map(meal => this.api.items(meal.id, '').pipe(
          map(items => ({ key: this.keyFor(meal.name), image: items.find(item => item.imagePath)?.imagePath || '' })),
          catchError(() => of({ key: this.keyFor(meal.name), image: '' }))
        ))).subscribe(groups => {
          const next = { ...this.covers };
          for (const group of groups) {
            if (group.key && group.image) next[group.key] = group.image;
          }
          this.covers = next;
        });
      },
      error: (err: Error) => this.error = err.message
    });
  }

  get tiles(): MealTile[] {
    return this.catalog().filter(tile => !tile.wide);
  }

  get sweet(): MealTile | null {
    return this.catalog().find(tile => tile.wide) || null;
  }

  choose(tile: MealTile): void {
    this.selected = this.selected === tile.key ? '' : tile.key;
  }

  next(): void {
    const tile = this.catalog().find(item => item.key === this.selected);
    if (!tile) return;
    this.router.navigate(['/meals', tile.mealId || tile.key]);
  }

  private catalog(): MealTile[] {
    const specs = [
      { key: 'breakfast', label: 'الفطار', tone: 'sun', icon: 'sun', wide: false, names: ['فطار', 'فطور'] },
      { key: 'lunch', label: 'الغداء', tone: 'lunch', icon: 'cloche', wide: false, names: ['غداء'] },
      { key: 'dinner', label: 'العشاء', tone: 'moon', icon: 'moon', wide: false, names: ['عشاء'] },
      { key: 'snacks', label: 'وجبات خفيفة', tone: 'snack', icon: 'cup', wide: false, names: [] as string[] },
      { key: 'drinks', label: 'مشروبات', tone: 'drink', icon: 'cup', wide: false, names: ['مشروب'] },
      { key: 'healthy', label: 'أكلات صحية', tone: 'leaf', icon: 'leaf', wide: false, names: [] as string[] },
      { key: 'sweets', label: 'حلويات', tone: 'sweet', icon: 'cake', wide: true, names: ['حلويات'] }
    ];
    return specs.map(spec => ({
      key: spec.key,
      label: spec.label,
      tone: spec.tone,
      icon: spec.icon,
      wide: spec.wide,
      mealId: spec.names.length ? (this.meals.find(meal => spec.names.some(name => (meal.name || '').includes(name)))?.id || null) : null
    }));
  }

  private keyFor(name?: string | null): string {
    const value = name || '';
    if (value.includes('فطار') || value.includes('فطور')) return 'breakfast';
    if (value.includes('غداء')) return 'lunch';
    if (value.includes('عشاء')) return 'dinner';
    if (value.includes('مشروب')) return 'drinks';
    if (value.includes('حلويات')) return 'sweets';
    return '';
  }
}
