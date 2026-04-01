import { render, screen } from '@testing-library/angular';
import { FormFieldComponent } from './form-field.component';
import { FormControl, Validators } from '@angular/forms';

describe('FormFieldComponent', () => {
  it('deve exibir rotulo', async () => {
    const control = new FormControl('');
    await render(FormFieldComponent, {
      inputs: { rotulo: 'Email', controle: control },
    });
    expect(screen.getByText('Email')).toBeTruthy();
  });

  it('nao deve exibir erros quando controle pristine', async () => {
    const control = new FormControl('', Validators.required);
    const { container } = await render(FormFieldComponent, {
      inputs: { rotulo: 'Nome', controle: control },
    });
    expect(container.querySelector('.erro-mensagem')).toBeNull();
  });

  it('deve exibir erro quando controle dirty e invalido', async () => {
    const control = new FormControl('', Validators.required);
    control.markAsDirty();
    const { container } = await render(FormFieldComponent, {
      inputs: { rotulo: 'Nome', controle: control },
    });
    expect(container.querySelector('.erro-mensagem')).toBeTruthy();
  });

  it('deve usar mensagem padrao para required', async () => {
    const control = new FormControl('', Validators.required);
    control.markAsDirty();
    await render(FormFieldComponent, {
      inputs: { rotulo: 'Nome', controle: control },
    });
    expect(screen.getByText('Campo obrigatório')).toBeTruthy();
  });

  it('deve usar mensagem customizada quando fornecida', async () => {
    const control = new FormControl('', Validators.required);
    control.markAsDirty();
    await render(FormFieldComponent, {
      inputs: { rotulo: 'Nome', controle: control, mensagensErro: { required: 'Preencha este campo' } },
    });
    expect(screen.getByText('Preencha este campo')).toBeTruthy();
  });
});
