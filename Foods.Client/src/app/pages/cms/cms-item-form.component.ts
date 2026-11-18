import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { CmsService, ContentRow, CountryLink, CountryRow, ItemRow, ItemVersion, LinkRow, MealRow, NamedRow, UnitRow } from '../../core/cms.service';
import { mediaUrl } from '../../core/environment';
import { ApiService } from '../../core/api.service';
import { IconComponent } from '../../core/icon.component';
import { PhotoComponent } from '../../core/photo.component';
import { Select2Component } from '../../core/select2.component';
import { CmsAlbumComponent } from './cms-album.component';

@Component({
  selector: 'app-cms-item-form',
  imports: [CommonModule, FormsModule, RouterLink, IconComponent, PhotoComponent, Select2Component, CmsAlbumComponent],
  template: `
    <a class="back" routerLink="/cms/items">الرجوع للقائمة</a>
    <h1><app-icon [name]="form.id ? 'edit' : 'add'"></app-icon> {{ form.id ? 'تعديل أكلة' : 'إضافة أكلة' }}</h1>
    <p class="error" *ngIf="error">{{ error }}</p>
    <form class="panel editor" (ngSubmit)="save()">
      <div class="fields">
        <label>الاسم<input [(ngModel)]="form.name" name="name" required></label>
        <label>ملخص<input [(ngModel)]="form.summary" name="summary"></label>
        <label>التصنيف
          <app-select2 name="category" [(ngModel)]="form.itemCategoryId" [options]="categories" placeholder="اختار التصنيف" [required]="true"></app-select2>
        </label>
        <div class="wide extra-cats" *ngIf="categories.length > 1">
          <span>تصنيفات تانية <small class="muted">(الأكلة تظهر فيها كمان)</small></span>
          <div class="chips">
            <button type="button" *ngFor="let c of categories" class="chip" [class.on]="hasExtra(c.id)"
                    [hidden]="c.id === form.itemCategoryId" (click)="toggleExtra(c.id)">{{ c.name }}</button>
          </div>
        </div>
        <label>المطبخ
          <app-select2 name="cuisine" [(ngModel)]="form.cuisineId" [options]="cuisines" placeholder="اختار المطبخ" [allowEmpty]="true" emptyLabel="بدون مطبخ"></app-select2>
        </label>
        <label>المجموعة
          <app-select2 name="group" [(ngModel)]="form.itemGroupId" [options]="groups" placeholder="اختار المجموعة" [required]="true"></app-select2>
        </label>
        <label>الترتيب<input type="number" min="0" step="1" [(ngModel)]="form.sort" name="sort">
          <small class="muted">الأصغر يظهر الأول</small>
        </label>
        <label class="wide">طريقة العمل<textarea [(ngModel)]="form.description" name="description"></textarea></label>
        <label class="wide visible-toggle">
          <input type="checkbox" [(ngModel)]="form.showToCustomer" name="showToCustomer">
          <span>تظهر للعميل في البحث والأكلات</span>
          <small class="muted" *ngIf="!form.showToCustomer">الأكلة مش هتظهر للعميل لوحدها، هتظهر بس كصنف جوه الأكلات اللي مضافة ليها.</small>
        </label>
      </div>
      <section>
        <h2>الصورة</h2>
        <div class="file-row">
          <app-photo [path]="form.imagePath" icon="items" alt=""></app-photo>
          <label class="file-btn">اختيار صورة<input type="file" accept="image/*" (change)="onFile($event)"></label>
        </div>
      </section>
      <section>
        <h2>ألبوم الصور</h2>
        <app-cms-album kind="item" [ownerId]="form.id"></app-cms-album>
      </section>
      <section>
        <h2>الوجبات</h2>
        <div class="checks">
          <label *ngFor="let meal of meals"><input type="checkbox" [checked]="selectedMeals.has(meal.id)" (change)="toggleMeal(meal.id)"> {{ meal.name }}</label>
        </div>
      </section>
      <section>
        <h2>البلاد</h2>
        <p class="muted">الأكلة ممكن تتقدم في أكتر من بلد. اختار بلد واحدة رئيسية، واكتب الاسم والوصف بلهجة كل بلد لو مختلفين.</p>
        <div class="country-list" *ngIf="countryLinks.length">
          <div class="country-card" *ngFor="let link of countryLinks; let i = index">
            <div class="country-head">
              <div class="country-select">
                <app-select2 [name]="'country' + i" [(ngModel)]="link.countryId" [options]="countryChoices" placeholder="البلد"></app-select2>
              </div>
              <label class="country-primary"><input type="radio" name="primaryCountry" [checked]="link.isPrimary" (change)="markPrimary(i)"> رئيسية</label>
              <button type="button" class="country-remove" (click)="countryLinks.splice(i, 1)"><app-icon name="delete"></app-icon> حذف</button>
            </div>
            <div class="country-fields">
              <label>الاسم<input [(ngModel)]="link.name" [name]="'cn' + i" placeholder="الاسم باللهجة"></label>
              <label>الملخص<input [(ngModel)]="link.summary" [name]="'cs' + i" placeholder="الملخص باللهجة"></label>
              <label class="wide">الوصف / المقادير<textarea rows="5" [(ngModel)]="link.description" [name]="'cd' + i" placeholder="طريقة العمل باللهجة"></textarea></label>
            </div>
          </div>
        </div>
        <button type="button" class="add" (click)="addCountry()"><app-icon name="add"></app-icon> بلد</button>
      </section>
      <section class="version-bar">
        <h2>نسخة الوصفة</h2>
        <p class="muted">النسخة العامة بتتقدم لكل البلاد. لو بلد ليها مكونات أو طريقة عمل أو فيديوهات أو أصناف مختلفة، اعمل لها نسخة خاصة. العميل بيشوف نسخة بلده، ولو فتح الأكلة من مطبخ بلد تانية بيشوف نسخة البلد دي.</p>
        <div class="version-tabs">
          <button type="button" *ngFor="let v of versionChoices" [class.on]="v.key === current" (click)="current = v.key">
            {{ v.label }}<small *ngIf="v.key !== 'general'">{{ versions[v.key] ? 'نسخة خاصة' : 'بتاخد العامة' }}</small>
          </button>
        </div>
        <div class="version-note" *ngIf="current !== 'general'">
          <ng-container *ngIf="!versions[current]">
            <span>البلد دي بتاخد النسخة العامة دلوقتي.</span>
            <button type="button" class="add" (click)="makeOwn()"><app-icon name="add"></app-icon> اعمل نسخة خاصة للبلد دي</button>
          </ng-container>
          <ng-container *ngIf="versions[current]">
            <span>بتعدّل النسخة الخاصة بـ {{ countryName(current) }}.</span>
            <button type="button" class="ghost" (click)="dropOwn()">ارجع للنسخة العامة</button>
          </ng-container>
        </div>
      </section>
      <ng-container *ngIf="hasOwn">
      <section>
        <h2>المكونات</h2>
        <p class="muted">المكون الأساسي بيتحسب دايمًا. الاختياري العميل يقدر يضيفه أو يشيله، وتحدد هو مختار من الأول ولا لأ.</p>
        <div class="line opt-line" *ngFor="let line of lines; let i = index">
          <app-select2 [name]="'c' + i" [(ngModel)]="line.contentId" [options]="contents" placeholder="المكوّن"></app-select2>
          <app-select2 [name]="'u' + i" [(ngModel)]="line.unitId" [options]="units" placeholder="الوحدة"></app-select2>
          <input type="number" min="0" step="0.01" [(ngModel)]="line.quantity" [name]="'q' + i" placeholder="الكمية">
          <div class="kind">
            <select [(ngModel)]="line.isOptional" [name]="'co' + i">
              <option [ngValue]="false">أساسي</option>
              <option [ngValue]="true">اختياري</option>
            </select>
            <label class="def" *ngIf="line.isOptional"><input type="checkbox" [(ngModel)]="line.isDefault" [name]="'cd' + i"> مختار افتراضيًا</label>
          </div>
          <button type="button" class="ghost" (click)="lines.splice(i, 1)"><app-icon name="delete"></app-icon> حذف</button>
        </div>
        <button type="button" class="add" (click)="addLine()"><app-icon name="add"></app-icon> مكون</button>
      </section>
      <section>
        <h2>الأصناف</h2>
        <p class="muted">الصنف أكلة تانية بتتعمل مع الأكلة دي، ومكوناته بتتحسب معاها. الأساسي دايمًا موجود، والاختياري العميل يختاره أو يشيله.</p>
        <div class="line opt-line part-line" *ngFor="let part of parts; let i = index">
          <app-select2 [name]="'p' + i" [(ngModel)]="part.partItemId" [options]="partChoices" placeholder="الأكلة"></app-select2>
          <div class="kind">
            <select [(ngModel)]="part.isOptional" [name]="'po' + i">
              <option [ngValue]="false">أساسي</option>
              <option [ngValue]="true">اختياري</option>
            </select>
            <label class="def" *ngIf="part.isOptional"><input type="checkbox" [(ngModel)]="part.isDefault" [name]="'pd' + i"> مختار افتراضيًا</label>
          </div>
          <span class="hidden-tag" *ngIf="isHidden(part.partItemId)">مش ظاهرة للعميل لوحدها</span>
          <button type="button" class="ghost" (click)="parts.splice(i, 1)"><app-icon name="delete"></app-icon> حذف</button>
        </div>
        <button type="button" class="add" (click)="addPart()"><app-icon name="add"></app-icon> صنف</button>
      </section>
      <section>
        <h2>خطوات</h2>
        <div class="line step" *ngFor="let step of steps; let i = index">
          <input [(ngModel)]="step.body" [name]="'s' + i" placeholder="الخطوة">
          <button type="button" class="ghost" (click)="steps.splice(i, 1)"><app-icon name="delete"></app-icon> حذف</button>
        </div>
        <button type="button" class="add" (click)="steps.push({ sort: steps.length + 1, body: '' })"><app-icon name="add"></app-icon> خطوة</button>
      </section>
      <section>
        <h2>فيديو</h2>
        <div class="line video" *ngFor="let video of videos; let i = index">
          <input [(ngModel)]="video.title" [name]="'vt' + i" placeholder="العنوان">
          <input [(ngModel)]="video.url" [name]="'vu' + i" placeholder="الرابط">
          <button type="button" class="ghost" (click)="videos.splice(i, 1)"><app-icon name="delete"></app-icon> حذف</button>
        </div>
        <button type="button" class="add" (click)="videos.push({ sort: videos.length + 1, title: '', url: '' })"><app-icon name="add"></app-icon> فيديو</button>
      </section>
      </ng-container>
      <button type="submit" class="save">حفظ</button>
    </form>
  `,
  styles: [`
    .editor .extra-cats { display: flex; flex-direction: column; gap: 8px; font-weight: 700; }
    .editor .extra-cats .chips { display: flex; flex-wrap: wrap; gap: 8px; }
    .editor .extra-cats .chip { border: 1px solid #EBD6BF; background: #FFF; border-radius: 18px; padding: 6px 14px; font: inherit; font-weight: 600; cursor: pointer; }
    .editor .extra-cats .chip.on { background: var(--orange); border-color: var(--orange); color: #fff; }
    .editor .visible-toggle {
      flex-direction: row; flex-wrap: wrap; align-items: center; gap: 10px; cursor: pointer;
      background: #fff; border-radius: 14px; padding: 10px 14px; box-shadow: inset 0 0 0 1px var(--line);
    }
    .editor .visible-toggle input { width: 18px; height: 18px; padding: 0; margin: 0; flex: none; box-shadow: none; accent-color: var(--orange); }
    .editor .visible-toggle small { flex-basis: 100%; font-weight: 500; }
    .line.opt-line { grid-template-columns: 1.5fr 1fr 110px 250px auto; }
    .line.part-line { grid-template-columns: 2fr 250px 190px auto; }
    .kind { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
    .kind select { width: auto; min-width: 96px; }
    .editor .kind .def { flex-direction: row; align-items: center; gap: 6px; font-weight: 600; white-space: nowrap; cursor: pointer; }
    .kind .def input { width: 16px; height: 16px; padding: 0; margin: 0; box-shadow: none; accent-color: var(--orange); }
    .hidden-tag { font-size: .85rem; color: #8a6d00; background: #fff4cc; border-radius: 10px; padding: 4px 8px; white-space: nowrap; }
    @media (max-width: 900px) {
      .line.opt-line, .line.part-line { grid-template-columns: 1fr; }
    }
    .version-tabs { display: flex; flex-wrap: wrap; gap: 8px; }
    .version-tabs button {
      width: auto; padding: 8px 14px; border-radius: 14px; background: #fff; color: inherit;
      box-shadow: inset 0 0 0 1px var(--line); display: inline-flex; flex-direction: column; align-items: center; gap: 2px; font-weight: 700;
    }
    .version-tabs button small { font-weight: 500; font-size: .78rem; opacity: .75; }
    .version-tabs button.on { background: var(--orange); color: #fff; box-shadow: none; }
    .version-note { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; margin-top: 10px; }
    .version-note .add, .version-note .ghost { width: auto; }
    .country-list { display: flex; flex-direction: column; gap: 14px; width: 100%; align-self: stretch; }
    .country-card {
      display: flex; flex-direction: column; gap: 14px; width: 100%; box-sizing: border-box;
      background: #fff; border-radius: 18px; padding: 16px 18px; box-shadow: inset 0 0 0 1px var(--line);
    }
    .country-head { display: flex; align-items: center; gap: 14px; flex-wrap: wrap; }
    .country-select { flex: 1 1 260px; min-width: 0; max-width: 420px; }
    .country-head .country-primary {
      flex-direction: row; align-items: center; gap: 8px; width: auto;
      font-weight: 700; white-space: nowrap; cursor: pointer;
    }
    .country-primary input[type=radio] {
      width: 18px; height: 18px; padding: 0; margin: 0; flex: none;
      box-shadow: none; border-radius: 50%; accent-color: var(--orange);
    }
    .country-remove {
      margin-inline-start: auto; width: auto; padding: 6px 12px; border-radius: 12px;
      background: transparent; color: #c0392b; box-shadow: inset 0 0 0 1px #e8b4ad; font-size: .92rem;
      display: inline-flex; align-items: center; gap: 6px;
    }
    .country-fields { display: grid; grid-template-columns: 1fr 1fr; gap: 12px 14px; }
    .country-fields label { min-width: 0; }
    .country-fields .wide { grid-column: 1 / -1; }
    .country-fields textarea { min-height: 120px; resize: vertical; }
    @media (max-width: 700px) {
      .country-fields { grid-template-columns: 1fr; }
      .country-card { padding: 14px; }
    }
  `]
})
export class CmsItemFormComponent implements OnInit {
  categories: NamedRow[] = [];

