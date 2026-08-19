import { TestBed, ComponentFixture } from '@angular/core/testing';
import { HeaderStatsComponent } from './header-stats.component';

describe('HeaderStatsComponent', () => {
  let component: HeaderStatsComponent;
  let fixture: ComponentFixture<HeaderStatsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [HeaderStatsComponent]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(HeaderStatsComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });
});
