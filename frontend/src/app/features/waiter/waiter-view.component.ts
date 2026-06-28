import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { RestoApiService } from '../../core/api/resto-api.service';
import { NotificationService } from '../../core/notifications/notification.service';
import { SignalRService } from '../../core/signalr/signalr.service';
import { OrderDetail, Product, TableState } from '../../shared/models/resto.models';
import { ActionButtonComponent } from '../../shared/ui/action-button.component';
import { ConnectionPillComponent } from '../../shared/ui/connection-pill.component';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { LoadingSkeletonComponent } from '../../shared/ui/loading-skeleton.component';
import { OrderLineListComponent } from '../../shared/ui/order-line-list.component';
import { ProductPickerComponent } from '../../shared/ui/product-picker.component';
import { TableTileComponent } from '../../shared/ui/table-tile.component';

@Component({
  selector: 'app-waiter-view',
  standalone: true,
  imports: [
    DecimalPipe,
    TableTileComponent,
    OrderLineListComponent,
    ProductPickerComponent,
    LoadingSkeletonComponent,
    EmptyStateComponent,
    ConnectionPillComponent,
    ActionButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="min-h-screen bg-surface-salon pb-24">
      <header class="sticky top-0 z-10 flex items-center justify-between border-b border-slate-200 bg-surface-card/95 px-4 py-3 shadow-card backdrop-blur-sm">
        <h1 class="font-display text-xl font-bold text-slate-900">Salón — Mozo</h1>
        <app-connection-pill [state]="signalR.connectionState()" />
      </header>

      <div class="p-4">
        @if (loading()) {
          <app-loading-skeleton variant="grid" [count]="12" />
        } @else {
          <div class="mb-6 grid grid-cols-2 gap-3 md:grid-cols-4">
            @for (table of tables(); track table.number) {
              <app-table-tile
                [number]="table.number"
                [status]="table.status"
                [selected]="selectedTable()?.number === table.number"
                (selectedChange)="selectTable(table)"
              />
            }
          </div>
        }

        @if (selectedTable(); as table) {
          <div class="rounded-[var(--radius-card)] bg-surface-card p-4 shadow-elevated animate-fade-in">
            <h2 class="mb-3 font-display text-lg font-semibold text-slate-900">Mesa {{ table.number }}</h2>

            @if (order(); as currentOrder) {
              <app-order-line-list
                [lines]="currentOrder.lines"
                [editable]="currentOrder.status === 'Borrador' && !orderMutating()"
                (removeLine)="removeLine($event)"
              />

              @if (currentOrder.status === 'ConfirmadoEnCocina' && table.status !== 'EsperandoCuenta') {
                <div class="mt-4">
                  <app-action-button
                    variant="primary"
                    [fullWidth]="true"
                    [loading]="requestingBill()"
                    (action)="requestBill()"
                  >
                    Pedir cuenta
                  </app-action-button>
                </div>
              }

              @if (currentOrder.status === 'Borrador') {
                <div class="mt-6 border-t border-slate-100 pt-4">
                  <h3 class="mb-3 text-sm font-semibold text-slate-700">Agregar productos</h3>
                  <app-product-picker
                    [products]="products()"
                    [disabled]="!order() || orderMutating()"
                    (addProduct)="addProduct($event)"
                  />
                </div>
              }
            } @else {
              <p class="text-sm text-slate-500">Sin pedido activo.</p>
            }
          </div>
        } @else if (!loading()) {
          <app-empty-state
            message="Seleccioná una mesa para comenzar"
            hint="Las mesas libres se abren automáticamente al tocarlas"
            icon="🍽️"
          />
        }
      </div>

      @if (order(); as currentOrder) {
        <footer
          class="fixed bottom-0 left-0 right-0 border-t border-slate-200 bg-surface-card/95 px-4 py-3 shadow-elevated backdrop-blur-sm"
        >
          <div class="mx-auto flex max-w-7xl items-center justify-between">
            <span class="text-sm text-slate-500">Mesa {{ currentOrder.tableNumber }}</span>
            <span class="font-display text-lg font-bold text-slate-900">
              Total: {{ currentOrder.total | number: '1.2-2' }}
            </span>
          </div>
        </footer>
      }
    </section>
  `,
})
export class WaiterViewComponent implements OnInit {
  private readonly api = inject(RestoApiService);
  readonly signalR = inject(SignalRService);
  private readonly notifications = inject(NotificationService);

  readonly tables = signal<TableState[]>([]);
  readonly products = signal<Product[]>([]);
  readonly selectedTable = signal<TableState | null>(null);
  readonly order = signal<OrderDetail | null>(null);
  readonly loading = signal(true);
  readonly requestingBill = signal(false);
  readonly orderMutating = signal(false);

  ngOnInit(): void {
    void this.load();
    void this.signalR.connectSalon((table) => {
      this.tables.update((current) =>
        current.map((t) => (t.number === table.number ? table : t)),
      );
      const selected = this.selectedTable();
      if (selected?.number === table.number) {
        this.selectedTable.set(table);
      }
    });
  }

  async selectTable(table: TableState): Promise<void> {
    this.selectedTable.set(table);

    if (table.status === 'Libre') {
      try {
        await this.api.createOrder(table.number, table.rowVersion);
        await this.reloadTables();
        this.order.set(await this.api.getOrderByTable(table.number));
        const updatedTable = this.tables().find((t) => t.number === table.number);
        if (updatedTable) {
          this.selectedTable.set(updatedTable);
        }
      } catch (error) {
        this.handleError(error, 'No se pudo abrir la mesa.');
        await this.reloadTables();
      }
      return;
    }

    await this.loadOrder(table.number);
  }

  async addProduct(event: { productId: string; quantity: number; notes: string | null }): Promise<void> {
    const current = this.order();
    if (!current || this.orderMutating()) return;

    this.orderMutating.set(true);
    try {
      await this.api.addOrderLine(
        current.id,
        event.productId,
        event.quantity,
        event.notes,
        current.rowVersion,
      );
      await this.loadOrder(current.tableNumber);
    } catch (error) {
      await this.loadOrder(current.tableNumber);
      this.handleError(error, 'No se pudo agregar el producto.');
    } finally {
      this.orderMutating.set(false);
    }
  }

  async removeLine(lineId: string): Promise<void> {
    const current = this.order();
    if (!current || this.orderMutating()) return;

    this.orderMutating.set(true);
    try {
      await this.api.removeOrderLine(current.id, lineId, current.rowVersion);
      await this.loadOrder(current.tableNumber);
    } catch (error) {
      await this.loadOrder(current.tableNumber);
      this.handleError(error, 'No se pudo eliminar la línea.');
    } finally {
      this.orderMutating.set(false);
    }
  }

  async requestBill(): Promise<void> {
    const current = this.order();
    const table = this.selectedTable();
    if (!current || !table) return;

    this.requestingBill.set(true);
    try {
      await this.api.requestBill(current.id, current.rowVersion, table.rowVersion);
      await this.reloadTables();
      const updatedTable = this.tables().find((t) => t.number === table.number) ?? table;
      this.selectedTable.set(updatedTable);
      await this.loadOrder(table.number);
      this.notifications.showSuccess('Cuenta solicitada — el encargado fue notificado.');
    } catch (error) {
      this.handleError(error, 'No se pudo solicitar la cuenta.');
      await this.reloadTables();
      await this.loadOrder(table.number);
    } finally {
      this.requestingBill.set(false);
    }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const [tables, products] = await Promise.all([this.api.getTables(), this.api.getProducts()]);
      this.tables.set(tables);
      this.products.set(products);
    } catch (error) {
      this.handleError(error, 'No se pudieron cargar los datos del salón.');
    } finally {
      this.loading.set(false);
    }
  }

  private async reloadTables(): Promise<void> {
    this.tables.set(await this.api.getTables());
  }

  private async loadOrder(tableNumber: number): Promise<void> {
    this.order.set(await this.api.getOrderByTable(tableNumber));
  }

  private handleError(error: unknown, fallback: string): void {
    if (error instanceof HttpErrorResponse && error.status === 409) return;
    const message =
      error instanceof HttpErrorResponse && error.error?.error
        ? String(error.error.error)
        : fallback;
    this.notifications.showError(message);
  }
}
