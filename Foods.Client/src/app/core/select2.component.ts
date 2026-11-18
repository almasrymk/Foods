import { Component, forwardRef, HostListener, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { NamedRow } from './cms.service';

@Component({
  selector: 'app-select2',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="select2">
      <button type="button" class="select2-box" (click)="toggle($event)">{{ current || placeholder }}</button>
      <input class="select2-value" [required]="required" [value]="value || ''" tabindex="-1" aria-hidden="true">
      <div class="select2-panel" *ngIf="open">
        <input [(ngModel)]="query" [name]="searchName" placeholder="بحث" (click)="$event.stopPropagation()">
        <button type="button" *ngIf="allowEmpty" (click)="choose(null)">{{ emptyLabel }}</button>
        <button type="button" *ngFor="let row of filtered()" (click)="choose(row.id)">{{ row.name }}</button>
        <p class="muted" *ngIf="filtered().length === 0">لا توجد نتائج.</p>
      </div>
    </div>
  `,
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => Select2Component),
    multi: true
  }]
})
export class Select2Component implements ControlValueAccessor {
  @Input() options: NamedRow[] = [];
  @Input() placeholder = 'اختار';
  @Input() allowEmpty = false;
  @Input() emptyLabel = 'بدون';
  @Input() required = false;
  open = false;
  query = '';
  value: string | null = '';
  searchName = `find-${Math.random().toString(36).slice(2)}`;
  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string | null): void { this.value = value; }
  registerOnChange(fn: (value: string | null) => void): void { this.onChange = fn; }
  registerOnTouched(fn: () => void): void { this.onTouched = fn; }

  get current(): string {
    return this.options.find(row => row.id === this.value)?.name || '';
  }

  filtered(): NamedRow[] {
    const query = this.query.trim().toLowerCase();
    if (!query) return this.options;
    return this.options.filter(row => (row.name || '').toLowerCase().includes(query));
  }

  toggle(event: MouseEvent): void {
    event.stopPropagation();
    const next = !this.open;
    document.dispatchEvent(new CustomEvent('select2-close'));
    this.open = next;
    if (!next) this.query = '';
  }

  choose(id: string | null): void {
    this.value = id;
    this.onChange(id);
    this.onTouched();
    this.open = false;
    this.query = '';
  }

  @HostListener('document:click')
  close(): void {
    this.open = false;
  }

  @HostListener('document:select2-close')
  shut(): void {
    this.open = false;
  }
}
