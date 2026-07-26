export interface TdsSlab {
  incomeFrom: number;
  incomeTo?: number | null;
  ratePercent: number;
  displayOrder: number;
}

export interface TdsRuleSet {
  financialYearStart: number;
  standardDeduction: number;
  rebateIncomeLimit: number;
  cessRatePercent: number;
  slabs: TdsSlab[];
}

export interface SaveTdsRuleSetRequest {
  standardDeduction: number;
  rebateIncomeLimit: number;
  cessRatePercent: number;
  slabs: TdsSlab[];
}
