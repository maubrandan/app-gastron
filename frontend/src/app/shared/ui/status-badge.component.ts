import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="inline-flex items-center rounded-[var(--radius-pill)] px-2.5 py-0.5 text-xs font-semibold"
      [ngClass]="badgeClasses()"
    >
      {{ label() }}
    </span>
  `,
})
export class StatusBadgeComponent {
  readonly status = input.required<string>();

  label(): string {
    switch (this.status()) {
      case 'Borrador':
        return 'Borrador';
      case 'ConfirmadoEnCocina':
        return 'En cocina';
      case 'Cerrado':
        return 'Cerrado';
      default:
        return this.status();
    }
  }

  badgeClasses(): Record<string, boolean> {
    switch (this.status()) {
      case 'Borrador':
        return { 'bg-slate-700 text-slate-200': true };
      case 'ConfirmadoEnCocina':
        return { 'bg-emerald-900/60 text-emerald-300': true };
      case 'Cerrado':
        return { 'bg-slate-800 text-slate-400': true };
      default:
        return { 'bg-slate-700 text-slate-200': true };
    }
  }
}
