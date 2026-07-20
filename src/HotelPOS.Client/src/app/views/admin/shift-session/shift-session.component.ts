import { Component, OnInit } from '@angular/core';
import { CashSessionService } from '../../../services/cash-session.service';
import { CashSession } from '../../../models/cash-session.model';

@Component({
  standalone: false,
  selector: 'app-shift-session',
  templateUrl: './shift-session.component.html',
})
export class ShiftSessionComponent implements OnInit {
  currentSession: CashSession | null = null;
  history: CashSession[] = [];
  currentSalesTotal = 0;

  isLoading = false;
  loadError = '';
  actionError = '';
  isSaving = false;

  openingBalance = 0;
  actualCash = 0;
  closeNotes = '';

  constructor(private readonly cashSessionService: CashSessionService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = '';
    this.cashSessionService.getCurrentSession().subscribe({
      next: (session) => {
        this.currentSession = session;
        this.isLoading = false;
        if (session) {
          this.cashSessionService.getCurrentSessionSalesTotal().subscribe({
            next: (total) => (this.currentSalesTotal = total),
            error: (err) => console.error('Sales total load error:', err)
          });
        }
      },
      error: (err) => {
        this.loadError = 'Failed to load the current session. Please check the server connection.';
        this.isLoading = false;
        console.error('Current session load error:', err);
      }
    });
    this.cashSessionService.getHistory(30).subscribe({
      next: (history) => (this.history = history),
      error: (err) => console.error('Session history load error:', err)
    });
  }

  openSession(): void {
    if (this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';
    this.cashSessionService.openSession(this.openingBalance).subscribe({
      next: () => {
        this.isSaving = false;
        this.openingBalance = 0;
        this.load();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to open session.';
        console.error('Open session error:', err);
      }
    });
  }

  closeSession(): void {
    if (this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';
    this.cashSessionService.closeSession(this.actualCash, this.closeNotes || undefined).subscribe({
      next: () => {
        this.isSaving = false;
        this.actualCash = 0;
        this.closeNotes = '';
        this.load();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to close session.';
        console.error('Close session error:', err);
      }
    });
  }

  get expectedClosingBalance(): number {
    if (!this.currentSession) return 0;
    return this.currentSession.openingBalance + this.currentSalesTotal;
  }

  trackBySessionId(_index: number, session: CashSession): number {
    return session.id;
  }
}
