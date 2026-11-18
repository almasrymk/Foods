import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from './environment';
import { PriceCountry,
  ApiResult,
  AuthUser,
  ItemCard,
  CustomerPrices,
  ContentPriceRow,
  ItemDetail,
  MyItemRecipe,
  MyLineInput,
  ContentPickResult,
  FamilyMember,
  FamilyMemberInput,
  MealCard,
  MealItemFilter,
  OrderDetail,
  OrderListItem,
  Profile,
  SelectedItem,
  ShoppingLine,
  OrderHistoryFilter,
  OrderHistoryPage,
  UserSettings,
  NotificationPage, ItemPart } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private http: HttpClient) {}

  login(userName: string, password: string): Observable<AuthUser> {
    return this.post<AuthUser>('/Auth/Login', { userName, password });
  }

  register(name: string, userName: string, password: string, countryId: string, family: FamilyMemberInput[]): Observable<AuthUser> {
    return this.post<AuthUser>('/Auth/Register', { name, userName, password, imagePath: null, countryId, family });
  }

  countries(): Observable<{ id: string; name?: string | null; dialect?: string | null }[]> {
    return this.get('/Auth/Countries');
  }

  profile(): Observable<Profile> {
    return this.get<Profile>('/customer/profile');
  }

  saveCountry(countryId: string): Observable<Profile> {
    return this.http.put<ApiResult<Profile>>(`${environment.apiUrl}/customer/country`, { countryId }).pipe(
      map(result => this.unwrap(result)),
      catchError(error => this.fail(error))
    );
  }

  meals(): Observable<MealCard[]> {
    return this.get<MealCard[]>('/customer/meals');
  }

  mealFilters(mealId: string): Observable<MealItemFilter[]> {
    return this.get<MealItemFilter[]>(`/customer/meals/${mealId}/filters`);
  }

  cuisines(): Observable<MealItemFilter[]> {
    return this.get<MealItemFilter[]>('/customer/cuisines');
  }

  categories(): Observable<MealItemFilter[]> {
    return this.get<MealItemFilter[]>('/customer/categories');
  }

  groups(): Observable<MealItemFilter[]> {
    return this.get<MealItemFilter[]>('/customer/groups');
  }

  favoriteFilters(): Observable<MealItemFilter[]> {
    return this.get<MealItemFilter[]>('/customer/favorites/filters');
  }

  suggested(text = '', cuisineId?: string | null, skip = 0, take = 8, extra?: SearchExtra): Observable<ItemCard[]> {
    const params = new URLSearchParams();
    if (text) params.set('text', text);
    if (cuisineId) params.set('cuisineId', cuisineId);
    if (skip) params.set('skip', String(skip));
    if (take) params.set('take', String(take));
    if (extra?.categoryId) params.set('categoryIds', extra.categoryId);
    if (extra?.ingredient) params.set('ingredient', extra.ingredient);
    if (extra?.kitchenId) params.set('kitchenId', extra.kitchenId);
    if (extra?.groupId) params.set('groupId', extra.groupId);
    if (extra?.mealId) params.set('filterMealId', extra.mealId);
    const query = params.toString() ? `?${params.toString()}` : '';
    return this.get<ItemCard[]>(`/customer/suggested${query}`);
  }

  items(mealId: string, text: string, cuisineId?: string | null, skip = 0, take?: number | null, extra?: SearchExtra): Observable<ItemCard[]> {
    const params = new URLSearchParams();
    if (text) params.set('text', text);
    if (cuisineId) params.set('cuisineId', cuisineId);
    if (skip) params.set('skip', String(skip));
    if (take != null) params.set('take', String(take));
    if (extra?.categoryId) params.set('categoryIds', extra.categoryId);
    if (extra?.ingredient) params.set('ingredient', extra.ingredient);
    if (extra?.kitchenId) params.set('kitchenId', extra.kitchenId);
    if (extra?.groupId) params.set('groupId', extra.groupId);
    if (extra?.mealId) params.set('filterMealId', extra.mealId);
    const query = params.toString() ? `?${params.toString()}` : '';
    return this.get<ItemCard[]>(`/customer/meals/${mealId}/items${query}`);
  }

  menuItems(text = '', cuisineId?: string | null, skip = 0, take?: number | null, extra?: SearchExtra): Observable<ItemCard[]> {
    const params = new URLSearchParams();
    if (text) params.set('text', text);
    if (cuisineId) params.set('cuisineId', cuisineId);
    if (skip) params.set('skip', String(skip));
    if (take != null) params.set('take', String(take));
    if (extra?.categoryId) params.set('categoryIds', extra.categoryId);
    if (extra?.ingredient) params.set('ingredient', extra.ingredient);
    if (extra?.kitchenId) params.set('kitchenId', extra.kitchenId);
    if (extra?.groupId) params.set('groupId', extra.groupId);
    if (extra?.mealId) params.set('filterMealId', extra.mealId);
    const query = params.toString() ? `?${params.toString()}` : '';
    return this.get<ItemCard[]>(`/customer/menu-items${query}`);
  }

  /** cuisineId: opened from that cuisine (its country's version); countryId: keep showing that version. */
  item(id: string, opts: { cuisineId?: string | null; countryId?: string | null } = {}): Observable<ItemDetail> {
    const params = new URLSearchParams();
    if (opts.cuisineId) params.set('cuisineId', opts.cuisineId);
    if (opts.countryId) params.set('countryId', opts.countryId);
    const qs = params.toString();
    return this.get<ItemDetail>(`/customer/items/${id}${qs ? '?' + qs : ''}`);
  }

  /** The customer's own version of a dish. */
  myItem(id: string, countryId?: string | null): Observable<MyItemRecipe> {
    return this.get<MyItemRecipe>(`/customer/items/${id}/mine${countryId ? `?countryId=${countryId}` : ''}`);
  }

  saveMyItem(id: string, lines: MyLineInput[], steps: { sort: number; body: string }[], parts?: string[], countryId?: string | null): Observable<MyItemRecipe> {
    return this.http.put<ApiResult<MyItemRecipe>>(`${environment.apiUrl}/customer/items/${id}/mine`, { lines, steps, parts, countryId: countryId || null }).pipe(
      map(result => this.unwrap(result)),
      catchError(error => this.fail(error))
    );
  }

  /** Dishes the customer can add as a part of this dish. */
  partPicks(id: string, text: string, take = 12): Observable<ItemPart[]> {
    return this.get<ItemPart[]>(`/customer/items/${id}/part-picks?text=${encodeURIComponent(text)}&take=${take}`);
  }

  /** Picks or drops an optional ingredient (kind 0, refId = contentId) or optional part (kind 1, refId = part dish id). */
  setItemChoice(id: string, kind: 0 | 1, refId: string, selected: boolean, countryId?: string | null): Observable<boolean> {
    return this.put<boolean>(`/customer/items/${id}/choice`, { kind, refId, selected, countryId: countryId || null });
  }

  resetMyItem(id: string, countryId?: string | null): Observable<MyItemRecipe> {
    return this.http.delete<ApiResult<MyItemRecipe>>(`${environment.apiUrl}/customer/items/${id}/mine${countryId ? `?countryId=${countryId}` : ''}`).pipe(
      map(result => this.unwrap(result)),
      catchError(error => this.fail(error))
    );
  }

  /** Existing ingredients only (picker for the personal version). */
  searchContents(text: string, take = 20): Observable<ContentPickResult> {
    const params = new URLSearchParams();
    if (text) params.set('text', text);
    params.set('take', String(take));
    return this.get<ContentPickResult>(`/customer/contents?${params.toString()}`);
  }

  addFavorite(id: string): Observable<boolean> {
    return this.post<boolean>(`/customer/items/${id}/favorite`, {});
  }

  removeFavorite(id: string): Observable<boolean> {
    return this.http.delete<ApiResult<boolean>>(`${environment.apiUrl}/customer/items/${id}/favorite`).pipe(
      map(result => this.unwrap(result)),
      catchError(error => this.fail(error))
    );
  }

  favorites(text = '', cuisineId?: string | null, skip = 0, take?: number | null, extra?: SearchExtra): Observable<ItemCard[]> {
    const params = new URLSearchParams();
    if (text) params.set('text', text);
    if (cuisineId) params.set('cuisineId', cuisineId);
    if (skip) params.set('skip', String(skip));
    if (take != null) params.set('take', String(take));
    if (extra?.categoryId) params.set('categoryIds', extra.categoryId);
    if (extra?.ingredient) params.set('ingredient', extra.ingredient);
    if (extra?.kitchenId) params.set('kitchenId', extra.kitchenId);
    if (extra?.groupId) params.set('groupId', extra.groupId);
    if (extra?.mealId) params.set('filterMealId', extra.mealId);
    const query = params.toString() ? `?${params.toString()}` : '';
    return this.get<ItemCard[]>(`/customer/favorites${query}`);
  }

  family(): Observable<FamilyMember[]> {
    return this.get<FamilyMember[]>('/customer/family');
  }

  saveFamily(members: FamilyMemberInput[]): Observable<FamilyMember[]> {
    return this.http.put<ApiResult<FamilyMember[]>>(`${environment.apiUrl}/customer/family`, members).pipe(
      map(result => this.unwrap(result)),
      catchError(error => this.fail(error))
    );
  }

  calculate(items: SelectedItem[]): Observable<ShoppingLine[]> {
    return this.post<ShoppingLine[]>('/customer/orders/calculate', items.map(item => ({
      itemId: item.itemId,
      repeatCount: item.repeatCount,
      peopleCount: item.peopleCount,
      countryId: item.countryId || null
    })));
  }

  orders(): Observable<OrderListItem[]> {
    return this.get<OrderListItem[]>('/customer/orders');
  }

  order(id: string): Observable<OrderDetail> {
    return this.get<OrderDetail>(`/customer/orders/${id}`);
  }

  saveOrder(
    id: string | null,
    name: string,
    date: string,
    notes: string,
    items: SelectedItem[],
    lines: ShoppingLine[],
    confirm = false
  ): Observable<string> {
    const body = {
      id,
      name,
      date,
      notes,
      confirm,
      items: items.map(item => ({
        itemId: item.itemId,
        repeatCount: item.repeatCount,
        peopleCount: item.peopleCount,
        countryId: item.countryId || null
      })),
      lines: lines.map(line => ({
        contentId: line.contentId,
        unitId: line.unitId,
        quantity: line.quantity
      }))
    };
    // Confirmed purchase orders are always created as a new record.
    if (id && !confirm) {
      return this.http.put<ApiResult<string>>(`${environment.apiUrl}/customer/orders/${id}`, body).pipe(
        map(result => this.unwrap(result)),
        catchError(error => this.fail(error))
      );
    }
    return this.post<string>('/customer/orders', body);
  }

  /** Old orders (confirmed), newest first, 20 per page. */
  orderHistory(filter: OrderHistoryFilter, skip = 0, take = 20): Observable<OrderHistoryPage> {
    const params = new URLSearchParams();
    params.set('skip', String(skip));
    params.set('take', String(take));
    if (filter.text?.trim()) params.set('text', filter.text.trim());
    if (filter.state) params.set('state', filter.state);
    if (filter.from) params.set('from', filter.from);
    if (filter.to) params.set('to', filter.to);
    if (filter.dish?.trim()) params.set('dish', filter.dish.trim());
    if (filter.name?.trim()) params.set('name', filter.name.trim());
    return this.get<OrderHistoryPage>(`/customer/orders/history?${params.toString()}`);
  }

  /** Remember that the order was shared. */
  markOrderShared(id: string): Observable<string> {
    return this.post<string>(`/customer/orders/${id}/shared`, {});
  }

  /** Remember that the order was re-ordered (added to the current selection). */
  markOrderReordered(id: string): Observable<string> {
    return this.post<string>(`/customer/orders/${id}/reordered`, {});
  }

  /** Remember that the purchase order was printed. */
  markOrderPrinted(id: string): Observable<string> {
    return this.post<string>(`/customer/orders/${id}/printed`, {});
  }

  /** Give an order the user's own name (keeps its items and status). */
  renameOrder(id: string, name: string): Observable<string> {
    return this.http.put<ApiResult<string>>(`${environment.apiUrl}/customer/orders/${id}/name`, { name }).pipe(
      map(result => this.unwrap(result)),
      catchError(error => this.fail(error))
    );
  }

  reorder(id: string): Observable<string> {
    return this.post<string>(`/customer/orders/${id}/reorder`, {});
  }

  saveProfile(name: string): Observable<Profile> {
    return this.put<Profile>('/customer/profile', { name });
  }

  settings(): Observable<UserSettings> {
    return this.get<UserSettings>('/customer/settings');
  }

  saveSettings(settings: UserSettings): Observable<UserSettings> {
    return this.put<UserSettings>('/customer/settings', settings);
  }

  /** أسعار المكونات: every ingredient with the price in use (mine or the app's). */
  prices(text = '', contentId?: string): Observable<CustomerPrices> {
    const params = new URLSearchParams();
    if (text) params.set('text', text);
    if (contentId) params.set('contentId', contentId);
    return this.get<CustomerPrices>(`/customer/prices?${params.toString()}`);
  }

  /** البلد اللي العميل فيها: prices and currency (null = their own country). */
  priceCountry(): Observable<PriceCountry> {
    return this.get<PriceCountry>('/customer/prices/country');
  }

  setPriceCountry(countryId: string | null): Observable<PriceCountry> {
    return this.put<PriceCountry>('/customer/prices/country', { countryId });
  }

  setPricesEnabled(enabled: boolean): Observable<boolean> {
    return this.put<boolean>('/customer/prices/enabled', { enabled });
  }

  saveMyPrice(contentId: string, price: number, quantity: number, unitId: string | null): Observable<ContentPriceRow> {
    return this.put<ContentPriceRow>(`/customer/prices/${contentId}`, { price, quantity, unitId });
  }

  removeMyPrice(contentId: string): Observable<ContentPriceRow> {
    return this.http.delete<ApiResult<ContentPriceRow>>(`${environment.apiUrl}/customer/prices/${contentId}`).pipe(
      map(result => this.unwrap(result)),
      catchError(error => this.fail(error))
    );
  }

  notifications(skip: number, take: number, unread: boolean | null): Observable<NotificationPage> {
    const params = new URLSearchParams({ skip: String(skip), take: String(take) });
    if (unread !== null) params.set('unread', String(unread));
    return this.get<NotificationPage>(`/customer/notifications?${params.toString()}`);
  }

  unreadNotifications(): Observable<number> {
    return this.get<number>('/customer/notifications/unread-count');
  }

  markNotificationRead(id: string): Observable<number> {
    return this.post<number>(`/customer/notifications/${id}/read`, {});
  }

  markAllNotificationsRead(): Observable<number> {
    return this.post<number>('/customer/notifications/read-all', {});
  }

  /** Uploads an image through the shared endpoint; returns its /uploads/... path. */
  uploadImage(file: File): Observable<string> {
    const data = new FormData();
    data.append('file', file);
    return this.http.post<{ path: string }>(`${environment.apiUrl}/files/image`, data).pipe(
      map(result => result.path),
      catchError(error => this.fail(error))
    );
  }

  saveProfilePhoto(imagePath: string | null): Observable<Profile> {
    return this.put<Profile>('/customer/profile/photo', { imagePath });
  }

  private put<T>(path: string, body: unknown): Observable<T> {
    return this.http.put<ApiResult<T>>(`${environment.apiUrl}${path}`, body).pipe(
      map(result => this.unwrap(result)),
      catchError(error => this.fail(error))
    );
  }

  private get<T>(path: string): Observable<T> {
    return this.http.get<ApiResult<T>>(`${environment.apiUrl}${path}`).pipe(
      map(result => this.unwrap(result)),
      catchError(error => this.fail(error))
    );
  }

  private post<T>(path: string, body: unknown): Observable<T> {
    return this.http.post<ApiResult<T>>(`${environment.apiUrl}${path}`, body).pipe(
      map(result => this.unwrap(result)),
      catchError(error => this.fail(error))
    );
  }

  private unwrap<T>(result: ApiResult<T>): T {
    if (result?.response != null && (result.statusCode === 200 || result.statusCode === 0))
      return result.response;
    throw new Error((result?.errors || ['حدث خطأ']).join('، '));
  }

  private fail(error: { error?: ApiResult<unknown>; message?: string }) {
    const message = error?.error?.errors?.join('، ') || error?.message || 'تعذر الاتصال بالخادم';
    return throwError(() => new Error(message));
  }
}

