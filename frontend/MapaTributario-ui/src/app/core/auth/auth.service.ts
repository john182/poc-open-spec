import { HttpClient } from '@angular/common/http';
import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap, throwError } from 'rxjs';
import { isPlatformBrowser } from '@angular/common';

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

interface RefreshResponse {
  accessToken: string;
  expiresIn: number;
}

interface TokenPayload {
  sub: string;
  email: string;
  name: string;
  exp: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _http = inject(HttpClient);
  private readonly _router = inject(Router);
  private readonly _platformId = inject(PLATFORM_ID);
  private readonly _baseUrl = '/api/v1/auth';

  private readonly _isAuthenticated = signal(this._hasValidToken());
  private readonly _userName = signal(this._loadUserName());
  private _refreshInProgress = false;

  readonly isAuthenticated = this._isAuthenticated.asReadonly();
  readonly userName = this._userName.asReadonly();

  login(email: string, senha: string, lembrar = false): Observable<AuthResponse> {
    return this._http.post<AuthResponse>(`${this._baseUrl}/login`, { email, senha }).pipe(
      tap(authResponse => {
        this._setRemember(lembrar);
        this._handleAuthSuccess(authResponse);
      }),
    );
  }

  register(email: string, nome: string, senha: string): Observable<AuthResponse> {
    return this._http.post<AuthResponse>(`${this._baseUrl}/register`, { email, nome, senha }).pipe(
      tap(authResponse => {
        this._setRemember(true);
        this._handleAuthSuccess(authResponse);
      }),
    );
  }

  refresh(): Observable<RefreshResponse> {
    const refreshToken = this._getItem('refreshToken');
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token'));
    }
    return this._http.post<RefreshResponse>(`${this._baseUrl}/refresh`, { refreshToken }).pipe(
      tap(refreshResponse => {
        this._setItem('accessToken', refreshResponse.accessToken);
        this._updateUserFromToken(refreshResponse.accessToken);
        this._isAuthenticated.set(true);
      }),
    );
  }

  logout(): void {
    this._removeItem('accessToken');
    this._removeItem('refreshToken');
    if (isPlatformBrowser(this._platformId)) {
      localStorage.removeItem('rememberMe');
    }
    this._isAuthenticated.set(false);
    this._userName.set('');
    this._router.navigate(['/auth/login']);
  }

  getAccessToken(): string | null {
    return this._getItem('accessToken');
  }

  getRefreshToken(): string | null {
    return this._getItem('refreshToken');
  }

  get isRefreshInProgress(): boolean {
    return this._refreshInProgress;
  }

  set isRefreshInProgress(value: boolean) {
    this._refreshInProgress = value;
  }

  private _handleAuthSuccess(authResponse: AuthResponse): void {
    this._setItem('accessToken', authResponse.accessToken);
    this._setItem('refreshToken', authResponse.refreshToken);
    this._updateUserFromToken(authResponse.accessToken);
    this._isAuthenticated.set(true);
  }

  private _updateUserFromToken(token: string): void {
    const payload = this._decodeToken(token);
    if (payload) {
      this._userName.set(payload.name || payload.email);
    }
  }

  private _decodeToken(token: string): TokenPayload | null {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const payload = JSON.parse(atob(parts[1]));
      return {
        sub: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? payload.sub,
        email: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ?? payload.email,
        name: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ?? payload.name,
        exp: payload.exp,
      };
    } catch {
      return null;
    }
  }

  private _hasValidToken(): boolean {
    if (!isPlatformBrowser(this._platformId)) return false;
    const token = this._getItem('accessToken');
    if (!token) return false;
    const payload = this._decodeToken(token);
    if (!payload) return false;
    return payload.exp * 1000 > Date.now();
  }

  private _loadUserName(): string {
    if (!isPlatformBrowser(this._platformId)) return '';
    const token = this._getItem('accessToken');
    if (!token) return '';
    const payload = this._decodeToken(token);
    return payload?.name || payload?.email || '';
  }

  private _storage(): Storage | null {
    if (!isPlatformBrowser(this._platformId)) return null;
    return localStorage.getItem('rememberMe') === 'true' ? localStorage : sessionStorage;
  }

  private _getItem(key: string): string | null {
    if (!isPlatformBrowser(this._platformId)) return null;
    return localStorage.getItem(key) ?? sessionStorage.getItem(key);
  }

  private _setItem(key: string, value: string): void {
    const storage = this._storage();
    if (storage) {
      storage.setItem(key, value);
    }
  }

  private _removeItem(key: string): void {
    if (isPlatformBrowser(this._platformId)) {
      localStorage.removeItem(key);
      sessionStorage.removeItem(key);
    }
  }

  private _setRemember(lembrar: boolean): void {
    if (isPlatformBrowser(this._platformId)) {
      localStorage.setItem('rememberMe', String(lembrar));
    }
  }
}
