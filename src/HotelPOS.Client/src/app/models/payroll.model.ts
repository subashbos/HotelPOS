export interface SalaryStructure {
  id: number;
  employeeId: number;
  effectiveFrom: string;
  effectiveTo?: string;
  basic: number;
  hra: number;
  da: number;
  conveyanceAllowance: number;
  medicalAllowance: number;
  specialAllowance: number;
  grossMonthly: number;
  pfApplicable: boolean;
  esiApplicable: boolean;
  professionalTaxApplicable: boolean;
}

export interface SaveSalaryStructureRequest {
  id: number;
  employeeId: number;
  effectiveFrom: string;
  effectiveTo?: string;
  basic: number;
  hra: number;
  da: number;
  conveyanceAllowance: number;
  medicalAllowance: number;
  specialAllowance: number;
  pfApplicable: boolean;
  esiApplicable: boolean;
  professionalTaxApplicable: boolean;
}

export interface Payslip {
  id: number;
  payrollRunId: number;
  employeeId: number;
  employeeName?: string;
  grossEarnings: number;
  workingDays: number;
  paidDays: number;
  lopDays: number;
  lopAmount: number;
  pfEmployee: number;
  pfEmployer: number;
  esiEmployee: number;
  esiEmployer: number;
  professionalTax: number;
  tds: number;
  netPay: number;
  paymentStatus: string;
}

export interface PayrollRun {
  id: number;
  month: number;
  year: number;
  status: string;
  processedOn?: string;
  payslips: Payslip[];
}
