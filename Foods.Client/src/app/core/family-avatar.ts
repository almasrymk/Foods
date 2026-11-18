import { mediaUrl } from './environment';

/** Relations offered in the family popup (أخرى lets the user type their own). */
export const RELATIONS = ['أب', 'أم', 'زوج', 'زوجة', 'ابن', 'بنت', 'أخ', 'أخت', 'جد', 'جدة', 'أخرى'];

const MALE = new Set(['أب', 'زوج', 'ابن', 'أخ', 'جد']);
const FEMALE = new Set(['أم', 'زوجة', 'بنت', 'أخت', 'جدة']);

/** Same rules as the API (FamilyAvatar.For): relation gender + age band; جد/جدة have their own. */
export function familyAvatar(relation: string | null | undefined, age: number | null | undefined): string {
  const r = (relation || '').trim();
  if (r === 'جد') return 'assets/family/grandpa.svg';
  if (r === 'جدة') return 'assets/family/grandma.svg';
  const male = MALE.has(r), female = FEMALE.has(r);
  if (!male && !female) return 'assets/family/person.svg';
  const a = age ?? 30;
  const band = a < 4 ? 'baby' : a <= 15 ? 'kid' : a <= 25 ? 'young' : 'adult';
  const names: Record<string, [string, string]> = {
    baby: ['baby-boy', 'baby-girl'], kid: ['boy', 'girl'], young: ['young-man', 'young-woman'], adult: ['man', 'woman']
  };
  return `assets/family/${names[band][male ? 0 : 1]}.svg`;
}

/** A picture path from the API: uploads go through the API host, client assets stay relative. */
export function pictureUrl(path: string | null | undefined): string {
  if (!path) return '';
  return path.startsWith('/uploads') || path.startsWith('http') ? mediaUrl(path) : path;
}

/** Age in whole years and months from a yyyy-MM-dd birth date (null when missing/invalid). */
export function ageParts(birthDate: string | null | undefined, today = new Date()): { years: number; months: number } | null {
  if (!birthDate) return null;
  const [y, m, d] = birthDate.slice(0, 10).split('-').map(Number);
  if (!y || !m || !d) return null;
  let months = (today.getFullYear() - y) * 12 + (today.getMonth() + 1 - m);
  if (today.getDate() < d) months--;
  if (months < 0) return null;
  return { years: Math.floor(months / 12), months };
}

const ar = (n: number) => n.toLocaleString('ar-EG');

/** "٣٢ سنة", "سنتين", "٨ شهور", "شهر", "حديث الولادة". */
export function ageText(years: number | null | undefined, months?: number | null): string {
  if (years == null) return '';
  if (years < 1 && months != null) {
    if (months < 1) return 'حديث الولادة';
    if (months === 1) return 'شهر';
    if (months === 2) return 'شهرين';
    return `${ar(months)} ${months <= 10 ? 'شهور' : 'شهر'}`;
  }
  if (years === 1) return 'سنة';
  if (years === 2) return 'سنتين';
  return `${ar(years)} ${years <= 10 ? 'سنين' : 'سنة'}`;
}
