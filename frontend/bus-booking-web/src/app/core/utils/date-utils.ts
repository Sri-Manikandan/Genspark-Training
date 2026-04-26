/**
 * Formats a Date as YYYY-MM-DD using local calendar fields.
 *
 * Do NOT use Date.toISOString() for this — it converts to UTC first, which
 * shifts the date back by one day in any timezone ahead of UTC (e.g. IST +5:30).
 */
export function toLocalDateString(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}
