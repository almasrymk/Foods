import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

// Width of a classic (non-overlay) vertical scrollbar, so the full-width column (--fa-w on
// phones/tablets) never runs under it in a narrow desktop window. Overlay scrollbars give 0.
function trackScrollbar(): void {
  const root = document.documentElement;
  const update = () => root.style.setProperty('--sbw', `${Math.max(0, window.innerWidth - root.clientWidth)}px`);
  update();
  window.addEventListener('resize', update, { passive: true });
  new ResizeObserver(update).observe(document.body);
}
trackScrollbar();

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
