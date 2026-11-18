import '../../../core/utils/json.dart';
import '../../cart/domain/selected_item.dart';
import '../../cart/domain/shopping_line.dart';

class OrderListItem {
  OrderListItem({required this.id, this.name, required this.date, this.status = 0});
  final String id;
  final String? name;
  final String date;
  final int status;
  factory OrderListItem.fromJson(Map<String, dynamic> j) =>
      OrderListItem(id: strOr(j['id']), name: str(j['name']), date: strOr(j['date']), status: intOf(j['status']));
}

class OrderDetail {
  OrderDetail({required this.id, this.name, required this.date, this.notes, this.status = 0, this.items = const [], this.lines = const [], this.total, this.priceMissing = 0});
  /// Order money from the current prices (null = prices off).
  final double? total;
  final int priceMissing;
  final String id;
  final String? name;
  final String date;
  final String? notes;
  final int status;
  final List<SelectedItem> items;
  final List<ShoppingLine> lines;

  factory OrderDetail.fromJson(Map<String, dynamic> j) => OrderDetail(
    id: strOr(j['id']),
    name: str(j['name']),
    date: strOr(j['date']),
    notes: str(j['notes']),
    status: intOf(j['status']),
    items: listOf(j['items'], SelectedItem.fromJson),
    lines: listOf(j['lines'], ShoppingLine.fromJson),
    total: dblOrNull(j['total']),
    priceMissing: intOf(j['priceMissing']),
  );
}

/// Old-orders state: created / printed / reordered / shared.
enum OrderState {
  all('', 'الكل'),
  created('created', 'اتعمل'),
  printed('printed', 'اتطبع'),
  reordered('reordered', 'اتعاد طلبه'),
  shared('shared', 'اتشارك'),
  reprinted('reprinted', 'اتطبع تاني'),
  reshared('reshared', 'اتشارك تاني');

  const OrderState(this.code, this.label);
  final String code;
  final String label;

  static OrderState parse(String? code) => OrderState.values.firstWhere((s) => s.code == code, orElse: () => OrderState.created);
}

class OrderHistoryDish {
  OrderHistoryDish({required this.itemId, this.name, this.imagePath, this.repeatCount = 1, this.summary});
  final String itemId;
  final String? name;
  final String? imagePath;
  final int repeatCount;
  final String? summary;
  factory OrderHistoryDish.fromJson(Map<String, dynamic> j) => OrderHistoryDish(
    itemId: strOr(j['itemId']),
    name: str(j['name']),
    imagePath: str(j['imagePath']),
    repeatCount: intOf(j['repeatCount'], 1),
    summary: str(j['summary']),
  );
}

class OrderHistoryItem {
  OrderHistoryItem({
    required this.id,
    this.name,
    this.code,
    required this.date,
    this.createdAt,
    required this.state,
    this.dishCount = 0,
    this.lineCount = 0,
    this.dishes = const [],
    this.printedAt,
    this.sharedAt,
    this.total,
  });
  /// Order money (null = prices off).
  final double? total;
  final String id;
  String? name;
  String? printedAt;
  String? sharedAt;
  final String? code;
  final String date;
  final String? createdAt;
  OrderState state;
  final int dishCount;
  final int lineCount;
  final List<OrderHistoryDish> dishes;

  factory OrderHistoryItem.fromJson(Map<String, dynamic> j) => OrderHistoryItem(
    id: strOr(j['id']),
    name: str(j['name']),
    code: str(j['code']),
    date: strOr(j['date']),
    createdAt: str(j['createdAt']),
    state: OrderState.parse(str(j['state'])),
    dishCount: intOf(j['dishCount']),
    lineCount: intOf(j['lineCount']),
    dishes: listOf(j['dishes'], OrderHistoryDish.fromJson),
    printedAt: str(j['printedAt']),
    sharedAt: str(j['sharedAt']),
    total: dblOrNull(j['total']),
  );
}

class OrderHistoryPage {
  OrderHistoryPage({required this.items, required this.total, required this.hasMore, required this.counts});
  final List<OrderHistoryItem> items;
  final int total;
  final bool hasMore;
  final Map<OrderState, int> counts;

  factory OrderHistoryPage.fromJson(Map<String, dynamic> j) {
    final c = j['counts'] is Map ? Map<String, dynamic>.from(j['counts'] as Map) : const <String, dynamic>{};
    return OrderHistoryPage(
      items: listOf(j['items'], OrderHistoryItem.fromJson),
      total: intOf(j['total']),
      hasMore: boolOf(j['hasMore']),
      counts: {
        OrderState.all: intOf(c['all']),
        OrderState.created: intOf(c['created']),
        OrderState.printed: intOf(c['printed']),
        OrderState.reordered: intOf(c['reordered']),
        OrderState.shared: intOf(c['shared']),
        OrderState.reprinted: intOf(c['reprinted']),
        OrderState.reshared: intOf(c['reshared']),
      },
    );
  }
}

class OrderHistoryFilter {
  const OrderHistoryFilter({this.text = '', this.state = OrderState.all, this.from, this.to, this.name = '', this.dish = ''});
  final String text;
  final OrderState state;
  final DateTime? from;
  final DateTime? to;

  /// Advanced search: order name (or code) only, and a dish inside the order.
  final String name;
  final String dish;

  bool get hasAdvanced => from != null || to != null || name.isNotEmpty || dish.isNotEmpty;

  OrderHistoryFilter copyWith({String? text, OrderState? state}) =>
      OrderHistoryFilter(text: text ?? this.text, state: state ?? this.state, from: from, to: to, name: name, dish: dish);
}
