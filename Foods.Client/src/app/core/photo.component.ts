import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { mediaUrl } from './environment';
import { IconComponent } from './icon.component';

@Component({
  selector: 'app-photo',
  imports: [CommonModule, IconComponent],
  template: `
    <img *ngIf="imagePath && !failed" [src]="url(imagePath)" [alt]="alt" [attr.loading]="eager ? 'eager' : 'lazy'" decoding="async" (error)="failed = true">
    <span class="photo-fallback" *ngIf="!imagePath || failed"><app-icon [name]="icon"></app-icon></span>
  `
})
export class PhotoComponent {
  @Input() icon = 'items';
  @Input() alt = '';
  @Input() eager = false;
  imagePath?: string | null;
  failed = false;
  url = mediaUrl;

  @Input()
  set path(value: string | null | undefined) {
    this.imagePath = value;
    this.failed = false;
  }
}
