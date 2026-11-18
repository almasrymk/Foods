import { Component } from '@angular/core';

/**
 * The one app logo (FOODS cloche, from the favourites mockup) used on every screen.
 * The host element is sized and positioned by the page that places it.
 */
@Component({
  selector: 'fa-logo',
  template: `<img src="assets/screens/shared/logo-transparent.png" alt="FOODS">`,
  styles: [':host { display: block; } img { display: block; width: 100%; height: 100%; object-fit: contain; }']
})
export class FaLogoComponent {}
