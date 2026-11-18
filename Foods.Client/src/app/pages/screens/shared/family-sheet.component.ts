import { Component, EventEmitter, HostListener, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/api.service';
import { FamilyMember, FamilyMemberInput } from '../../../core/models';
import { RELATIONS, ageParts, ageText, familyAvatar, pictureUrl } from '../../../core/family-avatar';
import { FaIconComponent } from './icon.component';

interface Draft {
  name: string;
  relation: string;
  customRelation: string;
  birthDate: string;
  imagePath: string;
  /** legacy age for members saved before birth dates existed */
  age: number;
}

/**
 * أفراد الأسرة popup: list + add/edit/delete. Bottom sheet on phones, centred dialog on wide screens.
 * Age is never typed: it is computed from the birth date. Without a photo an automatic avatar is shown.
 */
@Component({
  selector: 'fa-family-sheet',
  imports: [FormsModule, FaIconComponent],
  template: `
    @if (open) {
      <div class="fs-bg" (click)="close()"></div>
      <section class="fs" dir="rtl" role="dialog" aria-modal="true" [attr.aria-label]="editing ? formTitle : 'أفراد الأسرة'">
        <i class="fs-grab" aria-hidden="true"></i>
        <header class="fs-head">
          @if (editing) {
            <button type="button" class="fs-icon-btn" (click)="cancelEdit()" aria-label="رجوع"><fa-icon name="chevronRight" [weight]="2.4" /></button>
            <h2>{{ formTitle }}</h2>
          } @else {
            <span class="fs-badge"><fa-icon name="group" [filled]="true" /></span>
            <h2>أفراد الأسرة <small>{{ members.length }}</small></h2>
          }
          <button type="button" class="fs-icon-btn close" (click)="close()" aria-label="إغلاق"><fa-icon name="x" [weight]="2.4" /></button>
        </header>

        @if (!editing) {
          <!-- list -->
          <div class="fs-body">
            @if (loading) {
              <p class="fs-muted">جاري التحميل...</p>
            } @else {
              <ul class="fs-list">
                @for (m of members; track $index; let i = $index) {
                  <li class="fs-item">
                    <img class="fs-ava" [src]="memberPicture(m)" [alt]="m.name || ''" (error)="fallback($event, m)">
                    <div class="fs-info">
                      <b>{{ m.name }}</b>
                      <span>
                        @if (m.relation) { <em>{{ m.relation }}</em> }
                        @if (memberAge(m); as a) { <i>{{ a }}</i> }
                        @if (m.birthDate) { <i class="d">🎂 {{ prettyDate(m.birthDate) }}</i> }
                      </span>
                    </div>
                    <button type="button" class="fs-icon-btn edit" (click)="edit(i)" [attr.aria-label]="'تعديل ' + m.name"><fa-icon name="pencil" [filled]="true" /></button>
                    <button type="button" class="fs-icon-btn del" (click)="remove(i)" [attr.aria-label]="'حذف ' + m.name"><fa-icon name="trash" [weight]="2" /></button>
                  </li>
                }
              </ul>
              @if (!members.length) { <p class="fs-muted">لسه مفيش أفراد. ضيف أول فرد في أسرتك.</p> }
            }
          </div>
          <footer class="fs-foot">
            <button type="button" class="fs-primary" (click)="add()"><fa-icon name="plusCircle" [weight]="2" />إضافة فرد جديد</button>
          </footer>
        } @else {
          <!-- add / edit form -->
          <form class="fs-body fs-form" (ngSubmit)="saveDraft()" novalidate>
            <div class="fs-photo">
              <img [src]="draftPicture" alt="" (error)="draft.imagePath = ''">
              <label class="fs-cam" [class.busy]="uploading" aria-label="اختيار صورة">
                <fa-icon name="pencil" [filled]="true" />
                <input type="file" accept="image/*" (change)="pickPhoto($event)" [disabled]="uploading">
              </label>
            </div>
            <div class="fs-photo-acts">
              <label class="fs-link">{{ draft.imagePath ? 'تغيير الصورة' : 'إضافة صورة' }}<input type="file" accept="image/*" (change)="pickPhoto($event)" [disabled]="uploading"></label>
              @if (draft.imagePath) { <button type="button" class="fs-link danger" (click)="draft.imagePath = ''">إزالة الصورة</button> }
              @if (!draft.imagePath) { <small>بدون صورة بنختار صورة تلقائية حسب القرابة والسن</small> }
            </div>

            <label class="fs-field"><span>الاسم <b class="req">*</b></span>
              <input name="name" [(ngModel)]="draft.name" maxlength="60" placeholder="مثلاً: سارة" autocomplete="off">
            </label>

            <div class="fs-field"><span>صلة القرابة</span>
              <div class="fs-chips" role="radiogroup" aria-label="صلة القرابة">
                @for (r of relations; track r) {
                  <button type="button" class="fs-chip" role="radio" [class.on]="draft.relation === r" [attr.aria-checked]="draft.relation === r" (click)="draft.relation = r">{{ r }}</button>
                }
              </div>
              @if (draft.relation === 'أخرى') {
                <input name="custom" [(ngModel)]="draft.customRelation" maxlength="30" placeholder="اكتب صلة القرابة (مثلاً: عمة)">
              }
            </div>

            <label class="fs-field"><span>تاريخ الميلاد</span>
              <input name="birth" type="date" [(ngModel)]="draft.birthDate" [max]="today" min="1900-01-01">
            </label>
            <p class="fs-age">
              <fa-icon name="calendar" [weight]="2" />
              @if (draftAge) { العمر: <b>{{ draftAge }}</b> } @else { العمر بيتحسب لوحده من تاريخ الميلاد }
            </p>

            @if (formError) { <p class="fs-err" role="alert">{{ formError }}</p> }
            <div class="fs-actions">
              <button type="button" class="fs-ghost" (click)="cancelEdit()">إلغاء</button>
              <button type="submit" class="fs-primary" [disabled]="saving || uploading">{{ saving ? 'جاري الحفظ...' : 'حفظ' }}</button>
            </div>
          </form>
        }
        @if (error && !editing) { <p class="fs-err list" role="alert">{{ error }}</p> }
      </section>
    }
  `,
  styles: [`
    :host { font-family: 'Tajawal', 'Cairo', sans-serif; }
    .fs-bg { position: fixed; inset: 0; z-index: 1000; background: rgba(46, 30, 18, .42); animation: fs-fade .2s ease; }
    .fs {
      position: fixed; z-index: 1001; left: 50%; bottom: 0; transform: translateX(-50%);
      width: min(100%, 560px); max-height: 92dvh; display: flex; flex-direction: column;
      background: #FFFCF8; border-radius: 26px 26px 0 0; box-shadow: 0 -10px 30px rgba(120, 72, 30, .2);
      color: #2E1E12; animation: fs-up .25s ease; box-sizing: border-box;
    }
    .fs * { box-sizing: border-box; }
    @media (min-width: 700px) {
      .fs { bottom: auto; top: 50%; transform: translate(-50%, -50%); border-radius: 26px; max-height: 86vh; animation: fs-fade .2s ease; }
      .fs-grab { display: none; }
    }
    @keyframes fs-up { from { transform: translate(-50%, 40px); opacity: 0; } }
    @keyframes fs-fade { from { opacity: 0; } }
    .fs-grab { display: block; width: 44px; height: 5px; border-radius: 3px; background: #E6D8C8; margin: 10px auto 0; }
    .fs-head { display: flex; align-items: center; gap: 10px; padding: 12px 18px 10px; border-bottom: 1px solid #F1E6D9; }
    .fs-head h2 { flex: 1; margin: 0; font-size: 20px; font-weight: 800; display: flex; align-items: center; gap: 8px; }
    .fs-head h2 small { font-size: 13px; font-weight: 800; color: #C45A00; background: #FDEBD6; border-radius: 10px; padding: 1px 9px; }
    .fs-badge { width: 38px; height: 38px; border-radius: 50%; display: grid; place-items: center; background: #FCE6CC; color: #F37202; font-size: 22px; }
    .fs-icon-btn {
      width: 38px; height: 38px; flex: none; border: 0; border-radius: 50%; display: grid; place-items: center;
      background: #F7EFE5; color: #4A3A2E; font-size: 18px; cursor: pointer; padding: 0;
    }
    .fs-body { overflow-y: auto; padding: 14px 18px; flex: 1; min-height: 0; }
    .fs-muted { text-align: center; color: #8A7A6E; font-size: 15px; padding: 18px 0; margin: 0; }
    .fs-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 10px; }
    .fs-item {
      display: flex; align-items: center; gap: 12px; padding: 10px 12px; border-radius: 18px;
      background: linear-gradient(180deg, #FFF7EC, #FDF0DF); border: 1px solid #F4E2CB;
    }
    .fs-ava { width: 56px; height: 56px; flex: none; border-radius: 50%; object-fit: cover; background: #fff; box-shadow: 0 2px 6px rgba(120, 72, 30, .15); }
    .fs-info { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 4px; }
    .fs-info b { font-size: 17px; font-weight: 800; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .fs-info span { display: flex; flex-wrap: wrap; gap: 6px; font-size: 13px; color: #6B5B4E; }
    .fs-info em { font-style: normal; font-weight: 800; color: #3E7B22; background: #E6F2DC; border-radius: 9px; padding: 1px 8px; }
    .fs-info i { font-style: normal; font-weight: 700; }
    .fs-info i.d { color: #8A7A6E; font-weight: 500; }
    .fs-icon-btn.edit { background: #E8F3E0; color: #3E8E1E; }
    .fs-icon-btn.del { background: #FDECEA; color: #D32F2F; }
    .fs-foot { padding: 12px 18px calc(14px + env(safe-area-inset-bottom)); border-top: 1px solid #F1E6D9; }
    .fs-primary {
      width: 100%; height: 50px; border: 0; border-radius: 25px; display: flex; align-items: center; justify-content: center; gap: 8px;
      color: #fff; font: inherit; font-size: 17px; font-weight: 800; cursor: pointer;
      background: linear-gradient(90deg, #FF8A12, #F36202); box-shadow: 0 4px 12px rgba(243, 98, 2, .28);
    }
    .fs-primary:disabled { opacity: .6; }
    .fs-ghost { height: 50px; border: 0; border-radius: 25px; padding: 0 22px; font: inherit; font-size: 16px; font-weight: 800; color: #2E1E12; background: #FBF1E4; cursor: pointer; }
    .fs-form { display: flex; flex-direction: column; gap: 14px; }
    .fs-photo { position: relative; width: 108px; height: 108px; margin: 2px auto 0; }
    .fs-photo img { width: 100%; height: 100%; border-radius: 50%; object-fit: cover; background: #fff; border: 4px solid #fff; box-shadow: 0 4px 14px rgba(120, 72, 30, .18); }
    .fs-cam {
      position: absolute; left: 0; bottom: 2px; width: 36px; height: 36px; border-radius: 50%; display: grid; place-items: center;
      background: #3E8E1E; color: #fff; border: 3px solid #fff; font-size: 15px; cursor: pointer; overflow: hidden;
    }
    .fs-cam.busy { opacity: .6; }
    .fs-cam input, .fs-link input { position: absolute; inset: 0; opacity: 0; cursor: pointer; width: 100%; }
    .fs-photo-acts { display: flex; flex-wrap: wrap; justify-content: center; align-items: center; gap: 6px 14px; margin-top: -6px; text-align: center; }
    .fs-photo-acts small { flex-basis: 100%; color: #8A7A6E; font-size: 12px; }
    .fs-link { position: relative; border: 0; background: none; padding: 0; font: inherit; font-size: 14px; font-weight: 800; color: #C45A00; cursor: pointer; }
    .fs-link.danger { color: #B71C1C; }
    .fs-field { display: flex; flex-direction: column; gap: 8px; font-size: 14px; font-weight: 800; color: #4A3A2E; }
    .fs-field .req { color: #D32F2F; }
    .fs-field input {
      height: 48px; padding: 0 14px; border-radius: 14px; border: 1.5px solid #E8D8C5; background: #fff;
      font: inherit; font-size: 16px; font-weight: 600; color: #2E1E12; outline: none; width: 100%;
    }
    .fs-field input:focus { border-color: #F59A3C; box-shadow: 0 0 0 3px rgba(243, 114, 2, .12); }
    .fs-chips { display: flex; flex-wrap: wrap; gap: 8px; }
    .fs-chip {
      height: 38px; padding: 0 16px; border-radius: 19px; border: 1.5px solid #EBDCCB; background: #fff;
      font: inherit; font-size: 15px; font-weight: 700; color: #4A3A2E; cursor: pointer;
    }
    .fs-chip.on { border-color: transparent; color: #fff; background: linear-gradient(90deg, #FF8A12, #F36202); box-shadow: 0 3px 8px rgba(243, 98, 2, .25); }
    .fs-age { margin: -4px 0 0; display: flex; align-items: center; gap: 8px; font-size: 14px; color: #6B5B4E; }
    .fs-age fa-icon { color: #F37202; font-size: 18px; }
    .fs-age b { color: #2E1E12; font-size: 16px; }
    .fs-err { margin: 0; color: #B71C1C; font-size: 14px; font-weight: 700; }
    .fs-err.list { padding: 0 18px 10px; }
    .fs-actions { display: flex; gap: 10px; position: sticky; bottom: -14px; padding: 10px 0 4px; background: #FFFCF8; }
    .fs-actions .fs-primary { flex: 1; }
  `]
})
export class FaFamilySheetComponent implements OnChanges {
  @Input() open = false;
  /** Open straight into the add form. */
  @Input() startAdding = false;
  @Output() closed = new EventEmitter<void>();
  @Output() changed = new EventEmitter<FamilyMember[]>();

  readonly relations = RELATIONS;
  readonly today = new Date().toISOString().slice(0, 10);
  members: FamilyMember[] = [];
  loading = false;
  error = '';
  editing = false;
  editIndex = -1;
  draft: Draft = this.blank();
  formError = '';
  saving = false;
  uploading = false;

  constructor(private api: ApiService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open'] && this.open) {
      this.editing = false;
      this.error = '';
      this.load();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open) this.editing ? this.cancelEdit() : this.close();
  }

  get formTitle(): string {
    return this.editIndex >= 0 ? 'تعديل فرد' : 'إضافة فرد جديد';
  }

  get draftAge(): string {
    const p = ageParts(this.draft.birthDate);
    return p ? ageText(p.years, p.months) : '';
  }

  get draftPicture(): string {
    if (this.draft.imagePath) return pictureUrl(this.draft.imagePath);
    const p = ageParts(this.draft.birthDate);
    const rel = this.draft.relation === 'أخرى' ? this.draft.customRelation : this.draft.relation;
    return familyAvatar(rel, p ? p.years : this.draft.age);
  }

  memberPicture(m: FamilyMember): string {
    return m.imagePath ? pictureUrl(m.imagePath) : (m.avatar || familyAvatar(m.relation, m.age));
  }

  memberAge(m: FamilyMember): string {
    const p = ageParts(m.birthDate);
    return p ? ageText(p.years, p.months) : ageText(m.age);
  }

  prettyDate(iso: string): string {
    const d = new Date(iso.slice(0, 10) + 'T00:00:00');
    return isNaN(d.getTime()) ? '' : d.toLocaleDateString('ar-EG', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  fallback(event: Event, m: FamilyMember): void {
    const img = event.target as HTMLImageElement;
    const auto = m.avatar || familyAvatar(m.relation, m.age);
    if (!img.src.endsWith(auto)) img.src = auto;
  }

  close(): void {
    this.editing = false;
    this.closed.emit();
  }

  add(): void {
    this.editIndex = -1;
    this.draft = this.blank();
    this.formError = '';
    this.editing = true;
  }

  edit(i: number): void {
    const m = this.members[i];
    const known = RELATIONS.includes(m.relation || '');
    this.editIndex = i;
    this.draft = {
      name: m.name || '',
      relation: m.relation ? (known ? m.relation : 'أخرى') : '',
      customRelation: m.relation && !known ? m.relation : '',
      birthDate: m.birthDate ? m.birthDate.slice(0, 10) : '',
      imagePath: m.imagePath || '',
      age: m.age
    };
    this.formError = '';
    this.editing = true;
  }

  cancelEdit(): void {
    this.editing = false;
    this.formError = '';
  }

  pickPhoto(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    if (!file.type.startsWith('image/')) { this.formError = 'اختار صورة'; return; }
    this.uploading = true;
    this.formError = '';
    squareCrop(file, 512).then(cropped => {
      this.api.uploadImage(cropped).subscribe({
        next: path => { this.draft.imagePath = path; this.uploading = false; },
        error: (e: Error) => { this.formError = e.message; this.uploading = false; }
      });
    });
  }

  saveDraft(): void {
    const name = this.draft.name.trim();
    if (!name) { this.formError = 'اكتب الاسم'; return; }
    if (this.draft.birthDate && this.draft.birthDate > this.today) { this.formError = 'تاريخ الميلاد لا يمكن أن يكون في المستقبل'; return; }
    const relation = this.draft.relation === 'أخرى' ? (this.draft.customRelation.trim() || 'أخرى') : this.draft.relation;
    const p = ageParts(this.draft.birthDate);
    const member: FamilyMember = {
      id: this.editIndex >= 0 ? this.members[this.editIndex].id : '',
      name,
      relation: relation || null,
      birthDate: this.draft.birthDate || null,
      imagePath: this.draft.imagePath || null,
      age: p ? p.years : this.draft.age
    };
    const next = [...this.members];
    if (this.editIndex >= 0) next[this.editIndex] = member; else next.push(member);
    this.persist(next, () => this.editing = false, msg => this.formError = msg);
  }

  remove(i: number): void {
    const m = this.members[i];
    if (this.members.length <= 1) { this.error = 'لازم يفضل فرد واحد على الأقل في الأسرة'; return; }
    if (!confirm(`حذف ${m.name} من أفراد الأسرة؟`)) return;
    this.persist(this.members.filter((_, k) => k !== i), () => {}, msg => this.error = msg);
  }

  private persist(list: FamilyMember[], done: () => void, fail: (msg: string) => void): void {
    this.saving = true;
    this.error = '';
    const body: FamilyMemberInput[] = list.map(m => ({
      name: m.name || '', age: m.age, birthDate: m.birthDate || null, relation: m.relation || null, imagePath: m.imagePath || null
    }));
    this.api.saveFamily(body).subscribe({
      next: saved => {
        this.saving = false;
        this.members = saved;
        this.changed.emit(saved);
        done();
      },
      error: (e: Error) => { this.saving = false; fail(e.message); }
    });
  }

  private load(): void {
    this.loading = true;
    this.api.family().subscribe({
      next: rows => {
        this.members = rows;
        this.loading = false;
        if (this.startAdding) this.add();
      },
      error: (e: Error) => { this.loading = false; this.error = e.message; }
    });
  }

  private blank(): Draft {
    return { name: '', relation: '', customRelation: '', birthDate: '', imagePath: '', age: 30 };
  }
}

/** Centred square crop, scaled down to `size` px, as a JPEG file (keeps uploads small and round previews tidy). */
export function squareCrop(file: File, size: number): Promise<File> {
  return new Promise(resolve => {
    const url = URL.createObjectURL(file);
    const img = new Image();
    img.onload = () => {
      const side = Math.min(img.naturalWidth, img.naturalHeight);
      const out = Math.min(size, side);
      const canvas = document.createElement('canvas');
      canvas.width = canvas.height = out;
      const ctx = canvas.getContext('2d');
      if (!ctx) { URL.revokeObjectURL(url); resolve(file); return; }
      ctx.drawImage(img, (img.naturalWidth - side) / 2, (img.naturalHeight - side) / 2, side, side, 0, 0, out, out);
      canvas.toBlob(blob => {
        URL.revokeObjectURL(url);
        resolve(blob ? new File([blob], 'photo.jpg', { type: 'image/jpeg' }) : file);
      }, 'image/jpeg', 0.88);
    };
    img.onerror = () => { URL.revokeObjectURL(url); resolve(file); };
    img.src = url;
  });
}
