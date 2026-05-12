import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-nav-rail',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './nav-rail.component.html',
  styleUrl: './nav-rail.component.css'
})
export class NavRailComponent {}
