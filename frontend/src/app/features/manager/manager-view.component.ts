import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { RestoApiService } from '../../core/api/resto-api.service';
import { HttpErrorReporter } from '../../core/http/http-error-reporter.service';
import { NotificationService } from '../../core/notifications/notification.service';
import { SalonStateService } from '../../core/salon/salon-state.service';
import { SignalRService } from '../../core/signalr/signalr.service';
import { KitchenOrder, OrderDetail, PaymentMethod, ReceiptType, TableState } from '../../shared/models/resto.models';
import { paymentMethodLabel } from '../../shared/utils/payment-method-label';
import { ReceiptPrintComponent } from '../../shared/ui/receipt-print.component';
import { elapsedMinutes, resolveKitchenUrgency } from '../../shared/utils/kitchen-urgency';
import { ActionButtonComponent } from '../../shared/ui/action-button.component';
import { ConnectionPillComponent } from '../../shared/ui/connection-pill.component';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { KitchenDelayRowComponent } from '../../shared/ui/kitchen-delay-row.component';
import { LoadingSkeletonComponent } from '../../shared/ui/loading-skeleton.component';
import { OrderLineListComponent } from '../../shared/ui/order-line-list.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';
import { TableTileComponent } from '../../shared/ui/table-tile.component';

@Component({
  selector: 'app-manager-view',
  standalone: true,
  imports: [
    DecimalPipe,
    TableTileComponent,
    StatusBadgeComponent,
    OrderLineListComponent,
    ActionButtonComponent,
    KitchenDelayRowComponent,
    LoadingSkeletonComponent,
    EmptyStateComponent,
    ConnectionPillComponent,
    ReceiptPrintComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="min-h-screen bg-surface-control p-4 text-slate-100 md:p-6">
      <header class="mb-6 flex items-center justify-between">
        <h1 class="font-display text-2xl font-bold">Estación de Control — Encargado</h1>
        <app-connection-pill [state]="signalR.connectionState()" />
      </header>

      <div class="grid gap-6 lg:grid-cols-2">
        <div class="rounded-[var(--radius-card)] border border-slate-700/50 bg-surface-control-card p-4 shadow-card">
          <h2 class="mb-3 text-lg font-semibold">Mesas</h2>

          @if (loading()) {
            <app-loading-skeleton variant="grid" [count]="12" />
          } @else {
            <div class="grid grid-cols-2 gap-3 md:grid-cols-3">
              @for (table of sortedTables(); track table.number) {
                <app-table-tile
                  variant="control"
                  [number]="table.number"
                  [status]="table.status"
                  [selected]="salon.selectedTable()?.number === table.number"
                  (selectedChange)="selectTable(table)"
                />
              }
            </div>
          }
        </div>

        <div class="space-y-6">
          <div class="rounded-[var(--radius-card)] border border-slate-700/50 bg-surface-control-card p-4 shadow-card">
            @if (salon.order(); as currentOrder) {
              <div class="mb-3 flex flex-wrap items-center gap-2">
                <h2 class="font-display text-lg font-semibold">Pedido Mesa {{ currentOrder.tableNumber }}</h2>
                <app-status-badge [status]="currentOrder.status" />
              </div>

              @if (salon.selectedTable()?.status === 'EsperandoCuenta') {
                <div class="mb-4 rounded-[var(--radius-card)] border border-amber-500/50 bg-amber-950/30 px-3 py-2 text-sm text-amber-200 animate-pulse">
                  Esta mesa solicitó la cuenta — priorizar cierre.
                </div>
              }

              <app-order-line-list [lines]="currentOrder.lines" [showPrices]="true" />

              <p class="my-4 font-display text-lg font-bold">
                Total: {{ currentOrder.total | number: '1.2-2' }}
              </p>

              @if (currentOrder.status === 'Borrador') {
                <app-action-button
                  variant="success"
                  [loading]="actionLoading()"
                  (action)="confirmOrder()"
                >
                  Confirmar y enviar a cocina
                </app-action-button>
              }

              @if (currentOrder.status === 'ConfirmadoEnCocina') {
                @if (!cashShift()) {
                  <div class="mb-4 rounded-[var(--radius-card)] border border-red-500/50 bg-red-950/30 px-3 py-2 text-sm text-red-200">
                    No hay turno de caja abierto. Abrí un turno en la vista de Caja antes de facturar.
                  </div>
                }

                <label class="mb-4 flex flex-col gap-1 text-sm">
                  <span class="text-slate-400">Medio de pago</span>
                  <select
                    class="rounded-[var(--radius-card)] border border-slate-600 bg-slate-900 px-3 py-2 text-slate-100"
                    [value]="selectedPaymentMethod()"
                    (change)="onPaymentMethodChange($event)"
                  >
                    <option value="Cash">Efectivo</option>
                    <option value="Card">Tarjeta</option>
                    <option value="Transfer">Transferencia</option>
                  </select>
                </label>

                <app-action-button
                  variant="danger"
                  [loading]="actionLoading()"
                  [disabled]="!cashShift()"
                  (action)="closeOrder()"
                >
                  Cerrar y facturar
                </app-action-button>
              }
            } @else if (salon.selectedTable()) {
              <app-empty-state
                message="No hay pedido activo en esta mesa"
                icon="📋"
              />
            } @else {
              <app-empty-state
                message="Seleccioná una mesa para ver el pedido"
                hint="Las mesas en amber solicitaron cuenta"
                icon="🪑"
              />
            }
          </div>

          <div class="rounded-[var(--radius-card)] border border-slate-700/50 bg-surface-control-card p-4 shadow-card">
            <h2 class="mb-3 text-lg font-semibold">Cocina — Demoras</h2>
            <ul class="space-y-2">
              @for (kitchenOrder of sortedKitchenOrders(); track kitchenOrder.id) {
                <app-kitchen-delay-row [order]="kitchenOrder" />
              } @empty {
                <li class="text-sm text-slate-400">Sin comandas en cocina.</li>
              }
            </ul>
          </div>
        </div>
      </div>

      @if (printOrder(); as printData) {
        <app-receipt-print [type]="printType()" [order]="printData" restaurantName="Resto" />
      }
    </section>
  `,
})
export class ManagerViewComponent implements OnInit {
  private readonly api = inject(RestoApiService);
  readonly signalR = inject(SignalRService);
  readonly salon = inject(SalonStateService);
  private readonly notifications = inject(NotificationService);
  private readonly httpErrors = inject(HttpErrorReporter);

  readonly kitchenOrders = signal<KitchenOrder[]>([]);
  readonly loading = signal(true);
  readonly actionLoading = signal(false);
  readonly printOrder = signal<OrderDetail | null>(null);
  readonly printType = signal<ReceiptType>('kitchen');
  readonly cashShift = signal<{ id: string } | null>(null);
  readonly selectedPaymentMethod = signal<PaymentMethod>('Cash');

  readonly sortedTables = computed(() =>
    [...this.salon.tables()].sort((a, b) => {
      if (a.status === 'EsperandoCuenta' && b.status !== 'EsperandoCuenta') return -1;
      if (b.status === 'EsperandoCuenta' && a.status !== 'EsperandoCuenta') return 1;
      return a.number - b.number;
    }),
  );

  readonly sortedKitchenOrders = computed(() =>
    [...this.kitchenOrders()].sort((a, b) => {
      const urgencyDiff = urgencyScore(b.sentToKitchenAt) - urgencyScore(a.sentToKitchenAt);
      if (urgencyDiff !== 0) return urgencyDiff;
      return new Date(a.sentToKitchenAt).getTime() - new Date(b.sentToKitchenAt).getTime();
    }),
  );

  ngOnInit(): void {
    void this.load();
    void this.loadCashShift();
    void this.signalR.connectSalon((table) => this.salon.applyTableUpdate(table));
    void this.signalR.connectKitchen({
      onOrderAdded: (order) => {
        this.kitchenOrders.update((current) =>
          current.some((entry) => entry.id === order.id) ? current : [...current, order],
        );
      },
      onOrderRemoved: (orderId) => {
        this.kitchenOrders.update((current) => current.filter((entry) => entry.id !== orderId));
      },
    });
  }

  selectTable(table: TableState): Promise<void> {
    return this.salon.selectTable(table);
  }

  async confirmOrder(): Promise<void> {
    const current = this.salon.order();
    if (!current) return;

    this.actionLoading.set(true);
    try {
      await this.api.confirmForKitchen(current.id, current.rowVersion);
      await this.salon.refreshAfterMutation(current.tableNumber);
      this.notifications.showSuccess('Pedido enviado a cocina.');
      const confirmed = this.salon.order();
      if (confirmed) {
        this.triggerPrint('kitchen', confirmed);
      }
    } catch (error) {
      this.httpErrors.report(error, 'No se pudo confirmar el pedido.');
      await this.salon.refreshAfterMutation(current.tableNumber);
    } finally {
      this.actionLoading.set(false);
    }
  }

  onPaymentMethodChange(event: Event): void {
    this.selectedPaymentMethod.set((event.target as HTMLSelectElement).value as PaymentMethod);
  }

  async closeOrder(): Promise<void> {
    const current = this.salon.order();
    const table = this.salon.selectedTable();
    if (!current || !table) return;

    this.actionLoading.set(true);
    try {
      const orderId = current.id;
      await this.api.closeAndBill(
        current.id,
        current.rowVersion,
        table.rowVersion,
        this.selectedPaymentMethod(),
      );
      const closed = await this.api.getOrderById(orderId);
      await this.salon.refreshAfterMutation(current.tableNumber);
      this.salon.order.set(null);
      this.notifications.showSuccess(
        `Mesa cerrada y facturada (${paymentMethodLabel(this.selectedPaymentMethod())}).`,
      );
      if (closed) {
        this.triggerPrint('bill', closed);
      }
    } catch (error) {
      this.httpErrors.report(error, 'No se pudo cerrar el pedido.');
      await this.salon.refreshAfterMutation(current.tableNumber);
    } finally {
      this.actionLoading.set(false);
    }
  }

  private async loadCashShift(): Promise<void> {
    try {
      const shift = await this.api.getCurrentCashShift();
      this.cashShift.set(shift ? { id: shift.id } : null);
    } catch {
      this.cashShift.set(null);
    }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const [tables, kitchen] = await Promise.all([
        this.api.getTables(),
        this.api.getActiveKitchenOrders(),
      ]);
      this.salon.tables.set(tables);
      this.kitchenOrders.set(kitchen);
    } catch (error) {
      this.httpErrors.report(error, 'No se pudieron cargar los datos.');
    } finally {
      this.loading.set(false);
    }
  }

  private triggerPrint(type: ReceiptType, order: OrderDetail): void {
    this.printType.set(type);
    this.printOrder.set(order);
    setTimeout(() => window.print(), 150);
  }
}

function urgencyScore(sentToKitchenAt: string): number {
  const minutes = elapsedMinutes(sentToKitchenAt);
  const urgency = resolveKitchenUrgency(minutes);
  if (urgency === 'critical') return 3;
  if (urgency === 'warning') return 2;
  return 1;
}
