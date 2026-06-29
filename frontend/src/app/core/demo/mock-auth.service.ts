import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AppRole, LoginResponse, StaffUser, UserProfile } from '../../shared/models/auth.models';
import { DemoStoreService } from './demo-store.service';

export const DEMO_PASSWORD = 'Resto123!';

interface StoredAuth {
  token: string;
  expiresAt: string;
  user: UserProfile;
}

@Injectable({ providedIn: 'root' })
export class MockAuthService {
  private readonly store = inject(DemoStoreService);
  private readonly router = inject(Router);
  private readonly storageKey = 'resto_auth';

  private readonly stored = this.loadStored();

  readonly currentUser = signal<UserProfile | null>(this.stored?.user ?? null);
  readonly token = signal<string | null>(this.stored?.token ?? null);
  readonly expiresAt = signal<string | null>(this.stored?.expiresAt ?? null);
  readonly isAuthenticated = computed(() => {
    const currentToken = this.token();
    const expiry = this.expiresAt();
    if (!currentToken) return false;
    if (expiry && new Date(expiry) <= new Date()) return false;
    return true;
  });

  async login(email: string, password: string): Promise<void> {
    const user = this.store.findUserByEmail(email);
    if (!user || password !== DEMO_PASSWORD) {
      throw new Error('Credenciales inválidas.');
    }

    const response: LoginResponse = {
      token: `demo-token-${user.id}`,
      expiresAt: new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString(),
      user: {
        id: user.id,
        email: user.email,
        displayName: user.displayName,
        roles: [...user.roles],
      },
    };

    this.persist(response);
    await this.router.navigateByUrl(this.getDefaultRoute(response.user.roles));
  }

  logout(): void {
    sessionStorage.removeItem(this.storageKey);
    this.token.set(null);
    this.currentUser.set(null);
    this.expiresAt.set(null);
    void this.router.navigate(['/login']);
  }

  hasRole(role: string): boolean {
    return this.currentUser()?.roles.includes(role) ?? false;
  }

  hasAnyRole(roles: string[]): boolean {
    const userRoles = this.currentUser()?.roles ?? [];
    return roles.some((role) => userRoles.includes(role));
  }

  getDefaultRoute(roles: string[] = this.currentUser()?.roles ?? []): string {
    if (roles.includes(AppRole.Admin) || roles.includes(AppRole.Manager)) return '/encargado';
    if (roles.includes(AppRole.Kitchen)) return '/cocina';
    if (roles.includes(AppRole.Waiter)) return '/mozo';
    return '/login';
  }

  async fetchCurrentUser(): Promise<UserProfile | null> {
    const user = this.currentUser();
    if (!user) {
      this.logout();
      return null;
    }
    return user;
  }

  listStaff(): Promise<StaffUser[]> {
    return Promise.resolve(this.store.listStaff());
  }

  createStaffUser(payload: {
    email: string;
    password: string;
    displayName: string;
    role: string;
  }): Promise<void> {
    try {
      this.store.createStaffUser(payload);
      return Promise.resolve();
    } catch (error) {
      return Promise.reject(error);
    }
  }

  deactivateStaffUser(userId: string): Promise<void> {
    try {
      this.store.deactivateStaffUser(userId);
      return Promise.resolve();
    } catch (error) {
      return Promise.reject(error);
    }
  }

  initializeFromStorage(): void {
    const data = this.loadStored();
    if (!data) return;

    if (new Date(data.expiresAt) <= new Date()) {
      this.logout();
      return;
    }

    this.token.set(data.token);
    this.currentUser.set(data.user);
    this.expiresAt.set(data.expiresAt);
  }

  private persist(response: LoginResponse): void {
    sessionStorage.setItem(this.storageKey, JSON.stringify(response));
    this.token.set(response.token);
    this.currentUser.set(response.user);
    this.expiresAt.set(response.expiresAt);
  }

  private loadStored(): StoredAuth | null {
    const raw = sessionStorage.getItem(this.storageKey);
    if (!raw) return null;

    try {
      return JSON.parse(raw) as StoredAuth;
    } catch {
      sessionStorage.removeItem(this.storageKey);
      return null;
    }
  }
}
