import '../../../core/utils/json.dart';

/// A meal type: breakfast, lunch, dinner...
class MealCard {
  MealCard({required this.id, this.name, this.description, this.imagePath, this.iconPath, this.color, this.textColor, this.count = 0});

  /// Dishes in this meal the customer can see.
  final int count;

  final String id;
  final String? name;
  final String? description;
  final String? imagePath;
  final String? iconPath;
  final String? color;
  final String? textColor;

  factory MealCard.fromJson(Map<String, dynamic> j) => MealCard(
    id: strOr(j['id']),
    name: str(j['name']),
    description: str(j['description']),
    imagePath: str(j['imagePath']),
    iconPath: str(j['iconPath']),
    color: str(j['color']),
    textColor: str(j['textColor']),
    count: intOf(j['count']),
  );
}

/// A dish in a list.
class ItemCard {
  ItemCard({
    required this.id,
    this.name,
    this.summary,
    this.imagePath,
    this.isFavorite = false,
    this.countryName,
    this.categoryName,
    this.cuisineId,
    this.cuisineName,
    this.groupName,
    this.images = const [],
    this.price,
    this.priceMissing = 0,
    this.recipeCountryId,
  });

  /// Set when the dish is shown in another country's version (that cuisine's country).
  final String? recipeCountryId;

  /// Dish price for the family (null = the customer did not turn prices on).
  final double? price;
  /// Ingredients of the dish with no price yet.
  final int priceMissing;

  final String id;
  final String? name;
  final String? summary;
  final String? imagePath;
  bool isFavorite;
  final String? countryName;
  final String? categoryName;
  final String? cuisineId;
  final String? cuisineName;
  final String? groupName;
  final List<String> images;

  factory ItemCard.fromJson(Map<String, dynamic> j) => ItemCard(
    id: strOr(j['id']),
    name: str(j['name']),
    summary: str(j['summary']),
    imagePath: str(j['imagePath']),
    isFavorite: boolOf(j['isFavorite']),
    countryName: str(j['countryName']),
    categoryName: str(j['categoryName']),
    cuisineId: str(j['cuisineId']),
    cuisineName: str(j['cuisineName']),
    groupName: str(j['groupName']),
    images: strList(j['images']),
    price: dblOrNull(j['price']),
    priceMissing: intOf(j['priceMissing']),
    recipeCountryId: str(j['recipeCountryId']),
  );
}

/// The price used for an ingredient: [price] EGP for [quantity] [unitName].
class PriceInfo {
  PriceInfo({required this.price, required this.quantity, required this.unitId, this.unitName, this.isMine = false});
  final double price;
  final double quantity;
  final String unitId;
  final String? unitName;
  /// The customer's own price (not the admin one).
  final bool isMine;

  static PriceInfo? fromJson(dynamic v) {
    if (v is! Map) return null;
    final j = Map<String, dynamic>.from(v);
    return PriceInfo(price: dbl(j['price']), quantity: dbl(j['quantity'], 1), unitId: strOr(j['unitId']), unitName: str(j['unitName']), isMine: boolOf(j['isMine']));
  }
}

/// Dish price: [total] for the family, [perPerson] for one, [missing] ingredients without a price.
class DishPrice {
  DishPrice({this.total = 0, this.perPerson = 0, this.people = 1, this.missing = 0});
  final double total;
  final double perPerson;
  final double people;
  final int missing;

  static DishPrice? fromJson(dynamic v) {
    if (v is! Map) return null;
    final j = Map<String, dynamic>.from(v);
    return DishPrice(total: dbl(j['total']), perPerson: dbl(j['perPerson']), people: dbl(j['people'], 1), missing: intOf(j['missing']));
  }
}

/// A filter chip (cuisine, category...) with how many dishes it has.
class FilterChipData {
  FilterChipData({this.id, required this.name, this.count = 0, this.iconPath, this.color, this.textColor, this.imagePath, this.isMine = false});

  /// A cuisine of the customer's own country (listed first; a meal screen starts on it).
  final bool isMine;

  final String? id;
  final String name;
  final int count;
  final String? iconPath;
  final String? color;
  final String? textColor;
  final String? imagePath;

