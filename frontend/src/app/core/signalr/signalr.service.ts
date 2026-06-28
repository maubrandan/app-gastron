import { Injectable, inject, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { KitchenOrder, TableState } from '../../shared/models/resto.models';

export type ConnectionState = 'connected' | 'disconnected' | 'reconnecting';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly auth = inject(AuthService);

  private connection: HubConnection | null = null;
  private connectionStart: Promise<void> | null = null;
  private reconnectAttempt = 0;
  private readonly backoffMs = [1000, 2000, 4000, 8000, 16000];

  readonly connectionState = signal<ConnectionState>('disconnected');

  private onKitchenOrderAdded: ((order: KitchenOrder) => void) | null = null;
  private onKitchenOrderRemoved: ((orderId: string) => void) | null = null;
  private onTableStateUpdated: ((table: TableState) => void) | null = null;

  async connectKitchen(
    handlers: {
      onOrderAdded: (order: KitchenOrder) => void;
      onOrderRemoved: (orderId: string) => void;
    },
  ): Promise<void> {
    this.onKitchenOrderAdded = handlers.onOrderAdded;
    this.onKitchenOrderRemoved = handlers.onOrderRemoved;
    await this.ensureConnection();
    await this.connection!.invoke('JoinKitchen');
  }

  async connectSalon(onTableUpdated: (table: TableState) => void): Promise<void> {
    this.onTableStateUpdated = onTableUpdated;
    await this.ensureConnection();
    await this.connection!.invoke('JoinSalon');
  }

  private async ensureConnection(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) return;

    if (!this.connection) {
      this.connection = new HubConnectionBuilder()
        .withUrl(environment.hubUrl, {
          accessTokenFactory: () => this.auth.token() ?? '',
        })
        .withAutomaticReconnect(this.backoffMs)
        .configureLogging(LogLevel.Warning)
        .build();

      this.connection.on('KitchenOrderAdded', (order: KitchenOrder) => {
        this.onKitchenOrderAdded?.({
          ...order,
          sentToKitchenAt: order.sentToKitchenAt,
        });
      });

      this.connection.on('KitchenOrderRemoved', (orderId: string) => {
        this.onKitchenOrderRemoved?.(orderId);
      });

      this.connection.on('TableStateUpdated', (table: TableState) => {
        this.onTableStateUpdated?.(table);
      });

      this.connection.onreconnecting(() => this.connectionState.set('reconnecting'));
      this.connection.onreconnected(() => {
        this.reconnectAttempt = 0;
        this.connectionState.set('connected');
      });
      this.connection.onclose(() => {
        this.connectionState.set('disconnected');
        void this.scheduleReconnect();
      });
    }

    if (this.connection.state === HubConnectionState.Disconnected) {
      this.connectionStart ??= this.connection.start().then(() => {
        this.reconnectAttempt = 0;
        this.connectionState.set('connected');
      }).finally(() => {
        this.connectionStart = null;
      });
      await this.connectionStart;
    }
  }

  private async scheduleReconnect(): Promise<void> {
    if (!this.connection) return;

    const delay = this.backoffMs[Math.min(this.reconnectAttempt, this.backoffMs.length - 1)];
    this.reconnectAttempt++;
    this.connectionState.set('reconnecting');

    await new Promise((resolve) => setTimeout(resolve, delay));

    try {
      await this.connection.start();
      this.reconnectAttempt = 0;
      this.connectionState.set('connected');
    } catch {
      void this.scheduleReconnect();
    }
  }
}
