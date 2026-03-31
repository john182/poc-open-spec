import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="auth-page" data-cy="login-page">
      <h1>Entrar</h1>
      <p>Página de login — será implementada na PBI #7</p>
      <a routerLink="/auth/signup">Criar conta</a>
    </div>
  `,
  styles: [`.auth-page { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 100vh; }`],
})
export class LoginComponent {}
