export interface ApiResult<T> {
  statusCode: number;
  response: T | null;
  errors: string[] | null;
}

export interface AuthUser {
  id: string;
  name: string;
  userName: string;
  imagePath?: string | null;
  token: string;
  role?: string | null;
}

export interface Profile {
  countryId?: string | null;
  countryName?: string | null;
  dialect?: string | null;
  name?: string | null;
  userName?: string | null;
  imagePath?: string | null;
}

export interface FamilyMemberInput {
  name: string;
  age: number;
  birthDate?: string | null;
  relation?: string | null;
  imagePath?: string | null;
}

export interface FamilyMember {
  id: string;
  name?: string | null;
  age: number;
  birthDate?: string | null;
  relation?: string | null;
  imagePath?: string | null;
  avatar?: string | null;
}

export interface UserSettings {
  language: 'ar' | 'en';
  notifyLogin: boolean;
  notifyWelcome: boolean;
  notifyBirthday: boolean;
  notifyOrders: boolean;
  notifyPlanning: boolean;
  /** The customer works with prices (saved with PUT customer/prices/enabled). */
  usePrices?: boolean;
}

export interface AppNotification {
  id: string;
  type: string;
  title?: string | null;
  body?: string | null;
  link?: string | null;
  createdAt: string;
  readAt?: string | null;
  image?: string | null;
}

export interface NotificationPage {
  items: AppNotification[];
  total: number;
  unread: number;
}

export interface MealCard {
  id: string;
  name?: string | null;
  description?: string | null;
  imagePath?: string | null;
  iconPath?: string | null;
  color?: string | null;
  textColor?: string | null;
  sort?: number | null;
  /** Dishes in this meal the customer can see. */
  count?: number | null;
}

export interface ItemCard {
  id: string;
  /** Set on another country's cuisine list: that country's version is picked. */
  recipeCountryId?: string | null;
  name?: string | null;
  summary?: string | null;
  imagePath?: string | null;
  isFavorite: boolean;
  countryName?: string | null;
  dialect?: string | null;
  categoryId?: string | null;
  categoryName?: string | null;
  cuisineId?: string | null;
  cuisineName?: string | null;
  /** Main photo first, then the album. */
  images?: string[];
  groupId?: string | null;
  groupName?: string | null;
  /** Dish price for the family (null = prices off). */
  price?: number | null;
  priceMissing?: number;
}

/** The price used for an ingredient: price EGP for quantity of unitName. */
export interface PriceInfo { price: number; quantity: number; unitId: string; unitName?: string | null; isMine: boolean; }

/** Dish price: total for the family, perPerson, ingredients with no price. */
export interface DishPrice { total: number; perPerson: number; people: number; missing: number; }

/** One ingredient in "أسعار المكونات". */
export interface ContentPriceRow {
  contentId: string;
  name?: string | null;
  imagePath?: string | null;
  unitId: string;
  unitName?: string | null;
  price?: number | null;
  quantity: number;
  priceUnitId: string;
  priceUnitName?: string | null;
  isMine: boolean;
  adminPrice?: number | null;
  adminQuantity: number;
  adminUnitName?: string | null;
  units: { id: string; name?: string | null }[];
  /** Advanced-search values of the ingredient. */
  cuisineId?: string | null;
  categoryId?: string | null;
  groupId?: string | null;
  placeId?: string | null;
}

export interface CustomerPrices {
  enabled: boolean; items: ContentPriceRow[]; currency?: string; countryId?: string | null; countryName?: string | null;
  /** The advanced-search sections (key: cuisine / category / group / place). */
  filters?: { key: string; title: string; options: { id: string; name?: string | null }[] }[] | null;
}

/** البلد اللي العميل فيها (prices and currency). */
export interface PriceCountry {
  countryId?: string | null;
  countryName?: string | null;
  currency: string;
  ownCountryId?: string | null;
  /** True = follows the customer's own country. */
  isDefault: boolean;
  countries: { id: string; name?: string | null; currency: string }[];
}

export interface MealItemFilter {
  /** A cuisine of the customer's own country (listed first; a meal screen starts on it). */
  isMine?: boolean;
  id?: string | null;
  name: string;
  count: number;
  iconPath?: string | null;
  color?: string | null;
  textColor?: string | null;
  imagePath?: string | null;
}

export interface ItemIngredient {
  contentId: string;
  name?: string | null;
  imagePath?: string | null;
  unitId: string;
  unitName?: string | null;
  quantity: number;
  purchasePlaceId?: string | null;
  purchasePlaceName?: string | null;
  images?: string[];
  /** Personal version (نسختك الخاصة): added by the customer / quantity or unit changed. */
  isAdded?: boolean;
  isChanged?: boolean;
  /** Optional ingredient: the customer can pick or drop it. */
  isOptional?: boolean;
  /** Picked (always true for a required ingredient). */
  selected?: boolean;
  /** Money of quantity (per person); null = no price / prices off. */
  cost?: number | null;
  priceInfo?: PriceInfo | null;
}

/** صنف: another dish that is part of this dish. */
export interface ItemPart {
  itemId: string;
  name?: string | null;
  summary?: string | null;
  imagePath?: string | null;
  isOptional: boolean;
  selected: boolean;
  /** The part's own details page is available to this customer. */
  canOpen: boolean;
  ingredientCount: number;
  /** Added by the customer in "تعديل الأكلة ليا". */
  isAdded?: boolean;
}