  factory FilterChipData.fromJson(Map<String, dynamic> j) => FilterChipData(
    id: str(j['id']),
    name: strOr(j['name']),
    count: intOf(j['count']),
    iconPath: str(j['iconPath']),
    color: str(j['color']),
    textColor: str(j['textColor']),
    imagePath: str(j['imagePath']),
    isMine: boolOf(j['isMine']),
  );

  /// A cuisine's picture: the image, or an icon path that is really an image.
  String? get picture {
    if ((imagePath ?? '').isNotEmpty) return imagePath;
    final p = iconPath ?? '';
    if (p.contains('/') || p.startsWith('http')) return p;
    return null;
  }
}

class ItemIngredient {
  ItemIngredient({
    required this.contentId,
    this.name,
    this.imagePath,
    required this.unitId,
    this.unitName,
    this.quantity = 0,
    this.purchasePlaceName,
    this.isAdded = false,
    this.isChanged = false,
    this.images = const [],
    this.isOptional = false,
    this.selected = true,
    this.cost,
    this.priceInfo,
  });

  /// Money of [quantity] (per person); null = no price or prices are off.
  final double? cost;
  final PriceInfo? priceInfo;

  final String contentId;
  /// Optional ingredient: the customer can pick or drop it.
  final bool isOptional;
  /// Picked (always true for a required ingredient).
  bool selected;
  final String? name;
  final String? imagePath;
  final List<String> images;
  final String unitId;
  final String? unitName;
  final double quantity;
  final String? purchasePlaceName;
  final bool isAdded;
  final bool isChanged;

  factory ItemIngredient.fromJson(Map<String, dynamic> j) => ItemIngredient(
    contentId: strOr(j['contentId']),
    name: str(j['name']),
    imagePath: str(j['imagePath']),
    unitId: strOr(j['unitId']),
    unitName: str(j['unitName']),
    quantity: dbl(j['quantity']),
    purchasePlaceName: str(j['purchasePlaceName']),
    isAdded: boolOf(j['isAdded']),
    isChanged: boolOf(j['isChanged']),
    images: strList(j['images']),
    isOptional: boolOf(j['isOptional']),
    selected: j['selected'] == null ? true : boolOf(j['selected']),
    cost: dblOrNull(j['cost']),
    priceInfo: PriceInfo.fromJson(j['priceInfo']),
  );
}

/// صنف: another dish that is part of this dish.
class ItemPart {
  ItemPart({
    required this.itemId,
    this.name,
    this.summary,
    this.imagePath,
    this.isOptional = false,
    this.selected = true,
    this.canOpen = false,
    this.ingredientCount = 0,
    this.isAdded = false,
  });

  /// Added by the customer in "تعديل الأكلة ليا".
  final bool isAdded;

  final String itemId;
  final String? name;
  final String? summary;
  final String? imagePath;
  final bool isOptional;
  bool selected;
  /// The part's own details page is available to this customer.
  final bool canOpen;
  final int ingredientCount;

  factory ItemPart.fromJson(Map<String, dynamic> j) => ItemPart(
    itemId: strOr(j['itemId']),
    name: str(j['name']),
    summary: str(j['summary']),
    imagePath: str(j['imagePath']),
    isOptional: boolOf(j['isOptional']),
    selected: j['selected'] == null ? true : boolOf(j['selected']),
    canOpen: boolOf(j['canOpen']),
    ingredientCount: dbl(j['ingredientCount']).round(),
    isAdded: boolOf(j['isAdded']),
  );
}

class ItemStep {
  ItemStep({required this.sort, this.body});
  final int sort;
  final String? body;
  factory ItemStep.fromJson(Map<String, dynamic> j) => ItemStep(sort: intOf(j['sort']), body: str(j['body']));
}

class ItemVideo {
  ItemVideo({required this.sort, this.title, this.url});
  final int sort;
  final String? title;
  final String? url;
  factory ItemVideo.fromJson(Map<String, dynamic> j) => ItemVideo(sort: intOf(j['sort']), title: str(j['title']), url: str(j['url']));
}

