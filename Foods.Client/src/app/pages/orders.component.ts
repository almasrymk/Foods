import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { OrderListItem } from '../core/models';

@Component({
  selector: 'app-orders',
  imports: [CommonModule, RouterLink, DatePipe],
  template: `
    <h1>الطلبات السابقة</h1>
    <p class="error" *ngIf="error">{{ error }}</p>
    <a class="card compact" *ngFor="let order of orders" [routerLink]="['/orders', order.id]">
      <h2>{{ order.name }}</h2>
      <p>{{ order.date | date:'yyyy-MM-dd' }}</p>
    </a>
  `
})
export class OrdersComponent implements OnInit {
  orders: OrderListItem[] = [];
  error = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.orders().subscribe({
      next: orders => this.orders = orders,
      error: (err: Error) => this.error = err.message
    });
  }
}
