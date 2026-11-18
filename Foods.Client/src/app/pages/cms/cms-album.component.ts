import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { AlbumImage, AlbumKind, CmsService } from '../../core/cms.service';
import { mediaUrl } from '../../core/environment';
import { IconComponent } from '../../core/icon.component';

/** "ألبوم الصور" of a dish or an ingredient: upload several photos, delete, move right/left. Saves right away. */
@Component({
  selector: 'app-cms-album',
  imports: [CommonModule, IconComponent],
  template: `
    <p class="muted" *ngIf="!ownerId">احفظ الأول، وبعدين تقدر تضيف صور للألبوم.</p>
    <ng-container *ngIf="ownerId">
      <p class="error" *ngIf="error">{{ error }}</p>
      <div class="album-grid" *ngIf="images.length">
        <figure class="album-thumb" *ngFor="let image of images; let i = index; let first = first; let last = last">
          <img [src]="url(image.imagePath)" alt="" loading="lazy">
          <figcaption>
            <button type="button" class="ghost" title="قبل" [disabled]="busy || first" (click)="move(i, -1)">→</button>
            <button type="button" class="ghost" title="حذف" [disabled]="busy" (click)="remove(image)"><app-icon name="delete"></app-icon></button>
            <button type="button" class="ghost" title="بعد" [disabled]="busy || last" (click)="move(i, 1)">←</button>
          </figcaption>
        </figure>
      </div>
      <p class="muted" *ngIf="!images.length && !busy">مفيش صور في الألبوم لسه.</p>
      <label class="file-btn">{{ busy ? 'جاري الرفع...' : 'إضافة صور' }}<input type="file" accept="image/*" multiple [disabled]="busy" (change)="onFiles($event)"></label>
    </ng-container>
  `,
  styles: [`
    :host { display: flex; flex-direction: column; align-items: flex-start; gap: 10px; width: 100%; }
    .album-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(120px, 1fr)); gap: 12px; width: 100%; }
    .album-thumb { margin: 0; display: flex; flex-direction: column; gap: 6px; background: white; border-radius: 14px; padding: 6px; box-shadow: inset 0 0 0 1px var(--line); }
    .album-thumb img { width: 100%; aspect-ratio: 4 / 3; object-fit: cover; border-radius: 10px; display: block; }
    .album-thumb figcaption { display: flex; justify-content: space-between; gap: 4px; }
    .album-thumb .ghost { width: auto; flex: 1; padding: 4px 0; min-width: 0; }
    .album-thumb .ghost:disabled { opacity: .35; }
  `]
})
export class CmsAlbumComponent implements OnChanges {
  @Input({ required: true }) kind!: AlbumKind;
  @Input() ownerId: string | null | undefined;
  images: AlbumImage[] = [];
  busy = false;
  error = '';
  url = mediaUrl;

  constructor(private cms: CmsService) {}

  ngOnChanges(): void {
    this.images = [];
    if (!this.ownerId) return;
    this.cms.album(this.kind, this.ownerId).subscribe({
      next: rows => this.images = rows,
      error: (err: Error) => this.error = err.message
    });
  }

  onFiles(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files || []);
    input.value = '';
    if (!files.length || !this.ownerId) return;
    this.busy = true;
    this.error = '';
    forkJoin(files.map(file => this.cms.upload(file))).subscribe({
      next: paths => this.cms.addAlbumImages(this.kind, this.ownerId!, paths).subscribe({
        next: rows => { this.images = rows; this.busy = false; },
        error: (err: Error) => { this.error = err.message; this.busy = false; }
      }),
      error: (err: Error) => { this.error = err.message; this.busy = false; }
    });
  }

  remove(image: AlbumImage): void {
    this.busy = true;
    this.cms.removeAlbumImage(this.kind, image.id).subscribe({
      next: () => { this.images = this.images.filter(e => e.id !== image.id); this.busy = false; },
      error: (err: Error) => { this.error = err.message; this.busy = false; }
    });
  }

  /** delta -1 = earlier (shown first), +1 = later. */
  move(index: number, delta: number): void {
    const target = index + delta;
    if (!this.ownerId || target < 0 || target >= this.images.length) return;
    const next = [...this.images];
    [next[index], next[target]] = [next[target], next[index]];
    this.images = next;
    this.busy = true;
    this.cms.reorderAlbum(this.kind, this.ownerId, next.map(e => e.id)).subscribe({
      next: rows => { this.images = rows; this.busy = false; },
      error: (err: Error) => { this.error = err.message; this.busy = false; }
    });
  }
}
