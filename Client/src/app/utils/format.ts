export function truncateAddress(addr: string): string {
  if (!addr || addr.length <= 12) return addr;
  return addr.slice(0, 6) + '…' + addr.slice(-4);
}

export function formatCurrency(value: number): string {
  return '$' + value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export function formatPercent(value: number): string {
  return (value >= 0 ? '+' : '') + value.toFixed(2) + '%';
}
