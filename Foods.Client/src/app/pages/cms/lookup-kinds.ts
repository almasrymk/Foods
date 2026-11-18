export interface LookupKind {
  key: string;
  title: string;
  icon: string;
  unit?: boolean;
  dialect?: boolean;
  cuisine?: boolean;
  /** Has an admin sort order (smaller first). */
  sorted?: boolean;
}

export const lookupKinds: LookupKind[] = [
  { key: 'ItemCategory', title: 'تصنيفات الأكل', icon: 'category', sorted: true },
  { key: 'Cuisine', title: 'المطبخ', icon: 'category', cuisine: true },
  { key: 'ItemGroup', title: 'مجموعات الأكل', icon: 'group', sorted: true },
  { key: 'ContentCategory', title: 'تصنيفات المكونات', icon: 'category' },
  { key: 'ContentGroup', title: 'مجموعات المكونات', icon: 'group' },
  { key: 'PurchasePlace', title: 'أماكن الشراء', icon: 'place', sorted: true },
  { key: 'Country', title: 'البلاد', icon: 'place', dialect: true },
  { key: 'Unit', title: 'وحدات القياس', icon: 'unit', unit: true }
];

export function findLookup(key: string): LookupKind | undefined {
  return lookupKinds.find(kind => kind.key === key);
}
