import { ShoppingLine } from './models';

/**
 * "أقل وحدة شراء" — how an ingredient is really bought.
 * 0 normal (grams / count as calculated), 1 package (bottle...), 2 kilo fractions (1/8 kg steps), 3 by money (EGP),
 * 4 by money while under 1/8 kg (0 < quantity < 1/8 kg shows the money amount), then kilo fractions.
 * Quantities stay in the recipe unit (grams/ml); money items keep the amount in EGP.
 */
export const PURCHASE_NORMAL = 0;
export const PURCHASE_PACKAGE = 1;
export const PURCHASE_KILO = 2;
export const PURCHASE_MONEY = 3;
export const PURCHASE_MONEY_KILO = 4;

function moneyKiloUnit(line: Partial<ShoppingLine>): number {
  const step = line.purchaseStep || 0;
  return step > 0 ? step : 125;
}

/** Mode 4: under 1/8 kg the item is bought by money. */
function isMoneyState(line: Partial<ShoppingLine>): boolean {
  const qty = line.quantity || 0;
  if (!(qty > 0 && qty < moneyKiloUnit(line))) return false;
  // With a 1/8 kg price, an amount that reaches the price is already 1/8 kg.
  return (line.purchaseUnitPrice || 0) <= 0 || moneyOf(line, qty) < line.purchaseUnitPrice!;
}

/** Mode 4 settings: gram step, price of one step, money increment and minimum. price 0 = old fixed amount. */
function moneyKilo(line: Partial<ShoppingLine>) {
  const unit = moneyKiloUnit(line);
  const price = line.purchaseUnitPrice || 0;
  const inc = (line.purchaseIncrement || 0) > 0 ? line.purchaseIncrement! : 5;
  const min = (line.purchaseMinAmount || 0) > 0 ? line.purchaseMinAmount! : inc;
  return { unit, price, inc, min };
}

/** Money for grams below 1/8 kg: proportional to the price, up to the next increment, never below the minimum. */
function moneyOf(line: Partial<ShoppingLine>, qty: number): number {
  const { unit, price, inc, min } = moneyKilo(line);
  const raw = qty / unit * price;
  // Small tolerance so grams saved with 3 decimals do not jump to the next step.
  return Math.max(min, Math.ceil(raw / inc - 1e-3) * inc);
}

/** Grams bought for a money amount; reaching the 1/8 kg price switches to 1/8 kg. */
function gramsFor(line: Partial<ShoppingLine>, money: number): number {
  const { unit, price } = moneyKilo(line);
  if (money >= price) return unit;
  return Math.round(money / price * unit * 1000) / 1000;
}

function moneyAmount(line: Partial<ShoppingLine>): number {
  if ((line.purchaseUnitPrice || 0) > 0) return moneyOf(line, line.quantity || 0);
  return line.purchaseIncrement || 30;
}

function trim(value: number): string {
  return Number.isInteger(value) ? String(value) : String(Math.round(value * 100) / 100);
}

export function purchaseMode(line: Partial<ShoppingLine>): number {
  return line.purchaseMode || PURCHASE_NORMAL;
}

/** How much one press of + / − adds or removes. */
export function purchaseStep(line: Partial<ShoppingLine>): number {
  const step = line.purchaseStep || 0;
  switch (purchaseMode(line)) {
    case PURCHASE_PACKAGE: return step > 0 ? step : 1;
    case PURCHASE_KILO: return step > 0 ? step : 125;
    case PURCHASE_MONEY_KILO: return moneyKiloUnit(line);
    case PURCHASE_MONEY: return (line.purchaseIncrement || 0) > 0 ? line.purchaseIncrement! : (step > 0 ? step : 10);
    default: {
      const qty = line.quantity || 0;
      return Math.abs(qty) >= 1 || qty === 0 ? 1 : 0.1;
    }
  }
}

/** The least amount that can be bought (one bottle, 1/8 kg, 20 EGP...). */
export function purchaseMin(line: Partial<ShoppingLine>): number {
  const mode = purchaseMode(line);
  if (mode === PURCHASE_NORMAL) return 0;
  if (mode === PURCHASE_MONEY_KILO) return moneyKiloUnit(line);
  const step = line.purchaseStep || 0;
  if (mode === PURCHASE_PACKAGE) return step > 0 ? step : 1;
  if (mode === PURCHASE_KILO) return step > 0 ? step : 125;
  return step > 0 ? step : 10;
}

