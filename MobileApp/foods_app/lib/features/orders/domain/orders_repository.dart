import '../../cart/domain/selected_item.dart';
import '../../cart/domain/shopping_line.dart';
import 'order_models.dart';

abstract interface class OrdersRepository {
  Future<List<ShoppingLine>> calculate(List<SelectedItem> items);
  Future<List<OrderListItem>> orders();
  Future<OrderDetail> order(String id);

  /// Creates (id null or confirm) or updates an order and returns its id.
  Future<String> saveOrder({
    String? id,
    required String name,
    required String date,
    String notes = '',
    required List<SelectedItem> items,
    required List<ShoppingLine> lines,
    bool confirm = false,
  });

  Future<OrderHistoryPage> history(OrderHistoryFilter filter, {int skip = 0, int take = 20});
  Future<void> markShared(String id);
  Future<void> markPrinted(String id);
  Future<void> markReordered(String id);
  Future<void> rename(String id, String name);
}
