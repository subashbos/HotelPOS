export const USER_ROLES = ['Admin', 'Manager', 'Cashier'] as const;

export interface AppUser {
  sNo: number;
  id: number;
  username: string;
  role: string;
  roleId?: number;
  isActive: boolean;
  mustChangePassword: boolean;
}
