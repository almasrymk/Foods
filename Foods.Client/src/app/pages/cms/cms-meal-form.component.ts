import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CmsService, MealRow } from '../../core/cms.service';
import { IconComponent } from '../../core/icon.component';
import { PhotoComponent } from '../../core/photo.component';
import { CmsColorPickerComponent } from './cms-color-picker.component';
import { CmsIconPickerComponent } from './cms-icon-picker.component';
import { CmsTone } from './cms-palette';

@Component({
  selector: 'app-cms-meal-form',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    IconComponent,
    PhotoComponent,
    CmsIconPickerComponent,
    CmsColorPickerComponent
  ],
  template: `
    <a class="back" routerLink="/cms/meals">الرجوع للقائمة</a>
    <h1><app-icon [name]="form.id ? 'edit' : 'add'"></app-icon> {{ form.id ? 'تعديل وجبة' : 'إضافة وجبة' }}</h1>
    <p class="error" *ngIf="error">{{ error }}</p>
    <form class="panel editor" (ngSubmit)="save()">
      <div class="fields">
        <label>الاسم<input [(ngModel)]="form.name" name="name" required></label>
        <label>الترتيب<input [(ngModel)]="form.sort" name="sort" type="number" min="0" step="1"></label>
        <label class="wide">الوصف<textarea [(ngModel)]="form.description" name="description"></textarea></label>
      </div>
      <section>
        <h2>الصورة</h2>
        <div class="file-row">
          <app-photo [path]="form.imagePath" icon="meals" alt=""></app-photo>
          <label class="file-btn">اختيار صورة<input type="file" accept="image/*" (change)="onImage($event)"></label>
        </div>
      </section>
      <section>
        <h2>الأيقونة</h2>
        <app-cms-icon-picker
          [value]="form.iconPath || ''"
          fallbackIcon="meals"
          (valueChange)="form.iconPath = $event"
          (upload)="onIconUpload($event)">
        </app-cms-icon-picker>
      </section>
      <section>
        <h2>اللون</h2>
        <app-cms-color-picker
          [color]="form.color || ''"
          [textColor]="form.textColor || ''"
          (pickTone)="pick($event)">
        </app-cms-color-picker>
      </section>
      <button type="submit" class="save">حفظ</button>
    </form>
  `
})
export class CmsMealFormComponent implements OnInit {
  form: MealRow = { id: '', name: '', description: '', imagePath: '', iconPath: '', color: '', textColor: '', sort: 0 };
  error = '';

  constructor(private cms: CmsService, private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.cms.get<MealRow>('Meal', id).subscribe({
      next: meal => this.form = {
        ...meal,
        description: meal.description || '',
        imagePath: meal.imagePath || '',
        iconPath: meal.iconPath || '',
        color: meal.color || '',
        textColor: meal.textColor || '',
        sort: meal.sort ?? 0
      },
      error: (err: Error) => this.error = err.message
    });
  }

  pick(tone: CmsTone): void {
    this.form.color = tone.color;
    this.form.textColor = tone.textColor;
  }

  onImage(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.cms.upload(file).subscribe({
      next: path => this.form.imagePath = path,
      error: (err: Error) => this.error = err.message
    });
  }

  onIconUpload(file: File): void {
    this.cms.upload(file).subscribe({
      next: path => this.form.iconPath = path,
      error: (err: Error) => this.error = err.message
    });
  }

  save(): void {
    const body = {
      id: this.form.id,
      name: this.form.name,
      description: this.form.description || '',
      imagePath: this.form.imagePath || null,
      iconPath: this.form.iconPath || null,
      color: this.form.color || null,
      textColor: this.form.textColor || null,
      sort: Number(this.form.sort) || 0
    };
    const done = () => this.router.navigateByUrl('/cms/meals');
    const fail = (err: Error) => this.error = err.message;
    if (this.form.id) this.cms.update('Meal', body).subscribe({ next: done, error: fail });
    else this.cms.create('Meal', body).subscribe({ next: done, error: fail });
  }
}
