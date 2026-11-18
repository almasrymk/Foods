import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { DraftService } from '../core/draft.service';
import { mediaUrl } from '../core/environment';
import { ItemCard } from '../core/models';
import { PhotoComponent } from '../core/photo.component';

@Component({
  selector: 'app-favorites',
  imports: [CommonModule, FormsModule, RouterLink, PhotoComponent],
  template: `
    <div class="dish-page">
      <div class="dish-head">
        <a class="back" routerLink="/meals" aria-label="الوجبات">›</a>
        <h1>أكلاتي المفضلة</h1>
        <span></span>
      </div>
      <form class="dish-search" (ngSubmit)="$event.preventDefault()">
        <input [(ngModel)]="text" name="text" placeholder="ابحث عن أكلة">
        <button type="submit" aria-label="بحث">
          <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="11" cy="11" r="6"></circle><path d="M16 16l4 4"></path></svg>
        </button>
      </form>
      <p class="error" *ngIf="error">{{ error }}</p>
      <p class="muted" *ngIf="!error && !shown.length">لا توجد أكلات مفضلة</p>
      <article class="dish" [class.chosen]="draft.has(item.id)" *ngFor="let item of shown">
        <a class="dish-photo" [routerLink]="['/items', item.id]">
          <app-photo [path]="item.imagePath" icon="items" [alt]="item.name || 'أكلة'"></app-photo>
        </a>
        <a class="dish-copy" [routerLink]="['/items', item.id]">
          <h2>{{ item.name }}</h2>
          <p>{{ item.summary || 'وصف الأكلة' }}</p>
        </a>
        <div class="pick">
          <div class="pick-top">
            <input class="dish-check" type="checkbox" [checked]="draft.has(item.id)" (change)="toggleSelect(item)" [attr.aria-label]="'اختيار ' + item.name">
            <button type="button" class="fav on" (click)="remove(item)" [attr.aria-label]="'إزالة ' + item.name">
              <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 20s-7-4.4-7-9a4 4 0 0 1 7-2 4 4 0 0 1 7 2c0 4.6-7 9-7 9z"></path></svg>
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
      <button type="button" class="picked-bar" *ngIf="draft.items.length && !sheet" (click)="sheet = true">
        <span class="cart" aria-hidden="true">
          <svg viewBox="0 0 24 24"><path d="M6 8h12l-1 12H7z"></path><path d="M9 8V6a3 3 0 0 1 6 0v2"></path></svg>
        </span>
        <b>{{ draft.items.length }} أكلات مختارة</b>
        <span class="go">‹</span>
      </button>
      <div class="sheet" *ngIf="sheet && draft.items.length">
        <button type="button" class="sheet-backdrop" (click)="sheet = false" aria-label="إغلاق"></button>
        <div class="sheet-card">
          <div class="sheet-head">
            <h2>الأكلات المختارة ({{ draft.items.length }})</h2>
            <button type="button" class="sheet-close" (click)="sheet = false" aria-label="إغلاق">×</button>
          </div>
          <div class="sheet-row" *ngFor="let entry of draft.items">
            <button type="button" class="sheet-trash" (click)="drop(entry.itemId)" aria-label="حذف">
              <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 7h16"></path><path d="M9 7V5h6v2"></path><path d="M7 7l1 13h8l1-13"></path></svg>
            </button>
            <span>{{ entry.name }}</span>
            <b>{{ entry.repeatCount }}</b>
          </div>
          <a class="sheet-order" routerLink="/builder">الطلب</a>
        </div>
      </div>
    </div>
  `
})
export class FavoritesComponent implements OnInit {
  items: ItemCard[] = [];
  text = '';
  error = '';
  repeats: Record<string, number> = {};
  image = mediaUrl;
  sheet = false;
  dragId = '';
  nudgeId = '';
  private dragX = 0;
  private nudgeX = 0;
  private startX = 0;

  constructor(private api: ApiService, public draft: DraftService) {}

  get shown(): ItemCard[] {
    const query = this.text.trim();
    if (!query) return this.items;
    return this.items.filter(item => (item.name || '').includes(query) || (item.summary || '').includes(query));
  }

  ngOnInit(): void {
    this.api.favorites().subscribe({
      next: items => {
        this.items = items;
        for (const item of items) {
          const chosen = this.draft.items.find(entry => entry.itemId === item.id);
          this.repeats[item.id] = chosen ? chosen.repeatCount : 0;
        }
      },
      error: (err: Error) => this.error = err.message
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

  drop(itemId: string): void {
    this.draft.remove(itemId);
    this.repeats[itemId] = 0;
  }

  remove(item: ItemCard): void {
    this.api.removeFavorite(item.id).subscribe({
      next: () => {
        this.items = this.items.filter(entry => entry.id !== item.id);
        this.drop(item.id);
      },
      error: (err: Error) => this.error = err.message
    });
  }
}
