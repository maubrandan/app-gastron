import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RestoApiService } from '../../core/api/resto-api.service';
import { HttpErrorReporter } from '../../core/http/http-error-reporter.service';
import { NotificationService } from '../../core/notifications/notification.service';
import {
  CashShiftDetail,
  ClosedOrderSummary,
  DailySummary,
  OrderDetail,
} from '../../shared/models/resto.models';
import { paymentMethodLabel } from '../../shared/utils/payment-method-label';
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
    FormsModule,
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
            <p class="mt-1 text-sm text-slate-400">Turno de caja, pagos y resumen de facturación</p>
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

        @if (shiftLoading()) {
          <app-loading-skeleton variant="grid" [count]="1" />
        } @else if (cashShift(); as shift) {
          <div class="mb-6 rounded-[var(--radius-card)] border border-emerald-700/50 bg-emerald-950/20 p-4 shadow-card">
            <div class="mb-4 flex flex-wrap items-start justify-between gap-4">
              <div>
                <p class="text-sm text-emerald-300">Turno abierto</p>
                <p class="font-display text-lg font-semibold">
                  Desde {{ shift.openedAt | date: 'dd/MM/yyyy HH:mm' }}
                </p>
                <p class="text-sm text-slate-400">Fondo inicial: {{ shift.openingFloat | number: '1.2-2' }}</p>
              </div>
              <div class="flex flex-col gap-2 sm:flex-row sm:items-end">
                <label class="flex flex-col gap-1 text-sm">
                  <span class="text-slate-400">Arqueo efectivo</span>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    class="w-36 rounded-[var(--radius-card)] border border-slate-600 bg-slate-900 px-3 py-2 text-slate-100"
                    [(ngModel)]="closingCashCounted"
                  />
                </label>
                <app-action-button
                  variant="danger"
                  [loading]="shiftActionLoading()"
                  (action)="closeShift()"
                >
                  Cerrar turno
                </app-action-button>
              </div>
            </div>

            <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
              <div class="rounded-[var(--radius-card)] bg-surface-control-card/60 p-3">
                <p class="text-xs text-slate-400">Pagos registrados</p>
                <p class="font-display text-xl font-bold">{{ shift.summary.paymentCount }}</p>
              </div>
              <div class="rounded-[var(--radius-card)] bg-surface-control-card/60 p-3">
                <p class="text-xs text-slate-400">Efectivo</p>
                <p class="font-display text-xl font-bold">{{ shift.summary.totalCash | number: '1.2-2' }}</p>
              </div>
              <div class="rounded-[var(--radius-card)] bg-surface-control-card/60 p-3">
                <p class="text-xs text-slate-400">Tarjeta</p>
                <p class="font-display text-xl font-bold">{{ shift.summary.totalCard | number: '1.2-2' }}</p>
              </div>
              <div class="rounded-[var(--radius-card)] bg-surface-control-card/60 p-3">
                <p class="text-xs text-slate-400">Transferencia</p>
                <p class="font-display text-xl font-bold">{{ shift.summary.totalTransfer | number: '1.2-2' }}</p>
              </div>
              <div class="rounded-[var(--radius-card)] bg-surface-control-card/60 p-3">
                <p class="text-xs text-slate-400">Efectivo esperado</p>
                <p class="font-display text-xl font-bold">{{ shift.summary.expectedCash | number: '1.2-2' }}</p>
              </div>
            </div>

            @if (cashDifference(); as diff) {
              <p
                class="mt-3 text-sm"
                [class.text-emerald-300]="diff === 0"
                [class.text-amber-300]="diff !== 0"
              >
                Diferencia de arqueo: {{ diff | number: '1.2-2' }}
              </p>
            }
          </div>
        } @else {
          <div class="mb-6 rounded-[var(--radius-card)] border border-amber-700/50 bg-amber-950/20 p-4 shadow-card">
            <p class="mb-3 text-sm text-amber-200">No hay turno de caja abierto. Abrí uno para registrar pagos.</p>
            <div class="flex flex-wrap items-end gap-3">
              <label class="flex flex-col gap-1 text-sm">
                <span class="text-slate-400">Fondo inicial</span>
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  class="w-36 rounded-[var(--radius-card)] border border-slate-600 bg-slate-900 px-3 py-2 text-slate-100"
                  [(ngModel)]="openingFloat"
                />
              </label>
              <app-action-button
                variant="success"
                [loading]="shiftActionLoading()"
                (action)="openShift()"
              >
                Abrir turno
              </app-action-button>
            </div>
          </div>
        }

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
                      <span>
                        Mesa {{ closed.tableNumber }} · {{ paymentMethodLabel(closed.paymentMethod) }}
                      </span>
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
                <p class="mb-1 text-sm text-slate-400">
                  Cerrado: {{ detail.closedAt | date: 'dd/MM/yyyy HH:mm' }}
                </p>
                @if (detail.paymentMethod) {
                  <p class="mb-4 text-sm text-slate-400">
                    Pago: {{ paymentMethodLabel(detail.paymentMethod) }}
                  </p>
                }
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
  private readonly httpErrors = inject(HttpErrorReporter);
  private readonly notifications = inject(NotificationService);

  readonly paymentMethodLabel = paymentMethodLabel;

  readonly selectedDate = signal(this.todayIso());
  readonly summary = signal<DailySummary | null>(null);
  readonly closedOrders = signal<ClosedOrderSummary[]>([]);
  readonly selectedOrderId = signal<string | null>(null);
  readonly orderDetail = signal<OrderDetail | null>(null);
  readonly loading = signal(true);
  readonly shiftLoading = signal(true);
  readonly shiftActionLoading = signal(false);
  readonly cashShift = signal<CashShiftDetail | null>(null);
  readonly printOrder = signal<OrderDetail | null>(null);
  readonly printType = signal<'bill'>('bill');

  openingFloat = 5000;
  closingCashCounted = 0;

  readonly cashDifference = computed(() => {
    const shift = this.cashShift();
    if (!shift) return null;
    return this.closingCashCounted - shift.summary.expectedCash;
  });

  ngOnInit(): void {
    void this.loadShift();
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

  async openShift(): Promise<void> {
    this.shiftActionLoading.set(true);
    try {
      await this.api.openCashShift(this.openingFloat);
      this.notifications.showSuccess('Turno de caja abierto.');
      await this.loadShift();
    } catch (error) {
      this.httpErrors.report(error, 'No se pudo abrir el turno.');
    } finally {
      this.shiftActionLoading.set(false);
    }
  }

  async closeShift(): Promise<void> {
    const shift = this.cashShift();
    if (!shift) return;

    this.shiftActionLoading.set(true);
    try {
      await this.api.closeCashShift(shift.id, this.closingCashCounted);
      this.notifications.showSuccess('Turno de caja cerrado.');
      this.cashShift.set(null);
      this.closingCashCounted = 0;
    } catch (error) {
      this.httpErrors.report(error, 'No se pudo cerrar el turno.');
    } finally {
      this.shiftActionLoading.set(false);
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

  private async loadShift(): Promise<void> {
    this.shiftLoading.set(true);
    try {
      const shift = await this.api.getCurrentCashShift();
      this.cashShift.set(shift);
      if (shift) {
        this.closingCashCounted = shift.summary.expectedCash;
      }
    } catch (error) {
      this.handleError(error, 'No se pudo cargar el turno de caja.');
    } finally {
      this.shiftLoading.set(false);
    }
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
    this.httpErrors.report(error, fallback);
  }
}
