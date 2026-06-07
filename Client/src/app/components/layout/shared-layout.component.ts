import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavRailComponent } from './nav-rail/nav-rail.component';
import { ToastComponent } from '../shared/toast/toast.component';
import { PortfolioHeaderComponent } from './portfolio-header/portfolio-header.component';

@Component({
  selector: 'app-shared-layout',
  standalone: true,
  imports: [RouterOutlet, NavRailComponent, ToastComponent, PortfolioHeaderComponent],
  templateUrl: './shared-layout.component.html',
  styleUrl: './shared-layout.component.css'
})
export class SharedLayoutComponent {
  sidebarOpen = signal(false);

  toggleSidebar(): void {
    this.sidebarOpen.update(v => !v);
  }
}