/** The kilo fractions people really buy (grams): 1/8, 1/4, 1/3, 1/2, 3/4. Same rule as the server PurchaseRules. */
const FRACTIONS = [125, 250, 1000 / 3, 500, 750];
const EPS = 0.01;

function round3(value: number): number {
  return Math.round(value * 1000) / 1000;
}

/** Fractions at or above the item minimum (min 1/4 drops 1/8; min 1 kilo keeps whole kilos only). */
export function allowedFractions(min: number): number[] {
  return FRACTIONS.filter(f => f >= min - EPS);
}

/** Allowed values around kilo k: k kilos (not 0), k kilos + each fraction, k+1 kilos. */
function kiloCandidates(min: number, k: number): number[] {
  const list: number[] = [];
  if (k > 0) list.push(k * 1000);
  for (const f of allowedFractions(min)) list.push(k * 1000 + f);
  list.push((k + 1) * 1000);
  return list;
}

/** Smallest allowed value at or above the needed grams. */
export function kiloUp(min: number, needed: number): number {
  if (needed <= 0) return 0;
  const k = Math.floor(needed / 1000);
  return round3(Math.min(...kiloCandidates(min, k).filter(v => v >= needed - EPS)));
}

/** Next allowed value above qty. */
function kiloNext(min: number, qty: number): number {
  const k = Math.max(0, Math.floor((qty + EPS) / 1000));
  return round3(Math.min(...kiloCandidates(min, k).filter(v => v > qty + EPS)));
}

/** Previous allowed value below qty, or 0 below the minimum. */
function kiloPrev(min: number, qty: number): number {
  const k = Math.max(0, Math.floor((qty - EPS) / 1000));
  const list = [...kiloCandidates(min, k), ...(k > 0 ? kiloCandidates(min, k - 1) : [])].filter(v => v < qty - EPS);
  return list.length ? round3(Math.max(...list)) : 0;
}

/** Next quantity after pressing + (delta 1) or − (delta -1), kept on the purchase steps. */
export function stepQuantity(line: Partial<ShoppingLine>, delta: number): number {
  let qty = line.quantity || 0;
  const step = purchaseStep(line);
  const mode = purchaseMode(line);
  if (mode === PURCHASE_NORMAL) return Math.max(0, Math.round((qty + delta * step) * 100) / 100);
  if (mode === PURCHASE_KILO) {
    const min = purchaseMin(line);
    return delta > 0 ? kiloNext(min, Math.max(0, qty)) : (qty <= 0 ? 0 : kiloPrev(min, qty));
  }
  if (mode === PURCHASE_MONEY_KILO && (line.purchaseUnitPrice || 0) > 0) {
    const { unit, price, inc, min } = moneyKilo(line);
    if (qty > 0 && qty < unit && !isMoneyState(line)) qty = unit;
    if (delta > 0) {
      if (qty <= 0) return gramsFor(line, min);
      if (qty < unit) return gramsFor(line, moneyOf(line, qty) + inc);
      return kiloNext(unit, qty);
    }
    if (qty <= 0) return 0;
    if (qty < unit) {
      const money = moneyOf(line, qty) - inc;
      return money < min ? 0 : gramsFor(line, money);
    }
    if (qty - unit < EPS) {
      // From 1/8 kg back into the money range: the largest amount below the price on the increment grid.
      const top = Math.max(min, Math.floor((price - inc) / inc + 1e-9) * inc);
      return top >= price ? 0 : gramsFor(line, top);
    }
    return kiloPrev(unit, qty);
  }
  if (mode === PURCHASE_MONEY_KILO) {
    // No 1/8 kg price set: the old fixed amount (1 g marker = "buy by money").
    const unit = moneyKiloUnit(line);
    if (delta > 0) {
      if (qty <= 0) return 1;
      if (qty < unit) return unit;
      return kiloNext(unit, qty);
    }
    if (qty < unit) return 0;
    if (qty - unit < EPS) return 1;
    return kiloPrev(unit, qty);
  }
  const min = purchaseMin(line);
  if (delta > 0) return qty <= 0 ? min : Math.round((qty + step) * 1000) / 1000;
  const next = Math.round((qty - step) * 1000) / 1000;
  return next < min ? 0 : next;
}

