import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const jwtInterceptor: HttpInterceptorFn = (request: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const authService = inject(AuthService);

  if (request.url.includes('/auth/')) {
    return next(request);
  }

  const accessToken = authService.getAccessToken();
  const authenticatedRequest = accessToken
    ? request.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } })
    : request;

  return next(authenticatedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !authService.isRefreshInProgress) {
        authService.isRefreshInProgress = true;
        return authService.refresh().pipe(
          switchMap(refreshResponse => {
            authService.isRefreshInProgress = false;
            const retryRequest = request.clone({
              setHeaders: { Authorization: `Bearer ${refreshResponse.accessToken}` },
            });
            return next(retryRequest);
          }),
          catchError(refreshError => {
            authService.isRefreshInProgress = false;
            authService.logout();
            return throwError(() => refreshError);
          }),
        );
      }
      return throwError(() => error);
    }),
  );
};
