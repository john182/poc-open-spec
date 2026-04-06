import { Component, inject, OnInit, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { MessageModule } from 'primeng/message';
import { CardModule } from 'primeng/card';
import { DividerModule } from 'primeng/divider';
import { PerfilService } from './perfil.service';
import { AuthService } from '../../core/auth/auth.service';

function senhasConferemValidator(control: AbstractControl): ValidationErrors | null {
  const novaSenha = control.get('novaSenha')?.value;
  const confirmarNovaSenha = control.get('confirmarNovaSenha')?.value;
  if (novaSenha && confirmarNovaSenha && novaSenha !== confirmarNovaSenha) {
    return { senhasNaoConferem: true };
  }
  return null;
}

@Component({
  selector: 'app-perfil',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonModule, InputTextModule, PasswordModule, MessageModule, CardModule, DividerModule],
  templateUrl: './perfil.component.html',
  styleUrl: './perfil.component.scss',
})
export class PerfilComponent implements OnInit {
  private readonly _fb = inject(FormBuilder);
  private readonly _perfilService = inject(PerfilService);
  private readonly _authService = inject(AuthService);

  readonly carregando = signal(true);
  readonly salvando = signal(false);
  readonly mensagemErro = signal('');
  readonly mensagemSucesso = signal('');

  readonly form = this._fb.nonNullable.group(
    {
      nome: ['', [Validators.required, Validators.minLength(2)]],
      email: [{ value: '', disabled: true }],
      senhaAtual: [''],
      novaSenha: [''],
      confirmarNovaSenha: [''],
    },
    { validators: senhasConferemValidator },
  );

  ngOnInit(): void {
    this._carregarPerfil();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      if (this.form.hasError('senhasNaoConferem')) {
        this.mensagemErro.set('As senhas não coincidem.');
        return;
      }
      return;
    }

    const { nome, senhaAtual, novaSenha, confirmarNovaSenha } = this.form.getRawValue();

    if (novaSenha && !senhaAtual) {
      this.mensagemErro.set('Informe a senha atual para alterar a senha.');
      return;
    }

    if (senhaAtual && !novaSenha) {
      this.mensagemErro.set('Informe a nova senha.');
      return;
    }

    if (novaSenha && novaSenha.length < 8) {
      this.mensagemErro.set('A nova senha deve ter pelo menos 8 caracteres.');
      return;
    }

    if (novaSenha && novaSenha !== confirmarNovaSenha) {
      this.mensagemErro.set('As senhas não coincidem.');
      return;
    }

    this.salvando.set(true);
    this.mensagemErro.set('');
    this.mensagemSucesso.set('');

    const dados = {
      nome,
      ...(senhaAtual && novaSenha ? { senhaAtual, novaSenha } : {}),
    };

    this._perfilService.atualizarPerfil(dados).subscribe({
      next: (resposta) => {
        this._authService.atualizarToken(resposta.accessToken);
        this.form.patchValue({ senhaAtual: '', novaSenha: '', confirmarNovaSenha: '' });
        this.mensagemSucesso.set('Perfil atualizado com sucesso!');
        this.salvando.set(false);
      },
      error: (erro) => {
        this.salvando.set(false);
        if (erro.status === 400) {
          const detalhes = erro.error?.detalhes ?? erro.error?.errors;
          if (Array.isArray(detalhes) && detalhes.length > 0) {
            this.mensagemErro.set(detalhes.join(', '));
          } else if (erro.error?.erro) {
            this.mensagemErro.set(erro.error.erro);
          } else {
            this.mensagemErro.set('Dados inválidos. Verifique os campos.');
          }
        } else if (erro.status === 401) {
          this.mensagemErro.set('Sessão expirada. Faça login novamente.');
        } else {
          this.mensagemErro.set('Erro ao atualizar perfil. Tente novamente.');
        }
      },
    });
  }

  private _carregarPerfil(): void {
    this.carregando.set(true);
    this._perfilService.obterPerfil().subscribe({
      next: (perfil) => {
        this.form.patchValue({ nome: perfil.nome, email: perfil.email });
        this.carregando.set(false);
      },
      error: () => {
        this.mensagemErro.set('Erro ao carregar perfil. Tente novamente.');
        this.carregando.set(false);
      },
    });
  }
}
