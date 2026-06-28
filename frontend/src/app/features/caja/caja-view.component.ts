import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { RestoApiService } from '../../core/api/resto-api.service';
import { NotificationService } from '../../core/notifications/notification.service';
import { ClosedOrderSummary, DailySummary, OrderDetail } from '../../shared/models/resto.models';
import { ActionButtonComponent } from '../../shared/ui/action-button.component';
import { LoadingSkeletonComponent } from '../../shared/ui/loading-skeleton.component';
import { OrderLineListComponent } from '../../shared/ui/order-line-list.component';
import { ReceiptPrintComponent } from '../../shared/ui/receipt-print.component';

@Component({
  selector: 'app-caja-view',
  standalone: true,
  imports: [
    DecimalPipe,
    DatePipe,
    LoadingSkeletonComponent,
    OrderLineListComponent,
    ActionButtonComponent,
    ReceiptPrintComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="min-h-screen bg-surface-control p-4 text-slate-100 md:p-6">
      <div class="mx-auto max-w-6xl">
        <header class="mb-6 flex flex-wrap items-end justify-between gap-4">
          <div>
            <h1 class="font-display text-2xl font-bold">Caja del día</h1>
            <p class="mt-1 text-sm text-slate-400">Resumen de pedidos cerrados y facturación</p>
          </div>
          <label class="flex flex-col gap-1 text-sm">
            <span class="text-slate-400">Fecha</span>
            <input
              type="date"
              class="rounded-[var(--radius-card)] border border-slate-600 bg-slate-900 px-3 py-2 text-slate-100"
              [value]="selectedDate()"
              (change)="onDateChange($event)"
            />
          </label>
        </header>

        @if (loading()) {
          <app-loading-skeleton variant="grid" [count]="3" />
        } @else if (summary(); as s) {
          <div class="mb-6 grid gap-4 sm:grid-cols-3">
            <div class="rounded-[var(--radius-card)] border border-slate-700/50 bg-surface-control-card p-4 shadow-card">
              <p class="text-sm text-slate-400">Pedidos cerrados</p>
              <p class="font-display text-3xl font-bold">{{ s.orderCount }}</p>
            </div>
            <div class="rounded-[var(--radius-card)] border border-slate-700/50 bg-surface-control-card p-4 shadow-card">
              <p class="text-sm text-slate-400">Facturación total</p>
              <p class="font-display text-3xl font-bold">{{ s.totalRevenue | number: '1.2-2' }}</p>
            </div>
            <div class="rounded-[var(--radius-card)] border border-slate-700/50 bg-surface-control-card p-4 shadow-card">
              <p class="text-sm text-slate-400">Ticket promedio</p>
              <p class="font-display text-3xl font-bold">{{ s.averageTicket | number: '1.2-2' }}</p>
            </div>
          </div>

          <div class="grid gap-6 lg:grid-cols-2">
            <div class="rounded-[var(--radius-card)] border border-slate-700/50 bg-surface-control-card p-4 shadow-card">
              <h2 class="mb-3 text-lg font-semibold">Pedidos del día</h2>
              <ul class="max-h-96 space-y-2 overflow-y-auto">
                @for (closed of closedOrders(); track closed.id) {
                  <li>
                    <button
                      type="button"
                      class="flex w-full items-center justify-between rounded-[var(--radius-card)] px-3 py-2 text-left transition hover:bg-slate-800"
                      [class.bg-slate-800]="selectedOrderId() === closed.id"
                      (click)="selectOrder(closed.id)"
                    >
                      <span>Mesa {{ closed.tableNumber }} · {{ closed.lineCount }} ítems</span>
                      <span class="tabular-nums font-semibold">{{ closed.total | number: '1.2-2' }}</span>
                    </button>
                  </li>
                } @empty {
                  <li class="text-sm text-slate-400">Sin pedidos cerrados en esta fecha.</li>
                }
              </ul>
            </div>

            <div class="rounded-[var(--radius-card)] border border-slate-700/50 bg-surface-control-card p-4 shadow-card">
              @if (orderDetail(); as detail) {
                <h2 class="mb-2 text-lg font-semibold">Detalle — Mesa {{ detail.tableNumber }}</h2>
                <p class="mb-4 text-sm text-slate-400">
                  Cerrado: {{ detail.closedAt | date: 'dd/MM/yyyy HH:mm' }}
                </p>
                <app-order-line-list [lines]="detail.lines" [showPrices]="true" />
                <p class="my-4 font-display text-lg font-bold">
                  Total: {{ detail.total | number: '1.2-2' }}
                </p>
                <app-action-button variant="primary" (action)="printBill()">
                  Reimprimir ticket
                </app-action-button>
              } @else {
                <p class="text-sm text-slate-400">Seleccioná un pedido para ver el detalle.</p>
              }
            </div>
          </div>
        }
      </div>

      @if (printOrder(); as printData) {
        <app-receipt-print
          #receipt
          [type]="printType()"
          [order]="printData"
          restaurantName="Resto"
        />
      }
    </section>
  `,
})
export class CajaViewComponent implements OnInit {
  private readonly api = inject(RestoApiService);
  private readonly notifications = inject(NotificationService);

  readonly selectedDate = signal(this.todayIso());
  readonly summary = signal<DailySummary | null>(null);
  readonly closedOrders = signal<ClosedOrderSummary[]>([]);
  readonly selectedOrderId = signal<string | null>(null);
  readonly orderDetail = signal<OrderDetail | null>(null);
  readonly loading = signal(true);
  readonly printOrder = signal<OrderDetail | null>(null);
  readonly printType = signal<'bill'>('bill');

  ngOnInit(): void {
    void this.loadDay();
  }

  onDateChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    if (value) {
      this.selectedDate.set(value);
      this.selectedOrderId.set(null);
      this.orderDetail.set(null);
      void this.loadDay();
    }
  }

  async selectOrder(orderId: string): Promise<void> {
    this.selectedOrderId.set(orderId);
    try {
      this.orderDetail.set(await this.api.getOrderById(orderId));
    } catch (error) {
      this.handleError(error, 'No se pudo cargar el pedido.');
    }
  }

  printBill(): void {
    const detail = this.orderDetail();
    if (!detail) return;
    this.printOrder.set(detail);
    setTimeout(() => window.print(), 100);
  }

  private async loadDay(): Promise<void> {
    this.loading.set(true);
    try {
      const date = this.selectedDate();
      const [summary, closed] = await Promise.all([
        this.api.getDailySummary(date),
        this.api.getClosedOrders(date),
      ]);
      this.summary.set(summary);
      this.closedOrders.set(closed);
    } catch (error) {
      this.handleError(error, 'No se pudo cargar la caja del día.');
    } finally {
      this.loading.set(false);
    }
  }

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private handleError(error: unknown, fallback: string): void {
    const message =
      error instanceof HttpErrorResponse && error.error?.error
        ? String(error.error.error)
        : fallback;
    this.notifications.showError(message);
  }
}
