import { Injectable, inject } from '@angular/core';
import { extractApiError, isConcurrencyConflict } from './extract-api-error';
import { NotificationService } from '../notifications/notification.service';

@Injectable({ providedIn: 'root' })
export class HttpErrorReporter {
  private readonly notifications = inject(NotificationService);

  report(error: unknown, fallback: string): void {
    if (isConcurrencyConflict(error)) {
      return;
    }

    this.notifications.showError(extractApiError(error, fallback));
  }
}
