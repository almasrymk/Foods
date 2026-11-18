import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../app/providers.dart';
import '../../../app/widgets/fa_chrome.dart';
import '../../../app/widgets/fa_top_bar.dart';
import '../../../app/widgets/fa_widgets.dart';
import '../../../core/ui/fa_kit.dart';
import '../../../core/widgets/async_views.dart';
import '../../../core/widgets/net_image.dart';
import '../../cart/domain/purchase_rules.dart';
import '../../cart/domain/shopping_line.dart';
import '../../cart/presentation/cart_controller.dart';
import '../domain/order_models.dart';
import 'order_print.dart';
import '../../prices/prices.dart';

export 'order_print.dart' show printOrder, shareOrder;

final orderDetailProvider = FutureProvider.autoDispose.family<OrderDetail, String>((ref, id) {
  ref.watch(pricesVersionProvider);
  return ref.watch(ordersRepositoryProvider).order(id);
});

const _leaf =
    'M6.05 8.05a7.001 7.001 0 0 0-.02 9.88c1.47-3.4 4.09-6.24 7.36-7.93A15.952 15.952 0 0 0 8 19.32c2.6 1.23 5.8.78 7.95-1.37C19.43 14.47 20 4 20 4S9.53 4.57 6.05 8.05z';
const _basket =
    'M17.21 9l-4.38-6.56a.993.993 0 0 0-.83-.42c-.32 0-.64.14-.83.43L6.79 9H2c-.55 0-1 .45-1 1 0 .09.01.18.04.27l2.54 9.27c.23.84 1 1.46 1.92 1.46h13c.92 0 1.69-.62 1.93-1.46l2.54-9.27L23 10c0-.55-.45-1-1-1h-4.79zM9 9l3-4.4L15 9H9z';
const _group =
    'M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z';
const _cloche = 'M12 6c-4.4 0-8 3.1-8.7 7h17.4C20 9.1 16.4 6 12 6zm0-1a1 1 0 1 0 0-2 1 1 0 0 0 0 2zM2 15h20v2H2z';
const _printer =
    'M19 8h-1V3H6v5H5c-1.66 0-3 1.34-3 3v6h4v4h12v-4h4v-6c0-1.66-1.34-3-3-3zM8 5h8v3H8V5zm8 12v2H8v-4h8v2zm2-2v-2H6v2H4v-4c0-.55.45-1 1-1h14c.55 0 1 .45 1 1v4h-2z';
const _shareP =
    'M18 16.08c-.76 0-1.44.3-1.96.77L8.91 12.7c.05-.23.09-.46.09-.7s-.04-.47-.09-.7l7.05-4.11c.54.5 1.25.81 2.04.81 1.66 0 3-1.34 3-3s-1.34-3-3-3-3 1.34-3 3c0 .24.04.47.09.7L8.04 9.81C7.5 9.31 6.79 9 6 9c-1.66 0-3 1.34-3 3s1.34 3 3 3c.79 0 1.5-.31 2.04-.81l7.12 4.16c-.05.21-.08.43-.08.65 0 1.61 1.31 2.92 2.92 2.92s2.92-1.31 2.92-2.92-1.31-2.92-2.92-2.92z';
const _chev = 'M15.41 7.41 14 6l-6 6 6 6 1.41-1.41L10.83 12z';

List<(String, List<ShoppingLine>)> _groups(OrderDetail o) {
  final map = <String, List<ShoppingLine>>{};
  for (final l in o.lines) {
    if (!(l.quantity > 0)) continue;
    final p = (l.purchasePlaceName ?? '').trim();
    map.putIfAbsent(p.isEmpty ? 'البقالة' : p, () => []).add(l);
  }
  return [for (final e in map.entries) (e.key, e.value)];
}

/// "طلب الشراء" (order.component.ts), sizes in 841px units.
class OrderDetailsPage extends ConsumerStatefulWidget {
  const OrderDetailsPage({super.key, required this.orderId, this.autoPrint = false});
  final String orderId;
  final bool autoPrint;

