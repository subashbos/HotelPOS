import { Component, OnInit } from '@angular/core';
import { TdsService } from '../../../services/tds.service';
import { TdsSlab } from '../../../models/tds.model';

@Component({
  standalone: false,
  selector: 'app-tds',
  templateUrl: './tds.component.html',
})
export class TdsComponent implements OnInit {
  financialYears: number[] = [];
  selectedYear: number | null = null;

  standardDeduction = 0;
  rebateIncomeLimit = 0;
  cessRatePercent = 0;
  slabs: TdsSlab[] = [];

  isLoading = false;
  loadError = '';
  actionError = '';
  isSaving = false;

  showNewYearForm = false;
  newYearValue: number | null = null;

  constructor(private readonly tdsService: TdsService) {}

  ngOnInit(): void {
    this.loadYears();
  }

  loadYears(selectYear?: number): void {
    this.isLoading = true;
    this.loadError = '';
    this.tdsService.getFinancialYears().subscribe({
      next: (years) => {
        this.financialYears = years;
        this.isLoading = false;
        const target = selectYear ?? years[0] ?? null;
        if (target !== null) {
          this.selectYear(target);
        } else {
          this.selectedYear = null;
        }
      },
      error: (err) => {
        this.loadError = 'Failed to load TDS financial years. Please check the server connection.';
        this.isLoading = false;
        console.error('TDS years load error:', err);
      }
    });
  }

  selectYear(year: number): void {
    this.selectedYear = year;
    this.actionError = '';
    this.tdsService.getRuleSet(year).subscribe({
      next: (ruleSet) => {
        this.standardDeduction = ruleSet.standardDeduction;
        this.rebateIncomeLimit = ruleSet.rebateIncomeLimit;
        this.cessRatePercent = ruleSet.cessRatePercent;
        this.slabs = ruleSet.slabs.map((s) => ({ ...s }));
      },
      error: (err) => {
        this.actionError = 'Failed to load the selected financial year.';
        console.error('TDS rule set load error:', err);
      }
    });
  }

  onYearChange(value: string): void {
    const year = Number.parseInt(value, 10);
    if (!Number.isNaN(year)) this.selectYear(year);
  }

  openNewYearForm(): void {
    this.newYearValue = new Date().getFullYear();
    this.showNewYearForm = true;
    this.actionError = '';
  }

  closeNewYearForm(): void {
    this.showNewYearForm = false;
  }

  createNewYear(): void {
    if (!this.newYearValue) return;
    this.selectedYear = this.newYearValue;
    this.standardDeduction = 0;
    this.rebateIncomeLimit = 0;
    this.cessRatePercent = 0;
    this.slabs = [{ incomeFrom: 0, incomeTo: null, ratePercent: 0, displayOrder: 1 }];
    this.showNewYearForm = false;
  }

  addSlabRow(): void {
    const last = this.slabs.at(-1);
    this.slabs.push({
      incomeFrom: last?.incomeTo ?? 0,
      incomeTo: null,
      ratePercent: 0,
      displayOrder: this.slabs.length + 1
    });
  }

  removeSlabRow(index: number): void {
    this.slabs.splice(index, 1);
    this.slabs.forEach((s, i) => (s.displayOrder = i + 1));
  }

  save(): void {
    if (!this.selectedYear || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';

    this.tdsService.saveRuleSet(this.selectedYear, {
      standardDeduction: this.standardDeduction,
      rebateIncomeLimit: this.rebateIncomeLimit,
      cessRatePercent: this.cessRatePercent,
      slabs: this.slabs
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.loadYears(this.selectedYear!);
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to save TDS rule set.';
        console.error('TDS save error:', err);
      }
    });
  }

  deleteYear(): void {
    if (!this.selectedYear) return;
    if (!confirm(`Delete the TDS configuration for FY${this.selectedYear}-${(this.selectedYear + 1) % 100}?`)) return;

    this.tdsService.deleteRuleSet(this.selectedYear).subscribe({
      next: () => this.loadYears(),
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to delete TDS rule set.';
        console.error('TDS delete error:', err);
      }
    });
  }

  trackBySlabIndex(index: number): number {
    return index;
  }
}
