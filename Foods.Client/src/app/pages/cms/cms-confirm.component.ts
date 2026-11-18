import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../../core/icon.component';

@Component({
  selector: 'app-cms-confirm',
  imports: [CommonModule, IconComponent],
  template: `
    <div class="confirm-back" *ngIf="name">
      <div class="confirm-box">
        <h2><app-icon name="delete"></app-icon> تأكيد الحذف</h2>
        <p>هل تريد حذف «{{ name }}»؟</p>
        <div class="row">
          <button type="button" (click)="confirm.emit()"><app-icon name="delete"></app-icon> حذف</button>
          <button type="button" class="ghost" (click)="cancel.emit()">إلغاء</button>
        </div>
      </div>
    </div>
  `
})
export class CmsConfirmComponent {
  @Input() name = '';
  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
}
