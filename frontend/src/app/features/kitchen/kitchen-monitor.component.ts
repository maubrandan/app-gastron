import { DatePipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { RestoApiService } from '../../core/api/resto-api.service';
import { KitchenAlertService } from '../../core/kitchen/kitchen-alert.service';
import { SignalRService } from '../../core/signalr/signalr.service';
import { KitchenOrder } from '../../shared/models/resto.models';
import { ConnectionPillComponent } from '../../shared/ui/connection-pill.component';
import { KitchenOrderCardComponent } from './kitchen-order-card.component';

const CATEGORY_STORAGE_KEY = 'resto.kitchen.category';

@Component({
  selector: 'app-kitchen-monitor',
  standalone: true,
  imports: [KitchenOrderCardComponent, ConnectionPillComponent, DatePipe, NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="relative min-h-screen bg-surface-kitchen p-4">
      <header class="mb-6 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 class="font-display text-2xl font-bold text-slate-100">Monitor de Cocina</h1>
          <p class="mt-1 text-sm text-slate-400">
            {{ visibleOrders().length }} comanda{{ visibleOrders().length === 1 ? '' : 's' }} activa{{ visibleOrders().length === 1 ? '' : 's' }}
            · {{ now() | date: 'HH:mm' }}
          </p>
        </div>
        <div class="flex flex-wrap items-center gap-3">
          <button
            type="button"
            class="rounded-[var(--radius-card)] border px-3 py-1.5 text-sm font-medium transition"
            [ngClass]="alerts.isMuted()
              ? 'border-slate-600 text-slate-400'
              : 'border-emerald-500/50 text-emerald-400'"
            (click)="toggleMute()"
          >
            {{ alerts.isMuted() ? '🔇 Silenciado' : '🔔 Alertas activas' }}
          </button>
          <app-connection-pill [state]="connectionState()" />
        </div>
      </header>

      <nav class="mb-4 flex flex-wrap gap-2">
        <button
          type="button"
          class="rounded-full px-4 py-1.5 text-sm font-medium transition"
          [ngClass]="selectedCategory() === null
            ? 'bg-emerald-600 text-white'
            : 'bg-slate-800 text-slate-300 hover:bg-slate-700'"
          (click)="selectCategory(null)"
        >
          Todas
        </button>
        @for (category of categories(); track category) {
          <button
            type="button"
            class="rounded-full px-4 py-1.5 text-sm font-medium transition"
            [ngClass]="selectedCategory() === category
              ? 'bg-emerald-600 text-white'
              : 'bg-slate-800 text-slate-300 hover:bg-slate-700'"
            (click)="selectCategory(category)"
          >
            {{ category }}
          </button>
        }
      </nav>

      @if (connectionState() !== 'connected') {
        <p class="mb-4 rounded-[var(--radius-card)] border border-amber-500/30 bg-amber-950/20 px-4 py-2 text-center text-sm text-amber-400">
          Sin conexión en tiempo real — la información puede estar desactualizada.
        </p>
      }

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        @for (order of visibleOrders(); track order.id) {
          <app-kitchen-order-card
            [order]="order"
            [selectedCategory]="selectedCategory()"
            [isNew]="newOrderIds().has(order.id)"
          />
        } @empty {
          <p class="col-span-full py-16 text-center text-slate-400">
            No hay comandas activas{{ selectedCategory() ? ' en ' + selectedCategory() : '' }}.
          </p>
        }
      </div>
    </section>
  `,
})
export class KitchenMonitorComponent implements OnInit {
  private readonly api = inject(RestoApiService);
  private readonly signalR = inject(SignalRService);
  readonly alerts = inject(KitchenAlertService);

  readonly orders = signal<KitchenOrder[]>([]);
  readonly categories = signal<string[]>([]);
  readonly selectedCategory = signal<string | null>(this.loadStoredCategory());
  readonly newOrderIds = signal<Set<string>>(new Set());
  readonly now = signal(new Date());
  readonly connectionState = this.signalR.connectionState;

  readonly visibleOrders = computed(() => {
    const category = this.selectedCategory();
    const all = this.orders();

    if (!category) return all;

    return all
      .map((order) => ({
        ...order,
        lines: order.lines.filter((l) => l.category === category),
      }))
      .filter((order) => order.lines.length > 0);
  });

  ngOnInit(): void {
    setInterval(() => this.now.set(new Date()), 60_000);
    void this.bootstrap();
  }

  selectCategory(category: string | null): void {
    this.selectedCategory.set(category);
    if (category) {
      localStorage.setItem(CATEGORY_STORAGE_KEY, category);
    } else {
      localStorage.removeItem(CATEGORY_STORAGE_KEY);
    }
  }

  toggleMute(): void {
    this.alerts.toggleMuted();
  }

  private loadStoredCategory(): string | null {
    return localStorage.getItem(CATEGORY_STORAGE_KEY);
  }

  private async bootstrap(): Promise<void> {
    const [initial, products] = await Promise.all([
      this.api.getActiveKitchenOrders(),
      this.api.getProducts(),
    ]);

    this.orders.set(initial);
    this.categories.set([...new Set(products.map((p) => p.category))].sort());

    await this.signalR.connectKitchen({
      onOrderAdded: (order) => {
        this.alerts.playNewOrderSound();
        this.markOrderAsNew(order.id);

        this.orders.update((current) => {
          if (current.some((o) => o.id === order.id)) return current;
          return [...current, order].sort(
            (a, b) => new Date(a.sentToKitchenAt).getTime() - new Date(b.sentToKitchenAt).getTime(),
          );
        });
      },
      onOrderRemoved: (orderId) => {
        this.orders.update((current) => current.filter((o) => o.id !== orderId));
        this.newOrderIds.update((current) => {
          const next = new Set(current);
          next.delete(orderId);
          return next;
        });
      },
    });
  }

  private markOrderAsNew(orderId: string): void {
    this.newOrderIds.update((current) => new Set([...current, orderId]));
    setTimeout(() => {
      this.newOrderIds.update((current) => {
        const next = new Set(current);
        next.delete(orderId);
        return next;
      });
    }, 5000);
  }
}
