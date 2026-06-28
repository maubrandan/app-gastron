import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type ConnectionState = 'connected' | 'reconnecting' | 'disconnected';

@Component({
  selector: 'app-connection-pill',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="inline-flex items-center gap-2 rounded-[var(--radius-pill)] bg-slate-900/80 px-3 py-1 text-xs text-slate-300 backdrop-blur-sm"
      [attr.title]="label()"
    >
      <span
        class="h-2.5 w-2.5 rounded-full"
        [class.bg-emerald-500]="state() === 'connected'"
        [class.bg-amber-400]="state() === 'reconnecting'"
        [class.bg-rose-500]="state() === 'disconnected'"
      ></span>
      {{ label() }}
    </div>
  `,
})
export class ConnectionPillComponent {
  readonly state = input.required<ConnectionState>();

  label(): string {
    switch (this.state()) {
      case 'connected':
        return 'Sincronizado';
      case 'reconnecting':
        return 'Reconectando…';
      default:
        return 'Sin conexión';
    }
  }
}
