// Static content shared by the mockup screens. Wire to the API later.

export interface FoodRow {
  id: string;
  title: string;
  desc: string;
  tag: string;
  tagIcon: string;
  /** المجموعة and التصنيف chips on top; the cuisine shows under the name. */
  group?: string;
  category?: string;
  cuisine?: string;
  image: string;
  /** Album (main photo first); a slider shows when there is more than one. */
  images?: string[];
  checked: boolean;
  favorite: boolean;
  qty: number;
  cuisineId?: string;
  /** Dish price for the family (only when the customer works with prices). */
  price?: number | null;
  priceMissing?: number;
  /** On another country's cuisine: picking the dish takes that country's version. */
  recipeCountryId?: string | null;
}

export interface Chip {
  label: string;
  icon: string;
  count?: number;
  /** Tag this chip filters on; empty = all. */
  tag: string;
}

const food = (name: string) => `assets/screens/foods/${name}.png`;

export const FOODS: Record<string, Omit<FoodRow, 'checked' | 'favorite' | 'qty'>> = {
  peppers: { id: 'peppers', title: 'فلفل شامية', desc: 'فلفل محشي بالأرز واللحمة بتتبيلة شامية أصيلة', tag: 'شرقي', tagIcon: 'cloche', image: food('peppers') },
  bamia: { id: 'bamia', title: 'بامية باللحمة', desc: 'بامية مطهوة مع قطع اللحمة بصلصة الطماطم الغنية', tag: 'شرقي', tagIcon: 'cloche', image: food('bamia') },
  pasta: { id: 'pasta', title: 'باستا بالدجاج', desc: 'باستا كريمية مع قطع الدجاج وصوص غني', tag: 'غربي', tagIcon: 'chef', image: food('pasta') },
  moussaka: { id: 'moussaka', title: 'مسقعة', desc: 'شرائح باذنجان مع اللحم المفروم وصوص الطماطم', tag: 'شرقي', tagIcon: 'cloche', image: food('moussaka') },
  maqluba: { id: 'maqluba', title: 'مقلوبة زهرة', desc: 'أرز متبل مع زهرة (قرنبيط) وخضار مشكلة', tag: 'شرقي', tagIcon: 'cloche', image: food('maqluba') },
  mahshi: { id: 'mahshi', title: 'محشي ورق عنب', desc: 'ورق عنب محشي بالأرز والخضار والتوابل الشرقية', tag: 'شرقي', tagIcon: 'cloche', image: food('mahshi') },
  koshari: { id: 'koshari', title: 'كشري مصري', desc: 'أرز وعدس ومكرونة مع صلصة الطماطم والدقة', tag: 'شرقي', tagIcon: 'cloche', image: food('koshari') }
};

export function row(id: keyof typeof FOODS, checked: boolean, qty: number, favorite: boolean): FoodRow {
  return { ...FOODS[id], checked, qty, favorite };
}

export const MEAL_TYPES: Record<string, string> = {
  breakfast: 'الفطار',
  lunch: 'الغداء',
  dinner: 'العشاء',
  snacks: 'وجبات خفيفة',
  drinks: 'مشروبات',
  healthy: 'أكلات صحية',
  desserts: 'حلويات'
};
