import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { DraftService } from '../core/draft.service';
import { ShoppingLine } from '../core/models';
import { LoadingComponent } from '../core/loading.component';

@Component({
  selector: 'app-builder',
  imports: [CommonModule, FormsModule, RouterLink, LoadingComponent],
  template: `
    <div class="dish-page">
      <div class="dish-head">
        <a class="back" routerLink="/meals" aria-label="رجوع">›</a>
        <h1>المكونات</h1>
        <span></span>
      </div>
      <p *ngIf="draft.items.length === 0">لم تختر أكلات بعد. <a routerLink="/meals">ارجع للوجبات</a></p>
      <app-loading *ngIf="loading"></app-loading>
      <p class="error" *ngIf="error">{{ error }}</p>
      <article class="ing" *ngFor="let line of draft.lines">
        <button type="button" class="ing-mark" (click)="removeLine(line)" aria-label="حذف">
          <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M8 4l2 2M16 4l-2 2M7 8h10l-1 12H8z"></path><path d="M9 4h6"></path><path d="M15 9l-6 8M9 9l6 8"></path></svg>
        </button>
        <div class="ing-body">
          <b class="ing-name">{{ line.name }}</b>
          <div class="ing-meta">
            <span class="ing-place">
              <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M6 8h12l-1 12H7z"></path><path d="M9 8V6a3 3 0 0 1 6 0v2"></path></svg>
              من {{ line.purchasePlaceName || 'البقالة' }}
            </span>
            <span class="ing-unit">{{ line.unitName }}</span>
            <div class="ing-count" [class.on]="line.quantity > 0">
              <button type="button" (click)="step(line, -1)" aria-label="نقص">−</button>
              <span [class.dragging]="dragId === key(line)" [style.transform]="dragShift(key(line))"
                (pointerdown)="dragStart($event, line)" (pointermove)="dragMove($event)"
                (pointerup)="dragEnd($event, line)" (pointercancel)="dragEnd($event, line)">{{ amount(line) }}</span>
              <button type="button" (click)="step(line, 1)" aria-label="زيادة">+</button>
            </div>
          </div>
        </div>
        <input class="dish-check" type="checkbox" [checked]="on(line)" (change)="toggle(line)" [attr.aria-label]="'اختيار ' + line.name">
      </article>
      <form *ngIf="draft.lines.length" (ngSubmit)="save()">
        <div class="ing-save" *ngIf="buy">
          <label>اسم الطلب<input [(ngModel)]="draft.name" name="orderName" required></label>
          <label>التاريخ<input [(ngModel)]="draft.date" name="orderDate" type="date" required></label>
          <label>ملاحظات<input [(ngModel)]="draft.notes" name="notes"></label>
        </div>
        <button class="picked-bar buy" type="button" *ngIf="!buy" (click)="buy = true">طلبات الشراء</button>
        <button class="picked-bar buy" type="submit" *ngIf="buy">حفظ الطلب</button>
      </form>
    </div>
  `
})
export class BuilderComponent implements OnInit {
  error = '';
  loading = false;
  buy = false;
  dragId = '';
  nudgeId = '';
  private dragX = 0;
  private nudgeX = 0;
  private startX = 0;
  private off: Record<string, boolean> = {};

  constructor(private api: ApiService, public draft: DraftService, private router: Router) {}

  ngOnInit(): void {
    if (this.draft.items.length && this.draft.lines.length === 0) this.calculate();
  }

  calculate(): void {
    this.error = '';
    this.loading = true;
    this.api.calculate(this.draft.items).subscribe({
      next: lines => {
        this.draft.lines = lines;
        this.loading = false;
      },
      error: (err: Error) => {
        this.error = err.message;
        this.loading = false;
      }
    });
  }

  key(line: ShoppingLine): string {
    return line.contentId + line.unitId;
  }

  on(line: ShoppingLine): boolean {
    return !this.off[this.key(line)];
  }

  toggle(line: ShoppingLine): void {
    const id = this.key(line);
    this.off[id] = this.on(line);
  }

  amount(line: ShoppingLine): number {
    return Math.round(line.quantity * 100) / 100;
  }

  dragShift(id: string): string {
    if (this.dragId === id) return `translateX(${this.dragX}px)`;
    if (this.nudgeId === id) return `translateX(${this.nudgeX}px)`;
    return '';
  }

  step(line: ShoppingLine, delta: number): void {
    this.bump(line, delta);
    const id = this.key(line);
    this.nudgeId = id;
    this.nudgeX = delta > 0 ? 18 : -18;
    setTimeout(() => {
      if (this.nudgeId === id) {
        this.nudgeId = '';
        this.nudgeX = 0;
      }
    }, 160);
  }

  dragStart(event: PointerEvent, line: ShoppingLine): void {
    this.dragId = this.key(line);
    this.startX = event.clientX;
    this.dragX = 0;
    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
  }

  dragMove(event: PointerEvent): void {
    if (!this.dragId) return;
    this.dragX = Math.max(-26, Math.min(26, event.clientX - this.startX));
  }

  dragEnd(event: PointerEvent, line: ShoppingLine): void {
    if (this.dragId !== this.key(line)) return;
    const delta = event.clientX - this.startX;
    this.dragId = '';
    this.dragX = 0;
    if (delta > 16) this.bump(line, 1);
    else if (delta < -16) this.bump(line, -1);
  }

  bump(line: ShoppingLine, delta: number): void {
    line.quantity = Math.max(0, Math.round((line.quantity + delta) * 100) / 100);
    this.off[this.key(line)] = line.quantity === 0;
  }

  removeLine(line: ShoppingLine): void {
    this.draft.lines = this.draft.lines.filter(item => this.key(item) !== this.key(line));
  }

  save(): void {
    this.error = '';
    // Keep only checked lines before confirming into a separate saved order.
    this.draft.lines = this.draft.lines.filter(line => this.on(line) && line.quantity > 0);
    this.draft.confirmOrder().subscribe({
      next: id => this.router.navigate(['/orders', id]),
      error: (err: Error) => this.error = err.message
    });
  }
}
