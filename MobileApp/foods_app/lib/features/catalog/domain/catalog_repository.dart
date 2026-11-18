import 'catalog_models.dart';

abstract interface class CatalogRepository {
  Future<List<MealCard>> meals();
  Future<List<FilterChipData>> mealFilters(String mealId);
  Future<List<FilterChipData>> cuisines();
  Future<List<FilterChipData>> categories();
  Future<List<FilterChipData>> groups();
  Future<List<FilterChipData>> favoriteFilters();
  Future<List<ItemCard>> items(ItemSource source, ItemQuery query, {int skip = 0, int take = 20});
  Future<ItemDetail> item(String id, {String? cuisineId, String? countryId});
  Future<void> setFavorite(String id, bool favorite);

  Future<MyItemRecipe> myItem(String id, {String? countryId});
  Future<void> saveMyItem(String id, List<Map<String, dynamic>> lines, List<Map<String, dynamic>> steps, {List<String>? parts, String? countryId});
  /// Dishes the customer can add as a part of this dish.
  Future<List<ItemPart>> partPicks(String id, String text, {int take = 12});
  Future<void> resetMyItem(String id, {String? countryId});
  /// Picks or drops an optional ingredient (kind 0, refId = contentId) or optional part (kind 1, refId = part dish id).
  Future<void> setItemChoice(String id, int kind, String refId, bool selected, {String? countryId});
  Future<ContentPickResult> searchContents(String text, {int take = 20});
}
