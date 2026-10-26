import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateProductRequest, Product } from './product.model';

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/products';

  /** Lists all products, or only those of the given colour (case-insensitive). */
  list(colour?: string | null): Observable<Product[]> {
    const trimmed = colour?.trim();
    const params = trimmed ? new HttpParams().set('colour', trimmed) : undefined;
    return this.http.get<Product[]>(this.baseUrl, { params });
  }

  create(request: CreateProductRequest): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, request);
  }
}
