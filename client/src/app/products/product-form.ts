import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { toErrorMessages } from '../core/api-error';
import { Product } from './product.model';
import { ProductsService } from './products.service';

/** Validation rules mirror the API so most errors are caught before a round-trip. */
@Component({
  selector: 'app-product-form',
  imports: [ReactiveFormsModule],
  templateUrl: './product-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductForm {
  private readonly productsService = inject(ProductsService);

  readonly created = output<Product>();

  protected readonly submitting = signal(false);
  protected readonly errors = signal<string[]>([]);

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    colour: ['', [Validators.required, Validators.maxLength(50), Validators.pattern(/^[\p{L} -]+$/u)]],
    price: [0, [Validators.required, Validators.min(0), Validators.max(1_000_000)]],
    description: ['', Validators.maxLength(2000)],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.submitting.set(true);
    this.errors.set([]);

    this.productsService
      .create({
        name: value.name.trim(),
        colour: value.colour.trim(),
        price: Number(value.price),
        description: value.description.trim() || null,
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (product) => {
          this.form.reset();
          this.created.emit(product);
        },
        error: (error: unknown) => this.errors.set(toErrorMessages(error)),
      });
  }
}
