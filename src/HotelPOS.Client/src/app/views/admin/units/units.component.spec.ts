import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { UnitsComponent } from './units.component';
import { UnitOfMeasurementService } from '../../../services/unit-of-measurement.service';
import { UnitOfMeasurement } from '../../../models/item.model';

describe('UnitsComponent', () => {
  let component: UnitsComponent;
  let fixture: ComponentFixture<UnitsComponent>;
  let unitServiceSpy: jasmine.SpyObj<UnitOfMeasurementService>;

  const mockUnits: UnitOfMeasurement[] = [
    { id: 1, name: 'kg', displayOrder: 1 },
    { id: 2, name: 'litre', displayOrder: 2 }
  ];

  beforeEach(async () => {
    unitServiceSpy = jasmine.createSpyObj('UnitOfMeasurementService', [
      'getUnits',
      'createUnit',
      'updateUnit',
      'deleteUnit'
    ]);

    await TestBed.configureTestingModule({
      declarations: [UnitsComponent],
      imports: [FormsModule],
      providers: [
        { provide: UnitOfMeasurementService, useValue: unitServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    unitServiceSpy.getUnits.and.returnValue(of(mockUnits));
    fixture = TestBed.createComponent(UnitsComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should load units on init sorted by displayOrder', () => {
    fixture.detectChanges();

    expect(unitServiceSpy.getUnits).toHaveBeenCalled();
    expect(component.units).toHaveSize(2);
    expect(component.units[0].name).toBe('kg');
    expect(component.isLoading).toBeFalse();
  });

  it('should handle load error', () => {
    unitServiceSpy.getUnits.and.returnValue(throwError(() => new Error('Load failed')));

    fixture.detectChanges();

    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load units. Please check the server connection.');
  });

  it('should add unit successfully', () => {
    unitServiceSpy.createUnit.and.returnValue(of({ id: 3, name: 'Piece', displayOrder: 3 }));
    component.newName = 'Piece';
    component.newDisplayOrder = 3;

    component.addUnit();

    expect(unitServiceSpy.createUnit).toHaveBeenCalledWith({ name: 'Piece', displayOrder: 3 });
    expect(component.newName).toBe('');
  });

  it('should not add unit if name is empty', () => {
    component.newName = '   ';

    component.addUnit();

    expect(unitServiceSpy.createUnit).not.toHaveBeenCalled();
  });

  it('should handle create unit error', () => {
    unitServiceSpy.createUnit.and.returnValue(throwError(() => ({ error: { message: 'Duplicate unit' } })));
    component.newName = 'kg';

    component.addUnit();

    expect(component.saveError).toBe('Duplicate unit');
  });

  it('should start and cancel edit mode', () => {
    component.startEdit(mockUnits[0]);
    expect(component.editingId).toBe(1);
    expect(component.editName).toBe('kg');

    component.cancelEdit();
    expect(component.editingId).toBeNull();
  });

  it('should save edit successfully', () => {
    unitServiceSpy.updateUnit.and.returnValue(of(void 0));
    component.startEdit(mockUnits[0]);
    component.editName = 'Kilogram';

    component.saveEdit(mockUnits[0]);

    expect(unitServiceSpy.updateUnit).toHaveBeenCalledWith(1, { name: 'Kilogram', displayOrder: 1 });
    expect(component.editingId).toBeNull();
  });

  it('should delete unit when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    unitServiceSpy.deleteUnit.and.returnValue(of(void 0));

    component.deleteUnit(mockUnits[0]);

    expect(unitServiceSpy.deleteUnit).toHaveBeenCalledWith(1);
  });

  it('should not delete unit when cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);

    component.deleteUnit(mockUnits[0]);

    expect(unitServiceSpy.deleteUnit).not.toHaveBeenCalled();
  });
});
