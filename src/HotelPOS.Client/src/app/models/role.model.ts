export const PERMISSION_MODULES = [
  'Dashboard', 'Billing', 'Items', 'Categories', 'Units', 'Tables', 'Ledger', 'Journal', 'Settings',
  'Audit', 'Shift', 'Roles', 'SalesReport', 'HrEmployees', 'HrAttendance', 'HrLeave', 'HrPayroll', 'Expenses'
] as const;

export interface RolePermission {
  id: number;
  roleId: number;
  moduleName: string;
  canAccess: boolean;
  canEdit: boolean;
  canDelete: boolean;
}

export interface Role {
  id: number;
  name: string;
  description: string;
  permissions: RolePermission[];
}
