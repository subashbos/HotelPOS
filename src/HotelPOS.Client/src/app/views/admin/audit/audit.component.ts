import { Component, OnInit } from '@angular/core';
import { AuditService } from '../../../services/audit.service';
import { AuditLog } from '../../../models/audit.model';

function firstOfMonth(): string {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().substring(0, 10);
}
function today(): string {
  return new Date().toISOString().substring(0, 10);
}

@Component({
  standalone: false,
  selector: 'app-audit',
  templateUrl: './audit.component.html',
})
export class AuditComponent implements OnInit {
  logs: AuditLog[] = [];
  isLoading = false;
  loadError = '';

  fromDate = firstOfMonth();
  toDate = today();

  constructor(private readonly auditService: AuditService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = '';
    this.auditService.getLogs(this.fromDate, this.toDate).subscribe({
      next: (logs) => {
        this.logs = logs.sort((a, b) => (a.timestamp < b.timestamp ? 1 : -1));
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load the audit log. Please check the server connection.';
        this.isLoading = false;
        console.error('Audit log load error:', err);
      }
    });
  }

  trackByLogId(_index: number, log: AuditLog): number {
    return log.id;
  }
}