const KILO_LABELS: { grams: number; full: string; short: string }[] = [
  { grams: 125, full: '1/8', short: '⅛' },
  { grams: 250, full: '1/4', short: '¼' },
  { grams: 1000 / 3, full: '1/3', short: '⅓' },
  { grams: 500, full: '1/2', short: '½' },
  { grams: 750, full: '3/4', short: '¾' }
];
/** Older quantities saved on other eighths (3/8, 5/8, 7/8) still read correctly. */
const OLD_EIGHTHS: Record<number, { full: string; short: string }> = {
  3: { full: '3/8', short: '⅜' }, 5: { full: '5/8', short: '⅝' }, 7: { full: '7/8', short: '⅞' }
};

function kiloParts(grams: number): { whole: number; full: string; short: string } {
  let whole = Math.floor((grams + EPS) / 1000);
  const rest = grams - whole * 1000;
  if (rest > 1000 - 1) return { whole: whole + 1, full: '', short: '' };
  if (rest < 1) return { whole, full: '', short: '' };
  const label = KILO_LABELS.find(e => Math.abs(rest - e.grams) < 1);
  if (label) return { whole, full: label.full, short: label.short };
  const eighths = Math.round(rest / 125);
  if (eighths >= 8) return { whole: whole + 1, full: '', short: '' };
  const near = KILO_LABELS.find(e => e.grams === eighths * 125) || OLD_EIGHTHS[eighths];
  return near ? { whole, full: near.full, short: near.short } : { whole, full: '', short: '' };
}

/** "3/4 كيلو", "1/3 كيلو", "1 و 1/4 كيلو", "2 كيلو" from grams. */
export function kiloText(grams: number): string {
  const { whole, full } = kiloParts(grams);
  if (!full) return `${whole} كيلو`;
  return whole > 0 ? `${whole} و ${full} كيلو` : `${full} كيلو`;
}

/** Compact kilo token for the small counter bubble: "⅛", "⅓", "1¼", "2". */
export function kiloShort(grams: number): string {
  const { whole, short } = kiloParts(grams);
  if (!short) return String(whole);
  return whole > 0 ? whole + short : short;
}

/** Number shown on the counter. */
export function purchaseQtyText(line: Partial<ShoppingLine>): string {
  const qty = line.quantity || 0;
  switch (purchaseMode(line)) {
    case PURCHASE_PACKAGE: return trim(qty / purchaseMin(line));
    case PURCHASE_KILO: return qty > 0 ? kiloShort(qty) : '0';
    case PURCHASE_MONEY_KILO:
      if (isMoneyState(line)) return trim(moneyAmount(line));
      return qty > 0 ? kiloShort(Math.max(qty, moneyKiloUnit(line))) : '0';
    default: return trim(qty);
  }
}

/** Unit shown next to the counter. */
export function purchaseUnitText(line: Partial<ShoppingLine>): string {
  switch (purchaseMode(line)) {
    case PURCHASE_PACKAGE: return (line.packageName || 'عبوة').trim();
    case PURCHASE_KILO: return 'كيلو';
    case PURCHASE_MONEY: return 'جنيه';
    case PURCHASE_MONEY_KILO: return isMoneyState(line) ? 'جنيه' : 'كيلو';
    default: return (line.unitName || '').trim();
  }
}

/** Full text: "2 زجاجة", "1/8 كيلو", "20 جنيه", "300 جرام". */
export function purchaseText(line: Partial<ShoppingLine>): string {
  if (purchaseMode(line) === PURCHASE_MONEY_KILO) {
    if (isMoneyState(line)) return `${trim(moneyAmount(line))} جنيه`;
    return (line.quantity || 0) > 0 ? kiloText(line.quantity || 0) : '0 كيلو';
  }
  if (purchaseMode(line) === PURCHASE_KILO) return (line.quantity || 0) > 0 ? kiloText(line.quantity || 0) : '0 كيلو';
  const unit = purchaseUnitText(line);
  const qty = purchaseQtyText(line);
  return unit ? `${qty} ${unit}` : qty;
}
