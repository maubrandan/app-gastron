import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { AppRole } from '../models/auth.models';
import { ToastComponent } from './toast.component';
import { environment } from '../../../environments/environment';

interface NavLink {
  path: string;
  label: string;
  icon: string;
  roles: string[];
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-toast />

    <header class="border-b border-slate-200 bg-surface-card shadow-card">
      <nav class="mx-auto flex max-w-7xl items-center gap-1 px-4 py-3">
        <span class="mr-4 font-display text-lg font-bold text-slate-900">Resto</span>

        @if (environment.demoMode) {
          <span
            class="mr-2 rounded-full bg-amber-100 px-2 py-0.5 text-xs font-semibold uppercase tracking-wide text-amber-800"
          >
            Demo
          </span>
        }

        @for (link of visibleLinks(); track link.path) {
          <a
            [routerLink]="link.path"
            routerLinkActive="nav-active"
            class="relative rounded-[var(--radius-card)] px-4 py-2 text-sm font-medium text-slate-600 transition hover:bg-slate-100 hover:text-slate-900"
          >
            <span class="mr-1.5" aria-hidden="true">{{ link.icon }}</span>
            {{ link.label }}
          </a>
        }

        <div class="ml-auto flex items-center gap-3">
          @if (auth.currentUser(); as user) {
            <span class="hidden text-sm text-slate-600 sm:inline">{{ user.displayName }}</span>
            <button
              type="button"
              class="rounded-[var(--radius-card)] px-3 py-1.5 text-sm font-medium text-slate-600 transition hover:bg-slate-100 hover:text-slate-900"
              (click)="auth.logout()"
            >
              Cerrar sesión
            </button>
          }
        </div>
      </nav>
    </header>

    <main>
      <router-outlet />
    </main>
  `,
  styles: `
    .nav-active {
      color: var(--color-action-primary);
      background-color: rgb(37 99 235 / 0.08);
    }

    .nav-active::after {
      content: '';
      position: absolute;
      bottom: 0;
      left: 50%;
      width: 60%;
      height: 2px;
      background: var(--color-action-primary);
      border-radius: 2px;
      transform: translateX(-50%);
    }
  `,
})
export class AppShellComponent {
  readonly auth = inject(AuthService);
  readonly environment = environment;

  private readonly allLinks: NavLink[] = [
    { path: '/mozo', label: 'Mozo', icon: '🍽️', roles: [AppRole.Waiter, AppRole.Manager, AppRole.Admin] },
    { path: '/encargado', label: 'Encargado', icon: '📋', roles: [AppRole.Manager, AppRole.Admin] },
    { path: '/cocina', label: 'Cocina', icon: '👨‍🍳', roles: [AppRole.Kitchen, AppRole.Manager, AppRole.Admin] },
    { path: '/personal', label: 'Personal', icon: '👥', roles: [AppRole.Manager, AppRole.Admin] },
    { path: '/carta', label: 'Carta', icon: '📖', roles: [AppRole.Manager, AppRole.Admin] },
    { path: '/caja', label: 'Caja', icon: '💰', roles: [AppRole.Manager, AppRole.Admin] },
  ];

  readonly visibleLinks = computed(() =>
    this.allLinks.filter((link) => this.auth.hasAnyRole(link.roles)),
  );
}
