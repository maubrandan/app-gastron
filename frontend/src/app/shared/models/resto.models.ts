export type PaymentMethod = 'Cash' | 'Card' | 'Transfer';

export interface KitchenOrderLine {
  id: string;
  productName: string;
  quantity: number;
  notes: string | null;
  category: string;
}

export interface KitchenOrder {
  id: string;
  tableNumber: number;
  sentToKitchenAt: string;
  lines: KitchenOrderLine[];
}

export interface TableState {
  number: number;
  status: string;
  rowVersion: string;
  activeOrderId: string | null;
}

export interface OrderLine {
  id: string;
  productId: string;
  productName: string;
  category: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
  notes: string | null;
}

export interface OrderDetail {
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

export interface Product {
  id: string;
  name: string;
  price: number;
  category: string;
  isActive: boolean;
}

export interface CreateOrderResponse {
  orderId: string;
  orderRowVersion: string;
  tableRowVersion: string;
}

export interface DailySummary {
  orderCount: number;
  totalRevenue: number;
  averageTicket: number;
  ordersByHour: OrdersByHour[];
}

export interface OrdersByHour {
  hour: number;
  orderCount: number;
  revenue: number;
}

export interface ClosedOrderSummary {
  id: string;
  tableNumber: number;
  total: number;
  closedAt: string;
  lineCount: number;
  paymentMethod: PaymentMethod | null;
}

export interface CashShiftSummary {
  paymentCount: number;
  totalCash: number;
  totalCard: number;
  totalTransfer: number;
  totalRevenue: number;
  expectedCash: number;
}

export interface CashShiftDetail {
  id: string;
  openedAt: string;
  openedByUserId: string;
  openingFloat: number;
  status: string;
  summary: CashShiftSummary;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
}

export type ReceiptType = 'kitchen' | 'bill';
