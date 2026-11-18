import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { DraftService } from '../../../core/draft.service';
import { ApiService } from '../../../core/api.service';
import { mediaUrl } from '../../../core/environment';
import { purchaseText } from '../../../core/purchase';
import {
  OrderHistoryCounts,
  OrderHistoryFilter,
  OrderHistoryItem,
  OrderHistoryState,
  ShoppingLine
} from '../../../core/models';
import { FaScreenHeaderComponent } from '../shared/screen-header.component';
import { FaTabBarComponent } from '../shared/tab-bar.component';
import { FaIconComponent } from '../shared/icon.component';
import { FaSearchBarComponent } from '../shared/search-bar.component';
import { egp } from '../../../core/money';

interface StateChip { state: OrderHistoryState | ''; label: string; icon: string; key: keyof OrderHistoryCounts; }

interface OrderCardView extends OrderHistoryItem {
  open?: boolean;
  lines?: ShoppingLine[] | null;
  linesLoading?: boolean;
}

const PAGE_SIZE = 20;

/** الطلبات القديمة: confirmed orders from the server, newest first, 20 per page with infinite scroll. */
@Component({
  selector: 'app-orders-screen',
  imports: [RouterLink, NgTemplateOutlet, FaScreenHeaderComponent, FaTabBarComponent, FaIconComponent, FaSearchBarComponent],
  template: `
    <div class="fa-screen" dir="rtl">
      <fa-screen-header title="طلبات قديمة" art="assets/screens/art/orders.svg" titleIcon="receipt" subtitle="راجع طلباتك السابقة وأعد طلبها بسهولة" [height]="210" [titleY]="157" [backY]="120" />

      <fa-search-bar style="margin: calc(8 * var(--s)) calc(36 * var(--s)) 0 calc(38 * var(--s))" placeholder="ابحث باسم الطلب أو رقمه أو أكلة..."
                     [value]="text" [panel]="false" [active]="advanced || hasAdvanced"
                     (valueChange)="onText($event)" (advanced)="advanced = !advanced" />

      @if (advanced) {
        <div class="adv">
          <label class="adv-field"><span>من تاريخ</span>
            <input type="date" [value]="from" (change)="from = $any($event.target).value">
          </label>
          <label class="adv-field"><span>إلى تاريخ</span>
            <input type="date" [value]="to" (change)="to = $any($event.target).value">
          </label>
          <label class="adv-field wide"><span>اسم الطلب</span>
            <input type="text" placeholder="مثلاً: عزومة الجمعة" [value]="name" (input)="name = $any($event.target).value" (keydown.enter)="applyAdvanced()">
          </label>
          <label class="adv-field wide"><span>الحالة</span>
            <select [value]="state" (change)="state = $any($event.target).value">
              @for (chip of chips; track chip.state) {
                <option [value]="chip.state" [selected]="chip.state === state">{{ chip.label }}</option>
              }
            </select>
          </label>
          <label class="adv-field wide"><span>اسم أكلة في الطلب</span>
            <input type="text" placeholder="مثلاً: محشي" [value]="dish" (input)="dish = $any($event.target).value" (keydown.enter)="applyAdvanced()">
          </label>
          <div class="adv-actions">
            <button type="button" class="adv-apply" (click)="applyAdvanced()"><fa-icon name="search" [weight]="2.4" />بحث</button>
            <button type="button" class="adv-clear" (click)="clearAdvanced()">مسح</button>
          </div>
        </div>
      }

      <div class="fa-chips chips">
        @for (chip of chips; track chip.state) {
          <button type="button" class="fa-chip" [class.on]="chip.state === state" (click)="selectState(chip.state)">
            <fa-icon [name]="chip.icon" [weight]="2" />{{ chip.label }}@if (counts) { ({{ counts[chip.key] ?? 0 }})}
          </button>
        }
      </div>

      <div class="cards">
        @for (o of orders; track o.id) {
          <article class="card" [class.open]="o.open">
            <div class="head" role="button" tabindex="0" (click)="toggle(o)" (keydown.enter)="toggle(o)" [attr.aria-expanded]="!!o.open">
              <fa-icon class="rcpt" name="receipt" [weight]="2" />
              <span class="titles">
                @if (editingId === o.id) {
                  <span class="rename" (click)="$event.stopPropagation()">
                    <input #nameBox class="rename-input" type="text" maxlength="150" [value]="nameDraft"
                           (input)="nameDraft = nameBox.value" (keydown.enter)="saveName(o)" (keydown.escape)="editingId = ''"
                           aria-label="اسم الطلب">
                    <button type="button" class="rn-btn ok" (click)="saveName(o)" [disabled]="savingName" aria-label="حفظ الاسم"><fa-icon name="check" [weight]="3" /></button>
                    <button type="button" class="rn-btn" (click)="editingId = ''" aria-label="إلغاء"><fa-icon name="x" [weight]="3" /></button>
                  </span>
                } @else {
                  <span class="name-line">
                    <b class="code">{{ o.name || ('طلب # ' + (o.code || '')) }}</b>
                    <button type="button" class="rn-btn pen" (click)="editName(o, $event)" aria-label="تعديل اسم الطلب"><fa-icon name="pencil" [filled]="true" /></button>
                  </span>
                }
                <span class="date">{{ dateLabel(o.date) }} · {{ o.dishCount }} أكلة · {{ o.lineCount }} مكون</span>
                @if (o.total) { <span class="o-total">{{ money(o.total) }}</span> }
              </span>
              <span class="status" [class]="'status ' + o.state"><fa-icon [name]="stateIcon(o.state)" [weight]="2.6" />{{ stateLabel(o.state) }}</span>
              <span class="toggle"><fa-icon [name]="o.open ? 'chevronUp' : 'chevronDown'" [weight]="2.6" /></span>
            </div>

            <ng-template #quickButtons>
              <div class="quick">
              <button type="button" class="q-print" (click)="print(o)"><fa-icon name="printer" [weight]="2.2" />طباعة</button>
              <button type="button" class="q-share" (click)="share(o)"><fa-icon name="share" [weight]="2.2" />مشاركة</button>
              <button type="button" class="q-again" (click)="reorder(o)" [disabled]="!!reordering">
                <fa-icon name="refresh" [weight]="2.4" />{{ reordering === o.id ? 'جاري الإضافة...' : 'إعادة طلبه' }}
              </button>
              </div>
            </ng-template>

            @if (!o.open) { <ng-container [ngTemplateOutlet]="quickButtons" /> }

            @if (o.open) {
              <div class="inner">
                <section class="blk">
                  <header class="blk-head">
                    <span class="blk-ico"><fa-icon name="cloche" [weight]="2" /></span>
                    <h4>الأكلات</h4>
                    <span class="blk-count">{{ o.dishes.length }}</span>
                  </header>
                  <div class="dish-grid">
                    @for (d of o.dishes; track d.itemId) {
                      <div class="dish">
                        <img [src]="dishImage(d.imagePath)" [alt]="d.name || ''" loading="lazy">
                        <span class="dish-x">{{ d.repeatCount }}</span>
                        <span class="dish-name">{{ d.name }}</span>
                        @if (d.summary) { <span class="dish-desc">{{ d.summary }}</span> }
                      </div>
                    } @empty {
                      <p class="muted">لا توجد أكلات.</p>
                    }
                  </div>
                </section>

                <section class="blk">
                  <header class="blk-head green">
                    <span class="blk-ico"><fa-icon name="layers" [weight]="2" /></span>
                    <h4>المكونات</h4>
                    @if (o.lines?.length) { <span class="blk-count">{{ o.lines!.length }}</span> }
                  </header>
                  @if (o.linesLoading) {
                    <p class="muted">جاري التحميل...</p>
                  } @else {
                    @for (g of placeGroups(o.lines); track g.place) {
                      <div class="place-tag"><fa-icon name="cart" [weight]="2.2" />{{ g.place }}</div>
                      <div class="ing-grid">
                        @for (l of g.lines; track l.contentId + ':' + l.unitId) {
                          <div class="ing"><span class="ing-name">{{ l.name }}</span><b class="ing-qty">{{ qty(l) }}</b></div>
                        }
                      </div>
                    } @empty {
                      <p class="muted">لا توجد مكونات.</p>
                    }
                  }
                </section>
              </div>
              <div class="actions">
                <a class="details" [routerLink]="['/orders', o.id]"><fa-icon name="doc" [weight]="2" />عرض التفاصيل</a>
              </div>
              <div class="quick-bottom"><ng-container [ngTemplateOutlet]="quickButtons" /></div>
            }
          </article>
        }

        @if (toast) { <div class="toast" role="status">{{ toast }}</div> }
        @if (loading) {
          @for (s of skeletons; track s) { <div class="card skel"></div> }
        } @else if (error) {
          <div class="empty err">{{ error }} <button type="button" class="retry" (click)="reload()">إعادة المحاولة</button></div>
        } @else if (!orders.length) {
          <div class="empty">{{ filtered ? 'لا توجد طلبات مطابقة للبحث.' : 'لا توجد طلبات قديمة بعد.' }}</div>
        }
        @if (loadingMore) { <div class="more">جاري تحميل المزيد...</div> }
        @if (!loading && !hasMore && orders.length > 0) { <div class="more end">عرضنا كل الطلبات ({{ total }})</div> }
      </div>
      <fa-tab-bar />
    </div>
  `,
  styles: [`
    .sq { width: calc(92 * var(--s)); height: calc(82 * var(--s)); border-radius: calc(26 * var(--s)) !important; }
    .sq.on { background: linear-gradient(180deg, #FF8A12, #F36202) !important; color: #fff !important; }

    .adv {
      display: grid; grid-template-columns: 1fr 1fr; gap: calc(14 * var(--s));
      margin: calc(18 * var(--s)) calc(36 * var(--s)) 0 calc(38 * var(--s)); padding: calc(20 * var(--s));
      border-radius: calc(26 * var(--s)); background: #FFFCF9; border: calc(1.5 * var(--s)) solid #F3E8DC;
      box-shadow: 0 calc(5 * var(--s)) calc(16 * var(--s)) rgba(120, 72, 30, .08);
    }
    .adv-field { display: flex; flex-direction: column; gap: calc(8 * var(--s)); min-width: 0; }
    .adv-field.wide { grid-column: 1 / -1; }
    .adv-field span { font-size: calc(22 * var(--s)); font-weight: 800; color: #2E1E12; }
    .adv-field input, .adv-field select {
      height: calc(64 * var(--s)); padding: 0 calc(16 * var(--s)); min-width: 0;
      border: calc(2 * var(--s)) solid #F3DCC4; border-radius: calc(18 * var(--s)); outline: none;
      background: #fff; font-family: inherit; font-size: calc(24 * var(--s)); color: #2E1E12;
    }
    .adv-field input:focus, .adv-field select:focus { border-color: #F26A0A; }
    .adv-actions { grid-column: 1 / -1; display: flex; gap: calc(14 * var(--s)); }
    .adv-actions button {
      height: calc(64 * var(--s)); border-radius: calc(32 * var(--s)) !important; padding: 0 calc(28 * var(--s));
      display: flex; align-items: center; justify-content: center; gap: calc(10 * var(--s));
      font-family: inherit; font-size: calc(24 * var(--s)); font-weight: 800; cursor: pointer;
    }
    .adv-apply { flex: 1; background: linear-gradient(90deg, #FF8A12, #F36202) !important; color: #fff !important; }
    .adv-apply fa-icon { font-size: calc(30 * var(--s)); }
    .adv-clear { background: #FBF1E4 !important; color: #8A5A2B !important; }

    .chips { margin: calc(23 * var(--s)) calc(33 * var(--s)) 0 calc(33 * var(--s)) !important; justify-content: flex-start !important; }
    .chips .fa-chip { height: calc(64 * var(--s)); }

    .cards { display: flex; flex-direction: column; gap: calc(17 * var(--s)); margin: calc(28 * var(--s)) calc(24 * var(--s)) 0 calc(25 * var(--s)); }
    .card {
      border-radius: calc(28 * var(--s)); background: #FFFCF9; border: calc(1.5 * var(--s)) solid #F3E8DC;
      box-shadow: 0 calc(5 * var(--s)) calc(16 * var(--s)) rgba(120, 72, 30, .08);
    }
    .card.open { border: calc(2 * var(--s)) solid #6EA43C; border-radius: calc(30 * var(--s)); padding-bottom: calc(20 * var(--s)); }
    .head {
      width: 100%; min-height: calc(102 * var(--s)); padding: calc(12 * var(--s)) calc(22 * var(--s)) calc(12 * var(--s)) calc(20 * var(--s)) !important;
      display: flex; align-items: center; gap: calc(16 * var(--s));
      background: none !important; border: 0 !important; cursor: pointer; text-align: right; font-family: inherit; color: inherit;
    }
    .rcpt { flex: none; font-size: calc(44 * var(--s)); color: #F37202; }
    .titles { flex: 1 1 auto; min-width: 0; display: flex; flex-direction: column; gap: calc(4 * var(--s)); }
    .code { font-size: calc(28 * var(--s)); font-weight: 800; color: #2E1E12; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .o-total {
      align-self: flex-start; margin-top: calc(6 * var(--s)); padding: 0 calc(12 * var(--s)); height: calc(32 * var(--s));
      display: inline-flex; align-items: center; border-radius: calc(16 * var(--s));
      background: #EAF4DE; border: calc(1.2 * var(--s)) solid #CFE3B6; color: #2F6A16; font-size: calc(20 * var(--s)); font-weight: 800; white-space: nowrap;
    }
    .date { font-size: calc(21 * var(--s)); font-weight: 500; color: #6B625C; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .status {
      flex: none; height: calc(46 * var(--s)); padding: 0 calc(18 * var(--s));
      display: flex; align-items: center; gap: calc(8 * var(--s)); border-radius: calc(23 * var(--s));
      font-size: calc(21 * var(--s)); font-weight: 800; white-space: nowrap;
    }
    .status fa-icon { font-size: calc(26 * var(--s)); }
    .status.created { background: #FDE9B8; color: #B86E04; }
    .status.printed { background: #E4F0D2; color: #2F7D1E; }
    .status.reordered { background: #DDEBFB; color: #1E5C9A; }
    .status.shared { background: #EFE3FA; color: #6B3FA0; }
    .status.reprinted { background: #D5EFE6; color: #16775A; }
    .status.reshared { background: #F8E0EE; color: #A0306E; }
    .toggle {
      flex: none; width: calc(50 * var(--s)); height: calc(50 * var(--s)); border-radius: 50%;
      background: #FBF1E4; color: #2E1E12; display: grid; place-items: center; font-size: calc(30 * var(--s));
    }

    .inner { margin: 0 calc(20 * var(--s)); display: flex; flex-direction: column; gap: calc(16 * var(--s)); }
    .blk {
      padding: calc(16 * var(--s)) calc(16 * var(--s)) calc(18 * var(--s));
      border-radius: calc(24 * var(--s)); background: #fff; border: calc(1.5 * var(--s)) solid #F3E6D8;
      box-shadow: 0 calc(3 * var(--s)) calc(10 * var(--s)) rgba(120, 72, 30, .05);
    }
    .blk-head { display: flex; align-items: center; gap: calc(12 * var(--s)); margin-bottom: calc(14 * var(--s)); }
    .blk-head h4 { margin: 0; font-size: calc(26 * var(--s)); font-weight: 800; color: #2E1E12; }
    .blk-ico {
      width: calc(48 * var(--s)); height: calc(48 * var(--s)); border-radius: calc(14 * var(--s)); flex: none;
      display: grid; place-items: center; font-size: calc(30 * var(--s));
      background: #FDEBDB; color: #F26A0A;
    }
    .blk-head.green .blk-ico { background: #E6F0D3; color: #3E8E1E; }
    .blk-count {
      margin-right: auto; min-width: calc(40 * var(--s)); height: calc(34 * var(--s)); padding: 0 calc(10 * var(--s));
      border-radius: calc(17 * var(--s)); display: grid; place-items: center;
      background: #F26A0A; color: #fff; font-size: calc(20 * var(--s)); font-weight: 800;
    }
    .blk-head.green .blk-count { background: #5AA22B; }

    .dish-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: calc(12 * var(--s)); }
    .dish {
      position: relative; display: flex; flex-direction: column; gap: calc(8 * var(--s));
      padding: calc(8 * var(--s)); border-radius: calc(18 * var(--s)); background: #FFF8F1; border: calc(1.5 * var(--s)) solid #F7E3CE;
    }
    .dish img { width: 100%; aspect-ratio: 4 / 3; object-fit: cover; border-radius: calc(12 * var(--s)); display: block; }
    .dish-x {
      position: absolute; left: calc(14 * var(--s)); top: calc(14 * var(--s));
      min-width: calc(36 * var(--s)); height: calc(36 * var(--s)); padding: 0 calc(8 * var(--s)); border-radius: calc(18 * var(--s)); box-sizing: border-box;
      display: grid; place-items: center; background: #F26A0A; color: #fff;
      font-size: calc(19 * var(--s)); font-weight: 800; direction: ltr;
      box-shadow: 0 calc(3 * var(--s)) calc(8 * var(--s)) rgba(243, 94, 2, .3);
    }
    .dish-name {
      text-align: center; font-size: calc(21 * var(--s)); font-weight: 800; color: #2E1E12; line-height: 1.25;
      display: -webkit-box; -webkit-box-orient: vertical; -webkit-line-clamp: 2; overflow: hidden;
    }
    .dish-desc {
      margin-top: calc(-4 * var(--s)); text-align: center; font-size: calc(17 * var(--s)); font-weight: 500; color: #7A6658; line-height: 1.25;
      display: -webkit-box; -webkit-box-orient: vertical; -webkit-line-clamp: 2; overflow: hidden;
    }

    .place-tag {
      display: inline-flex; align-items: center; gap: calc(8 * var(--s));
      height: calc(34 * var(--s)); padding: 0 calc(14 * var(--s)); margin: calc(4 * var(--s)) 0 calc(10 * var(--s));
      border-radius: calc(17 * var(--s)); background: #EEF5E2; color: #3E7B22; font-size: calc(19 * var(--s)); font-weight: 800;
    }
    .place-tag fa-icon { font-size: calc(22 * var(--s)); }
    .ing-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: calc(10 * var(--s)); margin-bottom: calc(8 * var(--s)); }
    .ing {
      display: flex; align-items: center; justify-content: space-between; gap: calc(8 * var(--s));
      min-height: calc(54 * var(--s)); padding: 0 calc(10 * var(--s)) 0 calc(8 * var(--s));
      border-radius: calc(16 * var(--s)); background: #FBF7F2; border: calc(1.5 * var(--s)) solid #F1E6DA;
    }
    .ing-name { min-width: 0; font-size: calc(21 * var(--s)); font-weight: 700; color: #2E1E12; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .ing-qty {
      flex: none; height: calc(34 * var(--s)); padding: 0 calc(10 * var(--s)); border-radius: calc(17 * var(--s));
      display: grid; place-items: center; background: #FDEBDB; color: #E0640A;
      font-size: calc(19 * var(--s)); font-weight: 800; white-space: nowrap;
    }
    .muted { margin: 0; padding: calc(8 * var(--s)) 0; color: #8A7B6E; font-size: calc(22 * var(--s)); }

    .actions { display: flex; gap: calc(16 * var(--s)); margin: calc(18 * var(--s)) calc(22 * var(--s)) 0; }
    .actions a, .actions button {
      flex: 1 1 0; height: calc(62 * var(--s)); border-radius: calc(31 * var(--s)) !important; display: flex; align-items: center;
      justify-content: center; gap: calc(12 * var(--s)); font-family: inherit; font-size: calc(25 * var(--s)); font-weight: 800;
      text-decoration: none; cursor: pointer; white-space: nowrap;
    }
    .actions fa-icon { font-size: calc(34 * var(--s)); }
    .name-line { display: flex; align-items: center; gap: calc(10 * var(--s)); min-width: 0; }
    .rn-btn {
      flex: none; width: calc(44 * var(--s)); height: calc(44 * var(--s)); padding: 0 !important;
      border: 0 !important; border-radius: 50% !important; cursor: pointer; display: grid; place-items: center;
      background: #FDEBDB !important; color: #F26A0A !important; font-size: calc(24 * var(--s));
    }
    .rn-btn.ok { background: #F26A0A !important; color: #fff !important; }
    .rn-btn:disabled { opacity: .6; }
    .rename { display: flex; align-items: center; gap: calc(8 * var(--s)); min-width: 0; }
    .rename-input {
      flex: 1 1 auto; min-width: 0; height: calc(52 * var(--s)); padding: 0 calc(14 * var(--s));
      border: calc(2 * var(--s)) solid #F7B98A; border-radius: calc(26 * var(--s)); outline: none;
      font-family: inherit; font-size: calc(24 * var(--s)); font-weight: 700; color: #2E1E12; background: #FFFDFA;
    }
    .rename-input:focus { border-color: #F26A0A; }
    .quick-bottom { margin-top: calc(16 * var(--s)); }
    .quick-bottom .quick { margin-bottom: 0; }
    .quick { display: flex; gap: calc(12 * var(--s)); margin: 0 calc(20 * var(--s)) calc(16 * var(--s)); }
    .quick button {
      flex: 1 1 0; height: calc(56 * var(--s)); border-radius: calc(28 * var(--s)) !important; padding: 0 !important;
      display: flex; align-items: center; justify-content: center; gap: calc(8 * var(--s));
      font-family: inherit; font-size: calc(22 * var(--s)); font-weight: 800; cursor: pointer; white-space: nowrap;
    }
    .quick fa-icon { font-size: calc(28 * var(--s)); }
    .q-print { background: linear-gradient(90deg, #FF8A12, #F36202) !important; color: #fff !important; }
    .q-share { background: #FBF1E4 !important; color: #E0640A !important; }
    .q-again { background: #FFF8F1 !important; color: #E0640A !important; border: calc(2 * var(--s)) solid #E0782A !important; }
    .q-again:disabled { opacity: .6; cursor: wait; }
    .toast {
      position: fixed; left: 50%; bottom: calc(300 * var(--s)); transform: translateX(-50%); z-index: 50;
      padding: calc(14 * var(--s)) calc(26 * var(--s)); border-radius: calc(26 * var(--s));
      background: #2E1E12; color: #fff; font-size: calc(22 * var(--s)); font-weight: 700; white-space: nowrap;
    }
    .details { background: #E7F3D6; color: #2F7D1E; }
    .again { background: #FFF8F1 !important; color: #E0640A !important; border: calc(2 * var(--s)) solid #E0782A !important; }
    .again:disabled { opacity: .6; cursor: wait; }

    .skel {
      height: calc(102 * var(--s));
      background: linear-gradient(90deg, #F6ECE1 25%, #FBF4EC 50%, #F6ECE1 75%); background-size: 200% 100%;
      animation: or-shimmer 1.2s linear infinite;
    }
    @keyframes or-shimmer { from { background-position: 200% 0; } to { background-position: 0 0; } }
    .empty { text-align: center; padding: calc(40 * var(--s)) calc(20 * var(--s)); color: #8A6A4A; font-size: calc(26 * var(--s)); font-weight: 700; }
    .empty.err { color: #C0392B; }
    .retry { margin-right: calc(10 * var(--s)); background: none !important; border: 0; color: #F26A0A !important; font: inherit; text-decoration: underline; cursor: pointer; }
    .more { text-align: center; padding: calc(18 * var(--s)) 0; color: #8A6A4A; font-size: calc(22 * var(--s)); font-weight: 700; }
    .more.end { color: #B0A090; font-weight: 500; }
  `]
})
export class OrdersScreenComponent implements OnInit, OnDestroy {
  money = egp;

