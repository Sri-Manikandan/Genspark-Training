import { Component } from '@angular/core';
import { Product } from '../models/products.model';

@Component({
  selector: 'app-products',
  templateUrl: './products.html',
  styleUrl: './products.css',
})
export class Products {
  Products: Product[] = [
    new Product(1, 'Apple iPhone 15', 100, 'The Apple iPhone 15 is a powerful smartphone with a 6.7-inch display, 128GB of storage, and a 50MP camera.', 'iphone.jpeg'),
    new Product(2, 'Samsung Galaxy S23', 200, 'The Samsung Galaxy S23 is a powerful smartphone with a 6.7-inch display, 128GB of storage, and a 50MP camera.', 'samsung.jpeg'),
    new Product(3, 'Google Pixel 8', 300, 'The Google Pixel 8 is a powerful smartphone with a 6.7-inch display, 128GB of storage, and a 50MP camera.', 'google-pixel.jpeg'),
  ];
}
