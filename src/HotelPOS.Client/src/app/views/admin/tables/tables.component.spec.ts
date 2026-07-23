import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { TablesComponent } from './tables.component';
import { TableService } from '../../../services/table.service';
import { DiningTable } from '../../../models/table.model';

describe('TablesComponent', () => {
  let component: TablesComponent;
  let fixture: ComponentFixture<TablesComponent>;
  let tableServiceSpy: jasmine.SpyObj<TableService>;

  const mockTables: DiningTable[] = [
    { id: 1, number: 1, name: 'Table 1', capacity: 4, isActive: true, isDeleted: false }
  ];

  beforeEach(async () => {
    tableServiceSpy = jasmine.createSpyObj('TableService', [
      'getTables',
      'createTable',
      'updateTable',
      'deleteTable'
    ]);

    await TestBed.configureTestingModule({
      declarations: [TablesComponent],
      imports: [FormsModule],
      providers: [
        { provide: TableService, useValue: tableServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    tableServiceSpy.getTables.and.returnValue(of(mockTables));
    fixture = TestBed.createComponent(TablesComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should load tables on init', () => {
    fixture.detectChanges();

    expect(tableServiceSpy.getTables).toHaveBeenCalled();
    expect(component.tables).toHaveSize(1);
    expect(component.isLoading).toBeFalse();
  });

  it('should handle load error', () => {
    tableServiceSpy.getTables.and.returnValue(throwError(() => new Error('Error')));

    fixture.detectChanges();

    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load tables. Please check the server connection.');
  });

  it('should open and close form modal', () => {
    component.openAddForm();
    expect(component.showForm).toBeTrue();
    expect(component.editingId).toBeNull();

    component.closeForm();
    expect(component.showForm).toBeFalse();
  });

  it('should open edit form with table data', () => {
    component.openEditForm(mockTables[0]);
    expect(component.showForm).toBeTrue();
    expect(component.editingId).toBe(1);
    expect(component.form.number).toBe(1);
  });

  it('should save new table', () => {
    tableServiceSpy.createTable.and.returnValue(of(mockTables[0]));
    component.openAddForm();
    component.form.number = 2;
    component.form.name = 'Table 2';
    component.form.capacity = 2;

    component.save();

    expect(tableServiceSpy.createTable).toHaveBeenCalledWith({ number: 2, name: 'Table 2', capacity: 2, isActive: true });
    expect(component.showForm).toBeFalse();
  });

  it('should handle save table error', () => {
    tableServiceSpy.createTable.and.returnValue(throwError(() => ({ error: { message: 'Save error' } })));
    component.openAddForm();
    component.form.name = 'Table 3';
    component.form.number = 3;

    component.save();

    expect(component.actionError).toBe('Save error');
    expect(component.isSaving).toBeFalse();
  });

  it('should delete table when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    tableServiceSpy.deleteTable.and.returnValue(of(void 0));

    component.deleteTable(mockTables[0]);

    expect(tableServiceSpy.deleteTable).toHaveBeenCalledWith(1);
  });
});
