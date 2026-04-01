import { Component, inject } from '@angular/core';
import { AppMenuComponent } from './app-menu.component';
import { LayoutService } from '../services/layout.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [AppMenuComponent],
  templateUrl: './app-sidebar.component.html',
  styleUrl: './app-sidebar.component.scss',
})
export class AppSidebarComponent {
  layoutService = inject(LayoutService);
}
