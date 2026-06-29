import { Injectable, inject, signal } from '@angular/core';
import { RestoApiService } from '../api/resto-api.service';
import { HttpErrorReporter } from '../http/http-error-reporter.service';
import { OrderDetail, TableState } from '../../shared/models/resto.models';

@Injectable({ providedIn: 'root' })
export class SalonStateService {
  private readonly api = inject(RestoApiService);
  private readonly httpErrors = inject(HttpErrorReporter);

  readonly tables = signal<TableState[]>([]);
  readonly selectedTable = signal<TableState | null>(null);
  readonly order = signal<OrderDetail | null>(null);
  readonly tablesLoading = signal(false);
  readonly orderMutating = signal(false);

  applyTableUpdate(table: TableState): void {
    this.tables.update((current) =>
      current.map((entry) => (entry.number === table.number ? table : entry)),
    );

    const selected = this.selectedTable();
    if (selected?.number === table.number) {
      this.selectedTable.set(table);
    }
  }

  async loadTables(): Promise<void> {
    this.tables.set(await this.api.getTables());
  }

  async loadOrder(tableNumber: number): Promise<void> {
    this.order.set(await this.api.getOrderByTable(tableNumber));
  }

  async refreshAfterMutation(tableNumber: number): Promise<void> {
    await this.loadTables();

    const table = this.tables().find((entry) => entry.number === tableNumber) ?? null;
    this.selectedTable.set(table);

    if (table?.status === 'Libre') {
      this.order.set(null);
      return;
    }

    await this.loadOrder(tableNumber);
  }

  /** Encargado: selecciona mesa sin abrirla. */
  async selectTable(table: TableState, loadErrorFallback = 'No se pudo cargar el pedido.'): Promise<void> {
    this.selectedTable.set(table);

    if (table.status === 'Libre') {
      this.order.set(null);
      return;
    }

    try {
      await this.loadOrder(table.number);
    } catch (error) {
      this.httpErrors.report(error, loadErrorFallback);
    }
  }

  /** Mozo: abre mesa libre al seleccionarla. */
  async selectAndOpenTable(table: TableState): Promise<void> {
    this.selectedTable.set(table);

    if (table.status === 'Libre') {
      try {
        await this.api.createOrder(table.number, table.rowVersion);
        await this.refreshAfterMutation(table.number);
      } catch (error) {
        this.httpErrors.report(error, 'No se pudo abrir la mesa.');
        await this.loadTables();
      }
      return;
    }

    try {
      await this.loadOrder(table.number);
    } catch (error) {
      this.httpErrors.report(error, 'No se pudo cargar el pedido.');
    }
  }

  async mutateOrder<T>(
    tableNumber: number,
    action: () => Promise<T>,
    errorFallback: string,
  ): Promise<T | null> {
    if (this.orderMutating()) {
      return null;
    }

    this.orderMutating.set(true);
    try {
      const result = await action();
      await this.loadOrder(tableNumber);
      return result;
    } catch (error) {
      await this.loadOrder(tableNumber);
      this.httpErrors.report(error, errorFallback);
      return null;
    } finally {
      this.orderMutating.set(false);
    }
  }
}
