import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { interval } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { KitchenOrder } from '../models/resto.models';
import { elapsedMinutes, resolveKitchenUrgency } from '../utils/kitchen-urgency';

@Component({
  selector: 'app-kitchen-delay-row',
  standalone: true,
  imports: [NgClass, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <li
      class="flex items-center justify-between gap-3 rounded-[var(--radius-card)] border px-3 py-2 text-sm transition-colors"
      [ngClass]="rowClasses()"
    >
      <div>
        <span class="font-semibold">Mesa {{ order().tableNumber }}</span>
        <span class="ml-2 text-xs opacity-70">
          enviada {{ order().sentToKitchenAt | date: 'HH:mm' }}
        </span>
      </div>
      <div class="flex items-center gap-2">
        @if (urgency() === 'critical') {
          <span class="rounded px-1.5 py-0.5 text-xs font-semibold bg-rose-600 text-white">URGENTE</span>
        }
        <time class="tabular-nums font-medium" [ngClass]="timerClasses()">
          {{ minutes() }} min
        </time>
      </div>
    </li>
  `,
})
export class KitchenDelayRowComponent {
  readonly order = input.required<KitchenOrder>();

  private readonly timeTick = signal(Date.now());

  constructor() {
    interval(30_000)
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.timeTick.set(Date.now()));
  }

  readonly minutes = computed(() => {
    this.timeTick();
    return elapsedMinutes(this.order().sentToKitchenAt);
  });

  readonly urgency = computed(() => resolveKitchenUrgency(this.minutes()));

  readonly rowClasses = computed(() => ({
    'border-slate-700 bg-surface-control-card text-slate-200': this.urgency() === 'normal',
    'border-amber-500 bg-amber-950/20 text-amber-100 animate-pulse shadow-glow-warning':
      this.urgency() === 'warning',
    'border-rose-600 bg-rose-950/30 text-rose-100 shadow-glow-critical': this.urgency() === 'critical',
  }));

  readonly timerClasses = computed(() => ({
    'text-slate-400': this.urgency() === 'normal',
    'text-amber-400': this.urgency() === 'warning',
    'text-rose-300': this.urgency() === 'critical',
  }));
}
