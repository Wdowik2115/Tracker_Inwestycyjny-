import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResponseDto, LoginDto, RegisterDto } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  private tokenKey = 'auth_token';

  isAuthenticated = signal<boolean>(false);
  currentUser = signal<AuthResponseDto | null>(null);

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    this.checkAuth();
  }

  private checkAuth(): void {
    const token = this.getToken();
    if (!token) return;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      if (payload.exp < Date.now() / 1000) {
        localStorage.removeItem(this.tokenKey);
        return;
      }
      this.isAuthenticated.set(true);
      // Restore currentUser from token claims if available
      if (payload.email || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress']) {
        const email = payload.email ?? payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'];
        this.currentUser.set({ token, userId: payload.sub ?? '', email });
      }
    } catch {
      localStorage.removeItem(this.tokenKey);
    }
  }

  register(dto: RegisterDto): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${this.apiUrl}/register`, dto).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
  }

  login(dto: LoginDto): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${this.apiUrl}/login`, dto).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
  }

  private handleAuthSuccess(response: AuthResponseDto): void {
    localStorage.setItem(this.tokenKey, response.token);
    this.isAuthenticated.set(true);
    this.currentUser.set(response);
    this.router.navigate(['/dashboard']);
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    this.isAuthenticated.set(false);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }
}