  hasExtra(id: string): boolean {
    return (this.form.categoryIds || []).includes(id);
  }

  toggleExtra(id: string): void {
    const now = this.form.categoryIds || [];
    this.form.categoryIds = now.includes(id) ? now.filter(e => e !== id) : [...now, id];
  }
  cuisines: NamedRow[] = [];
  groups: NamedRow[] = [];
  meals: MealRow[] = [];
  contents: ContentRow[] = [];
  units: UnitRow[] = [];
  countryOptions: CountryRow[] = [];
  countryLinks: { id?: string; countryId: string; isPrimary: boolean; name: string; summary: string; description: string }[] = [];

  get countryChoices(): NamedRow[] {
    return this.countryOptions.map(country => ({
      id: country.id,
      name: country.dialect ? `${country.name} · ${country.dialect}` : country.name
    }));
  }
  originalCountries: string[] = [];
  form: ItemRow = this.empty();
  selectedMeals = new Set<string>();
  mealLinks = new Map<string, string>();
  /** The general recipe ('general') and each country's own recipe (by country id). */
  versions: Record<string, RecipeState> = { general: emptyState() };
  current = 'general';

  get state(): RecipeState { return this.versions[this.current] || this.versions['general']; }
  get hasOwn(): boolean { return this.current === 'general' || !!this.versions[this.current]; }
  get lines(): RecipeState['lines'] { return this.state.lines; }
  get parts(): RecipeState['parts'] { return this.state.parts; }
  get steps(): RecipeState['steps'] { return this.state.steps; }
  get videos(): RecipeState['videos'] { return this.state.videos; }

