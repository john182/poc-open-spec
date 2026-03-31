import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FilterBarComponent } from './filter-bar.component';

describe('FilterBarComponent', () => {
  let fixture: ComponentFixture<FilterBarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FilterBarComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(FilterBarComponent);
  });

  it('deve criar o componente', () => {
    fixture.componentRef.setInput('filtros', []);
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('deve renderizar filtros', () => {
    fixture.componentRef.setInput('filtros', [
      { chave: 'nome', rotulo: 'Nome', tipo: 'text' },
    ]);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Nome');
  });

  it('deve emitir limpar ao clicar', () => {
    fixture.componentRef.setInput('filtros', []);
    fixture.detectChanges();
    let emitted = false;
    fixture.componentInstance.limpar.subscribe(() => (emitted = true));
    const btn = fixture.nativeElement.querySelector('button');
    btn?.click();
    expect(emitted).toBe(true);
  });

  it('deve emitir filtroMudou ao alterar valor', () => {
    fixture.componentRef.setInput('filtros', [
      { chave: 'nome', rotulo: 'Nome', tipo: 'text' },
    ]);
    fixture.detectChanges();
    let valores: Record<string, string> = {};
    fixture.componentInstance.filtroMudou.subscribe((v: Record<string, string>) => (valores = v));
    fixture.componentInstance.onValorMudou('nome', 'teste');
    expect(valores['nome']).toBe('teste');
  });

  it('deve limpar valores ao chamar onLimpar', () => {
    fixture.componentRef.setInput('filtros', []);
    fixture.detectChanges();
    fixture.componentInstance.valores = { nome: 'teste' };
    fixture.componentInstance.onLimpar();
    expect(fixture.componentInstance.valores).toEqual({});
  });
});
