import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { ApiService } from './api.service';

/** Unread notifications count shared by the bell badges; polls while the app is open. */
@Injectable({ providedIn: 'root' })
export class NotificationsService implements OnDestroy {
  private readonly count = new BehaviorSubject<number>(0);
  readonly unread$ = this.count.asObservable();
  private timer = 0;
  private lastFetch = 0;

  constructor(private api: ApiService) {}

  /** Fetches the count (at most every few seconds) and keeps polling every minute. */
  refresh(force = false): void {
    if (!localStorage.getItem('foods.token')) { this.count.next(0); return; }
    if (!this.timer) this.timer = window.setInterval(() => this.refresh(true), 60_000);
    const now = Date.now();
    if (!force && now - this.lastFetch < 5_000) return;
    this.lastFetch = now;
    this.api.unreadNotifications().subscribe({ next: n => this.count.next(n || 0), error: () => {} });
  }

  set(count: number): void {
    this.count.next(Math.max(0, count));
  }

  ngOnDestroy(): void {
    window.clearInterval(this.timer);
  }
}