  readonly chips: StateChip[] = [
    { state: '', label: 'الكل', icon: 'grid', key: 'all' },
    { state: 'created', label: 'إنشاء بدون طباعة', icon: 'doc', key: 'created' },
    { state: 'printed', label: 'تم الطباعة', icon: 'printer', key: 'printed' },
    { state: 'reordered', label: 'أعيد طلبه', icon: 'refresh', key: 'reordered' },
    { state: 'shared', label: 'تمت المشاركة', icon: 'share', key: 'shared' },
    { state: 'reprinted', label: 'إعادة الطباعة', icon: 'printer', key: 'reprinted' },
    { state: 'reshared', label: 'إعادة المشاركة', icon: 'share', key: 'reshared' }
  ];
  readonly skeletons = [1, 2, 3, 4];

  orders: OrderCardView[] = [];
  counts: OrderHistoryCounts | null = null;
  total = 0;
  hasMore = false;
  loading = false;
  loadingMore = false;
  error = '';
  reordering = '';

  text = '';
  state: OrderHistoryState | '' = '';
  advanced = false;
  from = '';
  to = '';
  dish = '';
  name = '';
  private applied: OrderHistoryFilter = {};

  private searchTimer = 0;
  private request?: Subscription;
  private generation = 0;

  editingId = '';
  nameDraft = '';
  savingName = false;
  toast = '';
  private toastTimer = 0;

