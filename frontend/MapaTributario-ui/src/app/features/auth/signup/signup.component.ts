import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [FormsModule, RouterModule, ButtonModule, InputTextModule, PasswordModule],
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.scss',
  host: { style: 'display: contents' },
})
export class SignupComponent {
  nome = '';
  email = '';
  senha = '';
  confirmarSenha = '';
}
