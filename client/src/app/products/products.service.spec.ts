import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Product } from './product.model';
import { ProductsService } from './products.service';

describe('ProductsService', () => {
  let service: ProductsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProductsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists all products when no colour is given', () => {
    let result: Product[] | undefined;
    service.list().subscribe((products) => (result = products));

    const request = http.expectOne('/api/v1/products');
    expect(request.request.params.has('colour')).toBe(false);
    request.flush([]);

    expect(result).toEqual([]);
  });

  it('passes a trimmed colour filter as a query parameter', () => {
    service.list('  red ').subscribe();

    const request = http.expectOne((r) => r.url === '/api/v1/products');
    expect(request.request.params.get('colour')).toBe('red');
    request.flush([]);
  });

  it('ignores a blank colour filter', () => {
    service.list('   ').subscribe();

    const request = http.expectOne('/api/v1/products');
    expect(request.request.params.keys()).toEqual([]);
    request.flush([]);
  });

  it('posts new products', () => {
    const body = { name: 'Lamp', colour: 'Black', price: 20, description: null };
    service.create(body).subscribe();

    const request = http.expectOne('/api/v1/products');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush({ ...body, id: '1', createdAt: new Date().toISOString() });
  });
});
