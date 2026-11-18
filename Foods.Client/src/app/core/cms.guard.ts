import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const cmsGuard: CanActivateFn = () => {
  if (localStorage.getItem('foods.role') === 'Admin' && localStorage.getItem('foods.token')) return true;
  return inject(Router).createUrlTree(['/login']);
};
