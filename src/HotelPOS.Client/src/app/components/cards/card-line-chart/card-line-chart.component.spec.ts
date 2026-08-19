import { TestBed, ComponentFixture } from '@angular/core/testing';
import { CardLineChartComponent } from './card-line-chart.component';

describe('CardLineChartComponent', () => {
  let component: CardLineChartComponent;
  let fixture: ComponentFixture<CardLineChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [CardLineChartComponent]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CardLineChartComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should execute ngAfterViewInit when canvas element is absent', () => {
    spyOn(document, 'getElementById').and.returnValue(null);
    expect(() => component.ngAfterViewInit()).not.toThrow();
  });

  it('should execute ngAfterViewInit when canvas element is present', () => {
    const canvasEl = document.createElement('canvas');
    canvasEl.id = 'line-chart';
    spyOn(canvasEl, 'getContext').and.returnValue({} as any);
    spyOn(document, 'getElementById').and.returnValue(canvasEl);

    expect(() => component.ngAfterViewInit()).not.toThrow();
  });
});
