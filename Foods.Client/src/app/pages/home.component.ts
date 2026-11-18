import { Component, ElementRef, NgZone, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiService } from '../core/api.service';
import { DraftService } from '../core/draft.service';
import { mediaUrl } from '../core/environment';
import { MealCard, ItemCard } from '../core/models';
import { HubNavComponent } from '../core/hub-nav.component';

interface HubItem extends ItemCard {
  mealName: string;
}

interface Kitchen {
  id: string;
  name: string;
  tone: string;
  art: string;
}

interface MealSlot {
  label: string;
  tone: string;
  art: string;
  keys: string[];
}

interface MealLink extends MealSlot {
  link: string[];
}

interface SpeechResultEvent {
  results: ArrayLike<ArrayLike<{ transcript: string }>>;
}

interface SpeechSession {
  lang: string;
  start: () => void;
  onresult: ((event: SpeechResultEvent) => void) | null;
}

@Component({
  selector: 'app-home',
  imports: [CommonModule, FormsModule, RouterLink, HubNavComponent],
  template: `
    <section class="hub">
      <header class="hub-top">
        <a class="hub-bell" routerLink="/orders" aria-label="التنبيهات">
          <svg viewBox="0 0 24 24"><path d="M6.5 9.2a5.5 5.5 0 0 1 11 0c0 4.2 1.4 5.6 1.4 5.6H5.1s1.4-1.4 1.4-5.6z"></path><path d="M10 18.2a2 2 0 0 0 4 0"></path></svg>
          <b *ngIf="draft.items.length">{{ draft.items.length }}</b>
        </a>
        <img class="hub-logo" src="logo.png?v=2" alt="Foodapp" width="72" height="72">
        <a class="hub-avatar" routerLink="/family" aria-label="الحساب">
          <svg viewBox="0 0 24 24"><circle cx="12" cy="8.2" r="3.1"></circle><path d="M6 19.2c1.1-3 3.2-4.5 6-4.5s4.9 1.5 6 4.5"></path></svg>
          <i class="dot"></i>
        </a>
      </header>

      <div class="hub-hello">
        <div>
          <h1>مرحباً بك <span aria-hidden="true">👋</span></h1>
          <p>استمتع بوجبة أسهل وأصح</p>
        </div>
        <img class="hub-bowl" src="assets/home/bowl-hero.jpg" alt="طبق دجاج مشوي بالخضار" width="640" height="480" fetchpriority="high" decoding="async">
      </div>

      <form class="hub-search" (ngSubmit)="scrollToFood()">
        <button type="submit" aria-label="بحث">
          <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="6"></circle><path d="M16 16l4 4"></path></svg>
        </button>
        <input #query [(ngModel)]="text" name="text" placeholder="ابحث عن أكلة، وصفة، مكون..." autocomplete="off">
        <button type="button" class="mic" [class.on]="listening" aria-label="بحث صوتي" (click)="listen()">
          <svg viewBox="0 0 24 24"><rect x="9" y="3" width="6" height="11" rx="3"></rect><path d="M6 11a6 6 0 0 0 12 0"></path><path d="M12 17v3"></path></svg>
        </button>
      </form>

      <p class="error" *ngIf="error">{{ error }}</p>

      <div class="hub-meals">
        <a class="hub-meal" *ngFor="let meal of featured" [class]="meal.tone" [routerLink]="meal.link">
          <span class="art">
            <img [src]="meal.art" [alt]="meal.label" width="240" height="160" decoding="async">
          </span>
          <svg *ngIf="meal.tone === 'sun'" viewBox="0 0 24 24"><circle cx="12" cy="12" r="3.2"></circle><path d="M12 3v2M12 19v2M3 12h2M19 12h2M5.6 5.6l1.4 1.4M17 17l1.4 1.4M18.4 5.6 17 7M7 17l-1.4 1.4"></path></svg>
          <svg *ngIf="meal.tone === 'lunch'" viewBox="0 0 24 24"><path d="M4 15h16"></path><path d="M6 15a6 6 0 0 1 12 0"></path><path d="M12 5v3"></path><path d="M9 20h6"></path></svg>
          <svg *ngIf="meal.tone === 'moon'" viewBox="0 0 24 24"><path d="M15 4a8 8 0 1 0 5 13 7 7 0 1 1-5-13z"></path></svg>
          <svg *ngIf="meal.tone === 'leaf'" viewBox="0 0 24 24"><path d="M4 12l8-8 8 6v6a4 4 0 0 1-4 4H8a4 4 0 0 1-4-4z"></path><path d="M10 20v-4"></path></svg>
          <b>{{ meal.label }}</b>
        </a>
      </div>

      <a class="hub-plan" routerLink="/meals">
        <div class="copy">
          <h2>خطط لأكل الشهر</h2>
          <p>اختر أكلاتك، وحدد عدد التكرار، ونحن نحسب لك المكونات تلقائياً</p>
          <span class="cta">
            <svg viewBox="0 0 24 24" aria-hidden="true"><rect x="4" y="5" width="16" height="15" rx="2"></rect><path d="M8 3v4M16 3v4M4 10h16"></path></svg>
            ابدأ التخطيط الآن
            <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M14 6l-6 6 6 6"></path></svg>
          </span>
        </div>
        <span class="scene" aria-hidden="true">
          <img src="assets/home/banner-cut.png" alt="" width="210" height="164" decoding="async">
        </span>
      </a>

      <div class="hub-head">
        <h2>تصفح حسب المطبخ</h2>
        <button type="button" class="more" (click)="showAll()">عرض الكل <span aria-hidden="true">‹</span></button>
      </div>
      <div class="hub-kitchens">
        <button type="button" *ngFor="let kitchen of kitchens" [class]="kitchen.tone" [class.on]="kitchenId === kitchen.id" (click)="pickKitchen(kitchen.id)" [attr.aria-label]="kitchen.name">
          <img [src]="kitchen.art" alt="">
        </button>
      </div>

      <div class="hub-head" id="suggested">
        <h2><i class="badge" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M8 12.5l2.5 2.5L16 9"></path></svg></i> أكلات مقترحة لك</h2>
        <a routerLink="/meals">عرض الكل <span aria-hidden="true">‹</span></a>
      </div>
      <div class="hub-foods">
        <article class="hub-food" *ngFor="let item of shown">
          <a class="pic" [routerLink]="['/items', item.id]">
            <img *ngIf="srcOf(item) as src; else empty" [src]="src" [alt]="item.name || 'أكلة'" width="320" height="220" loading="lazy" decoding="async" (error)="miss(item)">
            <ng-template #empty><span class="photo-fallback">{{ item.name }}</span></ng-template>
          </a>
          <button type="button" class="heart" [class.on]="item.isFavorite" (click)="toggleFavorite(item)" [attr.aria-label]="'مفضلة ' + item.name">
            <svg viewBox="0 0 24 24"><path d="M12 20s-7-4.4-7-9a4 4 0 0 1 7-2 4 4 0 0 1 7 2c0 4.6-7 9-7 9z"></path></svg>
          </button>
          <a class="name" [routerLink]="['/items', item.id]">{{ titleOf(item) }}</a>
          <div class="meta">
            <span [title]="'وقت تقديري'"><svg viewBox="0 0 24 24"><circle cx="12" cy="13" r="7"></circle><path d="M12 10v4l2 2"></path><path d="M9 3h6"></path></svg> {{ minutes(item) }}</span>
            <span><svg viewBox="0 0 24 24"><circle cx="12" cy="8" r="3"></circle><path d="M6 19c1.2-2.6 3.2-4 6-4s4.8 1.4 6 4"></path></svg> {{ people }}</span>
          </div>
          <div class="qty" *ngIf="count(item.id); else add">
            <button type="button" (click)="step(item, -1)" aria-label="نقص">−</button>
            <b>{{ count(item.id) }}</b>
            <button type="button" (click)="step(item, 1)" aria-label="زيادة">+</button>
          </div>
          <ng-template #add>
            <button type="button" class="plus" (click)="step(item, 1)" [attr.aria-label]="'إضافة ' + item.name">+</button>
          </ng-template>
        </article>
      </div>
      <p class="muted" *ngIf="!loading && suggestions.length && shown.length === 0">مفيش أكلات مطابقة للبحث.</p>
      <p class="muted" *ngIf="!loading && !suggestions.length">لسه مفيش أكلات متاحة في الوجبات.</p>

      <app-hub-nav></app-hub-nav>
    </section>
  `
})
export class HomeComponent implements OnInit {
  @ViewChild('query') query?: ElementRef<HTMLInputElement>;

