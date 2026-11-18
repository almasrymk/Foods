import { AfterViewInit, Component, ElementRef, Input, NgZone, OnDestroy, ViewChild } from '@angular/core';
import { mediaUrl } from '../../../core/environment';

/**
 * Swipeable photo album that fills its container exactly (same size, radius and object-fit as a plain img).
 * - 0 or 1 photo: renders a plain img, like before.
 * - More photos: horizontal scroll-snap track (touch swipe or mouse drag) + small dots, no autoplay, RTL safe.
 * Swiping or tapping a dot never reaches the parent row (no navigation / selection).
 */
@Component({
  selector: 'fa-photo-slider',
  template: `
    @if (urls.length <= 1) {
      <img [src]="urls[0] || fallback" [alt]="alt" [attr.loading]="eager ? null : 'lazy'" decoding="async"
           [style.object-fit]="fit">
    } @else {
      <div class="ps-track" #track
           (scroll)="onScroll()"
           (pointerdown)="onDown($event)"
           (pointermove)="onMove($event)"
           (pointerup)="onUp($event)"
           (pointercancel)="onUp($event)">
        @for (url of urls; track $index; let i = $index) {
          <div class="ps-slide">
            <img [src]="url" [alt]="alt" [attr.loading]="i === 0 && eager ? null : 'lazy'" decoding="async"
                 draggable="false" [style.object-fit]="fit">
          </div>
        }
      </div>
      <span class="ps-dots" (pointerdown)="$event.stopPropagation()" (click)="$event.stopPropagation(); $event.preventDefault()">
        @for (url of urls; track $index; let i = $index) {
          <i class="ps-dot" [class.on]="i === index" (click)="go(i, $event)" [attr.aria-label]="'صورة ' + (i + 1)"></i>
        }
      </span>
    }
  `,
  styles: [`
    :host { display: block; position: relative; width: 100%; height: 100%; overflow: hidden; border-radius: inherit; container-type: size; }
    :host > img { display: block; width: 100%; height: 100%; object-fit: cover; border-radius: inherit; }
    .ps-track {
      display: flex; width: 100%; height: 100%;
      overflow-x: auto; overflow-y: hidden;
      scroll-snap-type: x mandatory; overscroll-behavior-x: contain;
      scrollbar-width: none; -ms-overflow-style: none;
      touch-action: pan-x pan-y; user-select: none; -webkit-user-select: none;
      cursor: grab; border-radius: inherit;
    }
    .ps-track::-webkit-scrollbar { display: none; }
    .ps-track.dragging { cursor: grabbing; scroll-snap-type: none; }
    .ps-slide { flex: 0 0 100%; width: 100%; height: 100%; scroll-snap-align: start; scroll-snap-stop: always; }
    .ps-slide img { display: block; width: 100%; height: 100%; object-fit: cover; pointer-events: none; }
    .ps-dots {
      position: absolute; left: 50%; bottom: 6px; transform: translateX(-50%);
      display: flex; gap: 4px; padding: 3px 5px; border-radius: 999px;
      background: rgba(0, 0, 0, .28); pointer-events: auto; direction: ltr;
    }
    .ps-dot {
      display: block; width: 6px; height: 6px; border-radius: 50%;
      background: rgba(255, 255, 255, .6); cursor: pointer; transition: background .15s, transform .15s;
    }
    .ps-dot.on { background: #fff; transform: scale(1.2); }
    /* Dots never wider than the photo: they shrink with it, and small thumbnails get tiny dots. */
    .ps-dots { max-width: calc(100% - 6px); box-sizing: border-box; overflow: hidden; }
    .ps-dot { flex: 0 1 6px; min-width: 2px; height: auto; aspect-ratio: 1; }
    @container (max-width: 140px) {
      .ps-dots { bottom: 2px; gap: 2px; padding: 1px 2px; }
      .ps-dot { flex-basis: 3px; }
      .ps-dot.on { transform: scale(1.3); }
    }
  `]
})
export class FaPhotoSliderComponent implements AfterViewInit, OnDestroy {
  /** Photo paths (relative /uploads/... or full urls); passed through mediaUrl. */
  @Input() set images(value: (string | null | undefined)[] | null | undefined) {
    const seen = new Set<string>();
    this.urls = (value || [])
      .map(path => (path && (path.startsWith('assets/') || path.startsWith('data:')) ? path : mediaUrl(path)))
      .filter(url => !!url && !seen.has(url) && !!seen.add(url));
    this.index = 0;
  }
  @Input() alt = '';
  /** Shown when there is no photo at all. */
  @Input() fallback = '';
  /** Same object-fit as the image it replaces. */
  @Input() fit = 'cover';
  @Input() eager = false;
  @ViewChild('track') track?: ElementRef<HTMLDivElement>;

