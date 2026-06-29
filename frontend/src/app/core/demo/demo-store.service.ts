import { Injectable, inject } from '@angular/core';
import {
  ClosedOrderSummary,
  CreateOrderResponse,
  DailySummary,
  CashShiftDetail,
  KitchenOrder,
  KitchenOrderLine,
  OrderDetail,
  OrderLine,
  PaymentMethod,
  Product,
  TableState,
} from '../../shared/models/resto.models';
import { StaffUser } from '../../shared/models/auth.models';
import { createDemoSeed, DemoUser } from './demo-seed';
import { MockSignalRService } from './mock-signalr.service';

interface InternalOrder {
  id: string;
  tableNumber: number;
  status: string;
  total: number;
  rowVersion: string;
  createdAt: string;
  sentToKitchenAt: string | null;
  closedAt: string | null;
  paymentMethod: PaymentMethod | null;
  lines: OrderLine[];
}

@Injectable({ providedIn: 'root' })
export class DemoStoreService {
  private readonly signalr = inject(MockSignalRService);

  private versionCounter = 1;
  private tables: TableState[] = [];
  private products: Product[] = [];
  private staff: StaffUser[] = [];
  private users: DemoUser[] = [];
  private orders = new Map<string, InternalOrder>();
  private kitchenOrders: KitchenOrder[] = [];
  private closedOrders: ClosedOrderSummary[] = [];
  private currentShift: CashShiftDetail | null = null;

  constructor() {
    this.reset();
  }

  reset(): void {
    const seed = createDemoSeed();
    this.versionCounter = 1;
    this.tables = seed.tables.map((t) => ({ ...t }));
    this.products = seed.products.map((p) => ({ ...p }));
    this.staff = seed.staff.map((s) => ({ ...s }));
    this.users = seed.users.map((u) => ({ ...u }));
    this.orders.clear();
    this.kitchenOrders = [];
    this.closedOrders = [];
    this.openDefaultShift();
  }

  getCurrentCashShift(): CashShiftDetail | null {
    return this.currentShift ? { ...this.currentShift, summary: { ...this.currentShift.summary } } : null;
  }

  openCashShift(openingFloat: number): string {
    if (this.currentShift?.status === 'Open') {
      throw new Error('Ya hay un turno de caja abierto.');
    }

    const id = crypto.randomUUID();
    this.currentShift = {
      id,
      openedAt: new Date().toISOString(),
      openedByUserId: crypto.randomUUID(),
      openingFloat,
      status: 'Open',
      summary: this.emptyShiftSummary(openingFloat),
    };
    return id;
  }

  closeCashShift(shiftId: string, closingCashCounted: number): void {
    if (!this.currentShift || this.currentShift.id !== shiftId) {
      throw new Error('Turno de caja no encontrado.');
    }
    if (this.currentShift.status !== 'Open') {
      throw new Error('El turno de caja ya está cerrado.');
    }

    this.currentShift = {
      ...this.currentShift,
      status: 'Closed',
      summary: {
        ...this.currentShift.summary,
      },
    };
    this.currentShift = null;
    void closingCashCounted;
  }

  private openDefaultShift(): void {
    this.openCashShift(5000);
  }

  private emptyShiftSummary(openingFloat: number) {
    return {
      paymentCount: 0,
      totalCash: 0,
      totalCard: 0,
      totalTransfer: 0,
      totalRevenue: 0,
      expectedCash: openingFloat,
    };
  }

  private refreshShiftSummary(): void {
    if (!this.currentShift) return;

    const payments = this.closedOrders
      .map((o) => ({ method: o.paymentMethod, total: o.total }))
      .filter((p) => p.method);

    const totalCash = payments
      .filter((p) => p.method === 'Cash')
      .reduce((sum, p) => sum + p.total, 0);
    const totalCard = payments
      .filter((p) => p.method === 'Card')
      .reduce((sum, p) => sum + p.total, 0);
    const totalTransfer = payments
      .filter((p) => p.method === 'Transfer')
      .reduce((sum, p) => sum + p.total, 0);

    this.currentShift = {
      ...this.currentShift,
      summary: {
        paymentCount: payments.length,
        totalCash,
        totalCard,
        totalTransfer,
        totalRevenue: totalCash + totalCard + totalTransfer,
        expectedCash: this.currentShift.openingFloat + totalCash,
      },
    };
  }

  findUserByEmail(email: string): DemoUser | undefined {
    return this.users.find(
      (u) => u.email.toLowerCase() === email.toLowerCase() && u.isActive,
    );
  }

  getTables(): TableState[] {
    return this.tables.map((t) => ({ ...t }));
  }

  getProducts(includeInactive = false): Product[] {
    return this.products
      .filter((p) => includeInactive || p.isActive)
      .map((p) => ({ ...p }));
  }

  createProduct(name: string, price: number, category: string): string {
    const id = crypto.randomUUID();
    this.products.push({ id, name, price, category, isActive: true });
    return id;
  }

  updateProduct(id: string, name: string, price: number, category: string): void {
    const product = this.products.find((p) => p.id === id);
    if (!product) throw new Error('El producto no existe.');
    product.name = name;
    product.price = price;
    product.category = category;
  }

