import { Chip, FoodRow, row } from '../shared/screens.data';

export const FAVORITE_CHIPS: Chip[] = [
  { label: 'الكل', icon: 'grid', count: 12, tag: '' },
  { label: 'شرقي', icon: 'cloche', count: 5, tag: 'شرقي' },
  { label: 'غربي', icon: 'chef', count: 3, tag: 'غربي' },
  { label: 'مشويات', icon: 'grill', count: 2, tag: 'مشويات' },
  { label: 'صحي', icon: 'leaf', count: 2, tag: 'صحي' }
];

export function favoriteRows(): FoodRow[] {
  return [
    row('mahshi', true, 1, true),
    row('pasta', false, 0, true),
    row('moussaka', true, 2, true),
    row('koshari', false, 0, true),
    row('peppers', true, 1, true)
  ];
}
