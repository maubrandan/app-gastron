import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex min-h-screen items-center justify-center bg-surface-salon px-4">
      <div class="w-full max-w-md rounded-[var(--radius-card)] bg-surface-card p-8 shadow-elevated">
        <div class="mb-8 text-center">
          <h1 class="font-display text-2xl font-bold text-slate-900">Resto</h1>
          <p class="mt-2 text-sm text-slate-600">Ingresá con tu cuenta para continuar</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-4">
          <div>
            <label for="email" class="mb-1 block text-sm font-medium text-slate-700">Email</label>
            <input
              id="email"
              type="email"
              formControlName="email"
              autocomplete="username"
              class="w-full rounded-[var(--radius-card)] border border-slate-300 px-3 py-2.5 text-slate-900 outline-none focus:border-[var(--color-action-primary)] focus:ring-2 focus:ring-blue-500/20"
              placeholder="mozo1@resto.local"
            />
          </div>

          <div>
            <label for="password" class="mb-1 block text-sm font-medium text-slate-700">Contraseña</label>
            <input
              id="password"
              type="password"
              formControlName="password"
              autocomplete="current-password"
              class="w-full rounded-[var(--radius-card)] border border-slate-300 px-3 py-2.5 text-slate-900 outline-none focus:border-[var(--color-action-primary)] focus:ring-2 focus:ring-blue-500/20"
            />
          </div>

          @if (errorMessage()) {
            <p class="rounded-[var(--radius-card)] bg-red-50 px-3 py-2 text-sm text-red-700">
              {{ errorMessage() }}
            </p>
          }

          <button
            type="submit"
            [disabled]="form.invalid || submitting()"
            class="w-full rounded-[var(--radius-card)] bg-[var(--color-action-primary)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {{ submitting() ? 'Ingresando…' : 'Ingresar' }}
          </button>
        </form>

        <p class="mt-6 text-center text-xs text-slate-500">
          Demo: mozo1@resto.local / encargado@resto.local / kitchen@resto.local
        </p>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  async submit(): Promise<void> {
    if (this.form.invalid || this.submitting()) return;

    this.submitting.set(true);
    this.errorMessage.set(null);

    try {
      await this.auth.login(this.form.getRawValue().email, this.form.getRawValue().password);
    } catch (error) {
      const message =
        error instanceof HttpErrorResponse && error.error?.error
          ? String(error.error.error)
          : 'No se pudo iniciar sesión. Verificá tus credenciales.';
      this.errorMessage.set(message);
    } finally {
      this.submitting.set(false);
    }
  }
}
