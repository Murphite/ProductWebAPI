import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in · Products',
    loadComponent: () => import('./login/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'products',
    title: 'Products',
    canActivate: [authGuard],
    loadComponent: () => import('./products/products-page').then((m) => m.ProductsPage),
  },
  { path: '', pathMatch: 'full', redirectTo: 'products' },
  { path: '**', redirectTo: 'products' },
];
