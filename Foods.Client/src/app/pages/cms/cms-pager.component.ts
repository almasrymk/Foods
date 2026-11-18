import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-cms-pager',
  imports: [CommonModule],
  template: `
    <div class="pager" *ngIf="total > size">
      <button type="button" class="ghost" (click)="pageChange.emit(page - 1)" [disabled]="page === 0">السابق</button>
      <span>{{ from }}–{{ to }} من {{ total }}</span>
      <button type="button" class="ghost" (click)="pageChange.emit(page + 1)" [disabled]="to >= total">التالي</button>
    </div>
  `
})
export class CmsPagerComponent {
  @Input() total = 0;
  @Input() page = 0;
  @Input() size = 10;
  @Output() pageChange = new EventEmitter<number>();

  get from(): number { return this.total === 0 ? 0 : this.page * this.size + 1; }
  get to(): number { return Math.min(this.total, (this.page + 1) * this.size); }
}
