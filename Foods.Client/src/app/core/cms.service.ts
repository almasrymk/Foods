import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from './environment';
import { ApiResult } from './models';

export interface NamedRow {
  id: string;
  name?: string | null;
  iconPath?: string | null;
  color?: string | null;
  textColor?: string | null;
}

export interface UnitRow extends NamedRow {
  nickName?: string | null;
  basicQuantity: number;
  beDivided: boolean;
}

export interface UserRow extends NamedRow {
  userName?: string | null;
  role?: string | null;
  countryId?: string | null;
  countryName?: string | null;
  dialect?: string | null;
}

export interface CountryRow extends NamedRow {
  dialect?: string | null;
  /** Currency of prices in this country (ج.م، ر.س...). */
  currency?: string | null;
  /** Neighbor country ids, comma separated. */
  neighbors?: string | null;
}

export interface CountryLink {
  id: string;
  itemId: string;
  countryId: string;
  isPrimary: boolean;
  name?: string | null;
  summary?: string | null;
  description?: string | null;
}

export interface MealRow extends NamedRow {
  description?: string | null;
  imagePath?: string | null;
  iconPath?: string | null;
  color?: string | null;
  textColor?: string | null;
  sort?: number | null;
}

export interface CuisineRow extends NamedRow {
  /** بلد المطبخ: its customers see it first (null = guessed from the name). */
  countryId?: string | null;
  iconPath?: string | null;
  imagePath?: string | null;
  color?: string | null;
  textColor?: string | null;
  sort?: number | null;
}

export interface ItemRow extends NamedRow {
  imagePath?: string | null;
  itemCategoryId?: string;
  cuisineId?: string | null;
  itemGroupId?: string;
  summary?: string | null;
  description?: string | null;
  /** Shown in the customer's search and meal lists (false = only used as a part of other dishes). */
  showToCustomer?: boolean;
  /** Admin order (smaller first). */
  sort?: number | null;
  /** Other categories of the dish (the main one is itemCategoryId). */
  categoryIds?: string[] | null;
}

/** صنف: another dish inside this dish. */
export interface ItemPartRow {
  id?: string;
  partItemId: string;
  name?: string | null;
  imagePath?: string | null;
  showToCustomer?: boolean;
  isOptional: boolean;
  isDefault: boolean;
  sort?: number;
}

export interface ContentRow extends NamedRow {
  contentCategoryId: string;
  contentGroupId: string;
  unitId: string;
  miniQuantity: number;
  imagePath?: string | null;
  purchasePlaceId?: string | null;
  description?: string | null;
  /** أقل وحدة شراء: 0 عادي، 1 عبوة، 2 كسور الكيلو، 3 بالفلوس، 4 بالفلوس لحد 1/8 كيلو */
  purchaseMode?: number;
  packageName?: string | null;
  purchaseStep?: number;
  purchaseIncrement?: number;
  /** Mode 4: price of 1/8 kg (EGP). */
  purchaseUnitPrice?: number;
  /** Mode 4: least money amount (EGP). */
  purchaseMinAmount?: number;
  /** سعر المكون: price (EGP) of priceQuantity of priceUnitId (null = the content unit). */
  price?: number;
  priceQuantity?: number;
  priceUnitId?: string | null;
  /** المطبخ اللي المكون تبعه. */
  cuisineId?: string | null;
  /** Admin order (smaller first). */
  sort?: number | null;
}

/** المكون في بلد: اسمه ووصفه بلهجتها وسعره بعملتها. */
export interface ContentCountryRow {
  countryId: string;
  name?: string | null;
  description?: string | null;
  price: number;
  priceQuantity: number;
  priceUnitId?: string | null;
}

export interface VersionLine { contentId: string; unitId: string; quantity: number; isOptional: boolean; isDefault: boolean; }
export interface VersionPart { partItemId: string; isOptional: boolean; isDefault: boolean; }
/** نسخة الأكلة: العامة (countryId null) أو نسخة بلد. */
export interface ItemVersion {
  countryId: string | null;
  lines: VersionLine[];
  parts: VersionPart[];
  steps: { sort: number; body: string | null }[];
  videos: { sort: number; title: string | null; url: string | null }[];
}

/** Album owner: 'item' (dish) or 'content' (ingredient). */
export type AlbumKind = 'item' | 'content';

export interface AlbumImage {
  id: string;
  imagePath: string;
  sort: number;
}

export interface LinkRow {
  id: string;
  itemId: string;
  mealId?: string;
  contentId?: string;
  unitId?: string;
  quantity?: number;
  isOptional?: boolean;
  isDefault?: boolean;
}

@Injectable({ providedIn: 'root' })
export class CmsService {
  constructor(private http: HttpClient) {}

  list<T>(controller: string, params?: Record<string, string>): Observable<T[]> {
    return this.http.get<ApiResult<T[]>>(`${environment.apiUrl}/${controller}/GetList`, { params }).pipe(
      map(result => this.unwrap(result) || []),
      catchError(error => this.fail(error))
    );
  }

  get<T>(controller: string, id: string): Observable<T> {
    return this.http.get<ApiResult<T>>(`${environment.apiUrl}/${controller}/GetById`, { params: { Id: id } }).pipe(
      map(result => this.unwrap(result)),
      catchError(error => this.fail(error))
    );
  }

  create(controller: string, body: unknown): Observable<string> {
    return this.http.post<ApiResult<{ id?: string } | string>>(`${environment.apiUrl}/${controller}/Create`, body).pipe(
      map(result => {
        const value = this.unwrap(result);
        if (typeof value === 'string') return value;
        return value?.id || '';
      }),
      catchError(error => this.fail(error))
    );
  }

