import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../app/providers.dart';
import '../domain/catalog_models.dart';
import '../../prices/prices.dart';

final mealsProvider = FutureProvider.autoDispose<List<MealCard>>((ref) => ref.watch(catalogRepositoryProvider).meals());
final cuisinesProvider = FutureProvider.autoDispose<List<FilterChipData>>((ref) => ref.watch(catalogRepositoryProvider).cuisines());

/// How many dishes the customer can see in all (the cuisines' "الكل" entry), for the "الكل" tile of every browse kind.
final browseTotalProvider = FutureProvider.autoDispose<int>((ref) async {
  final all = await ref.watch(cuisinesProvider.future);
  for (final c in all) {
    if ((c.id ?? '').isEmpty) return c.count;
  }
  return all.fold<int>(0, (a, c) => a + c.count);
});
final groupsProvider = FutureProvider.autoDispose<List<FilterChipData>>((ref) => ref.watch(catalogRepositoryProvider).groups());
final categoriesProvider = FutureProvider.autoDispose<List<FilterChipData>>((ref) => ref.watch(catalogRepositoryProvider).categories());
final mealFiltersProvider = FutureProvider.autoDispose.family<List<FilterChipData>, String>(
  (ref, mealId) => ref.watch(catalogRepositoryProvider).mealFilters(mealId),
);
final favoriteFiltersProvider = FutureProvider.autoDispose<List<FilterChipData>>(
  (ref) => ref.watch(catalogRepositoryProvider).favoriteFilters(),
);
/// Which dish, and which country version of it: the cuisine it was opened from, or an exact country.
typedef ItemKey = ({String id, String? cuisineId, String? countryId});

final itemDetailProvider = FutureProvider.autoDispose.family<ItemDetail, ItemKey>(
  (ref, key) {
    ref.watch(pricesVersionProvider);
    return ref.watch(catalogRepositoryProvider).item(key.id, cuisineId: key.cuisineId, countryId: key.countryId);
  },
);

/// Favorite changes made in this session, so every screen shows the same heart.
final favoriteOverridesProvider = NotifierProvider<FavoriteOverrides, Map<String, bool>>(FavoriteOverrides.new);

class FavoriteOverrides extends Notifier<Map<String, bool>> {
  @override
  Map<String, bool> build() => const {};

  bool isFavorite(String id, bool fromServer) => state[id] ?? fromServer;

  Future<void> toggle(String id, bool current) async {
    final next = !current;
    state = {...state, id: next};
    try {
      await ref.read(catalogRepositoryProvider).setFavorite(id, next);
    } catch (e) {
      state = {...state, id: current};
      rethrow;
    }
  }
}

/// What a browse screen / home lane lists: cuisines (المطبخ), categories (التصنيف) or groups (المجموعة).
enum BrowseKind {
  cuisine('cuisineId', 'اختر المطبخ', 'تصفح المطابخ واختر ما يناسبك من الأكلات', 'لا توجد مطابخ حالياً'),
  category('categoryId', 'اختر التصنيف', 'تصفح التصنيفات واختر ما يناسبك من الأكلات', 'لا توجد تصنيفات حالياً'),
  group('groupId', 'اختر المجموعة', 'تصفح المجموعات واختر ما يناسبك من الأكلات', 'لا توجد مجموعات حالياً');

  const BrowseKind(this.param, this.title, this.subtitle, this.empty);
  /// The meals list query parameter that filters by it.
  final String param;
  final String title;
  final String subtitle;
  final String empty;

  FutureProvider<List<FilterChipData>> get provider => switch (this) {
    BrowseKind.cuisine => cuisinesProvider,
    BrowseKind.category => categoriesProvider,
    BrowseKind.group => groupsProvider,
  };
}
