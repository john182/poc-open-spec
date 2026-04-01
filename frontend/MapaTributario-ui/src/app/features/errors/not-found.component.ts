import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterModule, ButtonModule],
  templateUrl: './not-found.component.html',
  styleUrl: './not-found.component.scss',
  host: { style: 'display: contents' },
})
export class NotFoundComponent {}