  get versionChoices(): { key: string; label: string }[] {
    const linked = this.countryLinks.map(link => link.countryId).filter(Boolean);
    return [{ key: 'general', label: 'العامة' }, ...[...new Set(linked)].map(id => ({ key: id, label: this.countryName(id) }))];
  }

  countryName(id: string): string {
    return this.countryOptions.find(country => country.id === id)?.name || 'البلد';
  }

  makeOwn(): void {
    this.versions = { ...this.versions, [this.current]: copyState(this.versions['general']) };
  }

  dropOwn(): void {
    const next = { ...this.versions };
    delete next[this.current];
    this.versions = next;
  }
  /** Every dish (for the parts picker). */
  allItems: ItemRow[] = [];

  /** Dishes that can be a part: all except this one. */
  get partChoices(): NamedRow[] {
    return this.allItems.filter(item => item.id !== this.form.id).map(item => ({
      id: item.id,
      name: item.showToCustomer === false ? `${item.name} (مخفية)` : item.name
    }));
  }
  error = '';
  image = mediaUrl;

  constructor(
    private cms: CmsService,
    private api: ApiService,
    private route: ActivatedRoute,
    private router: Router) {}

  ngOnInit(): void {
    this.cms.list<NamedRow>('ItemCategory').subscribe(rows => this.categories = rows);
    this.cms.list<NamedRow>('Cuisine').subscribe(rows => this.cuisines = rows);
    this.cms.list<NamedRow>('ItemGroup').subscribe(rows => this.groups = rows);
    this.cms.list<MealRow>('Meal').subscribe(rows => this.meals = rows);
    this.cms.list<ContentRow>('Content').subscribe(rows => this.contents = rows);
    this.cms.list<UnitRow>('Unit').subscribe(rows => this.units = rows);
    this.cms.list<CountryRow>('Country').subscribe(rows => this.countryOptions = rows);
    this.cms.list<ItemRow>('Item').subscribe(rows => this.allItems = rows);
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadItem(id);
  }

