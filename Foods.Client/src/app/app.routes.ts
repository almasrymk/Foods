import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { cmsGuard } from './core/cms.guard';
import { LoginComponent } from './pages/login.component';
import { RegisterComponent } from './pages/register.component';
import { HomeComponent } from './pages/home/home.component';
import { BuilderComponent } from './pages/builder.component';
import { OrderComponent } from './pages/order.component';
import { FavoritesScreenComponent } from './pages/screens/favorites/favorites.component';
import { MealTypeScreenComponent } from './pages/screens/meal-type/meal-type.component';
import { PlanScreenComponent } from './pages/screens/plan/plan.component';
import { CuisinesScreenComponent } from './pages/screens/cuisines/cuisines.component';
import { MealDetailsScreenComponent } from './pages/screens/meal-details/meal-details.component';
import { OrdersScreenComponent } from './pages/screens/orders/orders.component';
import { IngredientsScreenComponent } from './pages/screens/ingredients/ingredients.component';
import { SettingsScreenComponent } from './pages/screens/settings/settings.component';
import { NotificationsScreenComponent } from './pages/screens/notifications/notifications.component';
import { PricesScreenComponent } from './pages/screens/prices/prices.component';
import { FamilyComponent } from './pages/family.component';
import { CmsLookupsComponent } from './pages/cms/cms-lookups.component';
import { CmsLookupListComponent } from './pages/cms/cms-lookup-list.component';
import { CmsLookupFormComponent } from './pages/cms/cms-lookup-form.component';
import { CmsMealsComponent } from './pages/cms/cms-meals.component';
import { CmsMealFormComponent } from './pages/cms/cms-meal-form.component';
import { CmsItemsComponent } from './pages/cms/cms-items.component';
import { CmsItemFormComponent } from './pages/cms/cms-item-form.component';
import { CmsContentsComponent } from './pages/cms/cms-contents.component';
import { CmsContentFormComponent } from './pages/cms/cms-content-form.component';
import { CmsUsersComponent } from './pages/cms/cms-users.component';
import { CmsUserFormComponent } from './pages/cms/cms-user-form.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'home', component: HomeComponent, canActivate: [authGuard] },
  { path: 'meals', component: PlanScreenComponent, canActivate: [authGuard] },
  { path: 'meals/:id', component: MealTypeScreenComponent, canActivate: [authGuard] },
  { path: 'cuisines', component: CuisinesScreenComponent, canActivate: [authGuard] },
  { path: 'categories', component: CuisinesScreenComponent, canActivate: [authGuard], data: { kind: 'category' } },
  { path: 'groups', component: CuisinesScreenComponent, canActivate: [authGuard], data: { kind: 'group' } },
  { path: 'items/:id', component: MealDetailsScreenComponent, canActivate: [authGuard] },
  { path: 'builder', component: BuilderComponent, canActivate: [authGuard] },
  { path: 'orders', component: OrdersScreenComponent, canActivate: [authGuard] },
  { path: 'orders/:id', component: OrderComponent, canActivate: [authGuard] },
  { path: 'favorites', component: FavoritesScreenComponent, canActivate: [authGuard] },
  { path: 'family', component: FamilyComponent, canActivate: [authGuard] },
  { path: 'ingredients', component: IngredientsScreenComponent, canActivate: [authGuard] },
  { path: 'settings', component: SettingsScreenComponent, canActivate: [authGuard] },
  { path: 'notifications', component: NotificationsScreenComponent, canActivate: [authGuard] },
  { path: 'prices', component: PricesScreenComponent, canActivate: [authGuard] },
  { path: 'cms/login', redirectTo: 'login' },
  { path: 'cms/lookups', component: CmsLookupsComponent, canActivate: [cmsGuard] },
  { path: 'cms/lookups/:kind/new', component: CmsLookupFormComponent, canActivate: [cmsGuard] },
  { path: 'cms/lookups/:kind/:id', component: CmsLookupFormComponent, canActivate: [cmsGuard] },
  { path: 'cms/lookups/:kind', component: CmsLookupListComponent, canActivate: [cmsGuard] },
  { path: 'cms/meals/new', component: CmsMealFormComponent, canActivate: [cmsGuard] },
  { path: 'cms/meals/:id', component: CmsMealFormComponent, canActivate: [cmsGuard] },
  { path: 'cms/meals', component: CmsMealsComponent, canActivate: [cmsGuard] },
  { path: 'cms/cuisines', pathMatch: 'prefix', redirectTo: 'cms/lookups/Cuisine' },
  { path: 'cms/items/new', component: CmsItemFormComponent, canActivate: [cmsGuard] },
  { path: 'cms/items/:id', component: CmsItemFormComponent, canActivate: [cmsGuard] },
  { path: 'cms/items', component: CmsItemsComponent, canActivate: [cmsGuard] },
  { path: 'cms/contents/new', component: CmsContentFormComponent, canActivate: [cmsGuard] },
  { path: 'cms/contents/:id', component: CmsContentFormComponent, canActivate: [cmsGuard] },
  { path: 'cms/contents', component: CmsContentsComponent, canActivate: [cmsGuard] },
  { path: 'cms/users/new', component: CmsUserFormComponent, canActivate: [cmsGuard] },
  { path: 'cms/users/:id', component: CmsUserFormComponent, canActivate: [cmsGuard] },
  { path: 'cms/users', component: CmsUsersComponent, canActivate: [cmsGuard] },
  { path: 'cms', pathMatch: 'full', redirectTo: 'cms/lookups' },
  { path: '', pathMatch: 'full', redirectTo: 'home' }
];
