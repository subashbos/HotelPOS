import { TestBed, ComponentFixture } from '@angular/core/testing';
import { CardStatsComponent } from './card-stats.component';

describe('CardStatsComponent', () => {
  let component: CardStatsComponent;
  let fixture: ComponentFixture<CardStatsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [CardStatsComponent]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CardStatsComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });
});
