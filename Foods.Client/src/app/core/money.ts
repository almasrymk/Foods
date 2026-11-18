let currencyLabel = 'جنيه';

/** The currency of the country the customer is in (from customer/prices/country). ج.م is written "جنيه". */
export function setCurrency(code: string | null | undefined): void {
  const c = (code || '').trim();
  currencyLabel = !c || c === 'ج.م' ? 'جنيه' : c;
}

export function currency(): string {
  return currencyLabel;
}

/** "120 جنيه" / "12.5 ر.س": money as people write it, in the customer's currency. */
export function egp(v: number | null | undefined): string {
  const r = Math.round((v || 0) * 100) / 100;
  const t = Number.isInteger(r) ? String(r) : (Number.isInteger(r * 10) ? r.toFixed(1) : r.toFixed(2));
  return `${t} ${currencyLabel}`;
}

/** Quantity without extra zeros: 1000, 0.5, 333.333. */
export function qtyText(v: number): string {
  return Number.isInteger(v) ? String(v) : String(Math.round(v * 1000) / 1000);
}
