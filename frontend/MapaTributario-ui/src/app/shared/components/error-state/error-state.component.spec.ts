import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ErrorStateComponent } from './error-state.component';

describe('ErrorStateComponent', () => {
  let fixture: ComponentFixture<ErrorStateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ErrorStateComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(ErrorStateComponent);
  });

  it('deve criar o componente', () => {
    fixture.componentRef.setInput('mensagem', 'Algo deu errado');
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('deve exibir titulo padrao e mensagem', () => {
    fixture.componentRef.setInput('mensagem', 'Falha na conexão');
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Erro ao carregar dados');
    expect(el.textContent).toContain('Falha na conexão');
  });

  it('deve emitir tentarNovamente ao clicar retry', () => {
    fixture.componentRef.setInput('mensagem', 'Erro');
    fixture.detectChanges();
    let emitted = false;
    fixture.componentInstance.tentarNovamente.subscribe(() => (emitted = true));
    const btn = fixture.nativeElement.querySelector('button');
    btn?.click();
    expect(emitted).toBe(true);
  });

  it('deve ter o atributo data-cy', () => {
    fixture.componentRef.setInput('mensagem', 'Erro');
    fixture.detectChanges();
    const el = fixture.nativeElement.querySelector('[data-cy="error-state"]');
    expect(el).toBeTruthy();
  });
});
