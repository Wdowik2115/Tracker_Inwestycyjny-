import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, of } from 'rxjs';
import { AuthResponseDto, LoginDto, RegisterDto, UserDto } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private apiUrl = `${environment.apiUrl}/auth`;
  private userApiUrl = `${environment.apiUrl}/user`;
  private tokenKey = 'auth_token';

  isAuthenticated = signal<boolean>(false);
  currentUser = signal<AuthResponseDto | null>(null);
  userProfile = signal<UserDto | null>(null);

  constructor() {
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
      const email = payload.email ?? payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'];
      this.currentUser.set({ token, userId: payload.sub ?? '', email });
      this.fetchProfile().subscribe();
    } catch {
      localStorage.removeItem(this.tokenKey);
    }
  }

  fetchProfile(): Observable<UserDto | null> {
    if (!this.isAuthenticated()) return of(null);
    return this.http.get<UserDto>(`${this.userApiUrl}/profile`).pipe(
      tap(profile => this.userProfile.set(profile))
    );
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
    this.fetchProfile().subscribe();
    this.router.navigate(['/dashboard']);
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    this.isAuthenticated.set(false);
    this.currentUser.set(null);
    this.userProfile.set(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }
}
