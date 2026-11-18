import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FaIconComponent } from '../screens/shared/icon.component';
import { PhotoComponent } from '../../core/photo.component';
import {
  CMS_ICON_PALETTE,
  cmsIconLabel,
  isCmsImagePath
} from './cms-palette';

@Component({
  selector: 'app-cms-icon-picker',
  imports: [CommonModule, FaIconComponent, PhotoComponent],
  template: `
    <div class="pick-row">
      <button type="button" class="pick-btn" (click)="open = true">
        <app-photo *ngIf="isImage" [path]="value" [icon]="fallbackIcon" alt=""></app-photo>
        <span *ngIf="!isImage" class="preview">
          <fa-icon *ngIf="value" [name]="value" [weight]="1.7"></fa-icon>
          <span *ngIf="!value">—</span>
        </span>
        <span class="meta">
          <b>{{ label }}</b>
          <small>اختيار من البالة</small>
        </span>
      </button>
      <label *ngIf="allowUpload" class="file-btn">رفع صورة
        <input type="file" accept="image/*" (change)="onFile($event)">
      </label>
    </div>

    <div class="back" *ngIf="open" (click)="open = false">
      <div class="box" (click)="$event.stopPropagation()">
        <div class="head">
          <h2>اختيار الأيقونة</h2>
          <button type="button" class="ghost" (click)="open = false">إغلاق</button>
        </div>
        <div class="grid">
          <button type="button" class="tile none" [class.on]="!value" (click)="pick('')">
            <span class="none-mark">×</span>
            <span>بدون أيقونة</span>
          </button>
          <button type="button" class="tile" *ngFor="let icon of icons"
                  [class.on]="!isImage && value === icon.key"
                  (click)="pick(icon.key)">
            <fa-icon [name]="icon.key" [weight]="1.6"></fa-icon>
            <span>{{ icon.label }}</span>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .pick-row { display: flex; flex-wrap: wrap; gap: 12px; align-items: center; }
    .pick-btn {
      display: flex; align-items: center; gap: 14px; min-width: 220px; padding: 10px 14px;
      border: 1px solid var(--line, #ead9c8); border-radius: 16px; background: #fff7ef;
      color: #5B4636; cursor: pointer; text-align: right;
    }
    .preview {
      width: 64px; height: 64px; border-radius: 16px; background: #fff;
      display: grid; place-items: center; box-shadow: inset 0 0 0 1px rgba(0,0,0,.06);
      color: #F37202; font-size: 36px;
    }
    .preview fa-icon { width: 36px; height: 36px; font-size: 36px; }
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
      border-radius: 18px; border: 2px solid transparent; background: #fff7ef;
      color: #5B4636; font-weight: 700; font-size: 14px; cursor: pointer;
    }
    .tile fa-icon { width: 56px; height: 56px; font-size: 56px; color: #F37202; }
    .tile.on {
      border-color: #F37202; background: #fff;
      box-shadow: 0 0 0 2px rgba(243,114,2,.25);
    }
    .tile.none { background: #fff; color: #8a7463; }
    .none-mark {
      width: 56px; height: 56px; border-radius: 16px;
      display: grid; place-items: center; font-size: 32px; font-weight: 700; color: #8a7463;
      background: repeating-linear-gradient(45deg, #f3e7da, #f3e7da 6px, #fff 6px, #fff 12px);
      box-shadow: inset 0 0 0 1px rgba(0,0,0,.06);
    }
  `]
})
export class CmsIconPickerComponent {
  @Input() value = '';
  @Input() allowUpload = true;
  @Input() fallbackIcon = 'category';
  @Output() valueChange = new EventEmitter<string>();
  @Output() upload = new EventEmitter<File>();

  readonly icons = CMS_ICON_PALETTE;
  open = false;

  get isImage(): boolean {
    return isCmsImagePath(this.value);
  }

  get label(): string {
    if (this.isImage) return 'صورة مرفوعة';
    return this.value ? cmsIconLabel(this.value) : 'بدون أيقونة';
  }

  pick(key: string): void {
    this.valueChange.emit(key);
    this.open = false;
  }

  onFile(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.upload.emit(file);
  }
}
