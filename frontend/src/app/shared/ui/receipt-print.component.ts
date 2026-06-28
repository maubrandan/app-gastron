import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrderDetail, ReceiptType } from '../../shared/models/resto.models';

@Component({
  selector: 'app-receipt-print',
  standalone: true,
  imports: [DecimalPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    @media print {
      :host {
        display: block !important;
        position: absolute !important;
        left: 0 !important;
        top: 0 !important;
      }

      .receipt-container {
        position: static !important;
        width: 80mm;
        margin: 0;
        padding: 4mm;
        box-shadow: none !important;
        background: white !important;
        color: black !important;
      }
    }

    @media screen {
      .receipt-container {
        position: fixed;
        left: -9999px;
        top: 0;
        width: 80mm;
        background: white;
        color: black;
        padding: 8mm;
        font-family: 'Courier New', monospace;
        font-size: 12px;
        line-height: 1.4;
      }
    }
  `,
  template: `
    <div class="receipt-container">
      <header class="mb-3 text-center">
        <h1 class="text-base font-bold">{{ restaurantName() }}</h1>
        <p class="text-xs uppercase">{{ typeLabel() }}</p>
      </header>

      <div class="mb-3 border-b border-dashed border-black pb-2 text-xs">
        <p>Mesa: {{ order().tableNumber }}</p>
        @if (type() === 'kitchen' && order().sentToKitchenAt) {
          <p>Enviado: {{ order().sentToKitchenAt | date: 'dd/MM/yyyy HH:mm' }}</p>
        }
        @if (type() === 'bill' && order().closedAt) {
          <p>Cierre: {{ order().closedAt | date: 'dd/MM/yyyy HH:mm' }}</p>
          <p>Ticket: {{ order().id.slice(0, 8).toUpperCase() }}</p>
        }
      </div>

      @if (type() === 'kitchen') {
        @for (group of groupedLines(); track group.category) {
          <div class="mb-2">
            <p class="text-xs font-bold uppercase">{{ group.category }}</p>
            @for (line of group.lines; track line.id) {
              <div class="flex gap-1 text-xs">
                <span>{{ line.quantity }}×</span>
                <span>{{ line.productName }}</span>
              </div>
              @if (line.notes) {
                <p class="ml-4 text-xs italic">({{ line.notes }})</p>
              }
            }
          </div>
        }
      } @else {
        @for (line of order().lines; track line.id) {
          <div class="mb-1 text-xs">
            <div class="flex justify-between gap-2">
              <span>{{ line.quantity }}× {{ line.productName }}</span>
              <span class="tabular-nums">{{ line.subtotal | number: '1.2-2' }}</span>
            </div>
            @if (line.notes) {
              <p class="ml-2 italic">({{ line.notes }})</p>
            }
          </div>
        }
        <div class="mt-3 flex justify-between border-t border-dashed border-black pt-2 text-sm font-bold">
          <span>TOTAL</span>
          <span class="tabular-nums">{{ order().total | number: '1.2-2' }}</span>
        </div>
      }

      <footer class="mt-4 text-center text-xs">
        <p>Gracias por su visita</p>
        <p>{{ printedAt() | date: 'dd/MM/yyyy HH:mm' }}</p>
      </footer>
    </div>
  `,
})
export class ReceiptPrintComponent {
  readonly type = input.required<ReceiptType>();
  readonly order = input.required<OrderDetail>();
  readonly restaurantName = input('Resto');
  readonly printedAt = input(new Date());

  readonly typeLabel = computed(() => (this.type() === 'kitchen' ? 'Comanda de cocina' : 'Ticket de cuenta'));

  readonly groupedLines = computed(() => {
    const groups = new Map<string, OrderDetail['lines']>();
    for (const line of this.order().lines) {
      const existing = groups.get(line.category) ?? [];
      existing.push(line);
      groups.set(line.category, existing);
    }
    return [...groups.entries()].map(([category, lines]) => ({ category, lines }));
  });
}
