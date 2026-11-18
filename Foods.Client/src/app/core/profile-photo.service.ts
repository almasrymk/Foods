import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { ApiService } from './api.service';
import { mediaUrl } from './environment';

const KEY = 'foods.photo';

/** The signed-in user's photo (absolute URL, '' = default person icon), shared by every avatar. */
@Injectable({ providedIn: 'root' })
export class ProfilePhotoService {
  private readonly photo = new BehaviorSubject<string>(this.read());
  readonly photo$ = this.photo.asObservable();
  private synced = false;

  constructor(private api: ApiService) {}

  /** Stores the API path (/uploads/...) or clears it. */
  set(path: string | null | undefined): void {
    try { path ? localStorage.setItem(KEY, path) : localStorage.removeItem(KEY); } catch { /* storage blocked */ }
    this.photo.next(path ? mediaUrl(path) : '');
  }

  /** Refreshes once per app load from the profile (covers photos changed on another device). */
  sync(): void {
    if (this.synced || !localStorage.getItem('foods.token')) return;
    this.synced = true;
    this.api.profile().subscribe({ next: p => this.set(p.imagePath), error: () => { this.synced = false; } });
  }

  reset(): void {
    this.synced = false;
    this.set(null);
  }

  private read(): string {
    try { const p = localStorage.getItem(KEY); return p ? mediaUrl(p) : ''; } catch { return ''; }
  }
}
