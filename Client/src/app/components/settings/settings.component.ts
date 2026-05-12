import { Component, inject, OnInit, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent implements OnInit {
  private titleService = inject(Title);
  authService = inject(AuthService);

  currency = signal<'USD' | 'EUR'>(
    (localStorage.getItem('display_currency') as 'USD' | 'EUR') ?? 'USD'
  );

  currentPassword = signal('');
  newPassword = signal('');
  confirmPassword = signal('');

  ngOnInit(): void {
    this.titleService.setTitle('Settings — Investee');
  }

  setCurrency(c: 'USD' | 'EUR'): void {
    this.currency.set(c);
    localStorage.setItem('display_currency', c);
  }
}
