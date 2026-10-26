import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Subject, catchError, debounceTime, distinctUntilChanged, map, merge, of, startWith, switchMap, tap } from 'rxjs';
import { toErrorMessages } from '../core/api-error';
import { AuthService } from '../core/auth/auth.service';
import { Product } from './product.model';
import { ProductForm } from './product-form';
import { ProductsService } from './products.service';

@Component({
  selector: 'app-products-page',
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, ProductForm],
  templateUrl: './products-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductsPage {
  private readonly productsService = inject(ProductsService);
  private readonly refresh$ = new Subject<void>();

  protected readonly canWrite = inject(AuthService).canWrite;
  protected readonly colourFilter = new FormControl('', { nonNullable: true });
  protected readonly products = signal<Product[]>([]);
  protected readonly loading = signal(true);
  protected readonly errors = signal<string[]>([]);
  protected readonly notice = signal<string | null>(null);

  constructor() {
    const filter$ = this.colourFilter.valueChanges.pipe(
      debounceTime(300),
      map((value) => value.trim()),
      distinctUntilChanged(),
      startWith(''),
    );

    // switchMap cancels in-flight requests when the filter changes, so stale responses never win.
    merge(filter$, this.refresh$.pipe(map(() => this.colourFilter.value.trim())))
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.errors.set([]);
        }),
        switchMap((colour) =>
          this.productsService.list(colour).pipe(
            catchError((error: unknown) => {
              this.errors.set(toErrorMessages(error));
              return of<Product[]>([]);
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((products) => {
        this.products.set(products);
        this.loading.set(false);
      });
  }

  protected clearFilter(): void {
    this.colourFilter.setValue('');
  }

  protected onCreated(product: Product): void {
    this.notice.set(`"${product.name}" was added.`);
    this.refresh$.next();
  }
}
