import { Component, signal, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-nav-rail',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './nav-rail.component.html',
  styleUrl: './nav-rail.component.css'
})
export class NavRailComponent {
  public authService = inject(AuthService);
  isExpanded = signal(false);

  onLogout() {
    this.authService.logout();
  }
}
