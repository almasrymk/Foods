import '../../../core/network/api_client.dart';
import '../../../core/utils/json.dart';
import '../domain/catalog_models.dart';
import '../domain/catalog_repository.dart';

class CatalogRepositoryImpl implements CatalogRepository {
  CatalogRepositoryImpl(this._api);

  final ApiClient _api;

  List<FilterChipData> _chips(dynamic j) => listOf(j, FilterChipData.fromJson);

  @override
  Future<List<MealCard>> meals() => _api.get('/customer/meals', parse: (j) => listOf(j, MealCard.fromJson));

  @override
  Future<List<FilterChipData>> mealFilters(String mealId) => _api.get('/customer/meals/$mealId/filters', parse: _chips);

  @override
  Future<List<FilterChipData>> cuisines() => _api.get('/customer/cuisines', parse: _chips);

  @override
  Future<List<FilterChipData>> categories() => _api.get('/customer/categories', parse: _chips);

  @override
  Future<List<FilterChipData>> groups() => _api.get('/customer/groups', parse: _chips);

  @override
  Future<List<FilterChipData>> favoriteFilters() => _api.get('/customer/favorites/filters', parse: _chips);

  @override
  Future<List<ItemCard>> items(ItemSource source, ItemQuery query, {int skip = 0, int take = 20}) {
    final path = switch (source) {
      MealItemsSource(:final mealId) => '/customer/meals/$mealId/items',
      AllItemsSource() => '/customer/menu-items',
      SuggestedSource() => '/customer/suggested',
      FavoritesSource() => '/customer/favorites',
    };
    return _api.get(
      path,
      query: {...query.toQuery(), if (skip > 0) 'skip': skip, 'take': take},
      parse: (j) => listOf(j, ItemCard.fromJson),
    );
  }

  @override
  Future<ItemDetail> item(String id, {String? cuisineId, String? countryId}) =>
      _api.get('/customer/items/$id${_q({'cuisineId': cuisineId, 'countryId': countryId})}', parse: (j) => ItemDetail.fromJson(Map<String, dynamic>.from(j as Map)));

  @override
  Future<void> setFavorite(String id, bool favorite) async {
    if (favorite) {
      await _api.post('/customer/items/$id/favorite', parse: (_) => true);
    } else {
      await _api.delete('/customer/items/$id/favorite', parse: (_) => true);
    }
  }

  @override
  Future<MyItemRecipe> myItem(String id, {String? countryId}) =>
      _api.get('/customer/items/$id/mine${_q({'countryId': countryId})}', parse: (j) => MyItemRecipe.fromJson(Map<String, dynamic>.from(j as Map)));

  @override
  Future<void> setItemChoice(String id, int kind, String refId, bool selected, {String? countryId}) => _api.put(
    '/customer/items/$id/choice',
    body: {'kind': kind, 'refId': refId, 'selected': selected, 'countryId': ?countryId},
    parse: (_) {},
  );

  @override
  Future<void> saveMyItem(String id, List<Map<String, dynamic>> lines, List<Map<String, dynamic>> steps, {List<String>? parts, String? countryId}) => _api.put(
    '/customer/items/$id/mine',
    body: {'lines': lines, 'steps': steps, 'parts': ?parts, 'countryId': ?countryId},
    parse: (_) {},
  );

  @override
  Future<List<ItemPart>> partPicks(String id, String text, {int take = 12}) => _api.get(
    '/customer/items/$id/part-picks?text=${Uri.encodeQueryComponent(text)}&take=$take',
    parse: (j) => listOf(j, ItemPart.fromJson),
  );

  @override
  Future<void> resetMyItem(String id, {String? countryId}) =>
      _api.delete('/customer/items/$id/mine${_q({'countryId': countryId})}', parse: (_) {});

  /// "?a=1&b=2" from the values that are set ("" when none).
  static String _q(Map<String, String?> values) {
    final set = values.entries.where((e) => e.value != null && e.value!.isNotEmpty).toList();
    if (set.isEmpty) return '';
    return '?${set.map((e) => '${e.key}=${Uri.encodeQueryComponent(e.value!)}').join('&')}';
  }

  @override
  Future<ContentPickResult> searchContents(String text, {int take = 20}) => _api.get(
    '/customer/contents',
    query: {if (text.isNotEmpty) 'text': text, 'take': take},
    parse: (j) => ContentPickResult.fromJson(Map<String, dynamic>.from(j as Map)),
  );
}
