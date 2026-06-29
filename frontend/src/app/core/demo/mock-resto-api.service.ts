import { Injectable, inject } from '@angular/core';
import {
  ClosedOrderSummary,
  CreateOrderResponse,
  DailySummary,
  CashShiftDetail,
  KitchenOrder,
  OrderDetail,
  PaymentMethod,
  Product,
  TableState,
} from '../../shared/models/resto.models';
import { DemoStoreService } from './demo-store.service';

@Injectable({ providedIn: 'root' })
export class MockRestoApiService {
  private readonly store = inject(DemoStoreService);

  getTables(): Promise<TableState[]> {
    return Promise.resolve(this.store.getTables());
  }

  getProducts(includeInactive = false): Promise<Product[]> {
    return Promise.resolve(this.store.getProducts(includeInactive));
  }

  createProduct(name: string, price: number, category: string): Promise<string> {
    return Promise.resolve(this.store.createProduct(name, price, category));
  }

  updateProduct(id: string, name: string, price: number, category: string): Promise<void> {
    try {
      this.store.updateProduct(id, name, price, category);
      return Promise.resolve();
    } catch (error) {
      return Promise.reject(error);
    }
  }

  deactivateProduct(id: string): Promise<void> {
    try {
      this.store.deactivateProduct(id);
      return Promise.resolve();
    } catch (error) {
      return Promise.reject(error);
    }
  }

  getActiveKitchenOrders(category?: string | null): Promise<KitchenOrder[]> {
    return Promise.resolve(this.store.getActiveKitchenOrders(category));
  }

  getOrderByTable(tableNumber: number): Promise<OrderDetail | null> {
    return Promise.resolve(this.store.getOrderByTable(tableNumber));
  }

  getOrderById(orderId: string): Promise<OrderDetail | null> {
    return Promise.resolve(this.store.getOrderById(orderId));
  }

  getDailySummary(date: string): Promise<DailySummary> {
    return Promise.resolve(this.store.getDailySummary(date));
  }

  getClosedOrders(date: string): Promise<ClosedOrderSummary[]> {
    return Promise.resolve(this.store.getClosedOrders(date));
  }

  createOrder(tableNumber: number, tableRowVersion: string): Promise<CreateOrderResponse> {
    try {
      return Promise.resolve(this.store.createOrder(tableNumber, tableRowVersion));
    } catch (error) {
      return Promise.reject(error);
    }
  }

  addOrderLine(
    orderId: string,
    productId: string,
    quantity: number,
    notes: string | null,
    rowVersion: string,
  ): Promise<string> {
    try {
      return Promise.resolve(this.store.addOrderLine(orderId, productId, quantity, notes, rowVersion));
    } catch (error) {
      return Promise.reject(error);
    }
  }

  removeOrderLine(orderId: string, lineId: string, rowVersion: string): Promise<string> {
    try {
      return Promise.resolve(this.store.removeOrderLine(orderId, lineId, rowVersion));
    } catch (error) {
      return Promise.reject(error);
    }
  }

  confirmForKitchen(orderId: string, rowVersion: string): Promise<void> {
    try {
      this.store.confirmForKitchen(orderId, rowVersion);
      return Promise.resolve();
    } catch (error) {
      return Promise.reject(error);
    }
  }

  closeAndBill(
    orderId: string,
    orderRowVersion: string,
    tableRowVersion: string,
    paymentMethod: PaymentMethod,
  ): Promise<void> {
    try {
      this.store.closeAndBill(orderId, orderRowVersion, tableRowVersion, paymentMethod);
      return Promise.resolve();
    } catch (error) {
      return Promise.reject(error);
    }
  }

  getCurrentCashShift(): Promise<CashShiftDetail | null> {
    return Promise.resolve(this.store.getCurrentCashShift());
  }

  openCashShift(openingFloat: number): Promise<string> {
    try {
      return Promise.resolve(this.store.openCashShift(openingFloat));
    } catch (error) {
      return Promise.reject(error);
    }
  }

  closeCashShift(shiftId: string, closingCashCounted: number): Promise<void> {
    try {
      this.store.closeCashShift(shiftId, closingCashCounted);
      return Promise.resolve();
    } catch (error) {
      return Promise.reject(error);
    }
  }

  requestBill(orderId: string, orderRowVersion: string, tableRowVersion: string): Promise<void> {
    try {
      this.store.requestBill(orderId, orderRowVersion, tableRowVersion);
      return Promise.resolve();
    } catch (error) {
      return Promise.reject(error);
    }
  }
}
