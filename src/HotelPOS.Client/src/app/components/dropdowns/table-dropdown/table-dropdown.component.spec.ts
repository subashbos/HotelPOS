import { TestBed, ComponentFixture } from '@angular/core/testing';
import { TableDropdownComponent } from './table-dropdown.component';

describe('TableDropdownComponent', () => {
  let component: TableDropdownComponent;
  let fixture: ComponentFixture<TableDropdownComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [TableDropdownComponent]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(TableDropdownComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });
});
