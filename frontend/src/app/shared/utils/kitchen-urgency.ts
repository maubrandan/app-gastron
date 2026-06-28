export type KitchenUrgency = 'normal' | 'warning' | 'critical';

export function elapsedMinutes(sentToKitchenAt: string, now = Date.now()): number {
  const sentAt = new Date(sentToKitchenAt).getTime();
  return Math.floor((now - sentAt) / 60_000);
}

export function resolveKitchenUrgency(minutes: number): KitchenUrgency {
  if (minutes >= 20) return 'critical';
  if (minutes >= 15) return 'warning';
  return 'normal';
}
