import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageModule } from 'primeng/message';
import { AuthService } from '../../../core/auth/auth.service';
import { LayoutService } from '../../../layout/services/layout.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterModule, ButtonModule, InputTextModule, PasswordModule, CheckboxModule, MessageModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  host: { style: 'display: contents' },
})
export class LoginComponent {
  private readonly _fb = inject(FormBuilder);
  private readonly _authService = inject(AuthService);
  private readonly _router = inject(Router);
  // Injeta LayoutService para garantir que o tema salvo no localStorage seja aplicado
  private readonly _layoutService = inject(LayoutService);

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  readonly form = this._fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required, Validators.minLength(8)]],
    lembrar: [false],
  });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    const { email, senha, lembrar } = this.form.getRawValue();
    this._authService.login(email, senha, lembrar).subscribe({
      next: () => {
        this.loading.set(false);
        this._router.navigate(['/']);
      },
      error: (loginError) => {
        this.loading.set(false);
        if (loginError.status === 401) {
          this.errorMessage.set('Email ou senha inválidos.');
        } else if (loginError.status === 403) {
          this.errorMessage.set('Conta inativa. Entre em contato com o suporte.');
        } else {
          this.errorMessage.set('Erro ao realizar login. Tente novamente.');
        }
      },
    });
  }
}
