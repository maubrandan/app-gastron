import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';
import { AppRole } from './shared/models/auth.models';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    loadComponent: () =>
      import('./shared/ui/app-shell.component').then((m) => m.AppShellComponent),
    children: [
      { path: '', redirectTo: 'mozo', pathMatch: 'full' },
      {
        path: 'mozo',
        canActivate: [authGuard],
        data: { roles: [AppRole.Waiter, AppRole.Manager, AppRole.Admin] },
        loadComponent: () =>
          import('./features/waiter/waiter-view.component').then((m) => m.WaiterViewComponent),
      },
      {
        path: 'encargado',
        canActivate: [authGuard],
        data: { roles: [AppRole.Manager, AppRole.Admin] },
        loadComponent: () =>
          import('./features/manager/manager-view.component').then((m) => m.ManagerViewComponent),
      },
      {
        path: 'cocina',
        canActivate: [authGuard],
        data: { roles: [AppRole.Kitchen, AppRole.Manager, AppRole.Admin] },
        loadComponent: () =>
          import('./features/kitchen/kitchen-monitor.component').then((m) => m.KitchenMonitorComponent),
      },
      {
        path: 'personal',
        canActivate: [authGuard],
        data: { roles: [AppRole.Manager, AppRole.Admin] },
        loadComponent: () =>
          import('./features/staff/staff-management.component').then((m) => m.StaffManagementComponent),
      },
      {
        path: 'carta',
        canActivate: [authGuard],
        data: { roles: [AppRole.Manager, AppRole.Admin] },
        loadComponent: () =>
          import('./features/menu/menu-management.component').then((m) => m.MenuManagementComponent),
      },
      {
        path: 'caja',
        canActivate: [authGuard],
        data: { roles: [AppRole.Manager, AppRole.Admin] },
        loadComponent: () =>
          import('./features/caja/caja-view.component').then((m) => m.CajaViewComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'mozo' },
];
