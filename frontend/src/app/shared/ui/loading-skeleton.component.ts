import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (variant() === 'grid') {
      <div class="grid grid-cols-2 gap-4 md:grid-cols-4">
        @for (i of skeletonItems(); track i) {
          <div class="h-20 animate-pulse rounded-[var(--radius-card)] bg-slate-200"></div>
        }
      </div>
    } @else {
      <div class="space-y-3">
        @for (i of skeletonItems(); track i) {
          <div class="h-12 animate-pulse rounded-[var(--radius-card)] bg-slate-200"></div>
        }
      </div>
    }
  `,
})
export class LoadingSkeletonComponent {
  readonly variant = input<'grid' | 'list'>('grid');
  readonly count = input(8);

  skeletonItems(): number[] {
    return Array.from({ length: this.count() }, (_, i) => i);
  }
}