  constructor(private api: ApiService, private draft: DraftService, private router: Router) {}

  get hasAdvanced(): boolean {
    return !!(this.applied.from || this.applied.to || this.applied.dish || this.applied.name);
  }

  get filtered(): boolean {
    return !!(this.applied.text || this.applied.state || this.hasAdvanced);
  }

  ngOnInit(): void {
    this.reload();
  }

  ngOnDestroy(): void {
    window.clearTimeout(this.searchTimer);
    this.request?.unsubscribe();
  }

  onText(value: string): void {
    this.text = value;
    window.clearTimeout(this.searchTimer);
    this.searchTimer = window.setTimeout(() => this.reload(), 350);
  }

  selectState(state: OrderHistoryState | ''): void {
    if (this.state === state) return;
    this.state = state;
    this.reload();
  }

  applyAdvanced(): void {
    if (this.from && this.to && this.from > this.to) [this.from, this.to] = [this.to, this.from];
    this.reload();
  }

  clearAdvanced(): void {
    this.from = '';
    this.to = '';
    this.dish = '';
    this.name = '';
    this.state = '';
    this.reload();
  }

  reload(): void {
    window.clearTimeout(this.searchTimer);
    this.applied = { text: this.text.trim(), state: this.state, from: this.from, to: this.to, dish: this.dish.trim(), name: this.name.trim() };
    this.orders = [];
    this.hasMore = false;
    this.loadPage(true);
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    if (!this.hasMore || this.loading || this.loadingMore) return;
    const doc = document.documentElement;
    const remaining = doc.scrollHeight - (window.scrollY + window.innerHeight);
    if (remaining < 320) this.loadPage(false);
  }

