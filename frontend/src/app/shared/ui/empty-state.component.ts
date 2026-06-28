import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center rounded-[var(--radius-card)] border border-dashed border-slate-300 bg-slate-50/50 px-6 py-10 text-center">
      <span class="mb-3 text-3xl opacity-40" aria-hidden="true">{{ icon() }}</span>
      <p class="text-sm font-medium text-slate-600">{{ message() }}</p>
      @if (hint()) {
        <p class="mt-1 text-xs text-slate-400">{{ hint() }}</p>
      }
    </div>
  `,
})
export class EmptyStateComponent {
  readonly message = input.required<string>();
  readonly hint = input<string | null>(null);
  readonly icon = input('📋');
}
