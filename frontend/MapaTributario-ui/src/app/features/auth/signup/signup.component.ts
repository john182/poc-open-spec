import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="auth-page" data-cy="signup-page">
      <h1>Criar Conta</h1>
      <p>Página de cadastro — será implementada na PBI #7</p>
      <a routerLink="/auth/login">Já tenho conta</a>
    </div>
  `,
  styles: [`.auth-page { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 100vh; }`],
})
export class SignupComponent {}
