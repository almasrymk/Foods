import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/api.service';
import { DraftService } from '../../../core/draft.service';
import { FaIconComponent } from './icon.component';
import { FaPhotoSliderComponent } from './photo-slider.component';
import { FoodRow } from './screens.data';
import { egp } from '../../../core/money';

/** Selectable meal row: photo, tag, title, description, checkbox, heart and quantity stepper. */
@Component({
  selector: 'fa-food-row',
  imports: [RouterLink, FaIconComponent, FaPhotoSliderComponent],
  template: `
    <article class="fa-row" [class.sel-green]="greenSelection && selected" [class.priced]="item.price != null" [class.has-cuisine]="!!item.cuisine">
      <a class="fa-row-img" [routerLink]="['/items', item.id]" [queryParams]="cuisineId ? { cuisine: cuisineId } : null">
        @if ((item.images?.length || 0) > 1) {
          <fa-photo-slider [images]="item.images" [alt]="item.title" [fallback]="item.image" />
        } @else {
          <img [src]="item.image" [alt]="item.title" loading="lazy" decoding="async">
        }
        @if (selected) { <span class="fa-badge"><fa-icon name="check" [weight]="3" /></span> }
      </a>
      @if (item.group || item.category) {
        <div class="fa-tags">
          @if (item.group) { <span class="fa-tag2 group"><fa-icon name="layers" [weight]="1.8" />{{ item.group }}</span> }
          @if (item.category) { <span class="fa-tag2 category"><fa-icon name="grid" [weight]="1.8" />{{ item.category }}</span> }
        </div>
      } @else {
        <span class="fa-tag"><fa-icon [name]="item.tagIcon" [weight]="1.8" />{{ item.tag }}</span>
      }
      <a class="fa-row-title" [routerLink]="['/items', item.id]" [queryParams]="cuisineId ? { cuisine: cuisineId } : null">{{ item.title }}</a>
      @if (item.cuisine) { <span class="fa-row-cuisine"><fa-icon [name]="item.tagIcon" [weight]="1.8" />{{ item.cuisine }}</span> }
      <p class="fa-row-desc">{{ item.desc }}</p>
      @if ((item.price || 0) > 0) {
        <span class="fa-row-price"><fa-icon name="tag" [weight]="2.2" />{{ money(item.price) }}{{ item.priceMissing ? ' +' : '' }}</span>
      } @else if (item.price === 0) {
        <span class="fa-row-price none"><fa-icon name="tag" [weight]="2.2" />مفيش سعر لسه</span>
      }
      <i class="fa-vline"></i>
      <button type="button" class="fa-check" [class.on]="selected" (click)="toggleCheck()"
              [attr.aria-pressed]="selected" [attr.aria-label]="'اختيار ' + item.title">
        <fa-icon name="check" [weight]="3.2" />
      </button>
      <button type="button" class="fa-heart" [class.red]="redHeart" [disabled]="busy"
              (click)="toggleFavorite()"
              [attr.aria-pressed]="item.favorite" [attr.aria-label]="'مفضلة ' + item.title">
        @if (item.favorite) { <fa-icon name="heart" [filled]="true" /> } @else { <fa-icon name="heart" [weight]="1.8" /> }
      </button>
      <div class="fa-stepper">
        <button type="button" (click)="step(-1)" aria-label="نقص">−</button>
        <b class="fa-qty"
           [class.on]="item.qty > 0"
           [class.pull-plus]="pull === 'plus'"
           [class.pull-minus]="pull === 'minus'"
           [class.dragging]="dragging"
           [style.transform]="dragging ? dragTransform : null"
           (pointerdown)="onPointerDown($event)"
           (pointermove)="onPointerMove($event)"
           (pointerup)="onPointerUp($event)"
           (pointercancel)="onPointerUp($event)">{{ item.qty }}</b>
        <button type="button" (click)="step(1)" aria-label="زيادة">+</button>
      </div>
    </article>
  `
})
export class FaFoodRowComponent {
  @Input({ required: true }) item!: FoodRow;
  /** The list is on one cuisine: the details open on that cuisine country's version. */
  @Input() cuisineId: string | null = null;
  /** Highlight selected rows with a green frame. */
  @Input() greenSelection = true;
  /** Favourites draw the heart in red, the meal-type list in orange. */
  @Input() redHeart = false;
  @Output() favoriteChange = new EventEmitter<boolean>();
  @Output() selectionChange = new EventEmitter<number>();
  pull: '' | 'plus' | 'minus' = '';
  dragging = false;
  dragX = 0;
  busy = false;
  private pullTimer = 0;
  private startX = 0;
  private pointerId: number | null = null;
  private maxDrag = 40;
  private threshold = 24;

  constructor(private api: ApiService, public draft: DraftService) {}

  money = egp;

  get selected(): boolean {
    return this.item.checked || this.item.qty > 0;
  }

  get dragTransform(): string | null {
    if (!this.dragging) return null;
    return `translateX(${this.dragX}px)`;
  }

  toggleFavorite(): void {
    if (this.busy || !this.item?.id) return;
    const next = !this.item.favorite;
    this.busy = true;
    const call = next ? this.api.addFavorite(this.item.id) : this.api.removeFavorite(this.item.id);
    call.subscribe({
      next: () => {
        this.item.favorite = next;
        this.busy = false;
        this.favoriteChange.emit(next);
      },
      error: () => { this.busy = false; }
    });
  }

  toggleCheck(): void {
    if (this.selected) {
      this.applyQty(0);
      return;
    }
    this.applyQty(Math.max(1, this.item.qty || 1));
  }

  step(delta: number): void {
    this.applyQty(Math.max(0, this.item.qty + delta));
    this.bump(delta > 0 ? 'plus' : 'minus');
  }

  onPointerDown(event: PointerEvent): void {
    const target = event.currentTarget as HTMLElement;
    const panel = target.closest('.fa-stepper') as HTMLElement | null;
    if (panel) {
      const room = Math.max(24, (panel.clientWidth - target.clientWidth) / 2);
      this.maxDrag = room;
      this.threshold = room * 0.55;
    }
    this.pointerId = event.pointerId;
    this.startX = event.clientX;
    this.dragX = 0;
    this.dragging = true;
    this.pull = '';
    window.clearTimeout(this.pullTimer);
    target.setPointerCapture(event.pointerId);
  }

  onPointerMove(event: PointerEvent): void {
    if (!this.dragging || event.pointerId !== this.pointerId) return;
    const delta = event.clientX - this.startX;
    this.dragX = Math.max(-this.maxDrag, Math.min(this.maxDrag, delta));
  }

  onPointerUp(event: PointerEvent): void {
    if (!this.dragging || (this.pointerId !== null && event.pointerId !== this.pointerId)) return;
    const dx = this.dragX;
    this.dragging = false;
    this.dragX = 0;
    this.pointerId = null;
    if (dx >= this.threshold) this.step(1);
    else if (dx <= -this.threshold) this.step(-1);
  }

  private applyQty(qty: number): void {
    // Update the row immediately; DraftService debounces the server save.
    this.item.qty = qty;
    this.item.checked = qty > 0;
    this.draft.setQty({
      itemId: this.item.id,
      name: this.item.title,
      imagePath: this.item.image,
      peopleCount: null,
      countryId: this.item.recipeCountryId ?? null
    }, qty);
    this.selectionChange.emit(qty);
  }

  private bump(kind: 'plus' | 'minus'): void {
    window.clearTimeout(this.pullTimer);
    this.pull = kind;
    this.pullTimer = window.setTimeout(() => { this.pull = ''; }, 220);
  }
}
