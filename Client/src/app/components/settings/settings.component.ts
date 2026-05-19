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
  error = signal<string | null>(null);
  success = signal<string | null>(null);

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
      error: (err) => this.error.set('Failed to load profile')
    });
  }

  updateProfile(): void {
    this.loading.set(true);
    this.error.set(null);
    this.success.set(null);

    this.userService.updateProfile({
      firstName: this.firstName(),
      lastName: this.lastName(),
      preferredCurrency: this.currency()
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set('Profile updated successfully');
        localStorage.setItem('display_currency', this.currency());
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to update profile');
      }
    });
  }

  changePassword(): void {
    if (this.newPassword() !== this.confirmPassword()) {
      this.error.set('Passwords do not match');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.success.set(null);

    this.userService.changePassword({
      oldPassword: this.currentPassword(),
      newPassword: this.newPassword()
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set('Password changed successfully');
        this.currentPassword.set('');
        this.newPassword.set('');
        this.confirmPassword.set('');
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Failed to change password');
      }
    });
  }

  setCurrency(c: 'USD' | 'EUR'): void {
    this.currency.set(c);
  }
}
