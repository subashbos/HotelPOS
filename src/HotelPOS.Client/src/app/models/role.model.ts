export const PERMISSION_MODULES = [
  'Dashboard', 'Billing', 'Items', 'Categories', 'Units', 'Tables', 'Ledger', 'Journal', 'Settings',
  'Audit', 'Shift', 'Roles', 'SalesReport', 'HrEmployees', 'HrAttendance', 'HrLeave', 'HrPayroll',
  'HrPayrollRun', 'Expenses', 'Customers', 'Purchase', 'Tds', 'OrderManagement', 'CustomerManagement'
] as const;

// Display order of permission groups in the Roles & Access UI.
export const PERMISSION_GROUP_ORDER = [
  'Point of Sale',
  'Customers',
  'Purchasing & Inventory',
  'Finance & Reports',
  'Human Resources',
  'Administration'
] as const;

// Maps each permission module to the group it's shown under.
export const PERMISSION_MODULE_GROUPS: Record<string, string> = {
  Dashboard: 'Point of Sale',
  Billing: 'Point of Sale',
  Items: 'Point of Sale',
  Categories: 'Point of Sale',
  Tables: 'Point of Sale',
  Shift: 'Point of Sale',
  OrderManagement: 'Point of Sale',
  Customers: 'Customers',
  CustomerManagement: 'Customers',
  Purchase: 'Purchasing & Inventory',
  Units: 'Purchasing & Inventory',
  Ledger: 'Finance & Reports',
  Journal: 'Finance & Reports',
  SalesReport: 'Finance & Reports',
  Expenses: 'Finance & Reports',
  HrEmployees: 'Human Resources',
  HrAttendance: 'Human Resources',
  HrLeave: 'Human Resources',
  HrPayroll: 'Human Resources',
  HrPayrollRun: 'Human Resources',
  Tds: 'Human Resources',
  Settings: 'Administration',
  Audit: 'Administration',
  Roles: 'Administration'
};

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
