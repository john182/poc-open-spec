import { render, screen } from '@testing-library/angular';
import { FilterBarComponent } from './filter-bar.component';

describe('FilterBarComponent', () => {
  it('deve renderizar filtros', async () => {
    await render(FilterBarComponent, {
      inputs: {
        filtros: [{ chave: 'nome', rotulo: 'Nome', tipo: 'text' as const }],
      },
    });
    expect(screen.getByText('Nome')).toBeTruthy();
  });

  it('deve ter atributo data-cy', async () => {
    const { container } = await render(FilterBarComponent, {
      inputs: { filtros: [] },
    });
    expect(container.querySelector('[data-cy="filter-bar"]')).toBeTruthy();
  });

  it('deve ter botao limpar', async () => {
    await render(FilterBarComponent, {
      inputs: { filtros: [] },
    });
    expect(screen.getByText('Limpar')).toBeTruthy();
  });

  it('deve emitir filtroMudou ao alterar valor', async () => {
    const { fixture } = await render(FilterBarComponent, {
      inputs: {
        filtros: [{ chave: 'nome', rotulo: 'Nome', tipo: 'text' as const }],
      },
    });
    let valores: Record<string, string | number> = {};
    fixture.componentInstance.filtroMudou.subscribe((v: Record<string, string | number>) => (valores = v));
    fixture.componentInstance.onValorMudou('nome', 'teste');
    expect(valores['nome']).toBe('teste');
  });

  it('deve converter valor numerico', async () => {
    const { fixture } = await render(FilterBarComponent, {
      inputs: {
        filtros: [{ chave: 'valor', rotulo: 'Valor', tipo: 'number' as const }],
      },
    });
    let valores: Record<string, string | number> = {};
    fixture.componentInstance.filtroMudou.subscribe((v: Record<string, string | number>) => (valores = v));
    fixture.componentInstance.onValorMudou('valor', '42');
    expect(valores['valor']).toBe(42);
  });

  it('deve limpar valores ao chamar onLimpar', async () => {
    const { fixture } = await render(FilterBarComponent, {
      inputs: { filtros: [] },
    });
    fixture.componentInstance.valores = { nome: 'teste' };
    fixture.componentInstance.onLimpar();
    expect(fixture.componentInstance.valores).toEqual({});
  });
});
