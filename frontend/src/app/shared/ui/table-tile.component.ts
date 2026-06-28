import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-table-tile',
  standalone: true,
  imports: [NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="touch-target w-full rounded-[var(--radius-card)] border-2 p-4 text-left font-semibold transition-all duration-150"
      [ngClass]="tileClasses()"
      [attr.aria-label]="'Mesa ' + number() + ', ' + statusLabel()"
      [attr.aria-pressed]="selected()"
      (click)="selectedChange.emit()"
    >
      <span class="font-display text-lg">Mesa {{ number() }}</span>
      <span class="mt-1 block text-xs font-normal opacity-80">{{ statusLabel() }}</span>
    </button>
  `,
})
export class TableTileComponent {
  readonly number = input.required<number>();
  readonly status = input.required<string>();
  readonly selected = input(false);
  readonly variant = input<'salon' | 'control'>('salon');

  readonly selectedChange = output<void>();

  statusLabel(): string {
    switch (this.status()) {
      case 'Libre':
        return 'Libre';
      case 'Atendiendo':
        return 'Atendiendo';
      case 'EsperandoCuenta':
        return 'Esperando cuenta';
      default:
        return this.status();
    }
  }

  tileClasses(): Record<string, boolean> {
    const status = this.status();
    const selected = this.selected();
    const variant = this.variant();

    if (variant === 'control') {
      return {
        'border-slate-600 bg-surface-control-card text-slate-100 hover:border-slate-500':
          status !== 'EsperandoCuenta' && !selected,
        'border-amber-500 bg-amber-950/40 text-amber-100 animate-pulse':
          status === 'EsperandoCuenta' && !selected,
        'ring-2 ring-action-primary border-action-primary bg-surface-control-card text-slate-100':
          selected && status !== 'EsperandoCuenta',
        'ring-2 ring-amber-400 border-amber-400 bg-amber-950/50 text-amber-100':
          selected && status === 'EsperandoCuenta',
        'shadow-elevated': selected,
      };
    }

    return {
      'border-status-free bg-status-free-bg text-emerald-900': status === 'Libre' && !selected,
      'border-status-waiting bg-status-waiting-bg text-amber-900':
        status === 'EsperandoCuenta' && !selected,
      'border-status-occupied bg-status-occupied-bg text-blue-900':
        status !== 'Libre' && status !== 'EsperandoCuenta' && !selected,
      'ring-2 ring-action-primary border-action-primary shadow-elevated': selected,
    };
  }
}
