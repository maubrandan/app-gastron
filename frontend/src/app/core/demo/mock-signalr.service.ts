import { Injectable, signal } from '@angular/core';
import { KitchenOrder, TableState } from '../../shared/models/resto.models';
import { ConnectionState } from '../signalr/signalr.service';

@Injectable({ providedIn: 'root' })
export class MockSignalRService {
  readonly connectionState = signal<ConnectionState>('connected');

  private onKitchenOrderAdded: ((order: KitchenOrder) => void) | null = null;
  private onKitchenOrderRemoved: ((orderId: string) => void) | null = null;
  private onTableStateUpdated: ((table: TableState) => void) | null = null;

  async connectKitchen(handlers: {
    onOrderAdded: (order: KitchenOrder) => void;
    onOrderRemoved: (orderId: string) => void;
  }): Promise<void> {
    this.onKitchenOrderAdded = handlers.onOrderAdded;
    this.onKitchenOrderRemoved = handlers.onOrderRemoved;
    this.connectionState.set('connected');
  }

  async connectSalon(onTableUpdated: (table: TableState) => void): Promise<void> {
    this.onTableStateUpdated = onTableUpdated;
    this.connectionState.set('connected');
  }

  emitKitchenOrderAdded(order: KitchenOrder): void {
    queueMicrotask(() => this.onKitchenOrderAdded?.(order));
  }

  emitKitchenOrderRemoved(orderId: string): void {
    queueMicrotask(() => this.onKitchenOrderRemoved?.(orderId));
  }

  emitTableStateUpdated(table: TableState): void {
    queueMicrotask(() => this.onTableStateUpdated?.({ ...table }));
  }
}