  empty(): ItemRow {
    return { id: '', name: '', itemCategoryId: '', cuisineId: '', itemGroupId: '', summary: '', description: '', imagePath: '', showToCustomer: true, sort: 0, categoryIds: [] };
  }

  loadItem(id: string): void {
    this.cms.get<ItemRow>('Item', id).subscribe({
      next: detail => {
        this.form = { ...detail, summary: detail.summary || '', description: detail.description || '', showToCustomer: detail.showToCustomer !== false, sort: detail.sort ?? 0, categoryIds: detail.categoryIds || [] };
        this.loadLinks(detail.id);
      },
      error: (err: Error) => this.error = err.message
    });
  }

  loadLinks(itemId: string): void {
    this.cms.list<LinkRow>('ItemMeal', { ItemId: itemId }).subscribe(rows => {
      this.selectedMeals = new Set(rows.map(row => row.mealId || '').filter(Boolean));
      this.mealLinks = new Map(rows.filter(row => row.mealId).map(row => [row.mealId as string, row.id]));
    });
    this.cms.itemVersions(itemId).subscribe({
      next: rows => {
        const next: Record<string, RecipeState> = { general: emptyState() };
        for (const row of rows) next[row.countryId || 'general'] = fromVersion(row);
        this.versions = next;
      },
      error: (err: Error) => this.error = err.message
    });
    this.cms.list<CountryLink>('ItemCountry', { ItemId: itemId }).subscribe(rows => {
      this.countryLinks = rows.map(row => ({
        id: row.id,
        countryId: row.countryId,
        isPrimary: row.isPrimary,
        name: row.name || '',
        summary: row.summary || '',
        description: row.description || ''
      }));
      this.originalCountries = this.countryLinks.map(link => link.id).filter((linkId): linkId is string => !!linkId);
    });
  }

