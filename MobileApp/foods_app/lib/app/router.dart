import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../features/account/presentation/settings_page.dart';
import '../features/auth/presentation/auth_controller.dart';
import '../features/auth/presentation/login_page.dart';
import '../features/auth/presentation/register_page.dart';
import '../features/auth/presentation/family_step_page.dart';
import '../features/cart/presentation/ingredients_page.dart';
import '../features/catalog/domain/catalog_models.dart';
import '../features/catalog/presentation/cuisines_page.dart';
import '../features/catalog/presentation/catalog_providers.dart' show BrowseKind;
import '../features/catalog/presentation/favorites_page.dart';
import '../features/catalog/presentation/item_details_page.dart';
import '../features/catalog/presentation/meal_items_page.dart';
import '../features/catalog/presentation/meals_page.dart';
import '../features/home/presentation/home_page.dart';
import '../features/notifications/presentation/notifications_page.dart';
import '../features/orders/presentation/order_details_page.dart';
import '../features/orders/presentation/orders_history_page.dart';
import 'widgets/fa_chrome.dart';
import '../features/prices/prices.dart';

final _rootKey = GlobalKey<NavigatorState>();

/// Re-checks the redirect whenever the customer logs in or out.
class _AuthListenable extends ChangeNotifier {
  void ping() => notifyListeners();
}

final routerProvider = Provider<GoRouter>((ref) {
  final listenable = _AuthListenable();
  ref.listen(authControllerProvider.select((a) => (a.loggedIn, a.onboarding)), (_, _) => listenable.ping());
  ref.onDispose(listenable.dispose);

  String? emptyToNull(String? v) => (v == null || v.isEmpty) ? null : v;

  return GoRouter(
    navigatorKey: _rootKey,
    initialLocation: '/home',
    refreshListenable: listenable,
    redirect: (context, state) {
      final auth = ref.read(authControllerProvider);
      final loggedIn = auth.loggedIn;
      final guest = state.matchedLocation == '/login' || state.matchedLocation == '/register';
      if (loggedIn && auth.onboarding) return state.matchedLocation == '/welcome/family' ? null : '/welcome/family';
      if (state.matchedLocation == '/welcome/family') return loggedIn ? '/home' : '/login';
      if (!loggedIn && !guest) return '/login';
      if (loggedIn && guest) return '/home';
      return null;
    },
    routes: [
      GoRoute(path: '/login', builder: (_, _) => const LoginPage()),
      GoRoute(path: '/register', builder: (_, _) => const RegisterPage()),
      GoRoute(path: '/welcome/family', builder: (_, _) => const FamilyStepPage()),
      ShellRoute(
        builder: (_, state, child) => FaChrome(location: state.uri.toString(), child: child),
        routes: [
          GoRoute(
            path: '/home',
            pageBuilder: (_, s) => NoTransitionPage(key: s.pageKey, child: const HomePage()),
          ),
          GoRoute(
            path: '/favorites',
            pageBuilder: (_, s) => NoTransitionPage(key: s.pageKey, child: const FavoritesPage()),
          ),
          GoRoute(
            path: '/ingredients',
            pageBuilder: (_, s) => NoTransitionPage(key: s.pageKey, child: const IngredientsPage()),
          ),
          GoRoute(
            path: '/orders',
            pageBuilder: (_, s) => NoTransitionPage(key: s.pageKey, child: const OrdersHistoryPage()),
          ),
          GoRoute(
            path: '/settings',
            pageBuilder: (_, s) => NoTransitionPage(key: s.pageKey, child: const SettingsPage()),
          ),
          GoRoute(path: '/meals', builder: (_, _) => const MealsPage()),
          GoRoute(
            path: '/meals/:id',
            builder: (_, s) {
              final q = s.uri.queryParameters;
              return MealItemsPage(
                mealId: s.pathParameters['id']!,
                title: emptyToNull(q['name']),
                initialQuery: ItemQuery(
                  text: q['text'] ?? '',
                  cuisineId: emptyToNull(q['cuisineId']),
                  categoryId: emptyToNull(q['categoryId']),
                  ingredient: emptyToNull(q['ingredient']),
                  kitchenId: emptyToNull(q['kitchenId']),
                  groupId: emptyToNull(q['groupId']),
                  mealId: emptyToNull(q['filterMealId']),
                ),
              );
            },
          ),
          GoRoute(path: '/cuisines', builder: (_, _) => const CuisinesPage()),
          GoRoute(path: '/categories', builder: (_, _) => const CuisinesPage(kind: BrowseKind.category)),
          GoRoute(path: '/groups', builder: (_, _) => const CuisinesPage(kind: BrowseKind.group)),
          GoRoute(
            path: '/items/:id',
            builder: (_, s) => ItemDetailsPage(
              itemId: s.pathParameters['id']!,
              cuisineId: s.uri.queryParameters['cuisine'],
              countryId: s.uri.queryParameters['country'],
            ),
          ),
          GoRoute(
            path: '/orders/:id',
            builder: (_, s) => OrderDetailsPage(orderId: s.pathParameters['id']!),
          ),
          GoRoute(path: '/notifications', builder: (_, _) => const NotificationsPage()),
          GoRoute(path: '/prices', builder: (_, _) => const PricesPage()),
        ],
      ),
    ],
  );
});
