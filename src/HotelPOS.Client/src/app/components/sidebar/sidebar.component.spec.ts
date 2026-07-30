import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SidebarComponent } from './sidebar.component';
import { environment } from '../../../environments/environment';

describe('SidebarComponent', () => {
  let component: SidebarComponent;
  let fixture: ComponentFixture<SidebarComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [SidebarComponent],
      imports: [RouterTestingModule],
      providers: [provideHttpClient(), provideHttpClientTesting()],
      schemas: [CUSTOM_ELEMENTS_SCHEMA]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(SidebarComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create component', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiBaseUrl}/auth/permissions`).flush({});
  });

  it('should toggle collapse show state', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiBaseUrl}/auth/permissions`).flush({});

    component.toggleCollapseShow('bg-white m-2 py-3 px-6');
    expect(component.collapseShow).toBe('bg-white m-2 py-3 px-6');
  });

  it('fetches and reflects the permission snapshot on init', () => {
    fixture.detectChanges();
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/permissions`);

    expect(component.has('Items')).toBeFalse();

    req.flush({ Items: true, Roles: false });

    expect(component.has('Items')).toBeTrue();
    expect(component.has('Roles')).toBeFalse();
    expect(component.hasAny(['Roles', 'Items'])).toBeTrue();
    expect(component.hasAny(['Roles', 'Audit'])).toBeFalse();
  });
});
