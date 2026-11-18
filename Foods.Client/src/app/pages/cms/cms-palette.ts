export interface CmsTone {
  color: string;
  textColor: string;
  label: string;
}

export interface CmsIconOption {
  key: string;
  label: string;
}

export const CMS_COLOR_PALETTE: CmsTone[] = [
  { color: '#FFEAC5', textColor: '#3A2210', label: 'كريمي' },
  { color: '#E2F1FB', textColor: '#1E3A5F', label: 'أزرق' },
  { color: '#FFE8D6', textColor: '#4A2A12', label: 'خوخي' },
  { color: '#E4F2C6', textColor: '#1E2A1E', label: 'أخضر' },
  { color: '#FEDD8C', textColor: '#4A2A12', label: 'أصفر' },
  { color: '#E7D8F4', textColor: '#4E1F78', label: 'بنفسجي' },
  { color: '#FDE4EB', textColor: '#4A1F2E', label: 'وردي' },
  { color: '#FFF3E6', textColor: '#F37202', label: 'برتقالي' },
  { color: '#D8F3E8', textColor: '#1A4A3A', label: 'نعناعي' },
  { color: '#D9EEF2', textColor: '#1A4A55', label: 'تركواز' },
  { color: '#FFE0D6', textColor: '#6B2A1A', label: 'مرجاني' },
  { color: '#EEE8E1', textColor: '#4A3A2E', label: 'رمادي' }
];

export const CMS_ICON_PALETTE: CmsIconOption[] = [
  { key: 'cloche', label: 'غطاء' },
  { key: 'chef', label: 'شيف' },
  { key: 'grill', label: 'شواء' },
  { key: 'leaf', label: 'صحي' },
  { key: 'vegan', label: 'نباتي' },
  { key: 'utensils', label: 'أدوات' },
  { key: 'bowl', label: 'طبق' },
  { key: 'cake', label: 'حلويات' },
  { key: 'pepper', label: 'فلفل' },
  { key: 'fish', label: 'سمك' },
  { key: 'cup', label: 'مشروب' },
  { key: 'basket', label: 'سلة' },
  { key: 'heart', label: 'مفضل' },
  { key: 'grid', label: 'عام' }
];

export function isCmsImagePath(path?: string | null): boolean {
  return !!path && (path.includes('/') || path.startsWith('http'));
}

export function cmsToneLabel(color?: string | null): string {
  return CMS_COLOR_PALETTE.find(tone => tone.color === color)?.label || color || 'بدون لون';
}

export function cmsIconLabel(key?: string | null): string {
  return CMS_ICON_PALETTE.find(icon => icon.key === key)?.label || key || 'بدون أيقونة';
}
