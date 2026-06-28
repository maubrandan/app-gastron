import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ClosedOrderSummary,
  CreateOrderResponse,
  DailySummary,
  KitchenOrder,
  OrderDetail,
  Product,
  TableState,
} from '../../shared/models/resto.models';
import { NotificationService } from '../notifications/notification.service';

@Injectable({ providedIn: 'root' })
export class RestoApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(
    private readonly http: HttpClient,
    private readonly notifications: NotificationService,
  ) {}

  getTables(): Promise<TableState[]> {
    return firstValueFrom(this.http.get<TableState[]>(`${this.baseUrl}/tables`));
  }

  getProducts(includeInactive = false): Promise<Product[]> {
    let params = new HttpParams();
    if (includeInactive) {
      params = params.set('includeInactive', 'true');
    }
    return firstValueFrom(this.http.get<Product[]>(`${this.baseUrl}/products`, { params }));
  }

  createProduct(name: string, price: number, category: string): Promise<string> {
    return firstValueFrom(
      this.http.post<{ productId: string }>(`${this.baseUrl}/products`, { name, price, category }),
    ).then((r) => r.productId);
  }

  updateProduct(id: string, name: string, price: number, category: string): Promise<void> {
    return firstValueFrom(
      this.http.put(`${this.baseUrl}/products/${id}`, { name, price, category }),
    ).then(() => undefined);
  }

  deactivateProduct(id: string): Promise<void> {
    return firstValueFrom(this.http.post(`${this.baseUrl}/products/${id}/deactivate`, {})).then(
      () => undefined,
    );
  }

  getActiveKitchenOrders(category?: string | null): Promise<KitchenOrder[]> {
    let params = new HttpParams();
    if (category) {
      params = params.set('category', category);
    }
    return firstValueFrom(
      this.http.get<KitchenOrder[]>(`${this.baseUrl}/kitchen/active-orders`, { params }),
    );
  }

  getOrderByTable(tableNumber: number): Promise<OrderDetail | null> {
    return firstValueFrom(
      this.http.get<OrderDetail>(`${this.baseUrl}/orders/by-table/${tableNumber}`, {
        observe: 'response',
      }),
    )
      .then((response) => (response.status === 204 || response.body === null ? null : response.body))
      .catch((error: HttpErrorResponse) => (error.status === 404 ? null : Promise.reject(error)));
  }

  getOrderById(orderId: string): Promise<OrderDetail | null> {
    return firstValueFrom(this.http.get<OrderDetail>(`${this.baseUrl}/orders/${orderId}`)).catch(
      (error: HttpErrorResponse) => (error.status === 404 ? null : Promise.reject(error)),
    );
  }

  getDailySummary(date: string): Promise<DailySummary> {
    const params = new HttpParams().set('date', date);
    return firstValueFrom(
      this.http.get<DailySummary>(`${this.baseUrl}/reports/daily-summary`, { params }),
    );
  }

  getClosedOrders(date: string): Promise<ClosedOrderSummary[]> {
    const params = new HttpParams().set('date', date);
    return firstValueFrom(
      this.http.get<ClosedOrderSummary[]>(`${this.baseUrl}/reports/closed-orders`, { params }),
    );
  }

  createOrder(tableNumber: number, tableRowVersion: string): Promise<CreateOrderResponse> {
    return this.handleMutation(
      firstValueFrom(
        this.http.post<CreateOrderResponse>(`${this.baseUrl}/orders`, {
          tableNumber,
          tableRowVersion,
        }),
      ),
    );
  }

  addOrderLine(
    orderId: string,
    productId: string,
    quantity: number,
    notes: string | null,
    rowVersion: string,
  ): Promise<string> {
    return this.handleMutation(
      firstValueFrom(
        this.http.post<{ rowVersion: string }>(`${this.baseUrl}/orders/${orderId}/lines`, {
          productId,
          quantity,
          notes,
          rowVersion,
        }),
      ).then((r) => r.rowVersion),
    );
  }

  removeOrderLine(orderId: string, lineId: string, rowVersion: string): Promise<string> {
    return this.handleMutation(
      firstValueFrom(
        this.http.request<{ rowVersion: string }>('DELETE', `${this.baseUrl}/orders/${orderId}/lines/${lineId}`, {
          body: { rowVersion },
        }),
      ).then((r) => r.rowVersion),
    );
  }

  confirmForKitchen(orderId: string, rowVersion: string): Promise<void> {
    return this.handleMutation(
      firstValueFrom(
        this.http.post(`${this.baseUrl}/orders/${orderId}/confirm-for-kitchen`, { rowVersion }),
      ).then(() => undefined),
    );
  }

  closeAndBill(orderId: string, orderRowVersion: string, tableRowVersion: string): Promise<void> {
    return this.handleMutation(
      firstValueFrom(
        this.http.post(`${this.baseUrl}/orders/${orderId}/close-and-bill`, {
          orderRowVersion,
          tableRowVersion,
        }),
      ).then(() => undefined),
    );
  }

  requestBill(orderId: string, orderRowVersion: string, tableRowVersion: string): Promise<void> {
    return this.handleMutation(
      firstValueFrom(
        this.http.post(`${this.baseUrl}/orders/${orderId}/request-bill`, {
          orderRowVersion,
          tableRowVersion,
        }),
      ).then(() => undefined),
    );
  }

  private async handleMutation<T>(promise: Promise<T>): Promise<T> {
    try {
      return await promise;
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 409) {
        this.notifications.showConcurrencyWarning();
      }
      throw error;
    }
  }
}
