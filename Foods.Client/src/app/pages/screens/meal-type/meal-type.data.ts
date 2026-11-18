import { Chip, FoodRow, row } from '../shared/screens.data';

export const MEAL_TYPE_CHIPS: Chip[] = [
  { label: 'الكل', icon: 'grid', tag: '' },
  { label: 'شرقي', icon: 'cloche', tag: 'شرقي' },
  { label: 'غربي', icon: 'chef', tag: 'غربي' },
  { label: 'مشويات', icon: 'grill', tag: 'مشويات' },
  { label: 'صحي', icon: 'leaf', tag: 'صحي' }
];

export function mealTypeRows(): FoodRow[] {
  return [
    row('peppers', true, 1, false),
    row('bamia', false, 0, false),
    row('pasta', true, 2, true),
    row('moussaka', true, 1, false),
    row('maqluba', false, 0, false)
  ];
}
