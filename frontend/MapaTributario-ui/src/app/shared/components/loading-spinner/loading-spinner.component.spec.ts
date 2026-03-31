import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoadingSpinnerComponent } from './loading-spinner.component';

describe('LoadingSpinnerComponent', () => {
  let fixture: ComponentFixture<LoadingSpinnerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoadingSpinnerComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(LoadingSpinnerComponent);
  });

  it('deve criar o componente', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('deve exibir mensagem quando fornecida', () => {
    fixture.componentRef.setInput('mensagem', 'Carregando...');
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Carregando...');
  });

  it('deve ter o atributo data-cy', () => {
    fixture.detectChanges();
    const el = fixture.nativeElement.querySelector('[data-cy="loading-spinner"]');
    expect(el).toBeTruthy();
  });
});
