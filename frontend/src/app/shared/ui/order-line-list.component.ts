import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { OrderLine } from '../models/resto.models';

@Component({
  selector: 'app-order-line-list',
  standalone: true,
  imports: [DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ul class="space-y-2">
      @for (line of lines(); track line.id) {
        <li
          class="flex items-center justify-between gap-2 rounded-[var(--radius-card)] bg-slate-50 px-3 py-2 animate-fade-in dark:bg-slate-800/60"
        >
          <div class="min-w-0 flex-1">
            <span class="font-medium">{{ line.quantity }}× {{ line.productName }}</span>
            @if (line.notes) {
              <span class="mt-0.5 block text-xs italic text-slate-500">{{ line.notes }}</span>
            }
            @if (showPrices()) {
              <span class="block text-xs text-slate-500">{{ line.subtotal | number: '1.2-2' }}</span>
            }
          </div>
          @if (editable()) {
            <button
              type="button"
              class="touch-target shrink-0 rounded-[var(--radius-card)] bg-rose-100 px-3 text-rose-700 transition hover:bg-rose-200"
              [attr.aria-label]="'Eliminar ' + line.productName"
              (click)="removeLine.emit(line.id)"
            >
              ✕
            </button>
          }
        </li>
      } @empty {
        <li class="text-sm text-slate-500">Sin ítems en el pedido.</li>
      }
    </ul>
  `,
})
export class OrderLineListComponent {
  readonly lines = input.required<OrderLine[]>();
  readonly editable = input(false);
  readonly showPrices = input(false);

  readonly removeLine = output<string>();
}