export interface ItemStep {
  sort: number;
  body?: string | null;
}

export interface ItemVideo {
  sort: number;
  title?: string | null;
  url?: string | null;
}

export interface ItemDetail {
  id: string;
  name?: string | null;
  summary?: string | null;
  description?: string | null;
  imagePath?: string | null;
  isFavorite: boolean;
  countryName?: string | null;
  dialect?: string | null;
  ingredients: ItemIngredient[];
  steps: ItemStep[];
  videos: ItemVideo[];
  images?: string[];
  /** The current customer changed this dish for themselves. */
  isCustomized?: boolean;
  groupName?: string | null;
  categoryName?: string | null;
  parts?: ItemPart[];
  showToCustomer?: boolean;
  /** Null when the customer did not turn prices on. */
  price?: DishPrice | null;
  /** The country whose version of the dish is shown (the picked cuisine's, else the customer's). */
  recipeCountryId?: string | null;
  recipeCountryName?: string | null;
  /** True when that is not the customer's own country. */
  isOtherCountry?: boolean;
}

/** The customer's own version of a dish (GET/PUT/DELETE customer/items/{id}/mine). */
export interface MyItemRecipe {
  itemId: string;
  isCustomized: boolean;
  stepsCustomized: boolean;
  lines: ItemIngredient[];
  removed: ItemIngredient[];
  steps: ItemStep[];
  /** All parts of the customer's version (selected = kept). */
  parts?: ItemPart[];
}

export interface MyLineInput {
  contentId: string;
  unitId: string;
  quantity: number;
  removed?: boolean;
}

export interface ContentPick {
  id: string;
  name?: string | null;
  imagePath?: string | null;
  unitId: string;
  unitName?: string | null;
  miniQuantity: number;
}

export interface UnitPick {
  id: string;
  name?: string | null;
}

export interface ContentPickResult {
  items: ContentPick[];
  units: UnitPick[];
}

export interface SelectedItem {
  itemId: string;
  name: string;
  imagePath?: string | null;
  repeatCount: number;
  peopleCount: number | null;
  /** The country version of the dish (picked from that country's cuisine); null = the customer's own. */
  countryId?: string | null;
}

export interface ShoppingLine {
  contentId: string;
  name?: string | null;
  imagePath?: string | null;
  purchasePlaceId?: string | null;
  purchasePlaceName?: string | null;
  unitId: string;
  unitName?: string | null;
  quantity: number;
  /** أقل وحدة شراء: 0 normal, 1 package, 2 kilo fractions, 3 money (see core/purchase.ts). */
  purchaseMode?: number;
  packageName?: string | null;
  purchaseStep?: number;
  purchaseIncrement?: number;
  /** Mode 4: price of 1/8 kg (one purchaseStep) and the least money amount. */
  purchaseUnitPrice?: number;
  purchaseMinAmount?: number;
  images?: string[];
  /** Money of one unit of the line (null = no price / prices off). */
  unitPrice?: number | null;
  price?: number | null;
}

/** 0 draft, 1 temporary save (حفظ مؤقت), 2 confirmed (محفوظ). */
export type OrderStatusCode = 0 | 1 | 2;

export interface OrderListItem {
  id: string;
  name?: string | null;
  date: string;
  status?: OrderStatusCode | number;
}

export interface OrderItem {
  itemId: string;
  countryId?: string | null;
  countryName?: string | null;
  name?: string | null;
  imagePath?: string | null;
  repeatCount: number;
  peopleCount?: number | null;
  price?: number | null;
}

export interface OrderDetail {
  id: string;
  name?: string | null;
  date: string;
  notes?: string | null;
  status?: OrderStatusCode | number;
  items: OrderItem[];
  lines: ShoppingLine[];
  total?: number | null;
  priceMissing?: number;
}

/** Old-orders state: إنشاء بدون طباعة / تم الطباعة / أعيد طلبه. */
export type OrderHistoryState = 'created' | 'printed' | 'reordered' | 'shared' | 'reprinted' | 'reshared';

export interface OrderHistoryDish {
  itemId: string;
  name?: string | null;
  imagePath?: string | null;
  repeatCount: number;
  summary?: string | null;
}

export interface OrderHistoryItem {
  id: string;
  name?: string | null;
  code?: string | null;
  date: string;
  createdAt?: string | null;
  state: OrderHistoryState;
  printedAt?: string | null;
  reorderedAt?: string | null;
  sharedAt?: string | null;
  dishCount: number;
  lineCount: number;
  dishes: OrderHistoryDish[];
  total?: number | null;
}

export interface OrderHistoryCounts { all: number; created: number; printed: number; reordered: number; shared: number; reprinted?: number; reshared?: number; }

export interface OrderHistoryPage {
  items: OrderHistoryItem[];
  total: number;
  hasMore: boolean;
  counts: OrderHistoryCounts;
}

export interface OrderHistoryFilter {
  text?: string;
  state?: OrderHistoryState | '';
  from?: string;
  to?: string;
  dish?: string;
  /** Order name or code only. */
  name?: string;
}
