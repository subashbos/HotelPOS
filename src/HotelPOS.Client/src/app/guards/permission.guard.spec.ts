import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot } from '@angular/router';
import { of } from 'rxjs';
import { permissionGuard } from './permission.guard';
import { PermissionService } from '../services/permission.service';

describe('permissionGuard', () => {
  let permissionServiceSpy: jasmine.SpyObj<PermissionService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    permissionServiceSpy = jasmine.createSpyObj('PermissionService', ['ensureLoaded', 'hasPermission']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        { provide: PermissionService, useValue: permissionServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });
  });

  function routeWithModules(modules?: string[]): ActivatedRouteSnapshot {
    return { data: { modules } } as unknown as ActivatedRouteSnapshot;
  }

  it('allows navigation without checking permissions when the route declares no modules', (done) => {
    const result = TestBed.runInInjectionContext(() =>
      permissionGuard(routeWithModules(undefined), {} as any));

    expect(result).toBeTrue();
    expect(permissionServiceSpy.ensureLoaded).not.toHaveBeenCalled();
    done();
  });

  it('allows navigation when the route declares an empty modules array', () => {
    const result = TestBed.runInInjectionContext(() =>
      permissionGuard(routeWithModules([]), {} as any));

    expect(result).toBeTrue();
    expect(permissionServiceSpy.ensureLoaded).not.toHaveBeenCalled();
  });

  it('allows navigation when the user holds the required module', (done) => {
    permissionServiceSpy.ensureLoaded.and.returnValue(of({ Items: true }));
    permissionServiceSpy.hasPermission.and.callFake((m: string) => m === 'Items');

    const result$ = TestBed.runInInjectionContext(() =>
      permissionGuard(routeWithModules(['Items']), {} as any)) as any;

    result$.subscribe((allowed: boolean) => {
      expect(allowed).toBeTrue();
      expect(routerSpy.navigate).not.toHaveBeenCalled();
      done();
    });
  });

  it('allows navigation when the user holds ANY of several required modules', (done) => {
    permissionServiceSpy.ensureLoaded.and.returnValue(of({ Purchase: false, SalesReport: true }));
    permissionServiceSpy.hasPermission.and.callFake((m: string) => m === 'SalesReport');

    const result$ = TestBed.runInInjectionContext(() =>
      permissionGuard(routeWithModules(['Purchase', 'SalesReport']), {} as any)) as any;

    result$.subscribe((allowed: boolean) => {
      expect(allowed).toBeTrue();
      done();
    });
  });

  it('blocks navigation and redirects to /admin/forbidden when the user lacks every required module', (done) => {
    permissionServiceSpy.ensureLoaded.and.returnValue(of({ Roles: false }));
    permissionServiceSpy.hasPermission.and.returnValue(false);

    const result$ = TestBed.runInInjectionContext(() =>
      permissionGuard(routeWithModules(['Roles']), {} as any)) as any;

    result$.subscribe((allowed: boolean) => {
      expect(allowed).toBeFalse();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/admin/forbidden']);
      done();
    });
  });
});
