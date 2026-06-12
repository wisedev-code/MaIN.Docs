import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { SessionService } from '../services/session.service';

const PROTECTED_PREFIXES = ['/api/chat', '/api/confirm', '/api/ensemble', '/api/artifact'];

export const sessionInterceptor: HttpInterceptorFn = (req, next) => {
  // req.url may be relative ('/api/chat/complete') or absolute
  // (https://maindoc-backend.../api/chat/complete) when the SPA is hosted
  // separately from the backend, so match on the pathname only.
  const path = new URL(req.url, window.location.origin).pathname;
  if (!PROTECTED_PREFIXES.some(prefix => path.startsWith(prefix))) {
    return next(req);
  }

  const sessionService = inject(SessionService);

  return from(sessionService.getToken()).pipe(
    switchMap(token => {
      const authedReq = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
      return next(authedReq).pipe(
        catchError(err => {
          if (err instanceof HttpErrorResponse && err.status === 401) {
            sessionService.clearToken();
            return from(sessionService.getToken(true)).pipe(
              switchMap(freshToken => {
                const retried = req.clone({ setHeaders: { Authorization: `Bearer ${freshToken}` } });
                return next(retried);
              })
            );
          }
          return throwError(() => err);
        })
      );
    })
  );
};
