import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Location } from '@angular/common';
import { FaIconComponent } from './icon.component';
import { FaTopBarComponent } from './top-bar.component';

/**
 * Header used by the list-style mockup screens: the shared top bar (avatar, logo,
 * notifications), then the title between green dashes and a back button. Positions are mockup px (941 wide).
 */
@Component({
  selector: 'fa-screen-header',
  imports: [FaIconComponent, FaTopBarComponent],
  template: `
    <fa-top-bar />
    @if (art) {
      <!-- Photo header, same layout as the meals plan screen ("اختر نوع الوجبة"). -->
      <div class="ph">
        <img class="ph-art" [src]="art" alt="">
        <button type="button" class="hd-back ph-back" (click)="location.back()" aria-label="رجوع">
          <fa-icon name="chevronLeft" [weight]="2.6" />
        </button>
        <div class="ph-title">
          <div class="ph-row">
            <fa-icon class="ph-icon" [name]="titleIcon || (heart ? 'heart' : 'cloche')" [weight]="2.2" />
            <h1>{{ title }}</h1>
          </div>
          <img class="ph-underline" src="assets/screens/plan/underline.png" alt="">
        </div>
        @if (subtitle) { <p class="ph-sub">{{ subtitle }}</p> }
      </div>
    } @else {
    <header class="head" [style.height]="px(height)">
      @if (share) {
        <button type="button" class="hd-share" [style.top]="px(backY)" (click)="shareClick.emit()" aria-label="مشاركة">
          <fa-icon name="share" [weight]="2.4" />
        </button>
      }
      <button type="button" class="hd-back" [style.top]="px(backY)" (click)="location.back()" aria-label="رجوع">
        <fa-icon name="chevronLeft" [weight]="2.6" />
      </button>
      <div class="title" [style.top]="px(titleY)" [style.font-size]="px(titleSize)">
        <img class="dash flip" src="assets/screens/shared/dash.png" alt="">
        @if (heart || titleIcon) {
          <fa-icon class="title-icon" [name]="titleIcon || 'heart'" [weight]="2.2" />
        }
        <h1>{{ title }}</h1>
        <img class="dash" src="assets/screens/shared/dash.png" alt="">
      </div>
      @if (subtitle) { <p class="sub" [style.top]="px(subY)">{{ subtitle }}</p> }
    </header>
    }
  `,
  styles: [`
    :host { display: block; }
    /* The shared top bar (avatar, logo, bell) replaces the mockup's logo area (top ~100px). */
    .head { position: relative; margin-top: calc(-100 * var(--s)); }
    /* ---- photo header (plan screen positions: art 0,110 356x292; title centre y 220; subtitle y 302) ---- */
    .ph { position: relative; height: calc(262 * var(--s)); }
    .ph-art {
      position: absolute; left: 0; top: calc(-33 * var(--s)); z-index: -1;
      width: calc(356 * var(--s)); height: calc(292 * var(--s)); object-fit: cover; pointer-events: none;
      -webkit-mask-image: linear-gradient(90deg, #000 78%, transparent), linear-gradient(180deg, transparent, #000 8%, #000 85%, transparent);
      -webkit-mask-composite: source-in;
      mask-image: linear-gradient(90deg, #000 78%, transparent), linear-gradient(180deg, transparent, #000 8%, #000 85%, transparent);
      mask-composite: intersect;
    }
    /* vector illustrations are shown whole, not cropped */
    .ph-art[src$='.svg'] { object-fit: contain; }
    .ph-back { left: calc(836 * var(--s)); top: calc(42 * var(--s)); }
    .ph-title {
      position: absolute; right: calc(126 * var(--s)); top: calc(77 * var(--s)); transform: translateY(-50%);
      display: flex; flex-direction: column; align-items: center;
    }
    .ph-row { display: flex; align-items: center; gap: calc(16 * var(--s)); }
    .ph-row h1 { margin: 0; font-size: calc(54 * var(--s)); font-weight: 800; color: #2E1E12; line-height: 1.15; white-space: nowrap; }
    .ph-icon { color: #F37202; font-size: calc(62 * var(--s)); }
    .ph-underline { width: calc(68 * var(--s)); height: calc(22 * var(--s)); margin-top: calc(-6 * var(--s)); }
    .ph-sub {
      position: absolute; right: calc(51 * var(--s)); top: calc(159 * var(--s)); transform: translateY(-50%); margin: 0;
      font-size: calc(24 * var(--s)); font-weight: 500; color: #3F3129; white-space: nowrap;
    }
    .hd-back, .hd-share {
      position: absolute; width: calc(72 * var(--s)); height: calc(70 * var(--s));
      display: grid; place-items: center; padding: 0; border: 0; cursor: pointer;
      border-radius: calc(20 * var(--s)); background: #FFFCF8 !important; color: #2E1E12;
      box-shadow: 0 calc(4 * var(--s)) calc(14 * var(--s)) rgba(120, 72, 30, .13);
      font-size: calc(34 * var(--s));
    }
    .hd-back { left: calc(818 * var(--s)); }
    .hd-share { left: calc(28 * var(--s)); }
    .title {
      position: absolute; left: 0; right: 0; transform: translateY(-50%);
      display: flex; justify-content: center; align-items: center; gap: calc(22 * var(--s));
    }
    .title h1 { margin: 0; font-size: 1em; font-weight: 800; color: #3A2412; line-height: 1.2; white-space: nowrap; display: block; }
    .title-icon { color: #F37202; font-size: calc(54 * var(--s)); margin-left: calc(-4 * var(--s)); }
    .dash { width: calc(62 * var(--s)); height: calc(22 * var(--s)); }
    .dash.flip { transform: scaleX(-1); }
    .sub {
      position: absolute; left: 0; right: 0; margin: 0; transform: translateY(-50%);
      text-align: center; font-size: calc(26 * var(--s)); font-weight: 500; color: #2E1E12;
    }
  `]
})
export class FaScreenHeaderComponent {
  @Input({ required: true }) title = '';
  @Input() subtitle = '';
  @Input() heart = false;
  /** Optional title icon (e.g. basket). Falls back to heart when heart=true. */
  @Input() titleIcon = '';
  @Input() share = false;
  /** Optional photo shown on the far left of the header (like the home page). */
  @Input() art = '';
  @Output() shareClick = new EventEmitter<void>();
  /** Header block height and element positions, in mockup px. */
  @Input() height = 260;
  @Input() titleY = 180;
  @Input() titleSize = 50;
  @Input() backY = 155;
  @Input() subY = 233;

  constructor(public location: Location) {}

  px(v: number): string {
    return `calc(${v} * var(--s))`;
  }
}
