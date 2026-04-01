import { Component, inject, signal } from '@angular/core';
import { AbstractControl, ReactiveFormsModule, FormBuilder, Validators, ValidationErrors } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { MessageModule } from 'primeng/message';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [ReactiveFormsModule, RouterModule, ButtonModule, InputTextModule, PasswordModule, MessageModule],
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.scss',
  host: { style: 'display: contents' },
})
export class SignupComponent {
  private readonly _fb = inject(FormBuilder);
  private readonly _authService = inject(AuthService);
  private readonly _router = inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  readonly form = this._fb.nonNullable.group({
    nome: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required, Validators.minLength(8)]],
    confirmarSenha: ['', [Validators.required]],
  }, { validators: [this._passwordMatchValidator] });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    const { email, nome, senha } = this.form.getRawValue();
    this._authService.register(email, nome, senha).subscribe({
      next: () => {
        this.loading.set(false);
        this._router.navigate(['/']);
      },
      error: (registerError) => {
        this.loading.set(false);
        if (registerError.status === 409) {
          this.errorMessage.set('Este email já está cadastrado.');
        } else if (registerError.status === 400) {
          const validationDetails = registerError.error?.detalhes ?? registerError.error?.errors;
          if (Array.isArray(validationDetails) && validationDetails.length > 0) {
            this.errorMessage.set(validationDetails.join(', '));
          } else {
            this.errorMessage.set('Dados inválidos. Verifique os campos.');
          }
        } else {
          this.errorMessage.set('Erro ao criar conta. Tente novamente.');
        }
      },
    });
  }

  private _passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const senha = control.get('senha');
    const confirmarSenha = control.get('confirmarSenha');
    if (senha && confirmarSenha && senha.value !== confirmarSenha.value) {
      return { passwordMismatch: true };
    }
    return null;
  }
}
