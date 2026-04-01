import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { PLATFORM_ID } from '@angular/core';
import { jwtInterceptor } from './jwt.interceptor';
import { AuthService } from '../auth/auth.service';

describe('jwtInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let authService: AuthService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([jwtInterceptor])),
        provideHttpClientTesting(),
        provideRouter([{ path: 'auth/login', component: {} as any }]),
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
  });

  afterEach(() => {
    httpTesting.verify();
    localStorage.clear();
  });

  it('deve adicionar header Authorization quando token existe', () => {
    localStorage.setItem('accessToken', 'my-token');

    http.get('/api/v1/estados').subscribe();

    const req = httpTesting.expectOne('/api/v1/estados');
    expect(req.request.headers.get('Authorization')).toBe('Bearer my-token');
    req.flush([]);
  });

  it('deve nao adicionar header quando nao ha token', () => {
    http.get('/api/v1/estados').subscribe();

    const req = httpTesting.expectOne('/api/v1/estados');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });

  it('deve nao interceptar requests para /auth/', () => {
    localStorage.setItem('accessToken', 'my-token');

    http.post('/api/v1/auth/login', {}).subscribe();

    const req = httpTesting.expectOne('/api/v1/auth/login');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('deve tentar refresh ao receber 401', () => {
    localStorage.setItem('accessToken', 'expired-token');
    localStorage.setItem('refreshToken', 'valid-refresh');

    const header = btoa(JSON.stringify({ alg: 'HS256' }));
    const payload = btoa(JSON.stringify({ sub: '1', email: 'a@b.com', name: 'A', exp: Math.floor(Date.now() / 1000) + 3600 }));
    const newToken = `${header}.${payload}.sig`;

    http.get('/api/v1/estados').subscribe();

    const firstReq = httpTesting.expectOne('/api/v1/estados');
    firstReq.flush(null, { status: 401, statusText: 'Unauthorized' });

    // Refresh request is triggered
    const refreshReq = httpTesting.expectOne('/api/v1/auth/refresh');
    expect(refreshReq.request.body).toEqual({ refreshToken: 'valid-refresh' });
    refreshReq.flush({ accessToken: newToken, expiresIn: 3600 });

    // Retry request with new token
    const retryReq = httpTesting.expectOne('/api/v1/estados');
    expect(retryReq.request.headers.get('Authorization')).toBe(`Bearer ${newToken}`);
    retryReq.flush([]);
  });

  it('deve fazer logout quando refresh falha', () => {
    localStorage.setItem('accessToken', 'expired-token');
    localStorage.setItem('refreshToken', 'invalid-refresh');
    const logoutSpy = vi.spyOn(authService, 'logout');

    http.get('/api/v1/estados').subscribe({ error: () => {} });

    httpTesting.expectOne('/api/v1/estados').flush(null, { status: 401, statusText: 'Unauthorized' });
    httpTesting.expectOne('/api/v1/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(logoutSpy).toHaveBeenCalled();
  });

  it('deve propagar erros nao-401 sem tentar refresh', () => {
    localStorage.setItem('accessToken', 'token');
    let errorStatus = 0;

    http.get('/api/v1/estados').subscribe({
      error: (err) => { errorStatus = err.status; },
    });

    httpTesting.expectOne('/api/v1/estados').flush(null, { status: 500, statusText: 'Server Error' });
    expect(errorStatus).toBe(500);
  });
});