  meals: MealCard[] = [];
  suggestions: HubItem[] = [];
  kitchens: Kitchen[] = [
    { id: 'east', name: 'شرقي', tone: 'east', art: 'assets/home/cuisine-east.png' },
    { id: 'west', name: 'غربي', tone: 'west', art: 'assets/home/cuisine-west.png' },
    { id: 'healthy', name: 'صحي', tone: 'healthy', art: 'assets/home/cuisine-healthy.png' },
    { id: 'vegan', name: 'نباتي', tone: 'vegan', art: 'assets/home/cuisine-vegan.png' },
    { id: 'sweet', name: 'حلويات', tone: 'sweet', art: 'assets/home/cuisine-sweet.png' }
  ];
  text = '';
  kitchenId = '';
  error = '';
  loading = true;
  listening = false;
  people = 4;
  broken: Record<string, boolean> = {};
  private readonly slots: MealSlot[] = [
    { label: 'الفطار', tone: 'sun', art: 'assets/home/cat-breakfast.png', keys: ['فطار', 'فطور'] },
    { label: 'الغداء', tone: 'lunch', art: 'assets/home/cat-lunch.png', keys: ['غداء'] },
    { label: 'العشاء', tone: 'moon', art: 'assets/home/cat-dinner.png', keys: ['عشاء'] },
    { label: 'وجبات خفيفة', tone: 'leaf', art: 'assets/home/cat-snack.png', keys: ['خفيف', 'سناك'] }
  ];

