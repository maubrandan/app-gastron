import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgClass } from '@angular/common';

export type ActionButtonVariant = 'primary' | 'success' | 'danger' | 'ghost';

@Component({
  selector: 'app-action-button',
  standalone: true,
  imports: [NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="touch-target inline-flex min-h-11 items-center justify-center gap-2 rounded-[var(--radius-card)] px-4 py-2 text-sm font-semibold transition-all duration-150 disabled:cursor-not-allowed disabled:opacity-50"
      [ngClass]="buttonClasses()"
      [disabled]="disabled() || loading()"
      (click)="action.emit()"
    >
      @if (loading()) {
        <span class="inline-block h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent"></span>
      }
      <ng-content />
    </button>
  `,
})
export class ActionButtonComponent {
  readonly variant = input<ActionButtonVariant>('primary');
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly fullWidth = input(false);

  readonly action = output<void>();

  buttonClasses(): Record<string, boolean> {
    const variant = this.variant();
    return {
      'w-full': this.fullWidth(),
      'bg-action-primary text-white hover:brightness-110': variant === 'primary',
      'bg-action-success text-white hover:brightness-110': variant === 'success',
      'bg-action-danger text-white hover:brightness-110': variant === 'danger',
      'border border-slate-600 bg-transparent text-slate-200 hover:bg-slate-800': variant === 'ghost',
    };
  }
}
