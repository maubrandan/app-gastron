import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../notifications/notification.service';

export const apiErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 409) {
        notifications.showConcurrencyWarning();
      } else if (error.status === 403) {
        notifications.showError('No tenés permisos para realizar esta acción.');
      }

      return throwError(() => error);
    }),
  );
};
