import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RestoApiService } from '../../core/api/resto-api.service';
import { HttpErrorReporter } from '../../core/http/http-error-reporter.service';
import { NotificationService } from '../../core/notifications/notification.service';
import { Product } from '../../shared/models/resto.models';
import { LoadingSkeletonComponent } from '../../shared/ui/loading-skeleton.component';

@Component({
  selector: 'app-menu-management',
  standalone: true,
  imports: [ReactiveFormsModule, DecimalPipe, LoadingSkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="min-h-screen bg-surface-salon p-4">
      <div class="mx-auto max-w-5xl">
        <header class="mb-6">
          <h1 class="font-display text-2xl font-bold text-slate-900">Gestión de carta</h1>
          <p class="mt-1 text-sm text-slate-600">Alta, edición y baja de productos del menú</p>
        </header>

        <div class="mb-8 rounded-[var(--radius-card)] bg-surface-card p-6 shadow-card">
          <h2 class="mb-4 text-lg font-semibold text-slate-900">
            {{ editingProduct() ? 'Editar producto' : 'Nuevo producto' }}
          </h2>
          <form [formGroup]="form" (ngSubmit)="saveProduct()" class="grid gap-4 md:grid-cols-2">
            <div>
              <label class="mb-1 block text-sm font-medium text-slate-700">Nombre</label>
              <input
                type="text"
                formControlName="name"
                class="w-full rounded-[var(--radius-card)] border border-slate-300 px-3 py-2"
              />
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-slate-700">Categoría</label>
              <input
                type="text"
                formControlName="category"
                list="category-suggestions"
                class="w-full rounded-[var(--radius-card)] border border-slate-300 px-3 py-2"
              />
              <datalist id="category-suggestions">
                @for (cat of categories(); track cat) {
                  <option [value]="cat"></option>
                }
              </datalist>
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-slate-700">Precio</label>
              <input
                type="number"
                formControlName="price"
                min="0.01"
                step="0.01"
                class="w-full rounded-[var(--radius-card)] border border-slate-300 px-3 py-2"
              />
            </div>
            <div class="flex items-end gap-2">
              <button
                type="submit"
                [disabled]="form.invalid || saving()"
                class="inline-flex min-h-11 items-center justify-center gap-2 rounded-[var(--radius-card)] bg-action-primary px-4 py-2 text-sm font-semibold text-white transition hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-50"
              >
                @if (saving()) {
                  <span class="inline-block h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent"></span>
                }
                {{ editingProduct() ? 'Guardar cambios' : 'Agregar producto' }}
              </button>
              @if (editingProduct()) {
                <button
                  type="button"
                  class="rounded-[var(--radius-card)] px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
                  (click)="cancelEdit()"
                >
                  Cancelar
                </button>
              }
            </div>
          </form>
        </div>

        <div class="rounded-[var(--radius-card)] bg-surface-card p-6 shadow-card">
          <h2 class="mb-4 text-lg font-semibold text-slate-900">Productos</h2>

          @if (loading()) {
            <app-loading-skeleton variant="list" [count]="5" />
          } @else {
            <div class="overflow-x-auto">
              <table class="w-full text-left text-sm">
                <thead>
                  <tr class="border-b border-slate-200 text-slate-600">
                    <th class="pb-2 pr-4 font-medium">Nombre</th>
                    <th class="pb-2 pr-4 font-medium">Categoría</th>
                    <th class="pb-2 pr-4 font-medium">Precio</th>
                    <th class="pb-2 pr-4 font-medium">Estado</th>
                    <th class="pb-2 font-medium">Acciones</th>
                  </tr>
                </thead>
                <tbody>
                  @for (product of products(); track product.id) {
                    <tr class="border-b border-slate-100" [class.opacity-50]="!product.isActive">
                      <td class="py-3 pr-4 font-medium text-slate-900">{{ product.name }}</td>
                      <td class="py-3 pr-4 text-slate-600">{{ product.category }}</td>
                      <td class="py-3 pr-4 tabular-nums">{{ product.price | number: '1.2-2' }}</td>
                      <td class="py-3 pr-4">
                        @if (product.isActive) {
                          <span class="rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-800">Activo</span>
                        } @else {
                          <span class="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">Inactivo</span>
                        }
                      </td>
                      <td class="py-3">
                        <div class="flex gap-2">
                          @if (product.isActive) {
                            <button
                              type="button"
                              class="text-sm font-medium text-blue-600 hover:underline"
                              (click)="startEdit(product)"
                            >
                              Editar
                            </button>
                            <button
                              type="button"
                              class="text-sm font-medium text-rose-600 hover:underline"
                              (click)="deactivate(product)"
                            >
                              Desactivar
                            </button>
                          }
                        </div>
                      </td>
                    </tr>
                  } @empty {
                    <tr>
                      <td colspan="5" class="py-8 text-center text-slate-500">No hay productos en la carta.</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </div>
      </div>
    </section>
  `,
})
export class MenuManagementComponent implements OnInit {
  private readonly api = inject(RestoApiService);
  private readonly notifications = inject(NotificationService);
  private readonly httpErrors = inject(HttpErrorReporter);
  private readonly fb = inject(FormBuilder);

  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly editingProduct = signal<Product | null>(null);

  readonly categories = signal<string[]>([]);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    category: ['', [Validators.required, Validators.maxLength(50)]],
    price: [0, [Validators.required, Validators.min(0.01)]],
  });

  ngOnInit(): void {
    void this.loadProducts();
  }

  startEdit(product: Product): void {
    this.editingProduct.set(product);
    this.form.patchValue({
      name: product.name,
      category: product.category,
      price: product.price,
    });
  }

  cancelEdit(): void {
    this.editingProduct.set(null);
    this.form.reset({ name: '', category: '', price: 0 });
  }

  async saveProduct(): Promise<void> {
    if (this.form.invalid) return;

    const { name, category, price } = this.form.getRawValue();
    this.saving.set(true);

    try {
      const editing = this.editingProduct();
      if (editing) {
        await this.api.updateProduct(editing.id, name, price, category);
        this.notifications.showSuccess('Producto actualizado.');
        this.cancelEdit();
      } else {
        await this.api.createProduct(name, price, category);
        this.notifications.showSuccess('Producto agregado a la carta.');
        this.form.reset({ name: '', category: '', price: 0 });
      }
      await this.loadProducts();
    } catch (error) {
      this.handleError(error, 'No se pudo guardar el producto.');
    } finally {
      this.saving.set(false);
    }
  }

  async deactivate(product: Product): Promise<void> {
    if (!confirm(`¿Desactivar "${product.name}"? No aparecerá en el menú del mozo.`)) return;

    try {
      await this.api.deactivateProduct(product.id);
      this.notifications.showSuccess('Producto desactivado.');
      if (this.editingProduct()?.id === product.id) {
        this.cancelEdit();
      }
      await this.loadProducts();
    } catch (error) {
      this.handleError(error, 'No se pudo desactivar el producto.');
    }
  }

  private async loadProducts(): Promise<void> {
    this.loading.set(true);
    try {
      const list = await this.api.getProducts(true);
      this.products.set(list);
      this.categories.set([...new Set(list.map((p) => p.category))].sort());
    } catch (error) {
      this.handleError(error, 'No se pudo cargar la carta.');
    } finally {
      this.loading.set(false);
    }
  }

  private handleError(error: unknown, fallback: string): void {
    this.httpErrors.report(error, fallback);
  }
}