class ItemDetail {
  ItemDetail({
    required this.id,
    this.name,
    this.summary,
    this.description,
    this.imagePath,
    this.isFavorite = false,
    this.countryName,
    this.ingredients = const [],
    this.steps = const [],
    this.videos = const [],
    this.images = const [],
    this.isCustomized = false,
    this.groupName,
    this.categoryName,
    this.parts = const [],
    this.price,
    this.recipeCountryId,
    this.recipeCountryName,
    this.isOtherCountry = false,
  });

  /// The country whose version is shown, and whether it is not the customer's own.
  final String? recipeCountryId;
  final String? recipeCountryName;
  final bool isOtherCountry;

  /// Null when the customer did not turn prices on.
  final DishPrice? price;

  /// أصناف: other dishes inside this dish.
  final List<ItemPart> parts;

  final String id;
  final String? name;
  final String? summary;
  final String? description;
  final String? groupName;
  final String? categoryName;
  final String? imagePath;
  bool isFavorite;
  final String? countryName;
  final List<ItemIngredient> ingredients;
  final List<ItemStep> steps;
  final List<ItemVideo> videos;
  final List<String> images;
  final bool isCustomized;

  factory ItemDetail.fromJson(Map<String, dynamic> j) => ItemDetail(
    id: strOr(j['id']),
    name: str(j['name']),
    summary: str(j['summary']),
    description: str(j['description']),
    imagePath: str(j['imagePath']),
    isFavorite: boolOf(j['isFavorite']),
    countryName: str(j['countryName']),
    ingredients: listOf(j['ingredients'], ItemIngredient.fromJson),
    steps: listOf(j['steps'], ItemStep.fromJson)..sort((a, b) => a.sort.compareTo(b.sort)),
    videos: listOf(j['videos'], ItemVideo.fromJson)..sort((a, b) => a.sort.compareTo(b.sort)),
    images: strList(j['images']),
    isCustomized: boolOf(j['isCustomized']),
    groupName: str(j['groupName']),
    categoryName: str(j['categoryName']),
    parts: listOf(j['parts'], ItemPart.fromJson),
    price: DishPrice.fromJson(j['price']),
    recipeCountryId: str(j['recipeCountryId']),
    recipeCountryName: str(j['recipeCountryName']),
    isOtherCountry: boolOf(j['isOtherCountry']),
  );
}

/// Search options shared by every dish list.
class ItemQuery {
  const ItemQuery({this.text = '', this.cuisineId, this.categoryId, this.ingredient, this.kitchenId, this.groupId, this.mealId});

  final String text;

  /// Cuisine chip of the screen itself.
  final String? cuisineId;
  final String? categoryId;
  final String? ingredient;

  /// Advanced search: المطبخ / المجموعة / الوجبة.
  final String? kitchenId;
  final String? groupId;
  final String? mealId;

  bool get hasExtra => categoryId != null || (ingredient ?? '').trim().isNotEmpty || kitchenId != null || groupId != null || mealId != null;

  ItemQuery copyWith({
    String? text,
    String? Function()? cuisineId,
    String? Function()? categoryId,
    String? Function()? ingredient,
    String? Function()? kitchenId,
    String? Function()? groupId,
    String? Function()? mealId,
  }) => ItemQuery(
    text: text ?? this.text,
    cuisineId: cuisineId != null ? cuisineId() : this.cuisineId,
    categoryId: categoryId != null ? categoryId() : this.categoryId,
    ingredient: ingredient != null ? ingredient() : this.ingredient,
    kitchenId: kitchenId != null ? kitchenId() : this.kitchenId,
    groupId: groupId != null ? groupId() : this.groupId,
    mealId: mealId != null ? mealId() : this.mealId,
  );

  /// Drops the advanced filters, keeps the text and the screen's cuisine chip.
  ItemQuery clearExtra() => ItemQuery(text: text, cuisineId: cuisineId);

  /// categoryId may hold several ids joined by commas (a dish in any of them matches).
  Map<String, dynamic> toQuery() => {
    'text': text.trim(),
    'cuisineId': cuisineId,
    'categoryIds': categoryId,
    'ingredient': ingredient,
    'kitchenId': kitchenId,
    'groupId': groupId,
    'filterMealId': mealId,
  };

