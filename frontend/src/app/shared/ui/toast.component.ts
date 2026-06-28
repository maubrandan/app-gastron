import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NgClass } from '@angular/common';
import { NotificationService } from '../../core/notifications/notification.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (notification.notification(); as toast) {
      <div
        role="alert"
        class="fixed left-4 right-4 top-4 z-50 animate-slide-down rounded-[var(--radius-card)] px-4 py-3 text-sm font-medium shadow-elevated"
        [ngClass]="toastClasses(toast.type)"
      >
        {{ toast.message }}
      </div>
    }
  `,
})
export class ToastComponent {
  readonly notification = inject(NotificationService);

  toastClasses(type: string): Record<string, boolean> {
    return {
      'bg-amber-500 text-slate-900': type === 'warning',
      'bg-emerald-600 text-white': type === 'success',
      'bg-rose-600 text-white': type === 'error',
    };
  }
}
