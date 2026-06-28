import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Product } from '../models/resto.models';

@Component({
  selector: 'app-product-picker',
  standalone: true,
  imports: [FormsModule, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-4">
      <div class="flex flex-wrap gap-2">
        @for (category of categories(); track category) {
          <button
            type="button"
            class="touch-target rounded-[var(--radius-pill)] px-4 py-1.5 text-sm font-medium transition"
            [class.bg-action-primary]="activeCategory() === category"
            [class.text-white]="activeCategory() === category"
            [class.bg-slate-100]="activeCategory() !== category"
            [class.text-slate-700]="activeCategory() !== category"
            (click)="selectedCategory.set(category)"
          >
            {{ category }}
          </button>
        }
      </div>

      <input
        type="search"
        class="w-full rounded-[var(--radius-card)] border border-slate-200 bg-white px-3 py-2 text-sm outline-none focus:border-action-primary focus:ring-2 focus:ring-action-primary/20"
        placeholder="Buscar producto…"
        [(ngModel)]="searchQuery"
      />

      <div class="flex items-center gap-3 rounded-[var(--radius-card)] bg-slate-50 px-3 py-2">
        <span class="text-sm font-medium text-slate-600">Cantidad</span>
        <button
          type="button"
          class="touch-target rounded-lg bg-white px-3 font-bold text-slate-700 shadow-card"
          [disabled]="quantity() <= 1"
          (click)="decrementQuantity()"
        >
          −
        </button>
        <span class="min-w-8 text-center font-semibold tabular-nums">{{ quantity() }}</span>
        <button
          type="button"
          class="touch-target rounded-lg bg-white px-3 font-bold text-slate-700 shadow-card"
          [disabled]="quantity() >= 99"
          (click)="incrementQuantity()"
        >
          +
        </button>
      </div>

      <input
        type="text"
        class="w-full rounded-[var(--radius-card)] border border-slate-200 bg-white px-3 py-2 text-sm outline-none focus:border-action-primary focus:ring-2 focus:ring-action-primary/20"
        placeholder="Notas (opcional)"
        [(ngModel)]="notes"
        maxlength="200"
      />

      <div class="grid grid-cols-1 gap-2 sm:grid-cols-2">
        @for (product of filteredProducts(); track product.id) {
          <button
            type="button"
            class="touch-target rounded-[var(--radius-card)] border border-slate-200 bg-white px-3 py-2 text-left shadow-card transition hover:border-action-primary/40 hover:shadow-elevated disabled:opacity-50"
            [disabled]="disabled()"
            (click)="addProduct.emit({ productId: product.id, quantity: quantity(), notes: notes || null })"
          >
            {{ product.name }}
            <span class="block text-xs text-slate-500">{{ product.price | number: '1.2-2' }}</span>
          </button>
        } @empty {
          <p class="col-span-full text-sm text-slate-500">No hay productos en esta categoría.</p>
        }
      </div>
    </div>
  `,
})
export class ProductPickerComponent {
  readonly products = input.required<Product[]>();
  readonly disabled = input(false);

  readonly addProduct = output<{ productId: string; quantity: number; notes: string | null }>();

  readonly selectedCategory = signal('');
  readonly quantity = signal(1);
  searchQuery = '';
  notes = '';

  readonly categories = computed(() => {
    const cats = [...new Set(this.products().map((p) => p.category))];
    return cats.sort();
  });

  readonly activeCategory = computed(() => this.selectedCategory() || this.categories()[0] || '');

  readonly filteredProducts = computed(() => {
    const category = this.activeCategory();
    const query = this.searchQuery.toLowerCase().trim();
    return this.products().filter((p) => {
      const matchesCategory = !category || p.category === category;
      const matchesSearch = !query || p.name.toLowerCase().includes(query);
      return matchesCategory && matchesSearch;
    });
  });

  decrementQuantity(): void {
    this.quantity.update((q) => Math.max(1, q - 1));
  }

  incrementQuantity(): void {
    this.quantity.update((q) => Math.min(99, q + 1));
  }
}
