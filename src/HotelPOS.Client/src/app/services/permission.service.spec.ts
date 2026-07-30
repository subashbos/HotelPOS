import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PermissionService } from './permission.service';
import { environment } from '../../environments/environment';

describe('PermissionService', () => {
  let service: PermissionService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(PermissionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('denies (false) for any module before a permission snapshot has loaded', () => {
    expect(service.hasPermission('Items')).toBeFalse();
    expect(service.isLoaded()).toBeFalse();
  });

  it('fetches the permission map from GET /api/auth/permissions and caches it', () => {
    let received: Record<string, boolean> | undefined;
    service.ensureLoaded().subscribe(p => (received = p));

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/permissions`);
    expect(req.request.method).toBe('GET');
    req.flush({ Items: true, Roles: false });

    expect(received).toEqual({ Items: true, Roles: false });
    expect(service.isLoaded()).toBeTrue();
    expect(service.hasPermission('Items')).toBeTrue();
    expect(service.hasPermission('Roles')).toBeFalse();
    expect(service.hasPermission('Unknown')).toBeFalse();
  });

  it('does not re-fetch on a second call once a snapshot is cached', () => {
    service.ensureLoaded().subscribe();
    httpMock.expectOne(`${environment.apiBaseUrl}/auth/permissions`).flush({ Items: true });

    service.ensureLoaded().subscribe();
    httpMock.expectNone(`${environment.apiBaseUrl}/auth/permissions`);
  });

  it('shares a single in-flight request when called concurrently before the response arrives', () => {
    service.ensureLoaded().subscribe();
    service.ensureLoaded().subscribe();

    httpMock.expectOne(`${environment.apiBaseUrl}/auth/permissions`).flush({ Items: true });
  });

  it('re-fetches after clear()', () => {
    service.ensureLoaded().subscribe();
    httpMock.expectOne(`${environment.apiBaseUrl}/auth/permissions`).flush({ Items: true });

    service.clear();
    expect(service.isLoaded()).toBeFalse();
    expect(service.hasPermission('Items')).toBeFalse();

    service.ensureLoaded().subscribe();
    httpMock.expectOne(`${environment.apiBaseUrl}/auth/permissions`).flush({ Items: false });
    expect(service.hasPermission('Items')).toBeFalse();
  });
});
