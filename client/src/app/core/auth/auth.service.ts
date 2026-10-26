import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';

export interface TokenResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  scopes: string[];
}

interface Session {
  username: string;
  accessToken: string;
  expiresAt: number;
  scopes: string[];
}

export const PRODUCTS_WRITE_SCOPE = 'products.write';
const STORAGE_KEY = 'products.session';

/**
 * Holds the current user's access token.
 *
 * The token lives in sessionStorage so it survives a page refresh but not a closed tab.
 * In a production deployment the SPA would use OIDC (authorization code + PKCE) against
 * the organisation's identity provider rather than posting credentials to the API.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = signal<Session | null>(this.restore());

  readonly username = computed(() => this.session()?.username ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly canWrite = computed(() => this.session()?.scopes.includes(PRODUCTS_WRITE_SCOPE) ?? false);

  login(username: string, password: string): Observable<void> {
    return this.http.post<TokenResponse>('/api/v1/auth/token', { username, password }).pipe(
      tap((response) =>
        this.store({
          username,
          accessToken: response.accessToken,
          expiresAt: Date.now() + response.expiresIn * 1000,
          scopes: response.scopes,
        }),
      ),
      map(() => undefined),
    );
  }

  logout(): void {
    sessionStorage.removeItem(STORAGE_KEY);
    this.session.set(null);
  }

  /** Returns a non-expired access token, or null. */
  accessToken(): string | null {
    const current = this.session();
    if (!current) {
      return null;
    }

    if (current.expiresAt <= Date.now()) {
      this.logout();
      return null;
    }

    return current.accessToken;
  }

  private store(session: Session): void {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    this.session.set(session);
  }

  private restore(): Session | null {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return null;
      }

      const session = JSON.parse(raw) as Session;
      return session.expiresAt > Date.now() ? session : null;
    } catch {
      return null;
    }
  }
}
