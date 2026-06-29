import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RestoApiService } from '../../core/api/resto-api.service';
import { HttpErrorReporter } from '../../core/http/http-error-reporter.service';
import { NotificationService } from '../../core/notifications/notification.service';
import { SalonStateService } from '../../core/salon/salon-state.service';
import { SignalRService } from '../../core/signalr/signalr.service';
import { Product, TableState } from '../../shared/models/resto.models';
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
            @for (table of salon.tables(); track table.number) {
              <app-table-tile
                [number]="table.number"
                [status]="table.status"
                [selected]="salon.selectedTable()?.number === table.number"
                (selectedChange)="selectTable(table)"
              />
            }
          </div>
        }

        @if (salon.selectedTable(); as table) {
          <div class="rounded-[var(--radius-card)] bg-surface-card p-4 shadow-elevated animate-fade-in">
            <h2 class="mb-3 font-display text-lg font-semibold text-slate-900">Mesa {{ table.number }}</h2>

            @if (salon.order(); as currentOrder) {
              <app-order-line-list
                [lines]="currentOrder.lines"
                [editable]="currentOrder.status === 'Borrador' && !salon.orderMutating()"
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
                    [disabled]="!salon.order() || salon.orderMutating()"
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

      @if (salon.order(); as currentOrder) {
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
  readonly salon = inject(SalonStateService);
  private readonly notifications = inject(NotificationService);
  private readonly httpErrors = inject(HttpErrorReporter);

  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly requestingBill = signal(false);

  ngOnInit(): void {
    void this.load();
    void this.signalR.connectSalon((table) => this.salon.applyTableUpdate(table));
  }

  selectTable(table: TableState): Promise<void> {
    return this.salon.selectAndOpenTable(table);
  }

  async addProduct(event: { productId: string; quantity: number; notes: string | null }): Promise<void> {
    const current = this.salon.order();
    if (!current) return;

    await this.salon.mutateOrder(
      current.tableNumber,
      () =>
        this.api.addOrderLine(
          current.id,
          event.productId,
          event.quantity,
          event.notes,
          current.rowVersion,
        ),
      'No se pudo agregar el producto.',
    );
  }

  async removeLine(lineId: string): Promise<void> {
    const current = this.salon.order();
    if (!current) return;

    await this.salon.mutateOrder(
      current.tableNumber,
      () => this.api.removeOrderLine(current.id, lineId, current.rowVersion),
      'No se pudo eliminar la línea.',
    );
  }

  async requestBill(): Promise<void> {
    const current = this.salon.order();
    const table = this.salon.selectedTable();
    if (!current || !table) return;

    this.requestingBill.set(true);
    try {
      await this.api.requestBill(current.id, current.rowVersion, table.rowVersion);
      await this.salon.refreshAfterMutation(table.number);
      this.notifications.showSuccess('Cuenta solicitada — el encargado fue notificado.');
    } catch (error) {
      await this.salon.refreshAfterMutation(table.number);
      this.httpErrors.report(error, 'No se pudo solicitar la cuenta.');
    } finally {
      this.requestingBill.set(false);
    }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.salon.tablesLoading.set(true);
    try {
      const [tables, products] = await Promise.all([this.api.getTables(), this.api.getProducts()]);
      this.salon.tables.set(tables);
      this.products.set(products);
    } catch (error) {
      this.httpErrors.report(error, 'No se pudieron cargar los datos del salón.');
    } finally {
      this.loading.set(false);
      this.salon.tablesLoading.set(false);
    }
  }
}
