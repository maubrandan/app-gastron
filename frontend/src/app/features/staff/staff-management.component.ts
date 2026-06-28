import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../core/auth/auth.service';
import { AppRole, StaffUser } from '../../shared/models/auth.models';
import { LoadingSkeletonComponent } from '../../shared/ui/loading-skeleton.component';

@Component({
  selector: 'app-staff-management',
  standalone: true,
  imports: [ReactiveFormsModule, LoadingSkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="min-h-screen bg-surface-salon p-4">
      <div class="mx-auto max-w-5xl">
        <header class="mb-6">
          <h1 class="font-display text-2xl font-bold text-slate-900">Gestión de personal</h1>
          <p class="mt-1 text-sm text-slate-600">Alta y baja de usuarios del restaurante</p>
        </header>

        <div class="mb-8 rounded-[var(--radius-card)] bg-surface-card p-6 shadow-card">
          <h2 class="mb-4 text-lg font-semibold text-slate-900">Nuevo usuario</h2>
          <form [formGroup]="form" (ngSubmit)="createUser()" class="grid gap-4 md:grid-cols-2">
            <div>
              <label class="mb-1 block text-sm font-medium text-slate-700">Nombre</label>
              <input
                type="text"
                formControlName="displayName"
                class="w-full rounded-[var(--radius-card)] border border-slate-300 px-3 py-2"
              />
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-slate-700">Email</label>
              <input
                type="email"
                formControlName="email"
                class="w-full rounded-[var(--radius-card)] border border-slate-300 px-3 py-2"
              />
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-slate-700">Contraseña</label>
              <input
                type="password"
                formControlName="password"
                class="w-full rounded-[var(--radius-card)] border border-slate-300 px-3 py-2"
              />
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-slate-700">Rol</label>
              <select formControlName="role" class="w-full rounded-[var(--radius-card)] border border-slate-300 px-3 py-2">
                <option [value]="roles.Waiter">Mozo</option>
                <option [value]="roles.Manager">Encargado</option>
                <option [value]="roles.Kitchen">Cocina</option>
                <option [value]="roles.Admin">Administrador</option>
              </select>
            </div>
            <div class="md:col-span-2">
              <button
                type="submit"
                [disabled]="form.invalid || creating()"
                class="inline-flex min-h-11 items-center justify-center rounded-[var(--radius-card)] bg-[var(--color-action-primary)] px-4 py-2 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
              >
                Crear usuario
              </button>
            </div>
          </form>
          @if (formError()) {
            <p class="mt-3 text-sm text-red-600">{{ formError() }}</p>
          }
          @if (formSuccess()) {
            <p class="mt-3 text-sm text-emerald-600">{{ formSuccess() }}</p>
          }
        </div>

        @if (loading()) {
          <app-loading-skeleton variant="list" [count]="4" />
        } @else {
          <div class="overflow-hidden rounded-[var(--radius-card)] bg-surface-card shadow-card">
            <table class="min-w-full text-left text-sm">
              <thead class="border-b border-slate-200 bg-slate-50 text-slate-600">
                <tr>
                  <th class="px-4 py-3 font-medium">Nombre</th>
                  <th class="px-4 py-3 font-medium">Email</th>
                  <th class="px-4 py-3 font-medium">Rol</th>
                  <th class="px-4 py-3 font-medium">Estado</th>
                  <th class="px-4 py-3 font-medium"></th>
                </tr>
              </thead>
              <tbody>
                @for (user of staff(); track user.id) {
                  <tr class="border-b border-slate-100">
                    <td class="px-4 py-3 text-slate-900">{{ user.displayName }}</td>
                    <td class="px-4 py-3 text-slate-600">{{ user.email }}</td>
                    <td class="px-4 py-3 text-slate-600">{{ formatRoles(user.roles) }}</td>
                    <td class="px-4 py-3">
                      <span
                        class="rounded-full px-2 py-0.5 text-xs font-medium"
                        [class.bg-emerald-50]="user.isActive"
                        [class.text-emerald-700]="user.isActive"
                        [class.bg-slate-100]="!user.isActive"
                        [class.text-slate-600]="!user.isActive"
                      >
                        {{ user.isActive ? 'Activo' : 'Inactivo' }}
                      </span>
                    </td>
                    <td class="px-4 py-3 text-right">
                      @if (user.isActive) {
                        <button
                          type="button"
                          class="text-sm font-medium text-red-600 hover:text-red-700 disabled:opacity-50"
                          [disabled]="deactivatingId() === user.id"
                          (click)="deactivateUser(user)"
                        >
                          Desactivar
                        </button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    </section>
  `,
})
export class StaffManagementComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly roles = AppRole;
  readonly staff = signal<StaffUser[]>([]);
  readonly loading = signal(true);
  readonly creating = signal(false);
  readonly deactivatingId = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly formSuccess = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    role: [AppRole.Waiter, Validators.required],
  });

  ngOnInit(): void {
    void this.loadStaff();
  }

  formatRoles(roles: string[]): string {
    const labels: Record<string, string> = {
      Waiter: 'Mozo',
      Manager: 'Encargado',
      Kitchen: 'Cocina',
      Admin: 'Admin',
    };
    return roles.map((role) => labels[role] ?? role).join(', ');
  }

  async createUser(): Promise<void> {
    if (this.form.invalid || this.creating()) return;

    this.creating.set(true);
    this.formError.set(null);
    this.formSuccess.set(null);

    try {
      await this.auth.createStaffUser(this.form.getRawValue());
      this.form.reset({ role: AppRole.Waiter });
      this.formSuccess.set('Usuario creado correctamente.');
      await this.loadStaff();
    } catch (error) {
      this.formError.set(this.extractError(error, 'No se pudo crear el usuario.'));
    } finally {
      this.creating.set(false);
    }
  }

  async deactivateUser(user: StaffUser): Promise<void> {
    this.deactivatingId.set(user.id);
    this.formError.set(null);

    try {
      await this.auth.deactivateStaffUser(user.id);
      await this.loadStaff();
    } catch (error) {
      this.formError.set(this.extractError(error, 'No se pudo desactivar el usuario.'));
    } finally {
      this.deactivatingId.set(null);
    }
  }

  private async loadStaff(): Promise<void> {
    this.loading.set(true);
    try {
      this.staff.set(await this.auth.listStaff());
    } catch (error) {
      this.formError.set(this.extractError(error, 'No se pudo cargar el personal.'));
    } finally {
      this.loading.set(false);
    }
  }

  private extractError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && error.error?.error) {
      return String(error.error.error);
    }
    return fallback;
  }
}
