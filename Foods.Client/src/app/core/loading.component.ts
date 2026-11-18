import { Component } from '@angular/core';

@Component({
  selector: 'app-loading',
  template: `
    <div class="loading" role="status">
      <img src="logo.png?v=2" alt="">
      <span>جاري التحميل</span>
    </div>
  `
})
export class LoadingComponent {}
