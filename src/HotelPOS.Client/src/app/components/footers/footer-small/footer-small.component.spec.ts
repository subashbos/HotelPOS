import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FooterSmallComponent } from './footer-small.component';

describe('FooterSmallComponent', () => {
  let component: FooterSmallComponent;
  let fixture: ComponentFixture<FooterSmallComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [FooterSmallComponent]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(FooterSmallComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });
});
