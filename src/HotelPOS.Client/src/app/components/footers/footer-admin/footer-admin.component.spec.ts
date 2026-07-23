import { TestBed, ComponentFixture } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { FooterAdminComponent } from './footer-admin.component';

describe('FooterAdminComponent', () => {
  let component: FooterAdminComponent;
  let fixture: ComponentFixture<FooterAdminComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [FooterAdminComponent],
      imports: [RouterTestingModule]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(FooterAdminComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });
});
