import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { NamedRow } from '../core/cms.service';
import { FamilyMember } from '../core/models';
import { Select2Component } from '../core/select2.component';
import { ageParts, ageText, familyAvatar, pictureUrl } from '../core/family-avatar';
import { FaFamilySheetComponent } from './screens/shared/family-sheet.component';

/** Account country + family members. Members are managed in the family popup (opens on arrival). */
@Component({
  selector: 'app-family',
  imports: [CommonModule, FormsModule, Select2Component, FaFamilySheetComponent],
  template: `
    <h1>الأسرة</h1>
    <label>بلدك
      <app-select2 name="country" [(ngModel)]="countryId" (ngModelChange)="saveCountry()" [options]="countryOptions" placeholder="اختار البلد" [required]="true"></app-select2>
    </label>
    <p class="muted" *ngIf="dialect">الأكلات هتظهر بلهجة {{ dialect }}.</p>
    <p>الكميات بتتحسب حسب سن كل فرد (من تاريخ ميلاده).</p>
    <ul class="fam-list">
      <li *ngFor="let m of members">
        <img [src]="picture(m)" [alt]="m.name || ''">
        <b>{{ m.name }}</b>
        <small>{{ m.relation }}<ng-container *ngIf="m.relation && age(m)"> · </ng-container>{{ age(m) }}</small>
      </li>
    </ul>
    <button type="button" (click)="sheetOpen = true">إدارة أفراد الأسرة</button>
    <p class="error" *ngIf="error">{{ error }}</p>
    <p *ngIf="saved">تم الحفظ</p>
    <fa-family-sheet [open]="sheetOpen" (closed)="sheetOpen = false" (changed)="members = $event" />
  `,
  styles: [`
    .fam-list { list-style: none; padding: 0; margin: 12px 0; display: flex; flex-wrap: wrap; gap: 12px; }
    .fam-list li { display: flex; flex-direction: column; align-items: center; gap: 4px; width: 96px; text-align: center; }
    .fam-list img { width: 64px; height: 64px; border-radius: 50%; object-fit: cover; background: #fff; box-shadow: 0 2px 6px rgba(120, 72, 30, .15); }
    .fam-list b { font-size: 15px; }
    .fam-list small { font-size: 12px; color: #7A6A5E; }
  `]
})
export class FamilyComponent implements OnInit {
  members: FamilyMember[] = [];
  countries: { id: string; name?: string | null; dialect?: string | null }[] = [];
  countryId = '';
  private loadedCountry = '';
  error = '';
  saved = false;
  sheetOpen = false;

  constructor(private api: ApiService) {}

  get countryOptions(): NamedRow[] {
    return this.countries.map(country => ({
      id: country.id,
      name: country.dialect ? `${country.name} · ${country.dialect}` : country.name
    }));
  }

  get dialect(): string {
    return this.countries.find(country => country.id === this.countryId)?.dialect || '';
  }

  picture(m: FamilyMember): string {
    return m.imagePath ? pictureUrl(m.imagePath) : (m.avatar || familyAvatar(m.relation, m.age));
  }

  age(m: FamilyMember): string {
    const p = ageParts(m.birthDate);
    return p ? ageText(p.years, p.months) : ageText(m.age);
  }

  ngOnInit(): void {
    this.api.countries().subscribe({ next: rows => this.countries = rows, error: (err: Error) => this.error = err.message });
    this.api.profile().subscribe({
      next: profile => { this.countryId = this.loadedCountry = profile.countryId || ''; },
      error: (err: Error) => this.error = err.message
    });
    this.api.family().subscribe({ next: rows => this.members = rows, error: (err: Error) => this.error = err.message });
    this.sheetOpen = true;
  }

  saveCountry(): void {
    this.saved = false;
    if (!this.countryId || this.countryId === this.loadedCountry) return;
    this.api.saveCountry(this.countryId).subscribe({
      next: () => { this.loadedCountry = this.countryId; this.saved = true; },
      error: (err: Error) => this.error = err.message
    });
  }
}
