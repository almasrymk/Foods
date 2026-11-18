import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { mediaUrl } from '../../../core/environment';
import { MealItemFilter } from '../../../core/models';
import { FaTabBarComponent } from '../shared/tab-bar.component';
import { FaTopBarComponent } from '../shared/top-bar.component';
import { FaIconComponent } from '../shared/icon.component';

interface CuisineTile {
  id: string;
  label: string;
  count: number;
  iconKey: string;
  iconImage: string;
  image: string;
  broken?: boolean;
  color: string;
  bg: string;
}

export type BrowseKind = 'cuisine' | 'category' | 'group';

/** Texts and the meals-list filter of each browse kind (cuisines / categories / groups). */
export const BROWSE_KINDS: Record<BrowseKind, { param: string; title: string; sub: string; empty: string; tab: string; path: string }> = {
  cuisine: { param: 'cuisineId', title: 'اختر المطبخ', sub: 'تصفح المطابخ واختر ما يناسبك من الأكلات', empty: 'لا توجد مطابخ حالياً', tab: 'المطبخ', path: '/cuisines' },
  category: { param: 'categoryId', title: 'اختر التصنيف', sub: 'تصفح التصنيفات واختر ما يناسبك من الأكلات', empty: 'لا توجد تصنيفات حالياً', tab: 'التصنيف', path: '/categories' },
  group: { param: 'groupId', title: 'اختر المجموعة', sub: 'تصفح المجموعات واختر ما يناسبك من الأكلات', empty: 'لا توجد مجموعات حالياً', tab: 'المجموعة', path: '/groups' }
};

/** Browse all cuisines (or categories / groups): same header and 3-column tiles as the meals plan screen, plus an "all" tile. */
@Component({
  selector: 'app-cuisines-screen',
  imports: [FaTopBarComponent, FaTabBarComponent, FaIconComponent],
  templateUrl: './cuisines.component.html',
  styleUrl: './cuisines.component.css'
})
export class CuisinesScreenComponent implements OnInit {
  tiles: CuisineTile[] = [];
  loading = true;
  kind: BrowseKind = 'cuisine';
  get texts() { return BROWSE_KINDS[this.kind]; }

  readonly skeletons = [1, 2, 3, 4, 5, 6];

  /** All the dishes the customer can see (the cuisines' "الكل" entry); a sum of the tiles until it loads. */
  allCount: number | null = null;

  get totalCount(): number {
    return this.allCount ?? this.tiles.reduce((sum, t) => sum + (t.count || 0), 0);
  }

  constructor(private router: Router, private api: ApiService, private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.kind = (this.route.snapshot.data['kind'] as BrowseKind) || 'cuisine';
    const source: Observable<MealItemFilter[]> =
      this.kind === 'category' ? this.api.categories() : this.kind === 'group' ? this.api.groups() : this.api.cuisines();
    this.api.cuisines().subscribe({
      next: filters => {
        const all = (filters || []).find(chip => !chip.id);
        if (all) this.allCount = all.count;
      }
    });
    source.subscribe({
      next: filters => {
        this.tiles = (filters || [])
          .filter(chip => !!chip.id)
          .map(chip => this.toTile(chip));
        this.loading = false;
      },
      error: () => {
        this.tiles = [];
        this.loading = false;
      }
    });
  }

  open(tile: CuisineTile): void {
    this.router.navigate(['/meals', 'all'], {
      queryParams: { [this.texts.param]: tile.id },
      state: { name: 'الكل' }
    });
  }

  openAll(): void {
    this.router.navigate(['/meals', 'all'], { state: { name: 'الكل' } });
  }

  private toTile(chip: MealItemFilter): CuisineTile {
    const path = (chip.iconPath || '').trim();
    const isImage = path.includes('/') || path.startsWith('http');
    return {
      id: chip.id || '',
      label: chip.name || '',
      count: chip.count || 0,
      iconKey: !isImage && path ? path : this.iconName(chip.name),
      iconImage: isImage ? mediaUrl(path) : '',
      image: chip.imagePath ? mediaUrl(chip.imagePath) : '',
      color: chip.textColor || '#2E1E12',
      bg: chip.color?.trim() || '#FFF3E6'
    };
  }

  private iconName(name: string): string {
    const value = (name || '').trim();
    if (/نباتي|نبات|فيجان|vegan/.test(value)) return 'vegan';
    if (/صحي|سلطة|دايت/.test(value)) return 'leaf';
    if (/مشوي|كباب|شيش|ستيك|مشويات/.test(value)) return 'grill';
    if (/غربي|باستا|بيتزا|برجر/.test(value)) return 'chef';
    return 'cloche';
  }
}
