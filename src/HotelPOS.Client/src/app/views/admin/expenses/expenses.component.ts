import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { ExpenseService } from '../../../services/expense.service';
import { Expense, EXPENSE_CATEGORIES } from '../../../models/expense.model';

interface ExpenseFormModel {
  date: string;
  title: string;
  description: string;
  amount: number;
  category: string;
  paymentMode: string;
}

function emptyForm(): ExpenseFormModel {
  return {
    date: new Date().toISOString().substring(0, 10),
    title: '',
    description: '',
    amount: 0,
    category: EXPENSE_CATEGORIES[0],
    paymentMode: 'Cash'
  };
}

@Component({
  standalone: false,
  selector: 'app-expenses',
  templateUrl: './expenses.component.html',
})
export class ExpensesComponent implements OnInit {
  expenses: Expense[] = [];
  isLoading = false;
  loadError = '';
  actionError = '';

  readonly categories = EXPENSE_CATEGORIES;

  showForm = false;
  editingId: number | null = null;
  form: ExpenseFormModel = emptyForm();
  isSaving = false;

  constructor(private readonly expenseService: ExpenseService) {}

  ngOnInit(): void {
    this.loadExpenses();
  }

  loadExpenses(): void {
    this.isLoading = true;
    this.loadError = '';
    this.expenseService.getExpenses().subscribe({
      next: (expenses) => {
        this.expenses = expenses.sort((a, b) => (a.date < b.date ? 1 : -1));
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load expenses. Please check the server connection.';
        this.isLoading = false;
        console.error('Expenses load error:', err);
      }
    });
  }

  get totalAmount(): number {
    return this.expenses.reduce((s, e) => s + e.amount, 0);
  }

  openAddForm(): void {
    this.editingId = null;
    this.form = emptyForm();
    this.actionError = '';
    this.showForm = true;
  }

  openEditForm(expense: Expense): void {
    this.editingId = expense.id;
    this.form = {
      date: expense.date.substring(0, 10),
      title: expense.title,
      description: expense.description ?? '',
      amount: expense.amount,
      category: expense.category,
      paymentMode: expense.paymentMode ?? 'Cash'
    };
    this.actionError = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.editingId = null;
  }

  save(): void {
    if (!this.form.title.trim() || this.form.amount <= 0 || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';

    const payload = {
      id: this.editingId ?? 0,
      date: this.form.date,
      title: this.form.title.trim(),
      description: this.form.description || undefined,
      amount: this.form.amount,
      category: this.form.category,
      paymentMode: this.form.paymentMode
    };

    const request$: Observable<unknown> = this.editingId
      ? this.expenseService.updateExpense(this.editingId, payload)
      : this.expenseService.createExpense(payload);

    request$.subscribe({
      next: () => {
        this.isSaving = false;
        this.closeForm();
        this.loadExpenses();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to save expense.';
        console.error('Expense save error:', err);
      }
    });
  }

  deleteExpense(expense: Expense): void {
    if (!confirm(`Delete expense "${expense.title}"?`)) return;
    this.expenseService.deleteExpense(expense.id).subscribe({
      next: () => this.loadExpenses(),
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to delete expense.';
        console.error('Expense delete error:', err);
      }
    });
  }

  trackByExpenseId(_index: number, expense: Expense): number {
    return expense.id;
  }
}
