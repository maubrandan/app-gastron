# Ejemplos — Kitchen Card

## Componente standalone completo

```typescript
import { Component, computed, input, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { interval } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

export interface KitchenOrderItem {
  id: string;
  name: string;
  quantity: number;
}

export interface KitchenOrder {
  id: string;
  tableNumber: number;
  confirmedAt: Date;
  items: KitchenOrderItem[];
}

type KitchenCardUrgency = 'normal' | 'warning' | 'critical';

@Component({
  selector: 'app-kitchen-order-card',
  standalone: true,
  imports: [NgClass],
  templateUrl: './kitchen-order-card.component.html',
})
export class KitchenOrderCardComponent {
  readonly order = input.required<KitchenOrder>();

  private readonly timeTick = signal(Date.now());

  constructor() {
    interval(30_000)
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.timeTick.set(Date.now()));
  }

  readonly elapsedMinutes = computed(() => {
    this.timeTick();
    const diffMs = Date.now() - this.order().confirmedAt.getTime();
    return Math.floor(diffMs / 60_000);
  });

  readonly urgency = computed<KitchenCardUrgency>(() => {
    const minutes = this.elapsedMinutes();
    if (minutes >= 20) return 'critical';
    if (minutes >= 15) return 'warning';
    return 'normal';
  });

  readonly cardClasses = computed(() => ({
    'bg-slate-850': this.urgency() !== 'critical',
    'border-slate-700': this.urgency() === 'normal',
    'border-amber-500 animate-pulse': this.urgency() === 'warning',
    'bg-rose-950/30 border-rose-600': this.urgency() === 'critical',
  }));
}
```

```html
<!-- kitchen-order-card.component.html -->
<article
  class="rounded-lg border p-4 transition-colors"
  [ngClass]="cardClasses()"
  [attr.aria-label]="'Comanda mesa ' + order().tableNumber + ', ' + elapsedMinutes() + ' minutos'"
>
  @if (urgency() === 'critical') {
    <span class="mb-2 inline-block rounded px-2 py-0.5 text-xs font-semibold bg-rose-600 text-white">
      URGENTE
    </span>
  }

  <header class="mb-2 flex items-center justify-between gap-2">
    <h2 class="text-lg font-semibold text-slate-100">Mesa {{ order().tableNumber }}</h2>
    <time class="shrink-0 text-sm tabular-nums text-slate-400">{{ elapsedMinutes() }} min</time>
  </header>

  <ul class="space-y-1 text-slate-200">
    @for (item of order().items; track item.id) {
      <li class="flex gap-2">
        <span class="font-medium text-slate-300">{{ item.quantity }}×</span>
        <span>{{ item.name }}</span>
      </li>
    }
  </ul>
</article>
```

## Grid del monitor de cocina

```html
<section class="min-h-screen bg-slate-950 p-4">
  <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
    @for (order of orders(); track order.id) {
      <app-kitchen-order-card [order]="order" />
    }
  </div>
</section>
```

## Nota sobre `bg-slate-850`

Si Tailwind no incluye `slate-850` por defecto, extender el tema:

```javascript
// tailwind.config.js
theme: {
  extend: {
    colors: {
      slate: {
        850: '#1a2332',
      },
    },
  },
},
```