  toggleMeal(id: string): void {
    if (this.selectedMeals.has(id)) this.selectedMeals.delete(id);
    else this.selectedMeals.add(id);
    this.selectedMeals = new Set(this.selectedMeals);
  }

  addCountry(): void {
    this.countryLinks.push({
      countryId: this.countryOptions[0]?.id || '',
      isPrimary: this.countryLinks.length === 0,
      name: this.form.name || '',
      summary: this.form.summary || '',
      description: this.form.description || ''
    });
  }

  markPrimary(index: number): void {
    this.countryLinks.forEach((link, i) => link.isPrimary = i === index);
  }

  addLine(): void {
    this.lines.push({ contentId: this.contents[0]?.id || '', unitId: this.units[0]?.id || '', quantity: 1, isOptional: false, isDefault: true });
  }

  addPart(): void {
    this.parts.push({ partItemId: '', isOptional: false, isDefault: true });
  }

  isHidden(itemId: string): boolean {
    return !!itemId && this.allItems.find(item => item.id === itemId)?.showToCustomer === false;
  }

  onFile(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.cms.upload(file).subscribe({
      next: path => this.form.imagePath = path,
      error: (err: Error) => this.error = err.message
    });
  }

  save(): void {
    const picked = this.countryLinks.filter(link => link.countryId);
    if (picked.length === 0) { this.error = 'حدد بلد واحدة على الأقل'; return; }
    if (new Set(picked.map(link => link.countryId)).size !== picked.length) { this.error = 'البلد متكررة على الأكلة'; return; }
    if (picked.filter(link => link.isPrimary).length !== 1) { this.error = 'اختار بلد رئيسية واحدة'; return; }
    for (const [key, version] of Object.entries(this.versions)) {
      const where = key === 'general' ? 'النسخة العامة' : `نسخة ${this.countryName(key)}`;
      const partIds = version.parts.map(part => part.partItemId).filter(Boolean);
      if (new Set(partIds).size !== partIds.length) { this.error = `الصنف متكرر في ${where}`; return; }
      const contentIds = version.lines.map(line => line.contentId).filter(Boolean);
      if (new Set(contentIds).size !== contentIds.length) { this.error = `المكوّن متكرر في ${where}`; return; }
    }
    this.error = '';
    const body = {
      id: this.form.id,
      name: this.form.name,
      itemCategoryId: this.form.itemCategoryId,
      cuisineId: this.form.cuisineId || null,
      itemGroupId: this.form.itemGroupId,
      summary: this.form.summary || null,
      description: this.form.description || null,
      imagePath: this.form.imagePath || null,
      showToCustomer: this.form.showToCustomer !== false,
      sort: Number(this.form.sort) || 0,
      categoryIds: (this.form.categoryIds || []).filter(id => id && id !== this.form.itemCategoryId)
    };
    const done = (id: string) => this.afterSave(id);
    if (this.form.id) this.cms.update('Item', body).subscribe({ next: () => done(this.form.id), error: (err: Error) => this.error = err.message });
    else this.cms.create('Item', body).subscribe({ next: id => done(id), error: (err: Error) => this.error = err.message });
  }

