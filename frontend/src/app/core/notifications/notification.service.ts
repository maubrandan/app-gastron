import { Injectable, signal } from '@angular/core';

export type NotificationType = 'success' | 'warning' | 'error';

export interface NotificationState {
  message: string;
  type: NotificationType;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  readonly notification = signal<NotificationState | null>(null);

  showConcurrencyWarning(): void {
    this.show(
      'La mesa fue actualizada por otro mozo. Los datos se recargaron.',
      'warning',
    );
  }

  showSuccess(message: string, durationMs = 4000): void {
    this.show(message, 'success', durationMs);
  }

  showError(message: string, durationMs = 4000): void {
    this.show(message, 'error', durationMs);
  }

  show(message: string, type: NotificationType = 'warning', durationMs = 4000): void {
    this.notification.set({ message, type });
    setTimeout(() => this.notification.set(null), durationMs);
  }
}
