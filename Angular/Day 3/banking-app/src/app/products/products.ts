import { Component, signal } from '@angular/core';
import { ProductModel } from '../models/product.model';
import { ProductApiService } from '../services/product.api.service';
import { Subject } from 'rxjs/internal/Subject';
import { debounceTime } from 'rxjs';

@Component({
  selector: 'app-products',
  imports: [],
  templateUrl: './products.html',
  styleUrl: './products.css',
})
export class Products {
  private allProducts: ProductModel[] = [];
  filteredProducts = signal<ProductModel[]>([]);

  private searchSubject = new Subject<string>();

  constructor(private productApiService: ProductApiService) {
    this.searchSubject.pipe(debounceTime(300)).subscribe((term) => {
      this.filteredProducts.set(this.filterProducts(this.allProducts, term));
    });

    this.productApiService.getProductsFromDummyJson().subscribe({
      next: (response: any) => {
        this.allProducts = response.products;
        this.filteredProducts.set(this.allProducts);
      },
      error: (error) => {
        console.error(error);
      },
    });
  }

  onSearchInput(term: string) {
    this.searchSubject.next(term);
  }

  searchProducts(term: string) {
    this.searchSubject.next(term);
  }

  private filterProducts(products: ProductModel[], query: string): ProductModel[] {
    if (!query.trim()) return products;
    const term = query.toLowerCase();
    return products.filter(
      (p) =>
        p.title.toLowerCase().includes(term) ||
        p.description.toLowerCase().includes(term)
    );
  }
}