  afterSave(itemId: string): void {
    const mealCalls: Observable<unknown>[] = [];
    for (const mealId of this.selectedMeals) {
      if (!this.mealLinks.has(mealId)) mealCalls.push(this.cms.create('ItemMeal', { itemId, mealId }));
    }
    for (const [mealId, linkId] of this.mealLinks) {
      if (!this.selectedMeals.has(mealId)) mealCalls.push(this.cms.remove('ItemMeal', linkId));
    }
    const keptCountries = new Set(this.countryLinks.filter(link => link.id).map(link => link.id as string));
    const countryCalls: Observable<unknown>[] = [
      ...this.countryLinks.filter(link => link.countryId && !link.id).map(link => this.cms.create('ItemCountry', { itemId, countryId: link.countryId, isPrimary: link.isPrimary, name: link.name || null, summary: link.summary || null, description: link.description || null })),
      ...this.countryLinks.filter(link => link.id && link.countryId).map(link => this.cms.update('ItemCountry', { id: link.id, itemId, countryId: link.countryId, isPrimary: link.isPrimary, name: link.name || null, summary: link.summary || null, description: link.description || null })),
      ...this.originalCountries.filter(id => !keptCountries.has(id)).map(id => this.cms.remove('ItemCountry', id))
    ];
    const leave = () => this.router.navigateByUrl('/cms/items');
    // The countries are linked first: a country's own recipe needs its link.
    const linked = new Set(this.countryLinks.map(link => link.countryId).filter(Boolean));
    const versions: ItemVersion[] = Object.entries(this.versions)
      .filter(([key]) => key === 'general' || linked.has(key))
      .map(([key, version]) => toVersion(key === 'general' ? null : key, version));
    const saveRecipes = () => this.cms.saveItemVersions(itemId, versions).subscribe({
      next: () => leave(),
      error: (err: Error) => this.error = err.message
    });
    const jobs = [...mealCalls, ...countryCalls];
    if (jobs.length === 0) { saveRecipes(); return; }
    let pending = jobs.length;
    jobs.forEach(call => call.subscribe({
      next: () => { if (--pending <= 0) saveRecipes(); },
      error: (err: Error) => this.error = err.message
    }));
  }
}