  private loadPage(first: boolean): void {
    this.request?.unsubscribe();
    const generation = ++this.generation;
    if (first) this.loading = true; else this.loadingMore = true;
    this.error = '';
    this.request = this.api.orderHistory(this.applied, first ? 0 : this.orders.length, PAGE_SIZE).subscribe({
      next: page => {
        if (generation !== this.generation) return;
        const known = new Set(this.orders.map(o => o.id));
        this.orders = [...this.orders, ...(page.items || []).filter(o => !known.has(o.id))];
        this.counts = page.counts;
        this.total = page.total;
        this.hasMore = page.hasMore;
        this.loading = false;
        this.loadingMore = false;
        // A short first page may not fill the screen: keep loading until it scrolls.
        setTimeout(() => this.onWindowScroll());
      },
      error: (err: Error) => {
        if (generation !== this.generation) return;
        this.error = err.message || 'تعذر تحميل الطلبات';
        this.loading = false;
        this.loadingMore = false;
      }
    });
  }

  toggle(o: OrderCardView): void {
    o.open = !o.open;
    if (o.open && o.lines === undefined) {
      o.linesLoading = true;
      this.api.order(o.id).subscribe({
        next: detail => {
          o.lines = (detail.lines || []).filter(line => line.quantity > 0);
          o.linesLoading = false;
        },
        error: () => { o.lines = null; o.linesLoading = false; }
      });
    }
  }

