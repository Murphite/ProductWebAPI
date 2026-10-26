export interface Product {
  id: string;
  name: string;
  description: string | null;
  colour: string;
  price: number;
  createdAt: string;
}

export interface CreateProductRequest {
  name: string;
  description: string | null;
  colour: string;
  price: number;
}
