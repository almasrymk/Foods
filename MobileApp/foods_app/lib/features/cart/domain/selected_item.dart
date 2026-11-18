import '../../../core/utils/json.dart';

/// A dish the customer picked, and how many times it will be cooked.
class SelectedItem {
  SelectedItem({required this.itemId, required this.name, this.imagePath, required this.repeatCount, this.peopleCount, this.price, this.countryId});

  /// The country version of the dish (another country's cuisine); null = the customer's own.
  String? countryId;

  /// Dish price in this order (null = prices off).
  double? price;

  final String itemId;
  String name;
  String? imagePath;
  int repeatCount;
  int? peopleCount;

  factory SelectedItem.fromJson(Map<String, dynamic> j) => SelectedItem(
    itemId: strOr(j['itemId']),
    name: strOr(j['name']),
    imagePath: str(j['imagePath']),
    repeatCount: intOf(j['repeatCount'], 1),
    peopleCount: intOrNull(j['peopleCount']),
    price: dblOrNull(j['price']),
    countryId: str(j['countryId']),
  );

  Map<String, dynamic> toRequest() => {'itemId': itemId, 'repeatCount': repeatCount, 'peopleCount': peopleCount, 'countryId': ?countryId};

  SelectedItem copy() => SelectedItem(itemId: itemId, name: name, imagePath: imagePath, repeatCount: repeatCount, peopleCount: peopleCount, price: price, countryId: countryId);
}
