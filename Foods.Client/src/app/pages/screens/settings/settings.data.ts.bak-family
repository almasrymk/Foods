// Settings screen helpers (design/mockups/settings.png). Data comes from the API.

export interface FamilyCard {
  id: string;
  name: string;
  age: number;
  birthDate?: string | null;
  image: string;
}

/** Flag artwork we have; other countries show a pin icon instead. */
export const COUNTRY_FLAGS: Record<string, string> = {
  'مصر': 'assets/screens/settings/flag-eg.png'
};

/** Family avatar by age: children get the child picture, adults alternate father/mother. */
export function familyImage(age: number, adultIndex: number): string {
  if (age < 14) return 'assets/screens/settings/child.png';
  return adultIndex % 2 === 0 ? 'assets/screens/settings/father.png' : 'assets/screens/settings/mother.png';
}

export type NotifyKey = 'notifyLogin' | 'notifyWelcome' | 'notifyBirthday' | 'notifyOrders' | 'notifyPlanning';

export const NOTIFY_ROWS: { key: NotifyKey; icon: string; label: string; sub: string }[] = [
  { key: 'notifyLogin', icon: 'bell', label: 'تذكير بالدخول', sub: 'لو مدخلتش التطبيق 3 أيام' },
  { key: 'notifyWelcome', icon: 'home', label: 'ترحيب بالعودة', sub: 'لما ترجع بعد غياب أسبوعين' },
  { key: 'notifyBirthday', icon: 'cake', label: 'أعياد ميلاد الأسرة', sub: 'قبلها بيوم ويوم عيد الميلاد' },
  { key: 'notifyOrders', icon: 'receipt', label: 'تذكير بالطلبات', sub: 'طلب لسه متطبعش ولا اتبعت' },
  { key: 'notifyPlanning', icon: 'calendar', label: 'تخطيط الأسبوع', sub: 'تذكير أسبوعي بتخطيط الوجبات' }
];

export const ABOUT_ROWS = [
  { id: 'version', icon: 'layers', label: 'الإصدار', sub: '1.0.0', soon: false },
  { id: 'privacy', icon: 'shield', label: 'سياسة الخصوصية', sub: '', soon: true },
  { id: 'terms', icon: 'doc', label: 'الشروط والأحكام', sub: '', soon: true }
];
