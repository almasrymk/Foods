import 'package:intl/intl.dart';

import '../../cart/domain/purchase_rules.dart';
import '../domain/order_models.dart';

String formatDay(String iso) {
  final d = DateTime.tryParse(iso);
  if (d == null) return iso;
  return DateFormat('d/M/yyyy').format(d);
}

/// Plain text of a purchase order, grouped by where to buy, for WhatsApp etc.
String orderShareText(OrderDetail order) {
  final b = StringBuffer()
    ..writeln('🛒 ${order.name?.isNotEmpty == true ? order.name : 'طلب شراء'}')
    ..writeln('📅 ${formatDay(order.date)}')
    ..writeln();
  final groups = <String, List<String>>{};
  for (final l in order.lines.where((l) => l.quantity > 0)) {
    final place = l.purchasePlaceName?.isNotEmpty == true ? l.purchasePlaceName! : 'البقالة';
    groups.putIfAbsent(place, () => []).add('• ${l.name ?? ''}: ${PurchaseRules.text(l)}');
  }
  groups.forEach((place, rows) {
    b.writeln('📍 $place');
    rows.forEach(b.writeln);
    b.writeln();
  });
  if (order.items.isNotEmpty) {
    b.writeln('🍽 الأكلات:');
    for (final i in order.items) {
      b.writeln('• ${i.name}${i.repeatCount > 1 ? ' × ${i.repeatCount}' : ''}');
    }
  }
  if ((order.notes ?? '').isNotEmpty)
    b
      ..writeln()
      ..writeln('📝 ${order.notes}');
  return b.toString().trim();
}
