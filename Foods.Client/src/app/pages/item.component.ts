import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { DraftService } from '../core/draft.service';
import { mediaUrl } from '../core/environment';
import { ItemDetail } from '../core/models';
import { PhotoComponent } from '../core/photo.component';

@Component({
  selector: 'app-item',
  imports: [CommonModule, FormsModule, RouterLink, PhotoComponent],
  template: `
    <a class="back" routerLink="/meals">الوجبات</a>
    <article class="stack" *ngIf="item">
      <app-photo [path]="item.imagePath" icon="items" [alt]="item.name || 'أكلة'"></app-photo>
      <div class="row">
        <h1>{{ item.name }}</h1>
        <button type="button" class="heart" [class.on]="item.isFavorite" (click)="toggleFavorite()">{{ item.isFavorite ? '♥' : '♡' }}</button>
      </div>
      <p class="dialect" *ngIf="item.dialect">{{ item.countryName }} · لهجة {{ item.dialect }}</p>
      <p class="muted">{{ item.summary }}</p>
      <div class="stepper">
        <button type="button" (click)="repeat = repeat > 1 ? repeat - 1 : 1">−</button>
        <input type="number" min="1" [(ngModel)]="repeat" name="repeat">
        <button type="button" (click)="repeat = repeat + 1">+</button>
      </div>
      <label>عدد العزومة<input type="number" min="1" [(ngModel)]="people" name="people" placeholder="حسب الأسرة"></label>
      <button type="button" (click)="choose()">{{ draft.has(item.id) ? 'تحديث الاختيار' : 'إضافة للخطة' }}</button>
      <h2>المكونات لفرد بالغ</h2>
      <ul class="lines">
        <li *ngFor="let ingredient of item.ingredients">
          <app-photo [path]="ingredient.imagePath" icon="contents" [alt]="ingredient.name || 'مكون'"></app-photo>
          <span>{{ ingredient.name }} — {{ ingredient.quantity }} {{ ingredient.unitName }}</span>
          <small *ngIf="ingredient.purchasePlaceName">{{ ingredient.purchasePlaceName }}</small>
        </li>
      </ul>
      <h2>طريقة العمل</h2>
      <p class="method" *ngIf="item.description">{{ item.description }}</p>
      <ol>
        <li *ngFor="let step of item.steps">{{ step.body }}</li>
      </ol>
      <h2>الفيديو</h2>
      <p *ngFor="let video of item.videos"><a [href]="video.url || ''" target="_blank">{{ video.title || video.url }}</a></p>
    </article>
    <p class="error" *ngIf="error">{{ error }}</p>
  `
})
export class ItemComponent implements OnInit {
  item: ItemDetail | null = null;
  error = '';
  repeat = 1;
  people: number | null = null;
  image = mediaUrl;

  constructor(private api: ApiService, private route: ActivatedRoute, public draft: DraftService) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') || '';
    this.api.item(id).subscribe({
      next: item => this.item = item,
      error: (err: Error) => this.error = err.message
    });
  }

  choose(): void {
    if (!this.item) return;
    this.draft.add({
      itemId: this.item.id,
      name: this.item.name || '',
      imagePath: this.item.imagePath,
      repeatCount: this.repeat || 1,
      peopleCount: this.people && this.people > 0 ? Number(this.people) : null
    });
  }

  toggleFavorite(): void {
    if (!this.item) return;
    const call = this.item.isFavorite ? this.api.removeFavorite(this.item.id) : this.api.addFavorite(this.item.id);
    call.subscribe({
      next: () => { if (this.item) this.item.isFavorite = !this.item.isFavorite; },
      error: (err: Error) => this.error = err.message
    });
  }
}
