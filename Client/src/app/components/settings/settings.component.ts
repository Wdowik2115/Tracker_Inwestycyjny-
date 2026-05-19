import { Component, inject, OnInit, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { UserDto } from '../../models';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent implements OnInit {
  private titleService = inject(Title);
  private userService = inject(UserService);
  authService = inject(AuthService);

  profile = signal<UserDto | null>(null);
  firstName = signal('');
  lastName = signal('');

  currency = signal<'USD' | 'EUR'>(
    (localStorage.getItem('display_currency') as 'USD' | 'EUR') ?? 'USD'
  );

  currentPassword = signal('');
  newPassword = signal('');
  confirmPassword = signal('');

  loading = signal(false);
  
  profileError = signal<string | null>(null);
  profileSuccess = signal<string | null>(null);
  
  securityError = signal<string | null>(null);
  securitySuccess = signal<string | null>(null);

  ngOnInit(): void {
    this.titleService.setTitle('Settings — Investee');
    this.loadProfile();
  }

  loadProfile(): void {
    this.userService.getProfile().subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.firstName.set(profile.firstName ?? '');
        this.lastName.set(profile.lastName ?? '');
        this.currency.set(profile.preferredCurrency as 'USD' | 'EUR');
      },
      error: (err) => this.profileError.set('Failed to load profile')
    });
  }

  hasUpperCase(str: string): boolean {
    return /[A-Z]/.test(str);
  }

  hasNumber(str: string): boolean {
    return /[0-9]/.test(str);
  }

  hasSpecialChar(str: string): boolean {
    return /[^a-zA-Z0-9]/.test(str);
  }

  isPasswordStrong(): boolean {
    const p = this.newPassword();
    return (
      p.length >= 8 &&
      this.hasUpperCase(p) &&
      this.hasNumber(p) &&
      this.hasSpecialChar(p)
    );
  }

  updateProfile(): void {
    this.loading.set(true);
    this.profileError.set(null);
    this.profileSuccess.set(null);

    this.userService.updateProfile({
      firstName: this.firstName(),
      lastName: this.lastName(),
      preferredCurrency: this.currency()
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.profileSuccess.set('Profile updated successfully');
        localStorage.setItem('display_currency', this.currency());
        this.authService.fetchProfile().subscribe();
      },
      error: () => {
        this.loading.set(false);
        this.profileError.set('Failed to update profile');
      }
    });
  }

  changePassword(): void {
    if (this.newPassword() !== this.confirmPassword()) {
      this.securityError.set('Passwords do not match');
      return;
    }

    if (!this.isPasswordStrong()) {
      this.securityError.set('New password does not meet requirements');
      return;
    }

    this.loading.set(true);
    this.securityError.set(null);
    this.securitySuccess.set(null);

    this.userService.changePassword({
      oldPassword: this.currentPassword(),
      newPassword: this.newPassword()
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.securitySuccess.set('Password changed successfully');
        this.currentPassword.set('');
        this.newPassword.set('');
        this.confirmPassword.set('');
      },
      error: (err) => {
        this.loading.set(false);
        this.securityError.set(err.error?.message ?? 'Failed to change password');
      }
    });
  }

  setCurrency(c: 'USD' | 'EUR'): void {
    this.currency.set(c);
  }
}
