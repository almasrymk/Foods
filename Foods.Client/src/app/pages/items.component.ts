import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiService } from '../core/api.service';
import { DraftService } from '../core/draft.service';
import { ItemCard } from '../core/models';
import { PhotoComponent } from '../core/photo.component';
import { LoadingComponent } from '../core/loading.component';
import { HubNavComponent } from '../core/hub-nav.component';

interface DishFilter {
  id: string;
  name: string;
}

@Component({
  selector: 'app-items',
  imports: [CommonModule, FormsModule, RouterLink, PhotoComponent, LoadingComponent, HubNavComponent],
  template: `
    <section class="menu">
      <header class="menu-brand">
        <img src="logo.png?v=2" alt="Foods">
      </header>
      <div class="menu-title">
        <h1>
          <i aria-hidden="true"></i>
          {{ mealName || 'الأكلات' }}
          <i aria-hidden="true"></i>
        </h1>
        <a class="back" routerLink="/meals" aria-label="الرجوع للوجبات">
          <svg viewBox="0 0 24 24"><path d="M14 6l-6 6 6 6"></path></svg>
        </a>
      </div>

      <div class="menu-search">
        <form (ngSubmit)="limit = 24">
          <input [(ngModel)]="text" name="text" placeholder="ابحث عن أكلة أو مكوّن أو وصفة...">
          <span aria-hidden="true">
            <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="6"></circle><path d="M16 16l4 4"></path></svg>
          </span>
        </form>
        <button type="button" class="filter" [class.on]="kitchenId" (click)="kitchenId = ''" aria-label="تصفية">
          <svg viewBox="0 0 24 24"><path d="M4 7h16"></path><path d="M7 12h10"></path><path d="M10 17h4"></path></svg>
        </button>
      </div>

      <div class="menu-chips">
        <button type="button" *ngFor="let chip of filters" [class.on]="kitchenId === chip.id" (click)="kitchenId = chip.id; limit = 24">
          {{ chip.name }}
        </button>
      </div>

      <p class="error" *ngIf="error">{{ error }}</p>
      <app-loading *ngIf="loading"></app-loading>

      <article class="menu-dish" [class.on]="draft.has(item.id)" *ngFor="let item of shown">
        <a class="pic" [routerLink]="['/items', item.id]">
          <app-photo [path]="item.imagePath" icon="items" [alt]="item.name || 'أكلة'"></app-photo>
          <span class="ok" *ngIf="draft.has(item.id)" aria-hidden="true">
            <svg viewBox="0 0 24 24"><path d="M6 12l4 4 8-8"></path></svg>
          </span>
        </a>
        <div class="copy">
          <span class="tag" [class]="kitchen(item).id">
            {{ kitchen(item).name }}
            <svg *ngIf="kitchen(item).id === 'east'" viewBox="0 0 24 24"><path d="M4 10h10a4 4 0 0 1 0 8H8"></path><path d="M14 10V6"></path></svg>
            <svg *ngIf="kitchen(item).id === 'west'" viewBox="0 0 24 24"><path d="M8 14a4 4 0 0 1 8 0"></path><path d="M12 6v4"></path></svg>
            <svg *ngIf="kitchen(item).id === 'grill'" viewBox="0 0 24 24"><path d="M6 16c2-2 4-2 6 0s4 2 6 0"></path><path d="M7 19h10"></path></svg>
            <svg *ngIf="kitchen(item).id === 'healthy'" viewBox="0 0 24 24"><path d="M5 19C8 8 16 5 20 4c-1 6-4 12-15 15z"></path></svg>
          </span>
          <a [routerLink]="['/items', item.id]">{{ item.name }}</a>
          <p>{{ item.summary || 'وصف الأكلة' }}</p>
        </div>
        <div class="menu-pick">
          <div class="top">
            <input class="tick" type="checkbox" [checked]="draft.has(item.id)" (change)="toggleSelect(item)" [attr.aria-label]="'اختيار ' + item.name">
            <button type="button" class="heart" [class.on]="item.isFavorite" (click)="toggleFavorite(item)" [attr.aria-label]="'مفضلة ' + item.name">
              <svg viewBox="0 0 24 24"><path d="M12 20s-7-4.4-7-9a4 4 0 0 1 7-2 4 4 0 0 1 7 2c0 4.6-7 9-7 9z"></path></svg>
            </button>
          </div>
          <div class="counter">
            <button type="button" (click)="step(item, -1)" aria-label="نقص">−</button>
            <span class="count" [class.dragging]="dragId === item.id" [style.transform]="dragShift(item.id)"
              (pointerdown)="dragStart($event, item)" (pointermove)="dragMove($event)"
              (pointerup)="dragEnd($event, item)" (pointercancel)="dragEnd($event, item)">{{ repeats[item.id] || 0 }}</span>
            <button type="button" (click)="step(item, 1)" aria-label="زيادة">+</button>
          </div>
        </div>
      </article>

      <button type="button" class="menu-more" *ngIf="matched > shown.length" (click)="limit = limit + 24">عرض المزيد</button>
      <p class="muted" *ngIf="!loading && !shown.length">مفيش أكلات مطابقة.</p>

      <div class="menu-go" *ngIf="draft.items.length">
        <span>{{ pickedLabel }} <i aria-hidden="true">✓</i></span>
        <a routerLink="/builder">متابعة <b aria-hidden="true">←</b></a>
      </div>
      <app-hub-nav></app-hub-nav>
    </section>
  `
})
export class ItemsComponent implements OnInit {
  source: ItemCard[] = [];
  mealName = '';
  text = '';
  kitchenId = '';
  kind = '';
  error = '';
  loading = true;
  limit = 24;
  repeats: Record<string, number> = {};
  filters: DishFilter[] = [
    { id: '', name: 'الكل' },
    { id: 'east', name: 'شرقي' },
    { id: 'west', name: 'غربي' },
    { id: 'grill', name: 'مشويات' },
    { id: 'healthy', name: 'صحي' }
  ];
  dragId = '';
  nudgeId = '';
  private dragX = 0;
  private nudgeX = 0;
  private startX = 0;
  private mealId = '';

