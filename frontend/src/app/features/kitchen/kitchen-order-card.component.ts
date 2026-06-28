import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { interval } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { KitchenOrder, KitchenOrderLine } from '../../shared/models/resto.models';
import { elapsedMinutes, resolveKitchenUrgency } from '../../shared/utils/kitchen-urgency';

type KitchenCardUrgency = 'normal' | 'warning' | 'critical';

@Component({
  selector: 'app-kitchen-order-card',
  standalone: true,
  imports: [NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article
      class="rounded-[var(--radius-card)] border p-4 shadow-card transition-all duration-300"
      [ngClass]="cardClasses()"
      [attr.aria-label]="'Comanda mesa ' + order().tableNumber + ', ' + elapsedMinutes() + ' minutos'"
    >
      @if (isNew()) {
        <span
          class="mb-2 inline-block rounded px-2 py-0.5 text-xs font-semibold bg-amber-500 text-slate-900 animate-pulse"
        >
          NUEVA
        </span>
      }

      @if (urgency() === 'critical') {
        <span
          class="mb-2 inline-block rounded px-2 py-0.5 text-xs font-semibold bg-rose-600 text-white animate-pulse"
        >
          URGENTE
        </span>
      }

      <header class="mb-3 flex items-center justify-between gap-2">
        <h2 class="font-display text-2xl font-bold tabular-nums text-slate-100">
          Mesa {{ order().tableNumber }}
        </h2>
        <time class="shrink-0 rounded px-2 py-0.5 text-sm tabular-nums" [ngClass]="timerClasses()">
          {{ elapsedMinutes() }} min
        </time>
      </header>

      @if (showGrouped()) {
        @for (group of groupedLines(); track group.category) {
          <div class="mb-2">
            <p class="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-500">{{ group.category }}</p>
            <ul class="space-y-1.5 text-slate-200">
              @for (item of group.lines; track item.id) {
                <li class="flex gap-2">
                  <span class="font-medium text-emerald-400">{{ item.quantity }}×</span>
                  <span>{{ item.productName }}</span>
                  @if (item.notes) {
                    <span class="text-slate-400 italic">({{ item.notes }})</span>
                  }
                </li>
              }
            </ul>
          </div>
        }
      } @else {
        <ul class="space-y-1.5 text-slate-200">
          @for (item of visibleLines(); track item.id) {
            <li class="flex gap-2">
              <span class="font-medium text-emerald-400">{{ item.quantity }}×</span>
              <span>{{ item.productName }}</span>
              @if (item.notes) {
                <span class="text-slate-400 italic">({{ item.notes }})</span>
              }
            </li>
          }
        </ul>
      }
    </article>
  `,
})
export class KitchenOrderCardComponent {
  readonly order = input.required<KitchenOrder>();
  readonly selectedCategory = input<string | null>(null);
  readonly isNew = input(false);

  private readonly timeTick = signal(Date.now());

  constructor() {
    interval(30_000)
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.timeTick.set(Date.now()));
  }

  readonly visibleLines = computed(() => {
    const category = this.selectedCategory();
    const lines = this.order().lines;
    if (!category) return lines;
    return lines.filter((l) => l.category === category);
  });

  readonly showGrouped = computed(() => !this.selectedCategory());

  readonly groupedLines = computed(() => {
    const groups = new Map<string, KitchenOrderLine[]>();
    for (const line of this.order().lines) {
      const existing = groups.get(line.category) ?? [];
      existing.push(line);
      groups.set(line.category, existing);
    }
    return [...groups.entries()].map(([category, lines]) => ({ category, lines }));
  });

  readonly elapsedMinutes = computed(() => {
    this.timeTick();
    return elapsedMinutes(this.order().sentToKitchenAt);
  });

  readonly urgency = computed<KitchenCardUrgency>(() =>
    resolveKitchenUrgency(this.elapsedMinutes()),
  );

  readonly cardClasses = computed(() => {
    if (this.isNew()) {
      return {
        'bg-amber-950/40 border-amber-500 animate-pulse shadow-glow-warning': true,
      };
    }
    const urgency = this.urgency();
    return {
      'bg-slate-850 border-slate-700': urgency === 'normal',
      'border-amber-500 animate-pulse shadow-glow-warning': urgency === 'warning',
      'bg-rose-950/30 border-rose-600 shadow-glow-critical': urgency === 'critical',
    };
  });

  readonly timerClasses = computed(() => ({
    'text-slate-400': this.urgency() === 'normal',
    'text-amber-400 font-semibold': this.urgency() === 'warning',
    'bg-rose-600 text-white font-semibold animate-pulse': this.urgency() === 'critical',
  }));
}