  constructor(private api: ApiService, public draft: DraftService, private zone: NgZone) {}

  ngOnInit(): void {
    this.api.family().subscribe({
      next: members => { if (members.length) this.people = members.length; }
    });
    this.api.meals().subscribe({
      next: meals => {
        this.meals = [...meals].sort((a, b) => this.rank(a.name) - this.rank(b.name));
        if (!this.meals.length) {
          this.loading = false;
          return;
        }
        forkJoin(this.meals.map(meal => this.api.items(meal.id, '').pipe(
          map(items => items.map(item => ({ ...item, mealName: meal.name || '' }))),
          catchError(() => of([] as HubItem[]))
        ))).subscribe({
          next: groups => {
            const merged: HubItem[] = [];
            const seen = new Set<string>();
            for (const group of groups) {
              for (const item of group) {
                if (seen.has(item.id)) continue;
                seen.add(item.id);
                merged.push(item);
              }
            }
            this.suggestions = this.pin(merged);
            this.stage(this.suggestions);
            this.loading = false;
          },
          error: () => this.loading = false
        });
      },
      error: (err: Error) => { this.error = err.message; this.loading = false; }
    });
  }

  get featured(): MealLink[] {
    return this.slots.map(slot => {
      const meal = this.meals.find(item => slot.keys.some(key => (item.name || '').includes(key)));
      return { ...slot, link: meal ? ['/meals', meal.id] : ['/meals'] };
    });
  }

