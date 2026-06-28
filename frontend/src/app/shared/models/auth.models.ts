export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: UserProfile;
}

export interface StaffUser {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  isActive: boolean;
}

export const AppRole = {
  Waiter: 'Waiter',
  Manager: 'Manager',
  Kitchen: 'Kitchen',
  Admin: 'Admin',
} as const;

export type AppRoleName = (typeof AppRole)[keyof typeof AppRole];