  deactivateProduct(id: string): void {
    const product = this.products.find((p) => p.id === id);
    if (!product) throw new Error('El producto no existe.');
    product.isActive = false;
  }

  getActiveKitchenOrders(category?: string | null): KitchenOrder[] {
    return this.kitchenOrders
      .filter((o) => !category || o.lines.some((l) => l.category === category))
      .map((o) => ({ ...o, lines: o.lines.map((l) => ({ ...l })) }));
  }

  getOrderByTable(tableNumber: number): OrderDetail | null {
    const table = this.getTable(tableNumber);
    if (!table?.activeOrderId) return null;
    return this.toOrderDetail(this.orders.get(table.activeOrderId) ?? null);
  }

  getOrderById(orderId: string): OrderDetail | null {
    return this.toOrderDetail(this.orders.get(orderId) ?? null);
  }

  getDailySummary(date: string): DailySummary {
    const closed = this.getClosedForDate(date);
    const totalRevenue = closed.reduce((sum, o) => sum + o.total, 0);
    const orderCount = closed.length;
    const averageTicket = orderCount > 0 ? totalRevenue / orderCount : 0;

    const ordersByHour = new Map<number, { orderCount: number; revenue: number }>();
    for (const order of closed) {
      const full = this.orders.get(order.id);
      if (!full?.closedAt) continue;
      const hour = new Date(full.closedAt).getHours();
      const entry = ordersByHour.get(hour) ?? { orderCount: 0, revenue: 0 };
      entry.orderCount += 1;
      entry.revenue += order.total;
      ordersByHour.set(hour, entry);
    }

    return {
      orderCount,
      totalRevenue,
      averageTicket,
      ordersByHour: [...ordersByHour.entries()]
        .sort(([a], [b]) => a - b)
        .map(([hour, data]) => ({ hour, ...data })),
    };
  }

  getClosedOrders(date: string): ClosedOrderSummary[] {
    return this.getClosedForDate(date).map((o) => ({ ...o }));
  }

  listStaff(): StaffUser[] {
    return this.staff.map((s) => ({ ...s, roles: [...s.roles] }));
  }

  createStaffUser(payload: {
    email: string;
    password: string;
    displayName: string;
    role: string;
  }): void {
    if (this.users.some((u) => u.email.toLowerCase() === payload.email.toLowerCase())) {
      throw new Error('Ya existe un usuario con ese email.');
    }
    const id = crypto.randomUUID();
    const user: DemoUser = {
      id,
      email: payload.email,
      displayName: payload.displayName,
      roles: [payload.role],
      isActive: true,
    };
    this.users.push(user);
    this.staff.push({
      id,
      email: payload.email,
      displayName: payload.displayName,
      roles: [payload.role],
      isActive: true,
    });
  }

  deactivateStaffUser(userId: string): void {
    const user = this.users.find((u) => u.id === userId);
    if (!user) throw new Error('El usuario no existe.');
    user.isActive = false;
    const staff = this.staff.find((s) => s.id === userId);
    if (staff) staff.isActive = false;
  }

  createOrder(tableNumber: number, tableRowVersion: string): CreateOrderResponse {
    const table = this.getTable(tableNumber);
    if (!table) throw new Error('La mesa no existe.');
    if (table.rowVersion !== tableRowVersion) throw new Error('La mesa fue modificada.');
    if (table.status !== 'Libre') throw new Error('La mesa no está libre para abrir un pedido.');

    const orderId = crypto.randomUUID();
    const order: InternalOrder = {
      id: orderId,
      tableNumber,
      status: 'Borrador',
      total: 0,
      rowVersion: this.nextVersion(),
      createdAt: new Date().toISOString(),
      sentToKitchenAt: null,
      closedAt: null,
      paymentMethod: null,
      lines: [],
    };

    this.orders.set(orderId, order);
    table.status = 'Atendiendo';
    table.activeOrderId = orderId;
    table.rowVersion = this.nextVersion();
    this.emitTable(table);

    return {
      orderId,
      orderRowVersion: order.rowVersion,
      tableRowVersion: table.rowVersion,
    };
  }

  addOrderLine(
    orderId: string,
    productId: string,
    quantity: number,
    notes: string | null,
    rowVersion: string,
  ): string {
    const order = this.getMutableOrder(orderId, rowVersion);
    if (order.status !== 'Borrador') throw new Error('Solo se pueden modificar pedidos en borrador.');

    const product = this.products.find((p) => p.id === productId && p.isActive);
    if (!product) throw new Error('El producto no existe o está inactivo.');

    const line: OrderLine = {
      id: crypto.randomUUID(),
      productId: product.id,
      productName: product.name,
      category: product.category,
      quantity,
      unitPrice: product.price,
      subtotal: product.price * quantity,
      notes,
    };

    order.lines.push(line);
    order.total = order.lines.reduce((sum, l) => sum + l.subtotal, 0);
    order.rowVersion = this.nextVersion();
    return order.rowVersion;
  }