export interface SearchExtra {
  /** One category id, or several joined by commas (a dish in any of them matches). */
  categoryId?: string | null;
  ingredient?: string | null;
  /** Advanced search: المطبخ / المجموعة / الوجبة. */
  kitchenId?: string | null;
  groupId?: string | null;
  mealId?: string | null;
}

/** SearchExtra ⇄ route query params (home search → /meals/all). */
export function extraToParams(extra: SearchExtra, into: Record<string, string> = {}): Record<string, string> {
  if (extra.categoryId) into['categoryId'] = extra.categoryId;
  if ((extra.ingredient || '').trim()) into['ingredient'] = (extra.ingredient || '').trim();
  if (extra.kitchenId) into['kitchenId'] = extra.kitchenId;
  if (extra.groupId) into['groupId'] = extra.groupId;
  if (extra.mealId) into['filterMealId'] = extra.mealId;
  return into;
}

export function extraFromParams(get: (key: string) => string | null): SearchExtra {
  return {
    categoryId: get('categoryId') || null,
    ingredient: (get('ingredient') || '').trim() || null,
    kitchenId: get('kitchenId') || null,
    groupId: get('groupId') || null,
    mealId: get('filterMealId') || null
  };
}

export function hasExtra(extra: SearchExtra | null | undefined): boolean {
  return !!extra && (!!extra.categoryId || !!(extra.ingredient || '').trim() || !!extra.kitchenId || !!extra.groupId || !!extra.mealId);
}