  editName(o: OrderCardView, event: Event): void {
    event.stopPropagation();
    this.editingId = o.id;
    this.nameDraft = o.name || '';
    setTimeout(() => (document.querySelector('.rename-input') as HTMLInputElement | null)?.select());
  }

  saveName(o: OrderCardView): void {
    const name = this.nameDraft.trim();
    if (this.savingName) return;
    if (!name) { this.showToast('اكتب اسم الطلب'); return; }
    if (name === (o.name || '')) { this.editingId = ''; return; }
    this.savingName = true;
    this.api.renameOrder(o.id, name).subscribe({
      next: () => {
        o.name = name;
        this.editingId = '';
        this.savingName = false;
        this.showToast('تم حفظ الاسم');
      },
      error: (err: Error) => {
        this.savingName = false;
        this.showToast(err?.message || 'تعذر حفظ الاسم');
      }
    });
  }

  /** Opens the purchase order and prints it (that also marks it as printed). */
  print(o: OrderCardView): void {
    this.router.navigate(['/orders', o.id], { queryParams: { print: 1 } });
  }

  share(o: OrderCardView): void {
    this.api.markOrderShared(o.id).subscribe({ next: () => (o.state = o.sharedAt || o.state === 'shared' || o.state === 'reshared' ? 'reshared' : 'shared'), error: () => undefined });
    const title = o.name || 'طلب الشراء';
    const url = `${window.location.origin}/orders/${o.id}`;
    if (navigator.share) {
      navigator.share({ title, url }).catch(() => undefined);
      return;
    }
    if (navigator.clipboard?.writeText) {
      navigator.clipboard.writeText(url).then(() => this.showToast('تم نسخ رابط الطلب'), () => undefined);
    }
  }

