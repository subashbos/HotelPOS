import { TestBed, ComponentFixture } from '@angular/core/testing';
import { CardBarChartComponent } from './card-bar-chart.component';

describe('CardBarChartComponent', () => {
  let component: CardBarChartComponent;
  let fixture: ComponentFixture<CardBarChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [CardBarChartComponent]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CardBarChartComponent);
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
    canvasEl.id = 'bar-chart';
    spyOn(canvasEl, 'getContext').and.returnValue({} as any);
    spyOn(document, 'getElementById').and.returnValue(canvasEl);

    expect(() => component.ngAfterViewInit()).not.toThrow();
  });
});
