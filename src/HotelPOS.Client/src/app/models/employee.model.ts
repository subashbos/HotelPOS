export const EMPLOYMENT_TYPES = ['Permanent', 'Probation', 'Contract', 'PartTime'] as const;
export const EMPLOYEE_STATUSES = ['Active', 'OnLeave', 'Suspended', 'Resigned', 'Terminated'] as const;

export interface Department {
  id: number;
  name: string;
  description?: string;
}

export interface Designation {
  id: number;
  title: string;
  departmentId: number;
  departmentName?: string;
}

export interface Employee {
  id: number;
  employeeCode: string;
  firstName: string;
  lastName?: string;
  gender?: string;
  dateOfBirth?: string;
  dateOfJoining: string;
  dateOfExit?: string;
  departmentId?: number;
  departmentName?: string;
  designationId?: number;
  designationTitle?: string;
  employmentType: string;
  status: string;
  phone?: string;
  email?: string;
  address?: string;
  pan?: string;
  aadhaar?: string;
  uan?: string;
  esicNumber?: string;
  bankName?: string;
  bankAccountNumber?: string;
  bankIfsc?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  reportingManagerId?: number;
}

export type SaveEmployeeRequest = Omit<Employee, 'departmentName' | 'designationTitle' | 'employeeCode'> & {
  employeeCode?: string;
};