  constructor(private api: ApiService, private route: ActivatedRoute, public draft: DraftService) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') || '';
    if (id === 'snacks' || id === 'healthy') {
      this.kind = id;
      this.mealName = id === 'snacks' ? 'وجبات خفيفة' : 'أكلات صحية';
      this.loadPool();
      return;
    }
    this.mealId = id;
    this.api.meals().subscribe({
      next: meals => this.mealName = meals.find(meal => meal.id === this.mealId)?.name || this.mealName,
      error: () => this.mealName = ''
    });
    this.load();
  }

  get matched(): number {
    return this.filtered().length;
  }

  get shown(): ItemCard[] {
    return this.filtered().slice(0, this.limit);
  }

  get pickedLabel(): string {
    const count = this.draft.items.length;
    if (count === 1) return 'تم اختيار أكلة واحدة';
    if (count === 2) return 'تم اختيار أكلتين';
    if (count >= 3 && count <= 10) return `تم اختيار ${count} أكلات`;
    return `تم اختيار ${count} أكلة`;
  }

  kitchen(item: ItemCard): DishFilter {
    const blob = `${item.name || ''} ${item.summary || ''}`;
    if (/مشوي|مشاوي/.test(blob)) return { id: 'grill', name: 'مشويات' };
    if (/بيتزا|برجر|مكرون|باستا|ساندويتش|بروست/.test(blob)) return { id: 'west', name: 'غربي' };
    if (/سلطة|شورب|دايت|صحي|شوفان/.test(blob)) return { id: 'healthy', name: 'صحي' };
    return { id: 'east', name: 'شرقي' };
  }

  load(): void {
    this.loading = true;
    this.api.items(this.mealId, '').subscribe({
      next: items => {
        this.take(items);
        this.loading = false;
      },
      error: (err: Error) => {
        this.error = err.message;
        this.loading = false;
      }
    });
  }

  dragShift(id: string): string {
    if (this.dragId === id) return `translateX(${this.dragX}px)`;
    if (this.nudgeId === id) return `translateX(${this.nudgeX}px)`;
    return '';
  }

  step(item: ItemCard, delta: number): void {
    this.bump(item, delta);
    this.nudgeId = item.id;
    this.nudgeX = delta > 0 ? 18 : -18;
    const id = item.id;
    setTimeout(() => {
      if (this.nudgeId === id) {
        this.nudgeId = '';
        this.nudgeX = 0;
      }
    }, 160);
  }

  dragStart(event: PointerEvent, item: ItemCard): void {
    this.dragId = item.id;
    this.startX = event.clientX;
    this.dragX = 0;
    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
  }

  dragMove(event: PointerEvent): void {
    if (!this.dragId) return;
    this.dragX = Math.max(-26, Math.min(26, event.clientX - this.startX));
  }

  dragEnd(event: PointerEvent, item: ItemCard): void {
    if (this.dragId !== item.id) return;
    const delta = event.clientX - this.startX;
    this.dragId = '';
    this.dragX = 0;
    if (delta > 16) this.bump(item, 1);
    else if (delta < -16) this.bump(item, -1);
  }

  bump(item: ItemCard, delta: number): void {
    const next = Math.max(0, (this.repeats[item.id] || 0) + delta);
    this.repeats[item.id] = next;
    if (next === 0) {
      this.draft.remove(item.id);
      return;
    }
    this.draft.add({
      itemId: item.id,
      name: item.name || '',
      imagePath: item.imagePath,
      repeatCount: next,
      peopleCount: null
    });
  }

  toggleSelect(item: ItemCard): void {
    if (this.draft.has(item.id)) {
      this.draft.remove(item.id);
      this.repeats[item.id] = 0;
      return;
    }
    if (!this.repeats[item.id]) this.repeats[item.id] = 1;
    this.draft.add({
      itemId: item.id,
      name: item.name || '',
      imagePath: item.imagePath,
      repeatCount: this.repeats[item.id] || 1,
      peopleCount: null
    });
  }

  toggleFavorite(item: ItemCard): void {
    const call = item.isFavorite ? this.api.removeFavorite(item.id) : this.api.addFavorite(item.id);
    call.subscribe({
      next: () => item.isFavorite = !item.isFavorite,
      error: (err: Error) => this.error = err.message
    });
  }

  private loadPool(): void {
    this.api.meals().subscribe({
      next: meals => {
        const pool = meals.filter(meal => /فطار|فطور|غداء|عشاء/.test(meal.name || ''));
        const targets = pool.length ? pool : meals;
        if (!targets.length) {
          this.loading = false;
          return;
        }
        forkJoin(targets.map(meal => this.api.items(meal.id, '').pipe(catchError(() => of([] as ItemCard[]))))).subscribe({
          next: groups => {
            const seen = new Set<string>();
            const merged: ItemCard[] = [];
            for (const group of groups) {
              for (const item of group) {
                if (seen.has(item.id)) continue;
                seen.add(item.id);
                merged.push(item);
              }
            }
            this.take(merged);
            this.loading = false;
          },
          error: (err: Error) => {
            this.error = err.message;
            this.loading = false;
          }
        });
      },
      error: (err: Error) => {
        this.error = err.message;
        this.loading = false;
      }
    });
  }

  private take(items: ItemCard[]): void {
    this.source = items;
    for (const item of items) {
      if (this.repeats[item.id] == null) {
        const chosen = this.draft.items.find(entry => entry.itemId === item.id);
        this.repeats[item.id] = chosen ? chosen.repeatCount : 0;
      }
    }
  }

  private filtered(): ItemCard[] {
    const query = this.text.trim();
    return this.source.filter(item => {
      const blob = `${item.name || ''} ${item.summary || ''}`;
      if (this.kind === 'snacks' && !/خفيف|سناك|ساندويتش|توست|زبادي|فاكهة|مقرمش/.test(blob)) return false;
      if (this.kind === 'healthy' && !/سلطة|شورب|مشوي|خضار|شوفان|عدس|صحي/.test(blob)) return false;
      if (query && !blob.includes(query)) return false;
      return !this.kitchenId || this.kitchen(item).id === this.kitchenId;
    });
  }
}