  @override
  ConsumerState<OrderDetailsPage> createState() => _OrderDetailsPageState();
}

class _OrderDetailsPageState extends ConsumerState<OrderDetailsPage> {
  bool _printed = false;

  @override
  Widget build(BuildContext context) {
    final u = context.u;
    final order = ref.watch(orderDetailProvider(widget.orderId));
    if (widget.autoPrint && !_printed && order.hasValue) {
      _printed = true;
      WidgetsBinding.instance.addPostFrameCallback((_) => printOrder(ref, order.value!));
    }
    Widget navBtn(String d, VoidCallback onTap, double size) => Tap(
      onTap: onTap,
      child: Container(
        width: u(83),
        height: u(76),
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: const Color(0xFFFFFDFA),
          borderRadius: BorderRadius.circular(u(20)),
          boxShadow: [BoxShadow(color: const Color(0x1F784820), offset: Offset(0, u(4)), blurRadius: u(14))],
        ),
        child: FaPath(d, size: u(size)),
      ),
    );
    return Directionality(
      textDirection: TextDirection.rtl,
      child: ColoredBox(
        color: const Color(0xFFFDF7F0),
        child: SafeArea(
          bottom: false,
          child: ListView(
            padding: EdgeInsets.only(bottom: faBottomSpace(context, ref) + u(10)),
            children: [
              const FaTopBar(),
              Container(
                height: u(76),
                margin: EdgeInsets.fromLTRB(u(30), u(9), u(31), 0),
                child: Stack(
                  alignment: Alignment.center,
                  children: [
                    Row(
                      children: [
                        navBtn(_chev, () => faBack(context), 58),
                        const Spacer(),
                        SizedBox(width: u(83)),
                      ],
                    ),
                    IgnorePointer(
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Text('طلب الشراء', style: Fa.text(u(31), weight: FontWeight.w800, height: 1)),
                          SizedBox(width: u(16)),
                          SizedBox(
                            width: u(58),
                            height: u(58),
                            child: Stack(
                              clipBehavior: Clip.none,
                              children: [
                                Positioned(
                                  left: u(-6),
                                  bottom: u(-8),
                                  child: FaPath(_basket, size: u(66), color: const Color(0xFFF7931E)),
                                ),
                                Positioned(
                                  left: u(2),
                                  top: u(-16),
                                  child: Transform.rotate(
                                    angle: -0.35,
                                    child: FaPath(_leaf, size: u(28), color: const Color(0xFF6DB33F)),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              Padding(
                padding: EdgeInsets.only(top: u(22)),
                child: Image.asset(
                  'assets/screens/purchase/hero-band.png',
                  width: double.infinity,
                  height: u(121),
                  fit: BoxFit.cover,
                  alignment: Alignment.bottomCenter,
                ),
              ),
              order.when(
                loading: () => _box(context, child: _msg(context, 'جاري تحميل طلب الشراء...')),
                error: (e, _) => _box(context, child: MessageView.error(e, onRetry: () => ref.invalidate(orderDetailProvider(widget.orderId)))),
                data: (o) => Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _box(context, child: _OrderCard(order: o)),
                    _actions(context, o),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _msg(BuildContext context, String t) => Padding(
    padding: EdgeInsets.symmetric(vertical: context.u(60), horizontal: context.u(24)),
    child: Text(
      t,
      textAlign: TextAlign.center,
      style: Fa.text(context.u(26), color: const Color(0xFF8A6A4A)),
    ),
  );

  Widget _box(BuildContext context, {required Widget child}) {
    final u = context.u;
    return Transform.translate(
      offset: Offset(0, u(-2)),
      child: Container(
        margin: EdgeInsets.only(right: u(29), left: u(28)),
        padding: EdgeInsets.fromLTRB(u(20), u(20), u(20), u(34)),
        decoration: BoxDecoration(
          color: const Color(0xFFFFFDFB),
          borderRadius: BorderRadius.circular(u(44)),
          boxShadow: [BoxShadow(color: const Color(0x1A965F28), offset: Offset(0, u(8)), blurRadius: u(28))],
        ),
        child: child,
      ),
    );
  }

  Widget _actions(BuildContext context, OrderDetail o) {
    final u = context.u;
    Widget act(String label, VoidCallback onTap, {required int kind}) => Expanded(
      child: Tap(
        onTap: onTap,
        child: Container(
          height: u(66),
          padding: EdgeInsets.symmetric(horizontal: u(10)),
          decoration: BoxDecoration(
            gradient: kind == 0 ? const LinearGradient(colors: [Color(0xFFFF8A1A), Color(0xFFF45D03)]) : null,
            color: kind == 1 ? const Color(0xFFFFF8F1) : (kind == 2 ? const Color(0xFFFFFDFA) : null),
            borderRadius: BorderRadius.circular(u(33)),
            border: kind == 1 ? Border.all(color: const Color(0xFFE0782A), width: u(2)) : null,
            boxShadow: [
              if (kind == 0) BoxShadow(color: const Color(0x3DF35E02), offset: Offset(0, u(6)), blurRadius: u(14)),
              if (kind == 2) BoxShadow(color: const Color(0x24784820), offset: Offset(0, u(5)), blurRadius: u(14)),
            ],
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              FaPath(
                kind == 2 ? _shareP : _printer,
                size: u(32),
                color: kind == 0 ? const Color(0xFFFFF5E8) : (kind == 1 ? const Color(0xFFE0640A) : const Color(0xFFF26A0A)),
              ),
              SizedBox(width: u(8)),
              Flexible(
                child: FittedBox(
                  fit: BoxFit.scaleDown,
                  child: Text(
                    label,
                    maxLines: 1,
                    style: Fa.text(
                      u(21),
                      weight: FontWeight.w800,
                      color: kind == 0 ? const Color(0xFFFFF5E8) : (kind == 1 ? const Color(0xFFE0640A) : const Color(0xFFF26A0A)),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
    return Padding(
      padding: EdgeInsets.fromLTRB(u(30), u(36), u(30), 0),
      child: Row(
        children: [
          act('الطلبات والأكلات', () => printOrder(ref, o, withDishes: true), kind: 0),
          SizedBox(width: u(12)),
          act('الطلبات فقط', () => printOrder(ref, o), kind: 1),
          SizedBox(width: u(12)),
          act('مشاركة', () => shareOrder(ref, o), kind: 2),
        ],
      ),
    );
  }
}

class _OrderCard extends ConsumerStatefulWidget {
  const _OrderCard({required this.order});
  final OrderDetail order;

  @override
  ConsumerState<_OrderCard> createState() => _OrderCardState();
}

class _OrderCardState extends ConsumerState<_OrderCard> {
  bool _editing = false;
  bool _saving = false;
  late final _name = TextEditingController(text: widget.order.name ?? '');

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final name = _name.text.trim();
    if (name.isEmpty || _saving) return;
    setState(() => _saving = true);
    try {
      await ref.read(ordersRepositoryProvider).rename(widget.order.id, name);
      ref.invalidate(orderDetailProvider(widget.order.id));
      if (mounted) setState(() => _editing = false);
    } catch (e) {
      if (mounted) showMessage(context, e, error: true);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Widget _nameBtn(BuildContext context, String icon, VoidCallback onTap, {bool ok = false, bool filled = false}) {
    final u = context.u;
    return Tap(
      onTap: onTap,
      child: Container(
        width: u(56),
        height: u(56),
        alignment: Alignment.center,
        decoration: BoxDecoration(shape: BoxShape.circle, color: ok ? const Color(0xFFF26A0A) : const Color(0xFFFDEBDB)),
        child: FaIcon(icon, size: u(30), color: ok ? Colors.white : const Color(0xFFF26A0A), filled: filled, weight: 3),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final u = context.u;
    final o = widget.order;
    final groups = _groups(o);
    final count = o.lines.where((l) => l.quantity > 0).length;
    Widget row(String? image, String fallback, String name, String qty, {bool last = false, bool dish = false, double? price, String? unitPrice}) => Container(
      height: u(66),
      margin: EdgeInsets.symmetric(horizontal: u(28)),
      decoration: BoxDecoration(
        border: last
            ? null
            : Border(
                bottom: BorderSide(color: const Color(0xFFEFE4D8), width: u(1.5)),
              ),
      ),
      child: Row(
        children: [
          NetImage(
            image,
            width: u(58),
            height: u(50),
            fit: dish ? BoxFit.cover : BoxFit.contain,
            radius: BorderRadius.circular(u(16)),
            fallbackAsset: fallback,
          ),
          SizedBox(width: u(20)),
          Expanded(
            child: Text(name, maxLines: 1, overflow: TextOverflow.ellipsis, style: Fa.text(u(26))),
          ),
          // The money of the quantity, and under it the price of one unit (e.g. 40 ج / كيلو).
          if (price != null) ...[
            Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(egp(price), style: Fa.text(u(22), weight: FontWeight.w800, color: const Color(0xFF2F6A16), height: 1)),
                if (unitPrice != null) ...[
                  SizedBox(height: u(4)),
                  Text(unitPrice, style: Fa.text(u(17), weight: FontWeight.w700, color: const Color(0xFF8B7B6C), height: 1)),
                ],
              ],
            ),
            SizedBox(width: u(12)),
          ],
          Container(
            constraints: BoxConstraints(minWidth: dish ? 0 : u(150)),
            height: u(42),
            padding: EdgeInsets.symmetric(horizontal: u(20)),
            alignment: Alignment.center,
            decoration: BoxDecoration(color: const Color(0xFFFDEEE1), borderRadius: BorderRadius.circular(u(25))),
            child: Text(
              qty,
              style: Fa.text(u(22), weight: FontWeight.w800, color: const Color(0xFFF06A08), height: 1),
            ),
          ),
        ],
      ),
    );
    Widget place(String title, String icon, List<Widget> rows, {bool dishes = false, int? badge}) => Container(
      margin: EdgeInsets.symmetric(horizontal: u(12)),
      padding: EdgeInsets.only(bottom: u(6)),
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        color: const Color(0xFFFFFCF8),
        borderRadius: BorderRadius.circular(u(32)),
        border: Border.all(color: const Color(0xFFF2E8DD), width: u(1.5)),
        boxShadow: [BoxShadow(color: const Color(0x12965F28), offset: Offset(0, u(4)), blurRadius: u(12))],
      ),
      child: Column(
        children: [
          Container(
            height: u(56),
            padding: EdgeInsets.symmetric(horizontal: u(28)),
            decoration: BoxDecoration(
              gradient: LinearGradient(colors: dishes ? const [Color(0xFFFDEBD8), Color(0xFFFFF3E6)] : const [Color(0xFFE6F0D3), Color(0xFFEEF4E0)]),
            ),
            child: Row(
              children: [
                FaPath(icon, size: u(42), color: dishes ? const Color(0xFFF26A0A) : const Color(0xFF3E9A2C)),
                SizedBox(width: u(18)),
                Text(title, style: Fa.text(u(25), weight: FontWeight.w800)),
                const Spacer(),
                if (badge != null)
                  Container(
                    constraints: BoxConstraints(minWidth: u(44)),
                    height: u(36),
                    padding: EdgeInsets.symmetric(horizontal: u(10)),
                    alignment: Alignment.center,
                    decoration: BoxDecoration(color: const Color(0xFFF26A0A), borderRadius: BorderRadius.circular(u(18))),
                    child: Text(
                      '$badge',
                      style: Fa.text(u(21), weight: FontWeight.w800, color: Colors.white, height: 1),
                    ),
                  ),
              ],
            ),
          ),
          ...rows,
        ],
      ),
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Container(
          constraints: BoxConstraints(minHeight: u(64)),
          margin: EdgeInsets.only(bottom: u(16)),
          child: _editing
              ? Row(
                  children: [
                    Expanded(
                      child: Container(
                        height: u(64),
                        padding: EdgeInsets.symmetric(horizontal: u(20)),
                        alignment: Alignment.centerRight,
                        decoration: BoxDecoration(
                          color: const Color(0xFFFFFDFA),
                          borderRadius: BorderRadius.circular(u(32)),
                          border: Border.all(color: const Color(0xFFF7B98A), width: u(2)),
                        ),
                        child: TextField(
                          controller: _name,
                          autofocus: true,
                          maxLength: 150,
                          onSubmitted: (_) => _save(),
                          style: Fa.text(u(26)),
                          decoration: const InputDecoration(
                            isCollapsed: true,
                            counterText: '',
                            filled: false,
                            border: InputBorder.none,
                            enabledBorder: InputBorder.none,
                            focusedBorder: InputBorder.none,
                          ),
                        ),
                      ),
                    ),
                    SizedBox(width: u(14)),
                    _nameBtn(context, 'check', _save, ok: true),
                    SizedBox(width: u(14)),
                    _nameBtn(context, 'x', () => setState(() => _editing = false)),
                  ],
                )
              : Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Flexible(
                      child: Text(
                        (o.name ?? '').isEmpty ? 'طلب الشراء' : o.name!,
                        textAlign: TextAlign.center,
                        style: Fa.text(u(27), weight: FontWeight.w800),
                      ),
                    ),
                    SizedBox(width: u(14)),
                    _nameBtn(context, 'pencil', () => setState(() => _editing = true), filled: true),
                  ],
                ),
        ),
        Container(
          height: u(76),
          margin: EdgeInsets.only(bottom: u(22)),
          padding: EdgeInsets.only(right: u(28), left: u(29)),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(u(42)),
            gradient: const LinearGradient(colors: [Color(0xFFFDE8D4), Color(0xFFFDF1E6)], begin: Alignment.centerLeft, end: Alignment.centerRight),
          ),
          child: Row(
            children: [
              FaPath(_group, size: u(46), color: const Color(0xFFF57C1F)),
              SizedBox(width: u(14)),
              Text('عدد الطلبات', style: Fa.text(u(25), weight: FontWeight.w800)),
              const Spacer(),
              Container(
                constraints: BoxConstraints(minWidth: u(88)),
                height: u(52),
                padding: EdgeInsets.symmetric(horizontal: u(18)),
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  gradient: const LinearGradient(colors: [Color(0xFFFF7A12), Color(0xFFF45F04)], begin: Alignment.topCenter, end: Alignment.bottomCenter),
                  borderRadius: BorderRadius.circular(u(26)),
                  boxShadow: [BoxShadow(color: const Color(0x40F35E02), offset: Offset(0, u(4)), blurRadius: u(10))],
                ),
                child: Text(
                  '$count',
                  style: Fa.text(u(25), weight: FontWeight.w800, color: Colors.white, height: 1),
                ),
              ),
            ],
          ),
        ),
        // إجمالي الطلب من أسعار المكونات (لو العميل مشغّل الأسعار).
        if (o.total != null)
          Container(
            height: u(76),
            margin: EdgeInsets.only(bottom: u(22)),
            padding: EdgeInsets.only(right: u(28), left: u(29)),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(u(42)),
              gradient: const LinearGradient(colors: [Color(0xFFE6F0D3), Color(0xFFF2F7E8)]),
            ),
            child: Row(
              children: [
                FaIcon('tag', size: u(40), color: const Color(0xFF3E7B22), weight: 2.2),
                SizedBox(width: u(14)),
                Text('إجمالي الطلب', style: Fa.text(u(25), weight: FontWeight.w800)),
                if (o.priceMissing > 0) ...[
                  SizedBox(width: u(10)),
                  Flexible(
                    child: Text(
                      '${o.priceMissing} بدون سعر',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Fa.text(u(19), weight: FontWeight.w500, color: const Color(0xFFC45A00)),
                    ),
                  ),
                ],
                const Spacer(),
                Text(
                  egp(o.lines.where((l) => l.quantity > 0).fold<double>(0, (a, l) => a + (l.cost ?? 0))),
                  style: Fa.text(u(28), weight: FontWeight.w800, color: const Color(0xFF2F6A16), height: 1),
                ),
              ],
            ),
          ),
        for (var gi = 0; gi < groups.length; gi++) ...[
          if (gi > 0) SizedBox(height: u(18)),
          place(groups[gi].$1, gi % 2 == 0 ? _leaf : _basket, badge: groups[gi].$2.length, [
            for (var i = 0; i < groups[gi].$2.length; i++)
              row(
                groups[gi].$2[i].imagePath,
                'assets/screens/ingredients/onion.png',
                groups[gi].$2[i].name ?? '',
                PurchaseRules.text(groups[gi].$2[i]),
                last: i == groups[gi].$2.length - 1,
                price: groups[gi].$2[i].cost,
                unitPrice: _unitPrice(groups[gi].$2[i]),
              ),
          ]),
        ],
        if (groups.isEmpty)
          Padding(
            padding: EdgeInsets.only(top: u(40), bottom: u(10)),
            child: Text(
              'لا توجد مكونات في هذا الطلب.',
              textAlign: TextAlign.center,
              style: Fa.text(u(24), color: const Color(0xFF8A6A4A)),
            ),
          ),
        if (o.items.isNotEmpty) ...[
          SizedBox(height: u(22)),
          place('الأكلات المختارة', _cloche, dishes: true, badge: o.items.length, [
            for (var i = 0; i < o.items.length; i++)
              row(
                o.items[i].imagePath,
                'assets/screens/foods/koshari.png',
                o.items[i].name,
                '${o.items[i].repeatCount} مرة${o.items[i].peopleCount != null ? ' · ${o.items[i].peopleCount} أفراد' : ''}',
                last: i == o.items.length - 1,
                dish: true,
                price: o.items[i].price,
              ),
          ]),
        ],
      ],
    );
  }
}

Future<void> reorder(BuildContext context, WidgetRef ref, String orderId) async {
  try {
    await ref.read(cartControllerProvider.notifier).mergeOrder(orderId);
    if (context.mounted) {
      showMessage(context, 'اتضافت الأكلات والمكونات لطلباتك');
      context.go('/ingredients');
    }
  } catch (e) {
    if (context.mounted) showMessage(context, e, error: true);
  }
}

Future<void> renameOrderDialog(BuildContext context, WidgetRef ref, String id, String current, {VoidCallback? onDone}) async {
  final ctrl = TextEditingController(text: current);
  final name = await showDialog<String>(
    context: context,
    builder: (c) => AlertDialog(
      title: const Text(
        'اسم الطلب',
        style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.w800),
      ),
      content: TextField(controller: ctrl, autofocus: true, maxLength: 100),
      actions: [
        TextButton(onPressed: () => Navigator.pop(c), child: const Text('إلغاء')),
        FilledButton(onPressed: () => Navigator.pop(c, ctrl.text.trim()), child: const Text('حفظ')),
      ],
    ),
  );
  if (name == null || name.isEmpty || name == current) return;
  try {
    await ref.read(ordersRepositoryProvider).rename(id, name);
    ref.invalidate(orderDetailProvider(id));
    onDone?.call();
  } catch (e) {
    if (context.mounted) showMessage(context, e, error: true);
  }
}

/// "40 ج / كيلو": the price of one unit of the line (none for a by-money line).
String? _unitPrice(ShoppingLine l) {
  final unit = (l.unitName ?? '').trim();
  if (l.unitPrice == null || unit.isEmpty || l.purchaseMode == PurchaseRules.money) return null;
  return '${egp(l.unitPrice!)} / $unit';
}
