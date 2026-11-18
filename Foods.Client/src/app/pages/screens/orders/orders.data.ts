import { Chip } from '../shared/screens.data';

export type OrderStatus = 'done' | 'progress' | 'cancelled';

export interface OrderLine { name: string; qty: string; }

export interface OrderCard {
  id: string;
  code: string;
  date: string;
  status: OrderStatus;
  open: boolean;
  dishes: { name: string; count: number; image: string }[];
  ingredients: OrderLine[];
}

export const ORDER_STATUS: Record<OrderStatus, { label: string; icon: string }> = {
  done: { label: 'مكتمل', icon: 'check' },
  progress: { label: 'قيد التنفيذ', icon: 'clock' },
  cancelled: { label: 'ملغي', icon: 'x' }
};

export const ORDER_CHIPS: Chip[] = [
  { label: 'الكل', icon: 'grid', count: 24, tag: 'all' },
  { label: 'هذا الشهر', icon: 'calendar', count: 8, tag: 'month' },
  { label: 'الشهر الماضي', icon: 'calendar', count: 12, tag: 'last' },
  { label: 'الأقدم', icon: 'history', tag: 'old' }
];

const riceMilk = { name: 'أرز بلبن الخفيفة', count: 1, image: 'assets/screens/orders/rice-pudding.png' };
const riceMilkLines: OrderLine[] = [
  { name: 'أرز', qty: '60 جرام' },
  { name: 'سكر', qty: '30 جرام' },
  { name: 'لبن', qty: '250 ملليتر' }
];

const order = (day: string, status: OrderStatus, open = false): OrderCard => ({
  id: day,
  code: `${day}-11-2026`,
  date: `2026-11-${day}`,
  status,
  open,
  dishes: [riceMilk],
  ingredients: riceMilkLines
});

export function orderCards(): OrderCard[] {
  return [order('09', 'done', true), order('08', 'progress'), order('07', 'cancelled'), order('05', 'done'), order('02', 'done')];
}
