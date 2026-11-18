import '../../../core/utils/json.dart';

/// One ingredient to buy, with its purchase rules (أقل وحدة شراء).
class ShoppingLine {
  ShoppingLine({
    required this.contentId,
    required this.unitId,
    required this.quantity,
    this.name,
    this.imagePath,
    this.images = const [],
    this.purchasePlaceId,
    this.purchasePlaceName,
    this.unitName,
    this.purchaseMode = 0,
    this.packageName,
    this.purchaseStep = 0,
    this.purchaseIncrement = 0,
    this.purchaseUnitPrice = 0,
    this.purchaseMinAmount = 0,
    this.unitPrice,
  });

  /// Money of one unit of this line (null = no price or prices are off).
  final double? unitPrice;

  /// Money of the line at its current quantity.
  double? get cost => unitPrice == null ? null : (unitPrice! * quantity * 100).round() / 100;

  final String contentId;
  final String unitId;
  double quantity;
  final String? name;
  final String? imagePath;
  final List<String> images;
  final String? purchasePlaceId;
  final String? purchasePlaceName;
  final String? unitName;
  final int purchaseMode;
  final String? packageName;
  final double purchaseStep;
  final double purchaseIncrement;
  final double purchaseUnitPrice;
  final double purchaseMinAmount;

  String get key => '$contentId:$unitId';

  factory ShoppingLine.fromJson(Map<String, dynamic> j) => ShoppingLine(
    contentId: strOr(j['contentId']),
    unitId: strOr(j['unitId']),
    quantity: dbl(j['quantity']),
    name: str(j['name']),
    imagePath: str(j['imagePath']),
    images: strList(j['images']),
    purchasePlaceId: str(j['purchasePlaceId']),
    purchasePlaceName: str(j['purchasePlaceName']),
    unitName: str(j['unitName']),
    purchaseMode: intOf(j['purchaseMode']),
    packageName: str(j['packageName']),
    purchaseStep: dbl(j['purchaseStep']),
    purchaseIncrement: dbl(j['purchaseIncrement']),
    purchaseUnitPrice: dbl(j['purchaseUnitPrice']),
    purchaseMinAmount: dbl(j['purchaseMinAmount']),
    unitPrice: dblOrNull(j['unitPrice']),
  );

  Map<String, dynamic> toJson() => {
    'contentId': contentId,
    'unitId': unitId,
    'quantity': quantity,
    'name': name,
    'imagePath': imagePath,
    'images': images,
    'purchasePlaceId': purchasePlaceId,
    'purchasePlaceName': purchasePlaceName,
    'unitName': unitName,
    'purchaseMode': purchaseMode,
    'packageName': packageName,
    'purchaseStep': purchaseStep,
    'purchaseIncrement': purchaseIncrement,
    'purchaseUnitPrice': purchaseUnitPrice,
    'purchaseMinAmount': purchaseMinAmount,
    'unitPrice': unitPrice,
  };

  ShoppingLine copyWith({double? quantity}) => ShoppingLine.fromJson(toJson())..quantity = quantity ?? this.quantity;
}
