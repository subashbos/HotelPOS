import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { ExpensesComponent } from './expenses.component';
import { ExpenseService } from '../../../services/expense.service';
import { Expense } from '../../../models/expense.model';

describe('ExpensesComponent', () => {
  let component: ExpensesComponent;
  let fixture: ComponentFixture<ExpensesComponent>;
  let expenseServiceSpy: jasmine.SpyObj<ExpenseService>;

  const mockExpense: Expense = {
    sNo: 1,
    id: 1,
    date: '2026-01-15',
    title: 'Electricity',
    amount: 1500,
    category: 'Utilities',
    paymentMode: 'Cash'
  };

  beforeEach(async () => {
    expenseServiceSpy = jasmine.createSpyObj('ExpenseService', ['getExpenses', 'createExpense', 'updateExpense', 'deleteExpense']);
    expenseServiceSpy.getExpenses.and.returnValue(of([mockExpense]));

    await TestBed.configureTestingModule({
      declarations: [ExpensesComponent],
      imports: [FormsModule],
      providers: [
        { provide: ExpenseService, useValue: expenseServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ExpensesComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load expenses sorted', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(expenseServiceSpy.getExpenses).toHaveBeenCalled();
    expect(component.expenses).toHaveSize(1);
    expect(component.totalAmount).toBe(1500);
  });

  it('should handle expenses load error', () => {
    spyOn(console, 'error');
    expenseServiceSpy.getExpenses.and.returnValue(throwError(() => new Error('Load failed')));
    fixture.detectChanges();
    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load expenses. Please check the server connection.');
  });

  it('should open add form and close form', () => {
    component.openAddForm();
    expect(component.showForm).toBeTrue();
    expect(component.editingId).toBeNull();

    component.closeForm();
    expect(component.showForm).toBeFalse();
  });

  it('should open edit form with expense data', () => {
    component.openEditForm(mockExpense);
    expect(component.showForm).toBeTrue();
    expect(component.editingId).toBe(1);
    expect(component.form.title).toBe('Electricity');
  });

  it('should save new expense', () => {
    expenseServiceSpy.createExpense.and.returnValue(of(mockExpense));
    component.openAddForm();
    component.form.title = 'Internet Bill';
    component.form.amount = 999;

    component.save();

    expect(expenseServiceSpy.createExpense).toHaveBeenCalled();
    expect(component.showForm).toBeFalse();
  });

  it('should update existing expense', () => {
    expenseServiceSpy.updateExpense.and.returnValue(of(void 0));
    component.openEditForm(mockExpense);
    component.form.amount = 1600;

    component.save();

    expect(expenseServiceSpy.updateExpense).toHaveBeenCalled();
    expect(component.showForm).toBeFalse();
  });

  it('should delete expense when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    expenseServiceSpy.deleteExpense.and.returnValue(of(void 0));

    component.deleteExpense(mockExpense);

    expect(expenseServiceSpy.deleteExpense).toHaveBeenCalledWith(1);
  });

  it('should track expense by id', () => {
    expect(component.trackByExpenseId(0, mockExpense)).toBe(1);
  });
});
