import { Component, OnInit } from '@angular/core';
import { EssService } from '../../../services/ess.service';
import { Payslip } from '../../../models/payroll.model';

@Component({
  standalone: false,
  selector: 'app-ess-payslips',
  templateUrl: './ess-payslips.component.html',
})
export class EssPayslipsComponent implements OnInit {
  payslips: Payslip[] = [];
  isLoading = false;
  loadError = '';

  constructor(private readonly essService: EssService) {}

  ngOnInit(): void {
    this.isLoading = true;
    this.essService.getPayslips().subscribe({
      next: (payslips) => {
        this.payslips = payslips;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load your payslips. Please check the server connection.';
        this.isLoading = false;
        console.error('Payslips load error:', err);
      }
    });
  }

  trackByPayslipId(_index: number, payslip: Payslip): number {
    return payslip.id;
  }
}
