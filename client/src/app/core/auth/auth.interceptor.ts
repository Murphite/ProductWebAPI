import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/** Attaches the bearer token to API calls and returns the user to sign-in when it is rejected. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const isApiRequest = request.url.startsWith('/api/');
  const isTokenRequest = request.url.startsWith('/api/v1/auth/');
  const token = isApiRequest && !isTokenRequest ? auth.accessToken() : null;

  const authorised = token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;

  return next(authorised).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !isTokenRequest) {
        auth.logout();
        void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      }

      return throwError(() => error);
    }),
  );
};