  urls: string[] = [];
  index = 0;
  private pointerId: number | null = null;
  private startX = 0;
  private startScroll = 0;
  private moved = false;
  private suppressClick = false;
  private readonly onClickCapture = (event: Event) => {
    if (!this.suppressClick) return;
    this.suppressClick = false;
    event.preventDefault();
    event.stopPropagation();
  };

  constructor(private host: ElementRef<HTMLElement>, private zone: NgZone) {}

  ngAfterViewInit(): void {
    // Capture phase so a swipe never turns into a click on the parent link / row.
    this.zone.runOutsideAngular(() => this.host.nativeElement.addEventListener('click', this.onClickCapture, true));
  }

  ngOnDestroy(): void {
    this.host.nativeElement.removeEventListener('click', this.onClickCapture, true);
  }

  onScroll(): void {
    const el = this.track?.nativeElement;
    if (!el || !el.clientWidth) return;
    const next = Math.max(0, Math.min(this.urls.length - 1, Math.round(Math.abs(el.scrollLeft) / el.clientWidth)));
    if (next !== this.index) this.index = next;
  }

  go(i: number, event?: Event): void {
    event?.stopPropagation();
    event?.preventDefault();
    this.scrollTo(i, 'smooth');
  }

  onDown(event: PointerEvent): void {
    this.moved = false;
    this.suppressClick = false;
    // Touch / pen swipe natively; the mouse needs a manual drag.
    if (event.pointerType !== 'mouse' || event.button !== 0) return;
    const el = this.track?.nativeElement;
    if (!el) return;
    this.pointerId = event.pointerId;
    this.startX = event.clientX;
    this.startScroll = el.scrollLeft;
  }

  onMove(event: PointerEvent): void {
    const el = this.track?.nativeElement;
    if (!el) return;
    if (this.pointerId === null || event.pointerId !== this.pointerId) {
      // Touch swipes scroll natively; keep their moves away from parent handlers.
      if (event.pointerType !== 'mouse') event.stopPropagation();
      return;
    }
    const dx = event.clientX - this.startX;
    if (!this.moved && Math.abs(dx) > 6) {
      this.moved = true;
      el.classList.add('dragging');
      el.setPointerCapture(event.pointerId);
    }
    if (this.moved) {
      event.stopPropagation();
      event.preventDefault();
      el.scrollLeft = this.startScroll - dx;
    }
  }

  onUp(event: PointerEvent): void {
    const el = this.track?.nativeElement;
    if (this.pointerId === null || event.pointerId !== this.pointerId || !el) return;
    this.pointerId = null;
    if (!this.moved) return;
    event.stopPropagation();
    this.suppressClick = true;
    window.setTimeout(() => { this.suppressClick = false; }, 350);
    if (el.hasPointerCapture(event.pointerId)) el.releasePointerCapture(event.pointerId);
    el.classList.remove('dragging');
    const width = el.clientWidth || 1;
    const dragged = (event.clientX - this.startX) / width;
    const rtl = getComputedStyle(el).direction === 'rtl';
    const from = Math.round(Math.abs(this.startScroll) / width);
    // Dragging the content toward the start of the reading direction shows the next photo.
    const forward = rtl ? dragged > 0 : dragged < 0;
    let target = from;
    if (Math.abs(dragged) > 0.15) target = forward ? from + 1 : from - 1;
    this.scrollTo(Math.max(0, Math.min(this.urls.length - 1, target)), 'smooth');
  }

  private scrollTo(i: number, behavior: ScrollBehavior): void {
    const el = this.track?.nativeElement;
    if (!el) return;
    const rtl = getComputedStyle(el).direction === 'rtl';
    el.scrollTo({ left: (rtl ? -1 : 1) * i * el.clientWidth, behavior });
    this.index = i;
  }
}
