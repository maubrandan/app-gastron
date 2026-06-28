import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AppRole, LoginResponse, StaffUser, UserProfile } from '../../shared/models/auth.models';

interface StoredAuth {
  token: string;
  expiresAt: string;
  user: UserProfile;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
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
    const response = await firstValueFrom(
      this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, { email, password }),
    );

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
    try {
      const profile = await firstValueFrom(
        this.http.get<UserProfile>(`${environment.apiUrl}/auth/me`),
      );
      this.currentUser.set(profile);
      this.updateStoredUser(profile);
      return profile;
    } catch {
      this.logout();
      return null;
    }
  }

  listStaff(): Promise<StaffUser[]> {
    return firstValueFrom(this.http.get<StaffUser[]>(`${environment.apiUrl}/auth/users`));
  }

  createStaffUser(payload: {
    email: string;
    password: string;
    displayName: string;
    role: string;
  }): Promise<void> {
    return firstValueFrom(
      this.http.post(`${environment.apiUrl}/auth/users`, payload),
    ).then(() => undefined);
  }

  deactivateStaffUser(userId: string): Promise<void> {
    return firstValueFrom(
      this.http.post(`${environment.apiUrl}/auth/users/${userId}/deactivate`, {}),
    ).then(() => undefined);
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

  private updateStoredUser(user: UserProfile): void {
    const raw = sessionStorage.getItem(this.storageKey);
    if (!raw) return;

    const parsed = JSON.parse(raw) as StoredAuth;
    parsed.user = user;
    sessionStorage.setItem(this.storageKey, JSON.stringify(parsed));
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