  /**
   * إعادة طلبه: add this order's meals and ingredients to the current selection
   * (no duplicates, quantities added), mark it "أعيد طلبه" and open طلباتي.
   */
  reorder(o: OrderCardView): void {
    if (this.reordering) return;
    this.reordering = o.id;
    this.draft.mergeOrder(o.id).subscribe({
      next: () => {
        this.api.markOrderReordered(o.id).subscribe({ error: () => undefined });
        o.state = 'reordered';
        this.reordering = '';
        this.router.navigateByUrl('/ingredients');
      },
      error: (err: Error) => {
        this.reordering = '';
        this.showToast(err?.message || 'تعذر إعادة الطلب');
      }
    });
  }

  private showToast(text: string): void {
    this.toast = text;
    window.clearTimeout(this.toastTimer);
    this.toastTimer = window.setTimeout(() => (this.toast = ''), 1500);
  }

  /** Ingredients grouped by where they are bought (البقالة، الخضارجي...). */
  placeGroups(lines?: ShoppingLine[] | null): { place: string; lines: ShoppingLine[] }[] {
    const map = new Map<string, ShoppingLine[]>();
    for (const line of lines || []) {
      const place = (line.purchasePlaceName || '').trim() || 'البقالة';
      if (!map.has(place)) map.set(place, []);
      map.get(place)!.push(line);
    }
    return [...map.entries()].map(([place, rows]) => ({ place, lines: rows }));
  }

  stateLabel(state: OrderHistoryState): string {
    return state === 'printed' ? 'تم الطباعة' : state === 'reordered' ? 'أعيد طلبه' : state === 'shared' ? 'تمت المشاركة' : state === 'reprinted' ? 'إعادة الطباعة' : state === 'reshared' ? 'إعادة المشاركة' : 'بدون طباعة';
  }

  stateIcon(state: OrderHistoryState): string {
    return state === 'printed' ? 'printer' : state === 'reordered' ? 'refresh' : state === 'shared' || state === 'reshared' ? 'share' : state === 'reprinted' ? 'printer' : 'doc';
  }

  dateLabel(value: string): string {
    const day = (value || '').slice(0, 10);
    const [y, m, d] = day.split('-');
    return y && m && d ? `${+d}/${+m}/${y}` : day;
  }

  dishImage(path?: string | null): string {
    return mediaUrl(path) || 'assets/screens/foods/koshari.png';
  }

  qty(line: ShoppingLine): string {
    return purchaseText(line);
  }

}
