import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  CMS_COLOR_PALETTE,
  CmsTone,
  cmsToneLabel
} from './cms-palette';

@Component({
  selector: 'app-cms-color-picker',
  imports: [CommonModule],
  template: `
    <button type="button" class="pick-btn" (click)="open = true">
      <span class="swatch" [style.background]="color || '#fff7ef'"></span>
      <span class="meta">
        <b>{{ label }}</b>
        <small>اختيار من البالة</small>
      </span>
    </button>

    <div class="back" *ngIf="open" (click)="open = false">
      <div class="box" (click)="$event.stopPropagation()">
        <div class="head">
          <h2>اختيار اللون</h2>
          <button type="button" class="ghost" (click)="open = false">إغلاق</button>
        </div>
        <div class="grid">
          <button type="button" class="tile none" [class.on]="!color" (click)="clear()">
            <span class="chip none-chip">×</span>
            <span>بدون لون</span>
          </button>
          <button type="button" class="tile" *ngFor="let tone of tones"
                  [class.on]="color === tone.color"
                  [style.background]="tone.color"
                  [style.color]="tone.textColor"
                  (click)="pick(tone)">
            <span class="chip"></span>
            <span>{{ tone.label }}</span>
          </button>
        </div>
        <div class="preview" *ngIf="color" [style.background]="color" [style.color]="textColor || '#2E1E12'">
          معاينة اللون
        </div>
      </div>
    </div>
  `,
  styles: [`
    .pick-btn {
      display: flex; align-items: center; gap: 14px; min-width: 220px; padding: 10px 14px;
      border: 1px solid var(--line, #ead9c8); border-radius: 16px; background: #fff7ef;
      color: #5B4636; cursor: pointer; text-align: right;
    }
    .swatch {
      width: 64px; height: 64px; border-radius: 16px;
      box-shadow: inset 0 0 0 1px rgba(0,0,0,.06);
    }
    .meta { display: grid; gap: 4px; }
    .meta b { font-size: 15px; }
    .meta small { color: #8a7463; font-size: 12px; }
    .back {
      position: fixed; inset: 0; background: rgba(61, 43, 34, .35);
      display: grid; place-items: center; z-index: 40; padding: 16px;
    }
    .box {
      width: min(720px, 100%); max-height: min(80vh, 720px); overflow: auto;
      background: white; border-radius: 20px; padding: 22px;
      box-shadow: var(--shadow, 0 12px 40px rgba(0,0,0,.18));
      display: flex; flex-direction: column; gap: 16px;
    }
    .head { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
    .head h2 { margin: 0; font-size: 20px; }
    .head .ghost {
      background: white; border: 1px solid var(--line, #ead9c8);
      border-radius: 14px; padding: 10px 14px; cursor: pointer;
    }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(120px, 1fr)); gap: 12px; }
    .tile {
      display: grid; justify-items: center; gap: 10px; padding: 18px 12px;
      border-radius: 18px; border: 2px solid transparent;
      font-weight: 700; font-size: 14px; cursor: pointer;
      box-shadow: inset 0 0 0 1px rgba(0,0,0,.06);
    }
    .tile .chip {
      width: 56px; height: 56px; border-radius: 16px; background: rgba(255,255,255,.35);
      box-shadow: inset 0 0 0 1px rgba(0,0,0,.04);
    }
    .tile.on {
      border-color: #F37202;
      box-shadow: 0 0 0 2px rgba(243,114,2,.25), inset 0 0 0 1px rgba(0,0,0,.04);
    }
    .tile.none { background: #fff; color: #8a7463; }
    .none-chip {
      background: repeating-linear-gradient(45deg, #f3e7da, #f3e7da 6px, #fff 6px, #fff 12px) !important;
      color: #8a7463; display: grid; place-items: center; font-size: 28px; font-weight: 700;
    }
    .preview {
      margin-top: 4px; padding: 14px 16px; border-radius: 14px; font-weight: 700; width: fit-content;
    }
  `]
})
export class CmsColorPickerComponent {
  @Input() color = '';
  @Input() textColor = '';
  @Output() colorChange = new EventEmitter<string>();
  @Output() textColorChange = new EventEmitter<string>();
  @Output() pickTone = new EventEmitter<CmsTone>();

  readonly tones = CMS_COLOR_PALETTE;
  open = false;

  get label(): string {
    return this.color ? cmsToneLabel(this.color) : 'بدون لون';
  }

  pick(tone: CmsTone): void {
    this.colorChange.emit(tone.color);
    this.textColorChange.emit(tone.textColor);
    this.pickTone.emit(tone);
    this.open = false;
  }

  clear(): void {
    this.colorChange.emit('');
    this.textColorChange.emit('');
    this.pickTone.emit({ color: '', textColor: '', label: 'بدون لون' });
    this.open = false;
  }
}