interface RecipeState {
  lines: { contentId: string; unitId: string; quantity: number; isOptional: boolean; isDefault: boolean }[];
  parts: { partItemId: string; isOptional: boolean; isDefault: boolean }[];
  steps: { sort: number; body: string }[];
  videos: { sort: number; title: string; url: string }[];
}

function emptyState(): RecipeState {
  return { lines: [], parts: [], steps: [], videos: [] };
}

function copyState(state: RecipeState): RecipeState {
  return {
    lines: state.lines.map(line => ({ ...line })),
    parts: state.parts.map(part => ({ ...part })),
    steps: state.steps.map(step => ({ ...step })),
    videos: state.videos.map(video => ({ ...video }))
  };
}

function fromVersion(row: ItemVersion): RecipeState {
  return {
    lines: row.lines.map(line => ({ contentId: line.contentId, unitId: line.unitId, quantity: line.quantity, isOptional: !!line.isOptional, isDefault: line.isDefault !== false })),
    parts: row.parts.map(part => ({ partItemId: part.partItemId, isOptional: !!part.isOptional, isDefault: part.isDefault !== false })),
    steps: row.steps.map(step => ({ sort: step.sort, body: step.body || '' })),
    videos: row.videos.map(video => ({ sort: video.sort, title: video.title || '', url: video.url || '' }))
  };
}

function toVersion(countryId: string | null, state: RecipeState): ItemVersion {
  return {
    countryId,
    lines: state.lines.filter(line => line.contentId && line.unitId).map(line => ({
      contentId: line.contentId, unitId: line.unitId, quantity: Number(line.quantity) || 0,
      isOptional: line.isOptional, isDefault: !line.isOptional || line.isDefault
    })),
    parts: state.parts.filter(part => part.partItemId).map(part => ({ partItemId: part.partItemId, isOptional: part.isOptional, isDefault: !part.isOptional || part.isDefault })),
    steps: state.steps.map((step, index) => ({ sort: index + 1, body: step.body })),
    videos: state.videos.map((video, index) => ({ sort: index + 1, title: video.title, url: video.url }))
  };
}
