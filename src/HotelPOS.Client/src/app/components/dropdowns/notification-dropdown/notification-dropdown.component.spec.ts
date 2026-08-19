import { TestBed, ComponentFixture } from '@angular/core/testing';
import { NotificationDropdownComponent } from './notification-dropdown.component';

describe('NotificationDropdownComponent', () => {
  let component: NotificationDropdownComponent;
  let fixture: ComponentFixture<NotificationDropdownComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [NotificationDropdownComponent]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(NotificationDropdownComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });
});
