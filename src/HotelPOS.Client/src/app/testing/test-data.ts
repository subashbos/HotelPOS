import itemFixtures from './fixtures/items.json';
import categoryFixtures from './fixtures/categories.json';
import unitFixtures from './fixtures/units.json';
import supplierFixtures from './fixtures/suppliers.json';
import roleFixtures from './fixtures/roles.json';
import settingsFixtures from './fixtures/settings.json';
import tdsFixtures from './fixtures/tds.json';
import employeeFixtures from './fixtures/employees.json';
import customerFixtures from './fixtures/customers.json';
import tableFixtures from './fixtures/tables.json';
import expenseFixtures from './fixtures/expenses.json';
import userFixtures from './fixtures/users.json';
import cashSessionFixtures from './fixtures/cash-sessions.json';
import purchaseFixtures from './fixtures/purchases.json';
import leaveFixtures from './fixtures/leave.json';
import payrollFixtures from './fixtures/payroll.json';
import attendanceFixtures from './fixtures/attendance.json';

import { Item, Category, UnitOfMeasurement } from '../models/item.model';
import { Supplier } from '../models/supplier.model';
import { Role, RolePermission } from '../models/role.model';
import { SystemSettings } from '../models/settings.model';
import { TdsRuleSet } from '../models/tds.model';
import { Employee, Department, Designation } from '../models/employee.model';
import { Customer } from '../models/customer.model';
import { DiningTable } from '../models/table.model';
import { Expense } from '../models/expense.model';
import { AppUser } from '../models/user.model';
import { CashSession } from '../models/cash-session.model';
import { Purchase } from '../models/purchase.model';
import { LeaveType, LeaveBalance } from '../models/leave.model';
import { SalaryStructure, PayrollRun, Payslip } from '../models/payroll.model';
import { Attendance } from '../models/attendance.model';

export const MOCK_ITEMS = itemFixtures.mockItems as Item[];
export const MOCK_ITEM = itemFixtures.mockItem as Item;

export const MOCK_CATEGORIES = categoryFixtures.mockCategories as Category[];
export const MOCK_CATEGORY = categoryFixtures.mockCategory as Category;

export const MOCK_UNITS = unitFixtures.mockUnits as UnitOfMeasurement[];

export const MOCK_SUPPLIERS = supplierFixtures.mockSuppliers as Supplier[];
export const MOCK_SUPPLIER = supplierFixtures.mockSupplier as Supplier;

export const MOCK_ROLES = roleFixtures.mockRoles as Role[];
export const MOCK_ROLE = roleFixtures.mockRole as Role;

export const MOCK_SETTINGS = settingsFixtures.mockSettings as SystemSettings;

export const MOCK_TDS_RULESET = tdsFixtures.mockRuleSet as TdsRuleSet;
export const MOCK_FINANCIAL_YEARS = tdsFixtures.mockFinancialYears as number[];

export const MOCK_EMPLOYEES = employeeFixtures.mockEmployees as Employee[];
export const MOCK_EMPLOYEE = employeeFixtures.mockEmployee as Employee;
export const MOCK_DEPARTMENTS = employeeFixtures.mockDepartments as Department[];
export const MOCK_DESIGNATIONS = employeeFixtures.mockDesignations as Designation[];

export const MOCK_CUSTOMERS = customerFixtures.mockCustomers as Customer[];
export const MOCK_CUSTOMER = customerFixtures.mockCustomer as Customer;

export const MOCK_TABLES = tableFixtures.mockTables as DiningTable[];
export const MOCK_TABLE = tableFixtures.mockTable as DiningTable;

export const MOCK_EXPENSES = expenseFixtures.mockExpenses as Expense[];
export const MOCK_EXPENSE = expenseFixtures.mockExpense as Expense;

export const MOCK_USERS = userFixtures.mockUsers as AppUser[];

export const MOCK_CASH_SESSION = cashSessionFixtures.mockSession as CashSession;
export const MOCK_CASH_HISTORY = cashSessionFixtures.mockHistory as CashSession[];

export const MOCK_PURCHASES = purchaseFixtures.mockPurchases as Purchase[];

export const MOCK_LEAVE_TYPES = leaveFixtures.mockLeaveTypes as LeaveType[];
export const MOCK_LEAVE_BALANCES = leaveFixtures.mockBalances as LeaveBalance[];

export const MOCK_SALARY_STRUCTURES = payrollFixtures.mockSalaryStructures as SalaryStructure[];
export const MOCK_PAYROLL_RUN = payrollFixtures.mockPayrollRun as PayrollRun;
export const MOCK_PAYSLIPS = payrollFixtures.mockPayslips as Payslip[];

export const MOCK_ATTENDANCE_LIST = attendanceFixtures.mockAttendanceList as Attendance[];

export const MOCK_SINGLE_ITEM_LIST = itemFixtures.mockSingleItemList as Item[];
export const MOCK_SINGLE_CUSTOMER_LIST = customerFixtures.mockSingleCustomerList as Customer[];
export const MOCK_SINGLE_TABLE_LIST = tableFixtures.mockSingleTableList as DiningTable[];
export const MOCK_SINGLE_CATEGORY_LIST = categoryFixtures.mockSingleCategoryList as Category[];
