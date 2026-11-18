import 'package:intl/intl.dart';

import '../../../core/network/api_client.dart';
import '../../../core/utils/json.dart';
import '../../cart/domain/selected_item.dart';
import '../../cart/domain/shopping_line.dart';
import '../domain/order_models.dart';
import '../domain/orders_repository.dart';

class OrdersRepositoryImpl implements OrdersRepository {
  OrdersRepositoryImpl(this._api);

  final ApiClient _api;
  static final _day = DateFormat('yyyy-MM-dd');

  @override
  Future<List<ShoppingLine>> calculate(List<SelectedItem> items) =>
      _api.post('/customer/orders/calculate', body: items.map((i) => i.toRequest()).toList(), parse: (j) => listOf(j, ShoppingLine.fromJson));

  @override
  Future<List<OrderListItem>> orders() => _api.get('/customer/orders', parse: (j) => listOf(j, OrderListItem.fromJson));

  @override
  Future<OrderDetail> order(String id) => _api.get('/customer/orders/$id', parse: (j) => OrderDetail.fromJson(Map<String, dynamic>.from(j as Map)));

  @override
  Future<String> saveOrder({
    String? id,
    required String name,
    required String date,
    String notes = '',
    required List<SelectedItem> items,
    required List<ShoppingLine> lines,
    bool confirm = false,
  }) {
    final body = {
      'id': id,
      'name': name,
      'date': date,
      'notes': notes,
      'confirm': confirm,
      'items': items.map((i) => i.toRequest()).toList(),
      'lines': lines.map((l) => {'contentId': l.contentId, 'unitId': l.unitId, 'quantity': l.quantity}).toList(),
    };
    // Confirmed purchase orders are always created as a new record.
    if (id != null && !confirm) return _api.put('/customer/orders/$id', body: body, parse: (j) => '$j');
    return _api.post('/customer/orders', body: body, parse: (j) => '$j');
  }

  @override
  Future<OrderHistoryPage> history(OrderHistoryFilter filter, {int skip = 0, int take = 20}) => _api.get(
    '/customer/orders/history',
    query: {
      'skip': skip,
      'take': take,
      'text': filter.text.trim(),
      'state': filter.state.code,
      'from': filter.from == null ? null : _day.format(filter.from!),
      'to': filter.to == null ? null : _day.format(filter.to!),
      if (filter.name.isNotEmpty) 'name': filter.name,
      if (filter.dish.isNotEmpty) 'dish': filter.dish,
    },
    parse: (j) => OrderHistoryPage.fromJson(Map<String, dynamic>.from(j as Map)),
  );

  @override
  Future<void> markShared(String id) => _api.post('/customer/orders/$id/shared', parse: (_) {});

  @override
  Future<void> markPrinted(String id) => _api.post('/customer/orders/$id/printed', parse: (_) {});

  @override
  Future<void> markReordered(String id) => _api.post('/customer/orders/$id/reordered', parse: (_) {});

  @override
  Future<void> rename(String id, String name) => _api.put('/customer/orders/$id/name', body: {'name': name}, parse: (_) {});
}