  removeOrderLine(orderId: string, lineId: string, rowVersion: string): string {
    const order = this.getMutableOrder(orderId, rowVersion);
    if (order.status !== 'Borrador') throw new Error('Solo se pueden modificar pedidos en borrador.');

    const index = order.lines.findIndex((l) => l.id === lineId);
    if (index === -1) throw new Error('La línea de pedido no existe.');

    order.lines.splice(index, 1);
    order.total = order.lines.reduce((sum, l) => sum + l.subtotal, 0);
    order.rowVersion = this.nextVersion();
    return order.rowVersion;
  }

  confirmForKitchen(orderId: string, rowVersion: string): void {
    const order = this.getMutableOrder(orderId, rowVersion);
    if (order.status !== 'Borrador') throw new Error('Solo se pueden confirmar pedidos en borrador.');
    if (order.lines.length === 0) throw new Error('No se puede enviar un pedido vacío a la cocina.');

    order.status = 'ConfirmadoEnCocina';
    order.sentToKitchenAt = new Date().toISOString();
    order.rowVersion = this.nextVersion();

    const kitchenOrder = this.toKitchenOrder(order);
    this.kitchenOrders.push(kitchenOrder);
    this.signalr.emitKitchenOrderAdded(kitchenOrder);
  }

  requestBill(orderId: string, orderRowVersion: string, tableRowVersion: string): void {
    const order = this.getMutableOrder(orderId, orderRowVersion);
    if (order.status !== 'ConfirmadoEnCocina') {
      throw new Error('Solo se puede solicitar cuenta en pedidos confirmados en cocina.');
    }

    const table = this.getTable(order.tableNumber);
    if (!table || table.rowVersion !== tableRowVersion) throw new Error('La mesa fue modificada.');
    if (table.status !== 'Atendiendo') throw new Error('Solo se puede solicitar cuenta en mesas en atención.');

    table.status = 'EsperandoCuenta';
    table.rowVersion = this.nextVersion();
    this.emitTable(table);
  }

  closeAndBill(
    orderId: string,
    orderRowVersion: string,
    tableRowVersion: string,
    paymentMethod: PaymentMethod,
  ): void {
    if (!this.currentShift || this.currentShift.status !== 'Open') {
      throw new Error('No hay turno de caja abierto. Abrí un turno antes de facturar.');
    }

    const order = this.getMutableOrder(orderId, orderRowVersion);
    if (order.status !== 'ConfirmadoEnCocina') {
      throw new Error('Solo se pueden cerrar pedidos confirmados en cocina.');
    }

    const table = this.getTable(order.tableNumber);
    if (!table || table.rowVersion !== tableRowVersion) throw new Error('La mesa fue modificada.');

    order.status = 'Cerrado';
    order.closedAt = new Date().toISOString();
    order.paymentMethod = paymentMethod;
    order.rowVersion = this.nextVersion();

    this.kitchenOrders = this.kitchenOrders.filter((k) => k.id !== orderId);
    this.signalr.emitKitchenOrderRemoved(orderId);

    this.closedOrders.push({
      id: order.id,
      tableNumber: order.tableNumber,
      total: order.total,
      closedAt: order.closedAt,
      lineCount: order.lines.length,
      paymentMethod,
    });

    this.refreshShiftSummary();

    table.status = 'Libre';
    table.activeOrderId = null;
    table.rowVersion = this.nextVersion();
    this.emitTable(table);
  }

  private getClosedForDate(date: string): ClosedOrderSummary[] {
    return this.closedOrders.filter((o) => o.closedAt.startsWith(date));
  }

  private getTable(number: number): TableState | undefined {
    return this.tables.find((t) => t.number === number);
  }

  private getMutableOrder(orderId: string, rowVersion: string): InternalOrder {
    const order = this.orders.get(orderId);
    if (!order) throw new Error('El pedido no existe.');
    if (order.rowVersion !== rowVersion) throw new Error('Otro mozo modificó este pedido.');
    return order;
  }

  private toOrderDetail(order: InternalOrder | null): OrderDetail | null {
    if (!order) return null;
    return {
      id: order.id,
      tableNumber: order.tableNumber,
      status: order.status,
      total: order.total,
      rowVersion: order.rowVersion,
      createdAt: order.createdAt,
      sentToKitchenAt: order.sentToKitchenAt,
      closedAt: order.closedAt,
      paymentMethod: order.paymentMethod,
      lines: order.lines.map((l) => ({ ...l })),
    };
  }

  private toKitchenOrder(order: InternalOrder): KitchenOrder {
    const lines: KitchenOrderLine[] = order.lines.map((l) => ({
      id: l.id,
      productName: l.productName,
      quantity: l.quantity,
      notes: l.notes,
      category: l.category,
    }));

    return {
      id: order.id,
      tableNumber: order.tableNumber,
      sentToKitchenAt: order.sentToKitchenAt!,
      lines,
    };
  }

  private emitTable(table: TableState): void {
    this.signalr.emitTableStateUpdated({ ...table });
  }

  private nextVersion(): string {
    this.versionCounter += 1;
    return String(this.versionCounter);
  }
}
