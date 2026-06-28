import { Injectable } from '@angular/core';

const MUTE_KEY = 'resto.kitchen.muted';

@Injectable({ providedIn: 'root' })
export class KitchenAlertService {
  private lastPlayedAt = 0;
  private readonly debounceMs = 2000;

  isMuted(): boolean {
    return localStorage.getItem(MUTE_KEY) === 'true';
  }

  setMuted(muted: boolean): void {
    localStorage.setItem(MUTE_KEY, muted ? 'true' : 'false');
  }

  toggleMuted(): boolean {
    const next = !this.isMuted();
    this.setMuted(next);
    return next;
  }

  playNewOrderSound(): void {
    if (this.isMuted()) return;

    const now = Date.now();
    if (now - this.lastPlayedAt < this.debounceMs) return;
    this.lastPlayedAt = now;

    try {
      const context = new AudioContext();
      const oscillator = context.createOscillator();
      const gain = context.createGain();

      oscillator.type = 'sine';
      oscillator.frequency.setValueAtTime(880, context.currentTime);
      oscillator.frequency.exponentialRampToValueAtTime(440, context.currentTime + 0.15);

      gain.gain.setValueAtTime(0.25, context.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.01, context.currentTime + 0.3);

      oscillator.connect(gain);
      gain.connect(context.destination);

      oscillator.start(context.currentTime);
      oscillator.stop(context.currentTime + 0.3);
      void context.close();
    } catch {
      // Audio no disponible en este navegador
    }
  }
}
