import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { ApiService, SearchExtra } from '../../../core/api.service';
import { DraftService } from '../../../core/draft.service';
import { mediaUrl } from '../../../core/environment';
import { ItemCard, MealItemFilter } from '../../../core/models';
import { FaScreenHeaderComponent } from '../shared/screen-header.component';
import { FaTabBarComponent } from '../shared/tab-bar.component';
import { FaIconComponent } from '../shared/icon.component';
import { FaFoodRowComponent } from '../shared/food-row.component';
import { FoodRow } from '../shared/screens.data';
import { FaSearchBarComponent } from '../shared/search-bar.component';

const PAGE_SIZE = 20;

/** المفضلة: saved meals (design/mockups/favorites.png). */
@Component({
  selector: 'app-favorites-screen',
  imports: [FaScreenHeaderComponent, FaTabBarComponent, FaIconComponent, FaFoodRowComponent, FaSearchBarComponent],
  template: `
    <div class="fa-screen" dir="rtl" style="padding-bottom: calc(335 * var(--s))">
      <fa-screen-header title="أكلاتي المفضلة" art="assets/screens/art/favorites.svg" subtitle="قائمة الأكلات المحفوظة لديك" [heart]="true"
                        [height]="258" [titleY]="180" [backY]="155" [subY]="233" />
      <fa-search-bar style="margin-top: calc(10 * var(--s))" placeholder="ابحث في المفضلة ..." [value]="query"
                     (valueChange)="query = $event; reload()" (extraChange)="extra = $event; reload()" />
      <div class="fa-chips" style="margin-top: calc(28 * var(--s))">
        @for (chip of filters; track chip.id ?? 'all') {
          <button type="button" class="fa-chip" [class.on]="(chip.id || '') === cuisineId" (click)="selectFilter(chip)">
            @if (imageOf(chip); as src) {
              <img class="chip-ico" [src]="src" alt="">
            } @else {
              <fa-icon [name]="iconOf(chip)" [weight]="1.8" />
            }
            {{ chip.name }} ({{ chip.count }})
          </button>
        }
      </div>
      <div class="fa-rows" style="margin-top: calc(33 * var(--s))">
        @for (item of rows; track item.id) {
          <fa-food-row [item]="item" [greenSelection]="true"
                       (favoriteChange)="onFavoriteChange(item, $event)"
                       (selectionChange)="onSelectionChange()" />
        }
        @if (loading) {
          <div class="fav-loading">جاري التحميل...</div>
        }
      </div>
      <fa-tab-bar />
    </div>
  `,
  styles: [`
    .chip-ico { width: calc(34 * var(--s)); height: calc(34 * var(--s)); object-fit: contain; }
    .fav-loading {
      text-align: center; padding: calc(24 * var(--s));
      color: #8A6A4A; font-size: calc(26 * var(--s)); font-weight: 700;
    }
  `]
})
export class FavoritesScreenComponent implements OnInit, OnDestroy {
  filters: MealItemFilter[] = [];
  rows: FoodRow[] = [];
  cuisineId = '';
  query = '';
  extra: SearchExtra = {};
  selected = 0;
  loading = false;
  private searchTimer = 0;
  private hasMore = true;
  private loadingMore = false;
  private draftSub?: Subscription;
  private pageSub?: Subscription;

  constructor(private api: ApiService, private draft: DraftService) {}

  ngOnInit(): void {
    this.draft.bootFromServer();
    this.selected = this.draft.selectedCount;
    this.draftSub = this.draft.selectionCount$.subscribe(count => {
      this.selected = count;
      this.syncRowsFromDraft();
    });
    this.api.favoriteFilters().subscribe({
      next: filters => { this.filters = filters; }
    });
    this.reload();
  }

  ngOnDestroy(): void {
    this.pageSub?.unsubscribe();
    window.clearTimeout(this.searchTimer);
    this.draftSub?.unsubscribe();
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    if (!this.hasMore || this.loading || this.loadingMore) return;
    const doc = document.documentElement;
    const remaining = doc.scrollHeight - (window.scrollY + window.innerHeight);
    if (remaining < 280) this.loadPage(false);
  }

  selectFilter(chip: MealItemFilter): void {
    this.cuisineId = chip.id || '';
    this.reload();
  }

  reload(): void {
    window.clearTimeout(this.searchTimer);
    this.searchTimer = window.setTimeout(() => {
      this.rows = [];
      this.hasMore = true;
      this.loadPage(true);
    }, this.query ? 250 : 0);
  }

  loadPage(reset: boolean): void {
    if (this.loadingMore && !reset) return;
    // A new search/filter supersedes any in-flight page instead of being dropped.
    if (reset) this.pageSub?.unsubscribe();
    if (!reset && !this.hasMore) return;

    const skip = reset ? 0 : this.rows.length;
    this.loadingMore = true;
    if (reset) this.loading = true;

    this.pageSub = this.api.favorites(this.query.trim(), this.cuisineId || null, skip, PAGE_SIZE, this.extra).subscribe({
      next: items => {
        const mapped = items.map(item => this.toRow(item));
        this.rows = reset ? mapped : [...this.rows, ...mapped];
        this.hasMore = mapped.length >= PAGE_SIZE;
        this.loadingMore = false;
        this.loading = false;
      },
      error: () => {
        this.loadingMore = false;
        this.loading = false;
        this.hasMore = false;
      }
    });
  }

  onSelectionChange(): void {
    this.selected = this.draft.selectedCount;
  }

  onFavoriteChange(item: FoodRow, favorite: boolean): void {
    if (favorite) return;
    this.rows = this.rows.filter(row => row.id !== item.id);
    this.api.favoriteFilters().subscribe({
      next: filters => { this.filters = filters; }
    });
  }

  imageOf(chip: MealItemFilter): string {
    const path = chip.iconPath || '';
    if (path.includes('/') || path.startsWith('http')) return mediaUrl(path);
    return '';
  }

  iconOf(chip: MealItemFilter): string {
    const path = (chip.iconPath || '').trim();
    if (path && !path.includes('/') && !path.startsWith('http')) return path;
    return this.iconName(chip.name);
  }

  iconName(name: string): string {
    const value = (name || '').trim();
    if (value === 'الكل') return 'grid';
    if (/نباتي|نبات|فيجان|vegan/.test(value)) return 'vegan';
    if (/صحي|سلطة|دايت/.test(value)) return 'leaf';
    if (/مشوي|كباب|شيش|ستيك|مشويات/.test(value)) return 'grill';
    if (/غربي|باستا|بيتزا|برجر/.test(value)) return 'chef';
    return 'cloche';
  }

  private syncRowsFromDraft(): void {
    for (const row of this.rows) {
      const qty = this.draft.count(row.id);
      row.qty = qty;
      row.checked = qty > 0;
    }
  }

  private toRow(item: ItemCard): FoodRow {
    const qty = this.draft.count(item.id);
    return {
      id: item.id,
      title: item.name || '',
      desc: item.summary || '',
      tag: item.cuisineName || item.categoryName || 'أكلة',
      tagIcon: this.iconName(item.cuisineName || item.categoryName || ''),
      group: item.groupName || '',
      category: item.categoryName || '',
      cuisine: item.cuisineName || '',
      image: mediaUrl(item.imagePath) || 'assets/screens/foods/peppers.png',
      images: item.images || [],
      checked: qty > 0,
      favorite: true,
      qty,
      cuisineId: item.cuisineId || '',
      price: item.price ?? null,
      priceMissing: item.priceMissing || 0
    };
  }
}
