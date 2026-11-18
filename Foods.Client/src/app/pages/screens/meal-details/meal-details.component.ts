import { Component, OnDestroy, OnInit } from '@angular/core';
import { Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../../core/api.service';
import { DraftService } from '../../../core/draft.service';
import { mediaUrl } from '../../../core/environment';
import { ContentPick, DishPrice, ItemDetail, ItemIngredient, ItemPart, MyItemRecipe, UnitPick } from '../../../core/models';
import { FaTabBarComponent } from '../shared/tab-bar.component';
import { FaTopBarComponent } from '../shared/top-bar.component';
import { FaIconComponent } from '../shared/icon.component';
import { FaPhotoSliderComponent } from '../shared/photo-slider.component';
import { MEAL_DETAILS } from './meal-details.data';
import { FaPriceSheetComponent } from '../shared/price-sheet.component';
import { egp } from '../../../core/money';

/** One ingredient row as drawn on the card. */
interface IngView {
  key: string;
  name: string;
  image: string;
  images: string[];
  qty: string;
  isAdded: boolean;
  isChanged: boolean;
  contentId: string;
  /** Optional ingredient (the customer can pick or drop it). */
  optional: boolean;
  selected: boolean;
  /** Money of the quantity (per person); null = no price yet. */
  cost: number | null;
}

/** One part (صنف) row. */
interface PartView {
  itemId: string;
  name: string;
  image: string;
  optional: boolean;
  selected: boolean;
  canOpen: boolean;
  count: number;
  /** Added by the customer in "تعديل الأكلة ليا". */
  added: boolean;
}

/** One editable ingredient line (personal version). */
interface EditLine {
  key: string;
  contentId: string;
  name: string;
  image: string;
  unitId: string;
  quantity: number | null;
  isNew: boolean;
}

const INGREDIENT_FALLBACK = 'assets/screens/ingredients/onion.png';
/** The design has 4 rows per column; more ingredients grow the card by one row height each. */
const DESIGN_ROWS = 4;
const ROW_HEIGHT = 68;

/** تفاصيل الأكلة: meal details (design/mockups/meal-details.png, drawn 958px wide). */
@Component({
  selector: 'app-meal-details-screen',
  imports: [FormsModule, FaTopBarComponent, FaTabBarComponent, FaIconComponent, FaPhotoSliderComponent, FaPriceSheetComponent],
  templateUrl: './meal-details.component.html',
  styleUrl: './meal-details.component.css'
})
export class MealDetailsScreenComponent implements OnInit, OnDestroy {
  meal = { ...MEAL_DETAILS };
  /** Hero album (main photo first). */
  heroImages: string[] = [];
  /** Real videos of the dish (admin → فيديوهات); the design thumbnails stand in for pictures. */
  videos: { label: string; url: string; image: string }[] = [];
  /** Stepper animation, same as the meals list (food-row). */
  pull: '' | 'plus' | 'minus' = '';
  dragging = false;
  dragX = 0;
  private pullTimer = 0;
  private startX = 0;
  private pointerId: number | null = null;
  private maxDrag = 40;
  private threshold = 24;
  private itemImage: string | null = null;
  toast = '';
  private toastTimer = 0;
  favorite = false;
  busy = false;

  /** Real ingredients / steps of the dish (the customer's own version when they changed it). */
  loaded = false;
  columns: IngView[][] = [[], []];
  steps: string[] = [];
  customized = false;
  /** أصناف: other dishes inside this dish. */
  parts: PartView[] = [];
  choiceBusy = false;
  /** Parts while editing "تعديل الأكلة ليا". */
  draftParts: PartView[] = [];
  removedParts: PartView[] = [];
  partPickerOpen = false;
  partPickerText = '';
  partPickerResults: PartView[] = [];
  partPickerBusy = false;
  private partTimer = 0;

  /** "تعديل الأكلة ليا" edit mode. */
  editing = false;
  saving = false;
  editError = '';
  draftLines: EditLine[] = [];
  removedLines: EditLine[] = [];
  draftSteps: { key: number; body: string }[] = [];
  units: UnitPick[] = [];
  pickerOpen = false;
  pickerText = '';
  pickerResults: ContentPick[] = [];
  pickerBusy = false;

  private itemId = '';
  private stepKey = 0;
  private searchTimer = 0;

  constructor(
    public location: Location,
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService,
    public draft: DraftService
  ) {}

  /** Same count as the meals list: the dish's repeat count in طلباتي. */
  get qty(): number {
    return this.itemId ? this.draft.count(this.itemId) : 0;
  }

  /** Extra card height (design units) when there are more than 4 rows per column. */
  get ingExtra(): number {
    const rows = Math.max(this.columns[0].length, this.columns[1].length);
    return Math.max(0, rows - DESIGN_ROWS) * ROW_HEIGHT;
  }

  ngOnInit(): void {
    this.draft.bootFromServer();
    // The same screen opens a part's details, so follow the id.
    this.route.paramMap.subscribe(params => {
      const id = params.get('id') || '';
      if (!id || id === this.itemId) return;
      this.itemId = id;
      // Opened from a country's cuisine (or a part of such a dish): show that country's version.
      const query = this.route.snapshot.queryParamMap;
      this.openCuisine = query.get('cuisine') || null;
      this.recipeCountry = query.get('country') || null;
      this.loaded = false;
      this.editing = false;
      this.parts = [];
      window.scrollTo(0, 0);
      this.load();
    });
  }

  ngOnDestroy(): void {
    window.clearTimeout(this.searchTimer);
    window.clearTimeout(this.partTimer);
    window.clearTimeout(this.pullTimer);
    window.clearTimeout(this.toastTimer);
  }

  /** The cuisine the dish was opened from, and the country whose version is shown. */
  private openCuisine: string | null = null;
  recipeCountry: string | null = null;
  otherCountryName = '';

  private loadItem() {
    return this.api.item(this.itemId, this.recipeCountry ? { countryId: this.recipeCountry } : { cuisineId: this.openCuisine });
  }

  /** Family size for the "4 أفراد" chip. */
  people = 1;

  /** The customer works with prices (the dish came with a price). */
  priced = false;
  dishPrice: DishPrice | null = null;
  /** Ingredient whose price sheet is open. */
  priceFor: string | null = null;
  money = egp;

  onPriceClosed(changed: boolean): void {
    this.priceFor = null;
    if (changed) this.loadItem().subscribe({ next: item => this.apply(item) });
  }

  private load(): void {
    this.api.family().subscribe({ next: members => { this.people = members.length || 1; this.setFacts(); } });
    this.loadItem().subscribe({
      next: item => this.apply(item)
    });
  }

  private apply(item: ItemDetail): void {
    this.recipeCountry = item.recipeCountryId || null;
    this.otherCountryName = item.isOtherCountry ? (item.recipeCountryName || '') : '';
    this.favorite = !!item.isFavorite;
    this.itemImage = item.imagePath || null;
    const thumbs = MEAL_DETAILS.videos;
    this.videos = (item.videos || [])
      .filter(v => !!(v.url || '').trim())
      .slice(0, 4)
      .map((v, i) => ({ label: (v.title || '').trim() || `فيديو ${i + 1}`, url: mediaUrl(v.url) || v.url || '', image: thumbs[i % thumbs.length].image }));
    this.heroImages = item.images || [];
    this.meal = {
      ...this.meal,
      title: item.name || this.meal.title,
      desc: item.summary || this.meal.desc,
      video: mediaUrl(item.imagePath) || this.meal.video
    };
    const rows = (item.ingredients || []).map((line, index) => this.toView(line, index));
    const half = Math.ceil(rows.length / 2);
    // Two columns, right column first (RTL), like the design.
    this.columns = [rows.slice(0, half), rows.slice(half)];
    this.steps = (item.steps || []).map(step => (step.body || '').trim()).filter(body => body.length > 0);
    this.customized = !!item.isCustomized;
    this.parts = (item.parts || []).map(part => this.toPart(part));
    this.groupName = (item.groupName || '').trim();
    this.categoryName = (item.categoryName || '').trim();
    this.dishPrice = item.price ?? null;
    this.priced = !!item.price;
    this.setFacts();
    this.loaded = true;
  }

  private groupName = '';
  private categoryName = '';

  /** المجموعة · التصنيف · عدد أفراد الأسرة. */
  private setFacts(): void {
    const n = this.people;
    const people = n === 1 ? 'فرد واحد' : n === 2 ? 'فردين' : n <= 10 ? `${n} أفراد` : `${n} فرد`;
    this.meal = {
      ...this.meal,
      // under the photo: only the group, the category and the family size
      facts: [
        ...(this.groupName ? [{ icon: 'layers', label: this.groupName }] : []),
        ...(this.categoryName ? [{ icon: 'grid', label: this.categoryName }] : []),
        { icon: 'person', label: people, filled: true }
      ],
      // with the name, above the photo: another country's version and the price
      topFacts: [
        ...(this.otherCountryName ? [{ icon: 'pin', label: `نسخة ${this.otherCountryName}` }] : []),
        ...(this.dishPrice
          ? [{ icon: 'tag', label: this.dishPrice.total > 0
              ? `${egp(this.dishPrice.total)}${this.dishPrice.missing ? ' +' : ''}`
              : 'مفيش سعر لسه' }]
          : [])
      ]
    };
  }

  private toView(line: ItemIngredient, index: number): IngView {
    return {
      key: line.contentId + ':' + index,
      name: line.name || '',
      image: mediaUrl(line.imagePath) || INGREDIENT_FALLBACK,
      images: line.images || [],
      qty: `${this.trim(line.quantity)} ${(line.unitName || '').trim()}`.trim(),
      isAdded: !!line.isAdded,
      isChanged: !!line.isChanged,
      contentId: line.contentId,
      optional: !!line.isOptional,
      selected: line.selected !== false,
      cost: line.cost ?? null
    };
  }

  private toPart(part: ItemPart): PartView {
    return {
      itemId: part.itemId,
      name: part.name || '',
      image: mediaUrl(part.imagePath) || MEAL_DETAILS.video,
      optional: !!part.isOptional,
      selected: part.selected !== false,
      canOpen: !!part.canOpen,
      count: part.ingredientCount || 0,
      added: !!part.isAdded
    };
  }

  removePart(index: number): void {
    const [part] = this.draftParts.splice(index, 1);
    if (part && !part.added) this.removedParts.push(part);
  }

  restorePart(index: number): void {
    const [part] = this.removedParts.splice(index, 1);
    if (part) this.draftParts.push(part);
  }

  openPartPicker(): void {
    this.partPickerOpen = true;
    this.partPickerText = '';
    this.searchParts();
  }

  closePartPicker(): void {
    this.partPickerOpen = false;
    this.partPickerResults = [];
    window.clearTimeout(this.partTimer);
  }

  onPartPickerText(value: string): void {
    this.partPickerText = value;
    window.clearTimeout(this.partTimer);
    this.partTimer = window.setTimeout(() => this.searchParts(), 250);
  }

  private searchParts(): void {
    this.partPickerBusy = true;
    const text = this.partPickerText.trim();
    this.api.partPicks(this.itemId, text).subscribe({
      next: rows => {
        if (text !== this.partPickerText.trim()) return;
        const taken = new Set(this.draftParts.map(part => part.itemId));
        this.partPickerResults = rows.map(row => this.toPart(row)).filter(row => !taken.has(row.itemId));
        this.partPickerBusy = false;
      },
      error: () => { this.partPickerResults = []; this.partPickerBusy = false; }
    });
  }

  pickPart(part: PartView): void {
    const removed = this.removedParts.findIndex(row => row.itemId === part.itemId);
    if (removed >= 0) this.draftParts.push(this.removedParts.splice(removed, 1)[0]);
    else if (!this.draftParts.some(row => row.itemId === part.itemId)) this.draftParts.push({ ...part, added: true, optional: false, selected: true });
    this.closePartPicker();
  }

  openPart(part: PartView): void {
    if (part.canOpen) this.router.navigate(['/items', part.itemId], this.recipeCountry ? { queryParams: { country: this.recipeCountry } } : {});
  }

  toggleIngredient(row: IngView): void {
    this.choose(0, row.contentId, row);
  }

  togglePart(part: PartView): void {
    this.choose(1, part.itemId, part);
  }

  /** Picks or drops an optional ingredient / part for this customer; طلباتي follows. */
  private choose(kind: 0 | 1, refId: string, row: { selected: boolean; name: string }): void {
    if (this.choiceBusy) return;
    const next = !row.selected;
    row.selected = next;
    this.choiceBusy = true;
    this.api.setItemChoice(this.itemId, kind, refId, next, this.recipeCountry).subscribe({
      next: () => {
        this.choiceBusy = false;
        this.draft.recipeChanged(this.itemId);
        this.showToast(next ? `اتضاف ${row.name}` : `اتشال ${row.name}`);
      },
      error: (err: Error) => {
        row.selected = !next;
        this.choiceBusy = false;
        this.showToast(err.message);
      }
    });
  }

  private trim(value: number): string {
    return Number.isInteger(value) ? String(value) : String(Math.round(value * 100) / 100);
  }

  step(delta: number): void {
    this.applyQty(Math.max(0, this.qty + delta));
    window.clearTimeout(this.pullTimer);
    this.pull = delta > 0 ? 'plus' : 'minus';
    this.pullTimer = window.setTimeout(() => { this.pull = ''; }, 220);
  }

  /** "إضافة إلى الخطة": puts the dish in طلباتي (at least once). */
  addToPlan(): void {
    this.applyQty(Math.max(1, this.qty));
    this.showToast('اتضافت لطلباتي');
  }

  openVideo(url: string): void {
    if (url) window.open(url, '_blank', 'noopener');
  }

  onPointerDown(event: PointerEvent): void {
    const target = event.currentTarget as HTMLElement;
    const panel = target.closest('.fa-stepper') as HTMLElement | null;
    if (panel) {
      const room = Math.max(24, (panel.clientWidth - target.clientWidth) / 2);
      this.maxDrag = room;
      this.threshold = room * 0.55;
    }
    this.pointerId = event.pointerId;
    this.startX = event.clientX;
    this.dragX = 0;
    this.dragging = true;
    this.pull = '';
    window.clearTimeout(this.pullTimer);
    target.setPointerCapture(event.pointerId);
  }

  onPointerMove(event: PointerEvent): void {
    if (!this.dragging || event.pointerId !== this.pointerId) return;
    this.dragX = Math.max(-this.maxDrag, Math.min(this.maxDrag, event.clientX - this.startX));
  }

  onPointerUp(event: PointerEvent): void {
    if (!this.dragging || (this.pointerId !== null && event.pointerId !== this.pointerId)) return;
    const dx = this.dragX;
    this.dragging = false;
    this.dragX = 0;
    this.pointerId = null;
    if (dx >= this.threshold) this.step(1);
    else if (dx <= -this.threshold) this.step(-1);
  }

  private applyQty(qty: number): void {
    if (!this.itemId) return;
    this.draft.setQty({ itemId: this.itemId, name: this.meal.title, imagePath: this.itemImage, peopleCount: null, countryId: this.otherCountryName ? this.recipeCountry : null }, qty);
  }

  private showToast(text: string): void {
    this.toast = text;
    window.clearTimeout(this.toastTimer);
    this.toastTimer = window.setTimeout(() => (this.toast = ''), 1800);
  }

  toggleFavorite(): void {
    if (this.busy || !this.itemId) return;
    const next = !this.favorite;
    this.busy = true;
    const call = next ? this.api.addFavorite(this.itemId) : this.api.removeFavorite(this.itemId);
    call.subscribe({
      next: () => {
        this.favorite = next;
        this.busy = false;
      },
      error: () => { this.busy = false; }
    });
  }

  // ---------------- personal version: edit mode ----------------

  startEdit(): void {
    if (!this.itemId || this.saving) return;
    this.editError = '';
    this.saving = true;
    this.api.myItem(this.itemId, this.recipeCountry).subscribe({
      next: mine => {
        this.fillDraft(mine);
        this.editing = true;
        this.saving = false;
      },
      error: (err: Error) => { this.editError = err.message; this.saving = false; }
    });
    if (!this.units.length) {
      this.api.searchContents('', 1).subscribe({ next: result => this.units = result.units || [] });
    }
  }

  private fillDraft(mine: MyItemRecipe): void {
    const toEdit = (line: ItemIngredient, isNew: boolean): EditLine => ({
      key: line.contentId,
      contentId: line.contentId,
      name: line.name || '',
      image: mediaUrl(line.imagePath) || INGREDIENT_FALLBACK,
      unitId: line.unitId,
      quantity: line.quantity,
      isNew
    });
    this.draftLines = (mine.lines || []).map(line => toEdit(line, !!line.isAdded));
    this.removedLines = (mine.removed || []).map(line => toEdit(line, false));
    this.draftSteps = (mine.steps || []).map(step => ({ key: ++this.stepKey, body: step.body || '' }));
    const parts = (mine.parts || []).map(part => this.toPart(part));
    this.draftParts = parts.filter(part => part.selected);
    this.removedParts = parts.filter(part => !part.selected);
    this.closePicker();
    this.closePartPicker();
  }

  cancelEdit(): void {
    this.editing = false;
    this.editError = '';
    this.closePicker();
    this.closePartPicker();
  }

  removeLine(index: number): void {
    const [line] = this.draftLines.splice(index, 1);
    if (line && !line.isNew) this.removedLines.push(line);
  }

  restoreLine(index: number): void {
    const [line] = this.removedLines.splice(index, 1);
    if (line) this.draftLines.push(line);
  }

  openPicker(): void {
    this.pickerOpen = true;
    this.pickerText = '';
    this.search();
  }

  closePicker(): void {
    this.pickerOpen = false;
    this.pickerResults = [];
    window.clearTimeout(this.searchTimer);
  }

  onPickerText(value: string): void {
    this.pickerText = value;
    window.clearTimeout(this.searchTimer);
    this.searchTimer = window.setTimeout(() => this.search(), 250);
  }

  private search(): void {
    this.pickerBusy = true;
    const text = this.pickerText.trim();
    this.api.searchContents(text, 12).subscribe({
      next: result => {
        if (text !== this.pickerText.trim()) return;
        const taken = new Set(this.draftLines.map(line => line.contentId));
        this.pickerResults = (result.items || []).filter(item => !taken.has(item.id));
        if (result.units?.length) this.units = result.units;
        this.pickerBusy = false;
      },
      error: () => { this.pickerBusy = false; }
    });
  }

  pick(item: ContentPick): void {
    const restored = this.removedLines.findIndex(line => line.contentId === item.id);
    if (restored >= 0) {
      this.restoreLine(restored);
    } else {
      this.draftLines.push({
        key: item.id,
        contentId: item.id,
        name: item.name || '',
        image: mediaUrl(item.imagePath) || INGREDIENT_FALLBACK,
        unitId: item.unitId,
        quantity: item.miniQuantity > 0 ? item.miniQuantity : 1,
        isNew: true
      });
    }
    this.closePicker();
  }

  addStep(): void {
    this.draftSteps.push({ key: ++this.stepKey, body: '' });
  }

  moveStep(index: number, delta: number): void {
    const target = index + delta;
    if (target < 0 || target >= this.draftSteps.length) return;
    [this.draftSteps[index], this.draftSteps[target]] = [this.draftSteps[target], this.draftSteps[index]];
  }

  removeStep(index: number): void {
    this.draftSteps.splice(index, 1);
  }

  save(): void {
    if (this.saving) return;
    const bad = this.draftLines.find(line => !line.quantity || line.quantity <= 0 || !line.unitId);
    if (bad) { this.editError = `اكتب كمية ووحدة لـ ${bad.name}`; return; }
    this.editError = '';
    this.saving = true;
    const lines = this.draftLines.map(line => ({ contentId: line.contentId, unitId: line.unitId, quantity: Number(line.quantity) }));
    const steps = this.draftSteps
      .map(step => step.body.trim())
      .filter(body => body.length > 0)
      .map((body, index) => ({ sort: index + 1, body }));
    const parts = this.draftParts.map(part => part.itemId);
    this.api.saveMyItem(this.itemId, lines, steps, parts, this.recipeCountry).subscribe({
      next: () => this.afterChange(),
      error: (err: Error) => { this.editError = err.message; this.saving = false; }
    });
  }

  resetToOriginal(): void {
    if (this.saving) return;
    if (!window.confirm('ترجع الأكلة لأصلها وتمسح تعديلاتك؟')) return;
    this.saving = true;
    this.api.resetMyItem(this.itemId, this.recipeCountry).subscribe({
      next: () => this.afterChange(),
      error: (err: Error) => { this.editError = err.message; this.saving = false; }
    });
  }

  private afterChange(): void {
    this.draft.recipeChanged(this.itemId);
    this.loadItem().subscribe({
      next: item => {
        this.apply(item);
        this.editing = false;
        this.saving = false;
      },
      error: (err: Error) => { this.editError = err.message; this.saving = false; }
    });
  }

  unitName(unitId: string): string {
    return this.units.find(unit => unit.id === unitId)?.name || '';
  }
}