  get shown(): HubItem[] {
    const query = this.text.trim();
    return this.suggestions.filter(item => {
      const blob = `${item.name || ''} ${item.summary || ''} ${item.mealName}`;
      if (query && !blob.includes(query)) return false;
      return !this.kitchenId || this.matches(item, this.kitchenId);
    }).slice(0, 12);
  }

  pickKitchen(id: string): void {
    this.kitchenId = this.kitchenId === id ? '' : id;
    this.scrollToFood();
  }

  showAll(): void {
    this.kitchenId = '';
    this.text = '';
    this.scrollToFood();
  }

  scrollToFood(): void {
    document.getElementById('suggested')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  listen(): void {
    const host = window as Window & {
      SpeechRecognition?: new () => SpeechSession;
      webkitSpeechRecognition?: new () => SpeechSession;
    };
    const Rec = host.SpeechRecognition || host.webkitSpeechRecognition;
    if (!Rec) {
      this.query?.nativeElement.focus();
      return;
    }
    const rec = new Rec();
    rec.lang = 'ar-EG';
    this.listening = true;
    rec.onresult = event => {
      this.zone.run(() => {
        this.listening = false;
        this.text = event.results[0][0].transcript;
        this.scrollToFood();
      });
    };
    try {
      rec.start();
    } catch {
      this.listening = false;
      this.query?.nativeElement.focus();
    }
  }

  count(id: string): number {
    return this.draft.items.find(entry => entry.itemId === id)?.repeatCount || 0;
  }

  step(item: HubItem, delta: number): void {
    const next = Math.max(0, this.count(item.id) + delta);
    if (next === 0) {
      this.draft.remove(item.id);
      return;
    }
    this.draft.add({
      itemId: item.id,
      name: item.name || '',
      imagePath: item.imagePath,
      repeatCount: next,
      peopleCount: this.people
    });
  }

  toggleFavorite(item: HubItem): void {
    const call = item.isFavorite ? this.api.removeFavorite(item.id) : this.api.addFavorite(item.id);
    call.subscribe({
      next: () => item.isFavorite = !item.isFavorite,
      error: (err: Error) => this.error = err.message
    });
  }

  srcOf(item: HubItem): string {
    const poster = this.poster(item.name);
    if (poster && !this.broken[item.id]) return poster;
    if (!this.broken[item.id] && item.imagePath) return mediaUrl(item.imagePath);
    return this.localArt(item);
  }

  miss(item: HubItem): void {
    if (this.broken[item.id]) return;
    this.broken = { ...this.broken, [item.id]: true };
  }

  titleOf(item: HubItem): string {
    const name = item.name || '';
    if (name === 'كشري') return 'كشري مصري';
    if (name === 'محشي صغير' || name.includes('ورق عنب')) return 'محشي ورق عنب';
    if (name === 'فراخ مشوية' || name === 'دجاج مشوي') return 'دجاج مشوي بالأعشاب';
    return name;
  }

  private stage(items: HubItem[]): void {
    const dolma = items.find(entry => (entry.name || '') === 'محشي صغير' || (entry.name || '').includes('ورق عنب'));
    if (!dolma) return;
    if (!this.count(dolma.id)) {
      this.draft.add({
        itemId: dolma.id,
        name: dolma.name || '',
        imagePath: dolma.imagePath,
        repeatCount: 2,
        peopleCount: this.people
      });
    }
    if (!dolma.isFavorite) {
      this.api.addFavorite(dolma.id).subscribe({
        next: () => dolma.isFavorite = true,
        error: () => dolma.isFavorite = true
      });
    }
  }

  private poster(name?: string | null): string {
    const value = name || '';
    if (value === 'كشري') return 'assets/home/food-koshari.png';
    if (value === 'محشي صغير' || value.includes('ورق عنب')) return 'assets/home/food-dolma.png';
    if (value === 'فراخ مشوية' || value === 'دجاج مشوي' || value.startsWith('دجاج مشوي')) return 'assets/home/food-chicken.png';
    return '';
  }

  private pin(items: HubItem[]): HubItem[] {
    const tests = [
      (name: string) => name === 'كشري',
      (name: string) => name === 'محشي صغير' || name.includes('ورق عنب'),
      (name: string) => name === 'فراخ مشوية' || name === 'دجاج مشوي'
    ];
    const picked: HubItem[] = [];
    const used = new Set<string>();
    for (const test of tests) {
      const item = items.find(entry => test(entry.name || '') && !used.has(entry.id));
      if (!item) continue;
      used.add(item.id);
      picked.push(item);
    }
    const rest = this.curate(items.filter(entry => !used.has(entry.id)));
    return [...picked, ...rest];
  }

  minutes(item: HubItem): number {
    const name = item.name || '';
    if (name.includes('كشري') || name.includes('كشرى')) return 60;
    if (name.includes('محشي') || name.includes('ورق عنب')) return 90;
    if (name.includes('دجاج') || name.includes('فراخ')) return 45;
    let hash = 0;
    for (const ch of name) hash = (hash + ch.charCodeAt(0)) % 5;
    return [30, 40, 50, 55, 75][hash];
  }

  private localArt(item: HubItem): string {
    const name = `${item.name || ''} ${item.summary || ''} ${item.mealName}`;
    if (/كشري|كشرى/.test(name)) return 'assets/home/koshari.jpg';
    if (/محشي|ورق عنب|دولمة/.test(name)) return 'assets/home/dolma.jpg';
    if (/دجاج|فراخ|مشوي/.test(name)) return 'assets/home/chicken.webp';
    if (/حلويات|حلى|كنافة|بسبوس|قطايف|كعك|بقلاو/.test(name)) return 'assets/home/dessert.webp';
    if (/سلطة/.test(name)) return 'assets/home/salad.webp';
    if (/فول|حمص|فلافل|نبات|خضار|عدس/.test(name)) return 'assets/home/vegan.webp';
    return '';
  }

  private curate(items: HubItem[]): HubItem[] {
    const hints = ['كشري', 'محشي ورق عنب', 'دجاج مشوي', 'فتة دجاج', 'ملوخية', 'كنافة', 'شاورما', 'حمص'];
    const picked: HubItem[] = [];
    const used = new Set<string>();
    const baseOf = (name?: string | null) => (name || '').replace(/\s+(الحارة|الخفيفة|العائلية|بالبهارات|بالحلة|بالفرن|على الفحم|الفاتر|بدون سكر)$/, '');
    for (const hint of hints) {
      const item = items.find(entry => (entry.name || '').includes(hint));
      if (!item || used.has(baseOf(item.name))) continue;
      used.add(baseOf(item.name));
      picked.push(item);
    }
    for (const item of items) {
      const base = baseOf(item.name);
      if (used.has(base)) continue;
      used.add(base);
      picked.push(item);
      if (picked.length >= 10) break;
    }
    return picked;
  }

  private rank(name?: string | null): number {
    const value = name || '';
    if (value.includes('فطار') || value.includes('فطور')) return 0;
    if (value.includes('غداء')) return 1;
    if (value.includes('عشاء')) return 2;
    return 3;
  }

  private matches(item: HubItem, kitchen: string): boolean {
    const blob = `${item.name || ''} ${item.summary || ''} ${item.mealName}`;
    if (kitchen === 'sweet') return /حلويات|حلى|كنافة|بسبوس|قطايف|مهلبي|بقلاو|كعك/.test(blob);
    if (kitchen === 'vegan') return /سلطة|خضار|حمص|فول|فلافل|طعمية|تبول|فتوش|مجدر|عدس|بابا|متبل/.test(blob);
    if (kitchen === 'healthy') return /سلطة|شورب|مشوي|خضار|عدس/.test(blob);
    if (kitchen === 'west') return /بيتزا|برجر|مكرون|باستا|ساندويتش/.test(blob);
    return !/حلويات|حلى|كنافة|بيتزا|برجر/.test(blob);
  }
}
