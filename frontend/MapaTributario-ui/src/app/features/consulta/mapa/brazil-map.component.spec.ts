import { render, fireEvent } from '@testing-library/angular';
import { BrazilMapComponent } from './brazil-map.component';

describe('BrazilMapComponent', () => {
  it('deve renderizar o mapa', async () => {
    const { container } = await render(BrazilMapComponent);
    expect(container.querySelector('[data-cy="brazil-map"]')).toBeTruthy();
  });

  it('deve renderizar 27 estados com atributo data-uf', async () => {
    const { container } = await render(BrazilMapComponent);
    const estados = container.querySelectorAll('[data-uf]');
    expect(estados.length).toBe(27);
  });

  it('deve emitir selecionar ao clicar em um estado', async () => {
    const selecionar = vi.fn();
    const { container } = await render(BrazilMapComponent, {
      on: { selecionar },
    });
    const sp = container.querySelector('[data-uf="SP"]') as Element;
    expect(sp).toBeTruthy();
    fireEvent.click(sp);
    expect(selecionar).toHaveBeenCalledWith('SP');
  });

  it('deve aplicar classe selecionado quando ufSelecionada corresponde', async () => {
    const { container } = await render(BrazilMapComponent, {
      inputs: { ufSelecionada: 'MG' },
    });
    const mg = container.querySelector('[data-uf="MG"]') as Element;
    expect(mg.classList.contains('selecionado')).toBe(true);
  });
});
