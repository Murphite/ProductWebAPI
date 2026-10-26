import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);

    auth.login('admin', 'secret').subscribe();
    controller
      .expectOne('/api/v1/auth/token')
      .flush({ accessToken: 'abc', tokenType: 'Bearer', expiresIn: 3600, scopes: [] });
  });

  afterEach(() => controller.verify());

  it('adds the bearer token to API requests', () => {
    http.get('/api/v1/products').subscribe();

    const request = controller.expectOne('/api/v1/products');
    expect(request.request.headers.get('Authorization')).toBe('Bearer abc');
    request.flush([]);
  });

  it('does not send the token to other origins', () => {
    http.get('https://example.com/api/v1/products').subscribe();

    const request = controller.expectOne('https://example.com/api/v1/products');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush([]);
  });

  it('signs the user out and redirects to login on 401', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    http.get('/api/v1/products').subscribe({ error: () => undefined });
    controller.expectOne('/api/v1/products').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login'], expect.anything());
  });
});
