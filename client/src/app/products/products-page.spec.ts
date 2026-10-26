import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Product } from './product.model';
import { ProductsPage } from './products-page';

describe('ProductsPage', () => {
  let fixture: ComponentFixture<ProductsPage>;
  let http: HttpTestingController;

  const products: Product[] = [
    { id: '1', name: 'Apple', colour: 'Red', price: 1.5, description: null, createdAt: '2026-01-01T00:00:00Z' },
    { id: '2', name: 'Sky', colour: 'Blue', price: 10, description: 'Big', createdAt: '2026-01-02T00:00:00Z' },
  ];

  beforeEach(async () => {
    vi.useFakeTimers();
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [ProductsPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ProductsPage);
  });

  afterEach(() => {
    http.verify();
    vi.useRealTimers();
  });

  const element = () => fixture.nativeElement as HTMLElement;

  it('loads and renders all products', async () => {
    http.expectOne('/api/v1/products').flush(products);
    fixture.detectChanges();

    const rows = element().querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Apple');
  });

  it('re-queries the API with the colour filter after debouncing', async () => {
    http.expectOne('/api/v1/products').flush(products);
    fixture.detectChanges();

    const input = element().querySelector<HTMLInputElement>('#colour-filter')!;
    input.value = 'red';
    input.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(300);

    const request = http.expectOne((r) => r.url === '/api/v1/products' && r.params.get('colour') === 'red');
    request.flush([products[0]]);
    fixture.detectChanges();

    expect(element().querySelectorAll('tbody tr').length).toBe(1);
  });

  it('shows an empty state when the filter matches nothing', async () => {
    http.expectOne('/api/v1/products').flush(products);
    fixture.detectChanges();

    const input = element().querySelector<HTMLInputElement>('#colour-filter')!;
    input.value = 'green';
    input.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(300);
    http.expectOne((r) => r.params.get('colour') === 'green').flush([]);
    fixture.detectChanges();

    expect(element().textContent).toContain('No products found with colour "green"');
  });

  it('shows an error message when loading fails', async () => {
    http.expectOne('/api/v1/products').flush(
      { title: 'An unexpected error occurred.' },
      { status: 500, statusText: 'Server Error' },
    );
    fixture.detectChanges();

    expect(element().querySelector('[role="alert"]')?.textContent).toContain('An unexpected error occurred.');
  });

  it('hides the create form for read-only users', async () => {
    http.expectOne('/api/v1/products').flush([]);
    fixture.detectChanges();

    expect(element().querySelector('app-product-form')).toBeNull();
  });
});