  update(controller: string, body: unknown): Observable<void> {
    return this.http.put<ApiResult<unknown>>(`${environment.apiUrl}/${controller}/Update`, body).pipe(
      map(result => { this.unwrap(result); }),
      catchError(error => this.fail(error))
    );
  }

  remove(controller: string, id: string): Observable<void> {
    return this.http.delete<ApiResult<unknown>>(`${environment.apiUrl}/${controller}/Delete`, { params: { Id: id } }).pipe(
      map(result => { this.unwrap(result); }),
      catchError(error => this.fail(error))
    );
  }

  saveRecipe(itemId: string, steps: { sort: number; body: string }[], videos: { sort: number; title: string; url: string }[]): Observable<void> {
    return this.http.put<ApiResult<string>>(`${environment.apiUrl}/ItemRecipe/Save`, { itemId, steps, videos }).pipe(
      map(result => { this.unwrap(result); }),
      catchError(error => this.fail(error))
    );
  }

  contentCountries(contentId: string): Observable<ContentCountryRow[]> {
    return this.http.get<ApiResult<ContentCountryRow[]>>(`${environment.apiUrl}/ContentCountry/GetList`, { params: { ContentId: contentId } }).pipe(
      map(result => this.unwrap(result) || []),
      catchError(error => this.fail(error))
    );
  }

  saveContentCountries(contentId: string, rows: ContentCountryRow[]): Observable<ContentCountryRow[]> {
    return this.http.put<ApiResult<ContentCountryRow[]>>(`${environment.apiUrl}/ContentCountry/Save`, { contentId, rows }).pipe(
      map(result => this.unwrap(result) || []),
      catchError(error => this.fail(error))
    );
  }

  itemVersions(itemId: string): Observable<ItemVersion[]> {
    return this.http.get<ApiResult<ItemVersion[]>>(`${environment.apiUrl}/ItemVersion/GetList`, { params: { ItemId: itemId } }).pipe(
      map(result => this.unwrap(result) || []),
      catchError(error => this.fail(error))
    );
  }

  saveItemVersions(itemId: string, versions: ItemVersion[]): Observable<ItemVersion[]> {
    return this.http.put<ApiResult<ItemVersion[]>>(`${environment.apiUrl}/ItemVersion/Save`, { itemId, versions }).pipe(
      map(result => this.unwrap(result) || []),
      catchError(error => this.fail(error))
    );
  }

  itemParts(itemId: string): Observable<ItemPartRow[]> {
    return this.http.get<ApiResult<ItemPartRow[]>>(`${environment.apiUrl}/ItemPart/GetList`, { params: { ItemId: itemId } }).pipe(
      map(result => this.unwrap(result) || []),
      catchError(error => this.fail(error))
    );
  }

  saveItemParts(itemId: string, parts: { partItemId: string; isOptional: boolean; isDefault: boolean }[]): Observable<ItemPartRow[]> {
    return this.http.put<ApiResult<ItemPartRow[]>>(`${environment.apiUrl}/ItemPart/Save`, { itemId, parts }).pipe(
      map(result => this.unwrap(result) || []),
      catchError(error => this.fail(error))
    );
  }

  album(kind: AlbumKind, ownerId: string): Observable<AlbumImage[]> {
    return this.http.get<ApiResult<AlbumImage[]>>(`${environment.apiUrl}/Album/${kind}/${ownerId}`).pipe(
      map(result => this.unwrap(result) || []),
      catchError(error => this.fail(error))
    );
  }

  addAlbumImages(kind: AlbumKind, ownerId: string, paths: string[]): Observable<AlbumImage[]> {
    return this.http.post<ApiResult<AlbumImage[]>>(`${environment.apiUrl}/Album/${kind}/${ownerId}`, { paths }).pipe(
      map(result => this.unwrap(result) || []),
      catchError(error => this.fail(error))
    );
  }

  removeAlbumImage(kind: AlbumKind, imageId: string): Observable<void> {
    return this.http.delete<ApiResult<unknown>>(`${environment.apiUrl}/Album/${kind}/image/${imageId}`).pipe(
      map(result => { this.unwrap(result); }),
      catchError(error => this.fail(error))
    );
  }

  reorderAlbum(kind: AlbumKind, ownerId: string, ids: string[]): Observable<AlbumImage[]> {
    return this.http.put<ApiResult<AlbumImage[]>>(`${environment.apiUrl}/Album/${kind}/${ownerId}/order`, { ids }).pipe(
      map(result => this.unwrap(result) || []),
      catchError(error => this.fail(error))
    );
  }

  upload(file: File): Observable<string> {
    const data = new FormData();
    data.append('file', file);
    return this.http.post<{ path: string }>(`${environment.apiUrl}/files/image`, data).pipe(
      map(result => result.path),
      catchError(error => this.fail(error))
    );
  }

  private unwrap<T>(result: ApiResult<T>): T {
    if (result && (result.statusCode === 200 || result.statusCode === 0 || String(result.statusCode) === 'OK')) return result.response as T;
    throw new Error((result?.errors || ['حدث خطأ']).join('، '));
  }

  private fail(error: { error?: { errors?: string[] | Record<string, string[]> }; message?: string }) {
    const raw = error?.error?.errors;
    const message = Array.isArray(raw) ? raw.join('، ') : error?.message || 'تعذر الاتصال بالخادم';
    return throwError(() => new Error(message));
  }
}
