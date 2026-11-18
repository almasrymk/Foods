// Static home page content (matches the mockup). Wire to the API later.

export interface MealCategory {
  title: string;
  image: string;
  textColor: string;
  color: string;
}

export interface Cuisine {
  title: string;
  image: string;
  color: string;
}

export interface SuggestedMeal {
  title: string;
  image: string;
  minutes: number;
  persons: number;
  isFavorite: boolean;
  quantity: number;
}

const img = (name: string) => `assets/home/${name}.png`;

export const HOME_CATEGORIES: MealCategory[] = [
  { title: 'الفطار', image: img('cat_breakfast'), textColor: '#4A2A12', color: '#FEDD8C' },
  { title: 'الغداء', image: img('cat_lunch'), textColor: '#4A2A12', color: '#FEDABD' },
  { title: 'العشاء', image: img('cat_dinner'), textColor: '#4E1F78', color: '#E7D8F4' },
  { title: 'وجبات خفيفة', image: img('cat_snacks'), textColor: '#1E2A1E', color: '#DBEFD9' }
];

export const HOME_CUISINES: Cuisine[] = [
  { title: 'شرقي', image: img('cu_sharqi'), color: '#FFEAC5' },
  { title: 'غربي', image: img('cu_gharbi'), color: '#E2F1FB' },
  { title: 'صحي', image: img('cu_sehi'), color: '#E4F2C6' },
  { title: 'نباتي', image: img('cu_nabati'), color: '#EEF8E9' },
  { title: 'حلويات', image: img('cu_halawiyat'), color: '#FDE4EB' }
];

export function suggestedMeals(): SuggestedMeal[] {
  return [
    { title: 'كشري مصري', image: img('meal_koshary'), minutes: 60, persons: 4, isFavorite: false, quantity: 0 },
    { title: 'محشي ورق عنب', image: img('meal_mahshi'), minutes: 90, persons: 4, isFavorite: true, quantity: 2 },
    { title: 'دجاج مشوي بالأعشاب', image: img('meal_chicken'), minutes: 45, persons: 4, isFavorite: false, quantity: 0 },
    { title: 'بول دجاج صحي', image: img('meal_bowl'), minutes: 30, persons: 2, isFavorite: false, quantity: 0 }
  ];
}
