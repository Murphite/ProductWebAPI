import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService, TokenResponse } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  const token = (scopes: string[], expiresIn = 3600): TokenResponse => ({
    accessToken: 'jwt-token',
    tokenType: 'Bearer',
    expiresIn,
    scopes,
  });

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('starts signed out', () => {
    expect(service.isAuthenticated()).toBe(false);
    expect(service.accessToken()).toBeNull();
  });

  it('stores the session after a successful login', () => {
    service.login('admin', 'secret').subscribe();

    const request = http.expectOne('/api/v1/auth/token');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ username: 'admin', password: 'secret' });
    request.flush(token(['products.read', 'products.write']));

    expect(service.isAuthenticated()).toBe(true);
    expect(service.username()).toBe('admin');
    expect(service.canWrite()).toBe(true);
    expect(service.accessToken()).toBe('jwt-token');
    expect(sessionStorage.getItem('products.session')).not.toBeNull();
  });

  it('does not grant write access without the write scope', () => {
    service.login('viewer', 'secret').subscribe();
    http.expectOne('/api/v1/auth/token').flush(token(['products.read']));

    expect(service.canWrite()).toBe(false);
  });

  it('treats an expired token as signed out', () => {
    service.login('admin', 'secret').subscribe();
    http.expectOne('/api/v1/auth/token').flush(token(['products.read'], -1));

    expect(service.accessToken()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  });

  it('clears the session on logout', () => {
    service.login('admin', 'secret').subscribe();
    http.expectOne('/api/v1/auth/token').flush(token(['products.read']));

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem('products.session')).toBeNull();
  });
});
