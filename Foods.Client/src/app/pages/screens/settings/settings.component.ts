import { setCurrency } from '../../../core/money';
import { Component, HostListener, OnInit } from '@angular/core';
import { Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/api.service';
import { AuthService } from '../../../core/auth.service';
import { ProfilePhotoService } from '../../../core/profile-photo.service';
import { mediaUrl } from '../../../core/environment';
import { familyAvatar, pictureUrl } from '../../../core/family-avatar';
import { FamilyMember, UserSettings , PriceCountry } from '../../../core/models';
import { FaTabBarComponent } from '../shared/tab-bar.component';
import { FaTopBarComponent } from '../shared/top-bar.component';
import { FaIconComponent } from '../shared/icon.component';
import { FaFamilySheetComponent, squareCrop } from '../shared/family-sheet.component';
import { ABOUT_ROWS, COUNTRY_FLAGS, FamilyCard, NOTIFY_ROWS, NotifyKey } from './settings.data';

interface CountryOption { id: string; name?: string | null; dialect?: string | null; }

/** الإعدادات: profile, app settings, family, notifications and about (design/mockups/settings.png). */
@Component({
  selector: 'app-settings-screen',
  imports: [RouterLink, FormsModule, FaTopBarComponent, FaTabBarComponent, FaIconComponent, FaFamilySheetComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsScreenComponent implements OnInit {
  readonly about = ABOUT_ROWS;
  readonly notifyRows = NOTIFY_ROWS;

  name = '';
  userName = '';
  photoPath = '';
  countryId = '';
  countryName = '';
  countries: CountryOption[] = [];
  countryOpen = false;
  family: FamilyCard[] = [];
  private members: FamilyMember[] = [];
  familyOpen = false;
  familyAdd = false;
  settings: UserSettings = { language: 'ar', notifyLogin: true, notifyWelcome: true, notifyBirthday: true, notifyOrders: true, notifyPlanning: true };

  accountOpen = false;
  nameDraft = '';
  /** photo being edited in the account sheet ('' = none) */
  photoDraft = '';
  uploading = false;
  toast = '';
  error = '';
  busy = false;
  private toastTimer = 0;

  constructor(
    public location: Location,
    private api: ApiService,
    private auth: AuthService,
    private photos: ProfilePhotoService
  ) {}

  get flag(): string {
    return COUNTRY_FLAGS[this.countryName] || '';
  }

  get photoUrl(): string {
    return this.photoPath ? mediaUrl(this.photoPath) : '';
  }

  get photoDraftUrl(): string {
    return this.photoDraft ? mediaUrl(this.photoDraft) : '';
  }

  ngOnInit(): void {
    this.name = this.auth.name();
    this.api.profile().subscribe({
      next: p => {
        this.name = p.name || this.name;
        this.userName = p.userName || '';
        this.countryId = p.countryId || '';
        this.countryName = p.countryName || '';
        this.photoPath = p.imagePath || '';
        this.photos.set(this.photoPath);
      },
      error: (e: Error) => this.fail(e)
    });
    this.api.countries().subscribe({ next: rows => this.countries = rows, error: () => {} });
    this.loadFamily();
    this.api.settings().subscribe({ next: s => this.settings = s, error: (e: Error) => this.fail(e) });
    this.api.priceCountry().subscribe({ next: c => { this.priceCountry = c; setCurrency(c.currency); }, error: () => {} });
  }

  // ------------------------------------------------------- price country
  priceCountry: PriceCountry | null = null;

  get priceCountryValue(): string {
    return this.priceCountry?.countryId || '';
  }

  /** "البلد اللي أنت فيها" list open (same inline dropdown as البلد). */
  liveOpen = false;

  get liveName(): string {
    return this.priceCountry?.countryName || this.countryName;
  }

  get liveFlag(): string {
    return COUNTRY_FLAGS[this.liveName] || '';
  }

  toggleLive(event: Event): void {
    event.stopPropagation();
    if (!this.priceCountry) return;
    this.liveOpen = !this.liveOpen;
    this.countryOpen = false;
  }

  pickLive(id: string): void {
    this.liveOpen = false;
    if (id === this.priceCountry?.countryId) return;
    this.pickPriceCountry(id);
  }

  /** البلد اللي أنت فيها: prices and currency follow it (own country = the default). */
  pickPriceCountry(id: string): void {
    const before = this.priceCountry;
    this.api.setPriceCountry(id || null).subscribe({
      next: c => { this.priceCountry = c; setCurrency(c.currency); },
      error: (e: Error) => { this.priceCountry = before ? { ...before } : null; this.fail(e); }
    });
  }

  // ------------------------------------------------------------- account
  openAccount(): void {
    this.nameDraft = this.name;
    this.photoDraft = this.photoPath;
    this.error = '';
    this.accountOpen = true;
  }

  pickPhoto(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    if (!file.type.startsWith('image/')) { this.error = 'اختار صورة'; return; }
    if (!this.accountOpen) this.openAccount();
    this.uploading = true;
    this.error = '';
    squareCrop(file, 512).then(cropped => this.api.uploadImage(cropped).subscribe({
      next: path => { this.photoDraft = path; this.uploading = false; },
      error: (e: Error) => { this.uploading = false; this.error = e.message; }
    }));
  }

  saveAccount(): void {
    const name = this.nameDraft.trim();
    if (name.length < 2) { this.error = 'اكتب اسمك (حرفين على الأقل)'; return; }
    this.busy = true;
    const photoChanged = this.photoDraft !== this.photoPath;
    const savePhoto = () => this.api.saveProfilePhoto(this.photoDraft || null).subscribe({
      next: p => {
        this.photoPath = p.imagePath || '';
        this.photos.set(this.photoPath);
        this.finishAccount();
      },
      error: (e: Error) => { this.busy = false; this.error = e.message; }
    });
    this.api.saveProfile(name).subscribe({
      next: p => {
        this.name = p.name || name;
        localStorage.setItem('foods.name', this.name);
        if (photoChanged) savePhoto(); else this.finishAccount();
      },
      error: (e: Error) => { this.busy = false; this.error = e.message; }
    });
  }

  private finishAccount(): void {
    this.busy = false;
    this.accountOpen = false;
    this.flash('تم حفظ بيانات الحساب');
  }

  // ------------------------------------------------------------- country
  toggleCountry(event: Event): void {
    event.stopPropagation();
    this.countryOpen = !this.countryOpen;
    this.liveOpen = false;
  }

  pickCountry(c: CountryOption): void {
    this.countryOpen = false;
    if (c.id === this.countryId) return;
    this.api.saveCountry(c.id).subscribe({
      next: p => {
        this.countryId = p.countryId || c.id;
        this.countryName = p.countryName || c.name || '';
        this.flash(`تم تغيير البلد إلى ${this.countryName}`);
      },
      error: (e: Error) => this.fail(e)
    });
  }

  @HostListener('document:click')
  closeMenus(): void {
    this.countryOpen = false;
    this.liveOpen = false;
  }

  // -------------------------------------------------------------- family
  openFamily(add = false): void {
    this.familyAdd = add;
    this.familyOpen = true;
  }

  onFamilyChanged(members: FamilyMember[]): void {
    this.setFamily(members);
  }

  private loadFamily(): void {
    this.api.family().subscribe({ next: members => this.setFamily(members), error: (e: Error) => this.fail(e) });
  }

  private setFamily(members: FamilyMember[]): void {
    this.members = members;
    this.family = members.map(m => ({
      id: m.id,
      name: m.name || '',
      age: m.age,
      birthDate: m.birthDate,
      image: m.imagePath ? pictureUrl(m.imagePath) : (m.avatar || familyAvatar(m.relation, m.age))
    }));
  }

  remove(member: FamilyCard): void {
    if (this.members.length <= 1) { this.flash('لازم يفضل فرد واحد على الأقل في الأسرة'); return; }
    if (!confirm(`حذف ${member.name} من أفراد الأسرة؟`)) return;
    const rest = this.members.filter(m => m.id !== member.id);
    this.api.saveFamily(rest.map(m => ({
      name: m.name || '', age: m.age, birthDate: m.birthDate ?? null, relation: m.relation ?? null, imagePath: m.imagePath ?? null
    }))).subscribe({
      next: saved => { this.setFamily(saved); this.flash(`تم حذف ${member.name}`); },
      error: (e: Error) => this.fail(e)
    });
  }

  // ------------------------------------------------------- notifications
  togglePrices(): void {
    const before = !!this.settings.usePrices;
    this.settings = { ...this.settings, usePrices: !before };
    this.api.setPricesEnabled(!before).subscribe({
      error: (e: Error) => { this.settings = { ...this.settings, usePrices: before }; this.fail(e); }
    });
  }

  toggle(key: NotifyKey): void {
    const before = { ...this.settings };
    this.settings = { ...this.settings, [key]: !this.settings[key] };
    this.api.saveSettings(this.settings).subscribe({
      next: s => this.settings = s,
      error: (e: Error) => { this.settings = before; this.fail(e); }
    });
  }

  logout(): void {
    this.auth.logout();
  }

  private fail(e: Error): void {
    this.flash(e.message || 'حدث خطأ');
  }

  private flash(text: string): void {
    this.toast = text;
    window.clearTimeout(this.toastTimer);
    this.toastTimer = window.setTimeout(() => this.toast = '', 2600);
  }
}
