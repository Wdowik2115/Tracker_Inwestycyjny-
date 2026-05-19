import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { RegisterDto } from '../../../models';
import { CryptoBackgroundComponent } from '../../shared/crypto-background/crypto-background.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, CryptoBackgroundComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent implements OnInit {
  email = signal('');
  password = signal('');
  confirmPassword = signal('');
  error = signal('');
  loading = signal(false);

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
    }
  }

  hasUpperCase(str: string): boolean {
    return /[A-Z]/.test(str);
  }

  hasNumber(str: string): boolean {
    return /[0-9]/.test(str);
  }

  hasSpecialChar(str: string): boolean {
    return /[!@#$%^&*(),.?":{}|<>]/.test(str);
  }

  isFormValid(): boolean {
    const p = this.password();
    return (
      this.email().includes('@') &&
      p.length >= 8 &&
      this.hasUpperCase(p) &&
      this.hasNumber(p) &&
      this.hasSpecialChar(p) &&
      p === this.confirmPassword()
    );
  }

  onSubmit(): void {
    if (this.password() !== this.confirmPassword()) {
      this.error.set('Passwords do not match');
      return;
    }

    if (this.password().length < 8) {
      this.error.set('Password must be at least 8 characters');
      return;
    }

    this.loading.set(true);
    this.error.set('');

    const dto: RegisterDto = {
      email: this.email(),
      password: this.password()
    };

    this.authService.register(dto).subscribe({
      next: () => {
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(
          err.error?.detail ||
          err.error?.title ||
          err.error?.message ||
          'Registration failed. Please try again.'
        );
      }
    });
  }
}