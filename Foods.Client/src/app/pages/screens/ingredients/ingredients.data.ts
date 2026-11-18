import { Chip } from '../shared/screens.data';

export interface IngredientRow {
  id: string;
  name: string;
  desc: string;
  unit: string;
  qty: number;
  checked: boolean;
  image: string;
}

const img = (name: string) => `assets/screens/ingredients/${name}.png`;

export const INGREDIENT_CHIPS: Chip[] = [
  { label: 'الكل', icon: 'grid', count: 12, tag: '' },
  { label: 'شرقي', icon: 'cloche', count: 5, tag: 'شرقي' },
  { label: 'غربي', icon: 'chef', count: 3, tag: 'غربي' },
  { label: 'مشويات', icon: 'grill', count: 2, tag: 'مشويات' },
  { label: 'صحي', icon: 'leaf', count: 2, tag: 'صحي' }
];

export function ingredientRows(): IngredientRow[] {
  return [
    { id: 'onion', name: 'بصل', desc: 'بصل أبيض طازج', unit: 'جم', qty: 80, checked: true, image: img('onion') },
    { id: 'oil', name: 'زيت للقلي', desc: 'زيت نباتي مكرر', unit: 'مل', qty: 15, checked: true, image: img('oil') },
    { id: 'garlic', name: 'ثوم', desc: 'ثوم طازج مفروم', unit: 'جم', qty: 11, checked: true, image: img('garlic') },
    { id: 'cumin', name: 'كمون', desc: 'كمون مطحون ناعم', unit: 'جم', qty: 2, checked: false, image: img('cumin') },
    { id: 'pepper', name: 'فلفل', desc: 'فلفل أخضر طازج', unit: 'حبة', qty: 1, checked: true, image: img('pepper') },
    { id: 'chickpeas', name: 'حمص منقوع', desc: 'حمص مسلوق أو منقوع', unit: 'جم', qty: 1, checked: false, image: img('chickpeas') },
    { id: 'meat', name: 'لحم مفروم', desc: 'لحم بقري طازج', unit: 'جم', qty: 500, checked: false, image: img('meat') }
  ];
}
