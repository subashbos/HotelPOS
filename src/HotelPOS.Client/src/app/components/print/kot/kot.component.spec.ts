import { TestBed, ComponentFixture } from '@angular/core/testing';
import { KotComponent } from './kot.component';
import { KotData } from '../../../models/print.model';

describe('KotComponent', () => {
  let component: KotComponent;
  let fixture: ComponentFixture<KotComponent>;

  const kotData: KotData = {
    tableNumber: 5,
    items: [{ itemName: 'Butter Chicken', quantity: 2 }]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [KotComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(KotComponent);
    component = fixture.componentInstance;
    component.data = kotData;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should use a shorter separator for thermal printers', () => {
    component.isThermal = true;
    expect(component.separator.length).toBe(30);

    component.isThermal = false;
    expect(component.separator.length).toBe(60);
  });

  it('should display the table number when set', () => {
    component.data = { ...kotData, tableNumber: 5 };
    expect(component.tableDisplay).toBe('5');
  });

  it('should display Takeaway / Online when the table number is 0', () => {
    component.data = { ...kotData, tableNumber: 0 };
    expect(component.tableDisplay).toBe('Takeaway / Online');
  });

  it('should expose the current date/time', () => {
    expect(component.now instanceof Date).toBeTrue();
  });
});
