import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EmptyStateComponent } from './empty-state.component';

describe('EmptyStateComponent', () => {
  let fixture: ComponentFixture<EmptyStateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmptyStateComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(EmptyStateComponent);
  });

  it('deve criar o componente', () => {
    fixture.componentRef.setInput('titulo', 'Vazio');
    fixture.componentRef.setInput('mensagem', 'Sem dados');
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('deve exibir titulo e mensagem', () => {
    fixture.componentRef.setInput('titulo', 'Nenhum resultado');
    fixture.componentRef.setInput('mensagem', 'Tente ajustar os filtros');
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Nenhum resultado');
    expect(el.textContent).toContain('Tente ajustar os filtros');
  });

  it('deve emitir acao ao clicar no botao', () => {
    fixture.componentRef.setInput('titulo', 'Vazio');
    fixture.componentRef.setInput('mensagem', 'Sem dados');
    fixture.componentRef.setInput('acaoLabel', 'Recarregar');
    fixture.detectChanges();
    let emitted = false;
    fixture.componentInstance.acao.subscribe(() => (emitted = true));
    const btn = fixture.nativeElement.querySelector('button');
    btn?.click();
    expect(emitted).toBe(true);
  });
});