  /// Route query (home search → /meals/all).
  Map<String, String> toRoute() => {
    if (text.trim().isNotEmpty) 'text': text.trim(),
    if (categoryId != null) 'categoryId': categoryId!,
    if ((ingredient ?? '').trim().isNotEmpty) 'ingredient': ingredient!.trim(),
    if (kitchenId != null) 'kitchenId': kitchenId!,
    if (groupId != null) 'groupId': groupId!,
    if (mealId != null) 'filterMealId': mealId!,
  };

  @override
  bool operator ==(Object other) =>
      other is ItemQuery &&
      other.text == text &&
      other.cuisineId == cuisineId &&
      other.categoryId == categoryId &&
      other.ingredient == ingredient &&
      other.kitchenId == kitchenId &&
      other.groupId == groupId &&
      other.mealId == mealId;

  @override
  int get hashCode => Object.hash(text, cuisineId, categoryId, ingredient, kitchenId, groupId, mealId);
}

/// Which list of dishes to page through.
sealed class ItemSource {
  const ItemSource();
}

/// Dishes of one meal type.
class MealItemsSource extends ItemSource {
  const MealItemsSource(this.mealId);
  final String mealId;
  @override
  bool operator ==(Object other) => other is MealItemsSource && other.mealId == mealId;
  @override
  int get hashCode => mealId.hashCode;
}

/// Every dish on the menu.
class AllItemsSource extends ItemSource {
  const AllItemsSource();
  @override
  bool operator ==(Object other) => other is AllItemsSource;
  @override
  int get hashCode => 1;
}

/// Suggested dishes on the home page.
class SuggestedSource extends ItemSource {
  const SuggestedSource();
  @override
  bool operator ==(Object other) => other is SuggestedSource;
  @override
  int get hashCode => 2;
}

/// The customer's favorite dishes.
class FavoritesSource extends ItemSource {
  const FavoritesSource();
  @override
  bool operator ==(Object other) => other is FavoritesSource;
  @override
  int get hashCode => 3;
}

/// The customer's own version of a dish (GET/PUT/DELETE /customer/items/{id}/mine).
class MyItemRecipe {
  MyItemRecipe({this.isCustomized = false, this.lines = const [], this.removed = const [], this.steps = const [], this.parts = const []});
  /// All parts of the customer's version (selected = kept).
  final List<ItemPart> parts;
  final bool isCustomized;
  final List<ItemIngredient> lines;
  final List<ItemIngredient> removed;
  final List<ItemStep> steps;

  factory MyItemRecipe.fromJson(Map<String, dynamic> j) => MyItemRecipe(
    isCustomized: boolOf(j['isCustomized']),
    lines: listOf(j['lines'], ItemIngredient.fromJson),
    removed: listOf(j['removed'], ItemIngredient.fromJson),
    steps: listOf(j['steps'], ItemStep.fromJson)..sort((a, b) => a.sort.compareTo(b.sort)),
    parts: listOf(j['parts'], ItemPart.fromJson),
  );
}

class ContentPick {
  ContentPick({required this.id, this.name, this.imagePath, required this.unitId, this.unitName, this.miniQuantity = 0});
  final String id;
  final String? name;
  final String? imagePath;
  final String unitId;
  final String? unitName;
  final double miniQuantity;

  factory ContentPick.fromJson(Map<String, dynamic> j) => ContentPick(
    id: strOr(j['id']),
    name: str(j['name']),
    imagePath: str(j['imagePath']),
    unitId: strOr(j['unitId']),
    unitName: str(j['unitName']),
    miniQuantity: dbl(j['miniQuantity']),
  );
}

class UnitPick {
  UnitPick({required this.id, this.name});
  final String id;
  final String? name;
  factory UnitPick.fromJson(Map<String, dynamic> j) => UnitPick(id: strOr(j['id']), name: str(j['name']));
}

class ContentPickResult {
  ContentPickResult({this.items = const [], this.units = const []});
  final List<ContentPick> items;
  final List<UnitPick> units;
  factory ContentPickResult.fromJson(Map<String, dynamic> j) =>
      ContentPickResult(items: listOf(j['items'], ContentPick.fromJson), units: listOf(j['units'], UnitPick.fromJson));
}
