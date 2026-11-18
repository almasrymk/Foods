import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../app/providers.dart';
import '../../../app/widgets/fa_chrome.dart';
import '../../../app/widgets/fa_widgets.dart';
import '../../../core/ui/fa_kit.dart';
import '../../../core/widgets/async_views.dart';
import '../../../core/widgets/net_image.dart';
import '../../cart/domain/purchase_rules.dart';
import '../../cart/domain/selected_item.dart';
import '../../cart/domain/shopping_line.dart';
import '../../catalog/domain/catalog_models.dart';
import '../domain/order_models.dart';
import 'order_details_page.dart';
import '../../prices/prices.dart';

const _chips = [
  (OrderState.all, 'الكل', 'grid'),
  (OrderState.created, 'إنشاء بدون طباعة', 'doc'),
  (OrderState.printed, 'تم الطباعة', 'printer'),
  (OrderState.reordered, 'أعيد طلبه', 'refresh'),
  (OrderState.shared, 'تمت المشاركة', 'share'),
  (OrderState.reprinted, 'إعادة الطباعة', 'printer'),
  (OrderState.reshared, 'إعادة المشاركة', 'share'),
];

String _dateLabel(String v) {
  final day = v.length >= 10 ? v.substring(0, 10) : v;
  final p = day.split('-');
  return p.length == 3 ? '${int.tryParse(p[2]) ?? p[2]}/${int.tryParse(p[1]) ?? p[1]}/${p[0]}' : day;
}

/// "طلبات قديمة" (orders.component.ts).
class OrdersHistoryPage extends ConsumerStatefulWidget {
  const OrdersHistoryPage({super.key});
  @override
  ConsumerState<OrdersHistoryPage> createState() => _OrdersHistoryPageState();
}

class _OrdersHistoryPageState extends ConsumerState<OrdersHistoryPage> {
  final _orders = <OrderHistoryItem>[];
  Map<OrderState, int> _counts = const {};
  OrderHistoryFilter _filter = const OrderHistoryFilter();
  bool _loading = false;
  bool _hasMore = true;
  bool _advanced = false;
  DateTime? _from, _to;
  OrderState _state = OrderState.all;
  final _name = TextEditingController();
  final _dish = TextEditingController();
  Object? _error;
  int _token = 0;
  int _total = 0;
  Timer? _typing;

  @override
  void initState() {
    super.initState();
    _reload();
  }

  @override
  void dispose() {
    _typing?.cancel();
    _name.dispose();
    _dish.dispose();
    super.dispose();
  }

  Future<void> _reload() async {
    _token++;
    setState(() {
      _orders.clear();
      _hasMore = true;
      _error = null;
      _loading = false;
    });
    await _more();
  }

  Future<void> _more() async {
    if (_loading || !_hasMore) return;
    final token = _token;
    setState(() => _loading = true);
    try {
      final page = await ref.read(ordersRepositoryProvider).history(_filter, skip: _orders.length);
      if (!mounted || token != _token) return;
      setState(() {
        _orders.addAll(page.items);
        _counts = page.counts;
        _hasMore = page.hasMore;
        _total = page.total;
      });
    } catch (e) {
      if (mounted && token == _token) setState(() => _error = e);
    } finally {
      if (mounted && token == _token) setState(() => _loading = false);
    }
  }

  void _setFilter(OrderHistoryFilter f) {
    _filter = f;
    _reload();
  }

  bool get _filtered => _filter.text.isNotEmpty || _filter.state != OrderState.all || _filter.hasAdvanced;

  void _applyAdvanced() {
    var from = _from, to = _to;
    if (from != null && to != null && from.isAfter(to)) (from, to) = (to, from);
    setState(() {
      _from = from;
      _to = to;
    });
    _setFilter(OrderHistoryFilter(text: _filter.text, state: _state, from: from, to: to, name: _name.text.trim(), dish: _dish.text.trim()));
  }

  Future<void> _pickDate(bool from) async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: (from ? _from : _to) ?? now,
      firstDate: DateTime(2020),
      lastDate: DateTime(now.year + 2),
    );
    if (picked != null) setState(() => from ? _from = picked : _to = picked);
  }

  Widget _advancedPanel(BuildContext context) {
    final s = context.s;
    String fmt(DateTime? d) => d == null ? 'اختار تاريخ' : '${d.day}/${d.month}/${d.year}';
    Widget field(String label, DateTime? value, bool from) => Expanded(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: Fa.text(s(22), weight: FontWeight.w800)),
          SizedBox(height: s(8)),
          Tap(
            onTap: () => _pickDate(from),
            child: Container(
              height: s(64),
              padding: EdgeInsets.symmetric(horizontal: s(16)),
              alignment: Alignment.centerRight,
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(s(18)),
                border: Border.all(color: const Color(0xFFF3DCC4), width: s(2)),
              ),
              child: Text(
                fmt(value),
                style: Fa.text(s(24), weight: FontWeight.w500, color: value == null ? const Color(0xFF8C8079) : Fa.ink),
              ),
            ),
          ),
        ],
      ),
    );
    InputDecoration deco(String hint) {
      final b = OutlineInputBorder(
        borderRadius: BorderRadius.circular(s(18)),
        borderSide: BorderSide(color: const Color(0xFFF3DCC4), width: s(2)),
      );
      return InputDecoration(
        hintText: hint,
        hintStyle: Fa.text(s(24), weight: FontWeight.w500, color: const Color(0xFF8C8079)),
        isDense: true,
        filled: true,
        fillColor: Colors.white,
        contentPadding: EdgeInsets.symmetric(horizontal: s(16), vertical: s(18)),
        border: b,
        enabledBorder: b,
        focusedBorder: b.copyWith(
          borderSide: BorderSide(color: const Color(0xFFF26A0A), width: s(2)),
        ),
      );
    }

    Widget text(String label, TextEditingController c, String hint) => Padding(
      padding: EdgeInsets.only(top: s(14)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: Fa.text(s(22), weight: FontWeight.w800)),
          SizedBox(height: s(8)),
          TextField(
            controller: c,
            onSubmitted: (_) => _applyAdvanced(),
            style: Fa.text(s(24), weight: FontWeight.w500),
            decoration: deco(hint),
          ),
        ],
      ),
    );

    final stateField = Padding(
      padding: EdgeInsets.only(top: s(14)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('الحالة', style: Fa.text(s(22), weight: FontWeight.w800)),
          SizedBox(height: s(8)),
          Container(
            height: s(64),
            padding: EdgeInsets.symmetric(horizontal: s(16)),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(s(18)),
              border: Border.all(color: const Color(0xFFF3DCC4), width: s(2)),
            ),
            child: DropdownButtonHideUnderline(
              child: DropdownButton<OrderState>(
                value: _state,
                isExpanded: true,
                iconSize: s(36),
                style: Fa.text(s(24), weight: FontWeight.w500),
                items: [for (final c in _chips) DropdownMenuItem(value: c.$1, child: Text(c.$2))],
                onChanged: (v) => setState(() => _state = v ?? OrderState.all),
              ),
            ),
          ),
        ],
      ),
    );

    return Container(
      margin: EdgeInsets.fromLTRB(s(38), s(18), s(36), 0),
      padding: EdgeInsets.all(s(20)),
      decoration: BoxDecoration(
        color: Fa.card,
        borderRadius: BorderRadius.circular(s(26)),
        border: Border.all(color: const Color(0xFFF3E8DC), width: s(1.5)),
        boxShadow: [Fa.shadow(context)],
      ),
      child: Column(
        children: [
          Row(
            children: [
              field('من تاريخ', _from, true),
              SizedBox(width: s(14)),
              field('إلى تاريخ', _to, false),
            ],
          ),
          text('اسم الطلب', _name, 'مثلاً: عزومة الجمعة'),
          stateField,
          text('اسم أكلة في الطلب', _dish, 'مثلاً: محشي'),
          SizedBox(height: s(14)),
          Row(
            children: [
              Expanded(
                child: Tap(
                  onTap: _applyAdvanced,
                  child: Container(
                    height: s(64),
                    decoration: BoxDecoration(gradient: Fa.chipGradient, borderRadius: BorderRadius.circular(s(32))),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        FaIcon('search', size: s(30), color: Colors.white, weight: 2.4),
                        SizedBox(width: s(10)),
                        Text('بحث', style: Fa.text(s(24), color: Colors.white)),
                      ],
                    ),
                  ),
                ),
              ),
              SizedBox(width: s(14)),
              Tap(
                onTap: () {
                  setState(() {
                    _from = _to = null;
                    _state = OrderState.all;
                  });
                  _name.clear();
                  _dish.clear();
                  _setFilter(OrderHistoryFilter(text: _filter.text));
                },
                child: Container(
                  height: s(64),
                  padding: EdgeInsets.symmetric(horizontal: s(28)),
                  alignment: Alignment.center,
                  decoration: BoxDecoration(color: const Color(0xFFFBF1E4), borderRadius: BorderRadius.circular(s(32))),
                  child: Text('مسح', style: Fa.text(s(24), color: const Color(0xFF8A5A2B))),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    ref.listen(pricesVersionProvider, (_, _) => _reload());
    return Directionality(
      textDirection: TextDirection.rtl,
      child: SafeArea(
        bottom: false,
        child: RefreshIndicator(
          color: Fa.orange,
          onRefresh: _reload,
          child: NotificationListener<ScrollNotification>(
            onNotification: (n) {
              if (n.metrics.axis == Axis.vertical && n.metrics.pixels > n.metrics.maxScrollExtent - 300) _more();
              return false;
            },
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: EdgeInsets.only(bottom: faBottomSpace(context, ref)),
              children: [
                const FaScreenHeader(
                  title: 'طلبات قديمة',
                  art: 'assets/screens/art/orders.svg',
                  titleIcon: 'receipt',
                  subtitle: 'راجع طلباتك السابقة وأعد طلبها بسهولة',
                ),
                Padding(
                  padding: EdgeInsets.only(top: s(8)),
                  child: FaSearchBar(
                    hint: 'ابحث باسم الطلب أو رقمه أو أكلة...',
                    left: 38,
                    right: 36,
                    query: ItemQuery(text: _filter.text),
                    active: _advanced || _filter.hasAdvanced,
                    onAdvanced: () => setState(() => _advanced = !_advanced),
                    onChanged: (q) => _setFilter(_filter.copyWith(text: q.text.trim())),
                  ),
                ),
                if (_advanced) _advancedPanel(context),
                Padding(
                  padding: EdgeInsets.only(top: s(23) - s(4)),
                  child: SizedBox(
                    height: s(64 + 20),
                    child: ListView(
                      scrollDirection: Axis.horizontal,
                      padding: EdgeInsets.only(left: s(33), right: s(33), top: s(4), bottom: s(16)),
                      children: [
                        for (var i = 0; i < _chips.length; i++) ...[
                          if (i > 0) SizedBox(width: s(10)),
                          FaChip(
                            label: _chips[i].$2,
                            badge: _counts.isEmpty ? null : (_counts[_chips[i].$1] ?? 0),
                            countCircle: true,
                            icon: _chips[i].$3,
                            on: _filter.state == _chips[i].$1,
                            fontSize: 22,
                            pad: _filter.state == _chips[i].$1 ? 26 : 16,
                            onTap: () {
                              _state = _chips[i].$1;
                              _setFilter(_filter.copyWith(state: _chips[i].$1));
                            },
                          ),
                        ],
                      ],
                    ),
                  ),
                ),
                Padding(
                  padding: EdgeInsets.fromLTRB(s(25), s(28) - s(16), s(24), 0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      for (var i = 0; i < _orders.length; i++) ...[
                        if (i > 0) SizedBox(height: s(17)),
                        _OrderCard(order: _orders[i], onChanged: () => setState(() {})),
                      ],
                      if (_error != null && _orders.isEmpty)
                        MessageView.error(_error!, onRetry: _reload)
                      else if (_loading)
                        FaNote(_orders.isEmpty ? 'جاري التحميل...' : 'جاري تحميل المزيد...')
                      else if (_orders.isEmpty)
                        FaNote(_filtered ? 'لا توجد طلبات مطابقة للبحث.' : 'لا توجد طلبات قديمة بعد.')
                      else if (!_hasMore)
                        Padding(
                          padding: EdgeInsets.symmetric(vertical: s(18)),
                          child: Text(
                            'عرضنا كل الطلبات ($_total)',
                            textAlign: TextAlign.center,
                            style: Fa.text(s(22), weight: FontWeight.w500, color: const Color(0xFFB0A090)),
                          ),
                        ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _OrderCard extends ConsumerStatefulWidget {
  const _OrderCard({required this.order, required this.onChanged});
  final OrderHistoryItem order;
  final VoidCallback onChanged;

  @override
  ConsumerState<_OrderCard> createState() => _OrderCardState();
}

class _OrderCardState extends ConsumerState<_OrderCard> {
  bool _open = false;
  bool _editing = false;
  bool _reordering = false;
  List<ShoppingLine>? _lines;
  OrderDetail? _detail;
  late final _name = TextEditingController(text: widget.order.name ?? '');

  OrderHistoryItem get o => widget.order;

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  Future<void> _toggle() async {
    setState(() => _open = !_open);
    if (_open && _lines == null) {
      try {
        final d = await ref.read(ordersRepositoryProvider).order(o.id);
        if (mounted) {
          setState(() {
            _detail = d;
            _lines = d.lines;
          });
        }
      } catch (_) {
        if (mounted) setState(() => _lines = const []);
      }
    }
  }

  Future<void> _saveName() async {
    final name = _name.text.trim();
    if (name.isEmpty) return;
    try {
      await ref.read(ordersRepositoryProvider).rename(o.id, name);
      o.name = name;
      if (mounted) {
        setState(() => _editing = false);
        showMessage(context, 'تم حفظ الاسم');
      }
    } catch (e) {
      if (mounted) showMessage(context, e, error: true);
    }
  }

  Future<void> _share() async {
    try {
      final detail = await ref.read(ordersRepositoryProvider).order(o.id);
      await shareOrder(ref, detail);
      o.state = o.sharedAt != null || o.state == OrderState.shared || o.state == OrderState.reshared ? OrderState.reshared : OrderState.shared;
      o.sharedAt ??= DateTime.now().toIso8601String();
      widget.onChanged();
    } catch (e) {
      if (mounted) showMessage(context, e, error: true);
    }
  }

  Future<void> _reorder() async {
    if (_reordering) return;
    setState(() => _reordering = true);
    await reorder(context, ref, o.id);
    ref.read(ordersRepositoryProvider).markReordered(o.id).catchError((_) {});
    o.state = OrderState.reordered;
    if (mounted) setState(() => _reordering = false);
    widget.onChanged();
  }

  (String, String, Color, Color) get _status => switch (o.state) {
    OrderState.printed => ('تم الطباعة', 'printer', const Color(0xFFE4F0D2), const Color(0xFF2F7D1E)),
    OrderState.reordered => ('أعيد طلبه', 'refresh', const Color(0xFFDDEBFB), const Color(0xFF1E5C9A)),
    OrderState.shared => ('تمت المشاركة', 'share', const Color(0xFFEFE3FA), const Color(0xFF6B3FA0)),
    OrderState.reprinted => ('إعادة الطباعة', 'printer', const Color(0xFFD5EFE6), const Color(0xFF16775A)),
    OrderState.reshared => ('إعادة المشاركة', 'share', const Color(0xFFF8E0EE), const Color(0xFFA0306E)),
    _ => ('بدون طباعة', 'doc', const Color(0xFFFDE9B8), const Color(0xFFB86E04)),
  };

  Widget _quick(BuildContext context) {
    final s = context.s;
    Widget btn(
      String icon,
      String label,
      VoidCallback onTap, {
      Gradient? gradient,
      Color? bg,
      Color color = const Color(0xFFE0640A),
      bool border = false,
    }) => Expanded(
      child: Tap(
        onTap: onTap,
        child: Container(
          height: s(56),
          decoration: BoxDecoration(
            gradient: gradient,
            color: bg,
            borderRadius: BorderRadius.circular(s(28)),
            border: border ? Border.all(color: const Color(0xFFE0782A), width: s(2)) : null,
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              FaIcon(icon, size: s(28), color: color, weight: 2.2),
              SizedBox(width: s(8)),
              Flexible(
                child: Text(
                  label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: Fa.text(s(22), weight: FontWeight.w800, color: color),
                ),
              ),
            ],
          ),
        ),
      ),
    );
    return Padding(
      padding: EdgeInsets.fromLTRB(s(20), 0, s(20), s(16)),
      child: Row(
        children: [
          btn('printer', 'طباعة', () => context.push('/orders/${o.id}'), gradient: Fa.chipGradient, color: Colors.white),
          SizedBox(width: s(12)),
          btn('share', 'مشاركة', _share, bg: const Color(0xFFFBF1E4)),
          SizedBox(width: s(12)),
          btn('refresh', _reordering ? 'جاري الإضافة...' : 'إعادة طلبه', _reorder, bg: const Color(0xFFFFF8F1), border: true),
        ],
      ),
    );
  }

  Widget _block(BuildContext context, {required String icon, required String title, required bool green, int? count, required Widget child}) {
    final s = context.s;
    return Container(
      padding: EdgeInsets.fromLTRB(s(16), s(16), s(16), s(18)),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(s(24)),
        border: Border.all(color: const Color(0xFFF3E6D8), width: s(1.5)),
        boxShadow: [Fa.shadow(context, y: 3, blur: 10, color: const Color(0x0D784820))],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: s(48),
                height: s(48),
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: green ? const Color(0xFFE6F0D3) : const Color(0xFFFDEBDB),
                  borderRadius: BorderRadius.circular(s(14)),
                ),
                child: FaIcon(icon, size: s(30), color: green ? const Color(0xFF3E8E1E) : const Color(0xFFF26A0A)),
              ),
              SizedBox(width: s(12)),
              Text(title, style: Fa.text(s(26), weight: FontWeight.w800)),
              const Spacer(),
              if (count != null)
                Container(
                  constraints: BoxConstraints(minWidth: s(40)),
                  height: s(34),
                  padding: EdgeInsets.symmetric(horizontal: s(10)),
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: green ? const Color(0xFF5AA22B) : const Color(0xFFF26A0A),
                    borderRadius: BorderRadius.circular(s(17)),
                  ),
                  child: Text(
                    '$count',
                    style: Fa.text(s(20), weight: FontWeight.w800, color: Colors.white, height: 1),
                  ),
                ),
            ],
          ),
          SizedBox(height: s(14)),
          child,
        ],
      ),
    );
  }

  Widget _inner(BuildContext context) {
    final s = context.s;
    final muted = Fa.text(s(22), weight: FontWeight.w500, color: const Color(0xFF8A7B6E));
    // Prices are on when the order came back with a total (null = the customer works without prices).
    final pricesOn = o.total != null || _detail?.total != null;
    double? dishPrice(String itemId) {
      for (final it in _detail?.items ?? const <SelectedItem>[]) {
        if (it.itemId == itemId) return it.price;
      }
      return null;
    }
    final groups = <String, List<ShoppingLine>>{};
    for (final l in _lines ?? const <ShoppingLine>[]) {
      final p = (l.purchasePlaceName ?? '').trim();
      groups.putIfAbsent(p.isEmpty ? 'البقالة' : p, () => []).add(l);
    }
    return Padding(
      padding: EdgeInsets.symmetric(horizontal: s(20)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _block(
            context,
            icon: 'cloche',
            title: 'الأكلات',
            green: false,
            count: o.dishes.length,
            child: o.dishes.isEmpty
                ? Text('لا توجد أكلات.', style: muted)
                : GridView.count(
                    crossAxisCount: 3,
                    padding: EdgeInsets.zero,
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    crossAxisSpacing: s(12),
                    mainAxisSpacing: s(12),
                    // Card height follows the content: shorter when no dish has a description.
                    // A little taller when prices are on: the dish price sits under the name.
                    childAspectRatio: pricesOn ? .84 : 1.0,
                    children: [
                      for (final d in o.dishes)
                        Container(
                          padding: EdgeInsets.all(s(8)),
                          decoration: BoxDecoration(
                            color: const Color(0xFFFFF8F1),
                            borderRadius: BorderRadius.circular(s(18)),
                            border: Border.all(color: const Color(0xFFF7E3CE), width: s(1.5)),
                          ),
                          child: Stack(
                            children: [
                              Column(
                                children: [
                                  AspectRatio(
                                    aspectRatio: 16 / 10,
                                    child: NetImage(
                                      d.imagePath,
                                      radius: BorderRadius.circular(s(12)),
                                      fallbackAsset: 'assets/screens/foods/koshari.png',
                                    ),
                                  ),
                                  SizedBox(height: s(8)),
                                  Text(
                                    d.name ?? '',
                                    maxLines: 2,
                                    textAlign: TextAlign.center,
                                    overflow: TextOverflow.ellipsis,
                                    style: Fa.text(s(21), weight: FontWeight.w800, height: 1.25),
                                  ),
                                  if (pricesOn) ...[
                                    SizedBox(height: s(6)),
                                    _money(context, dishPrice(d.itemId), size: 18),
                                  ],
                                ],
                              ),
                              Positioned(
                                left: s(6),
                                top: s(6),
                                child: Container(
                                  constraints: BoxConstraints(minWidth: s(36)),
                                  height: s(36),
                                  padding: EdgeInsets.symmetric(horizontal: s(8)),
                                  alignment: Alignment.center,
                                  decoration: BoxDecoration(color: const Color(0xFFF26A0A), borderRadius: BorderRadius.circular(s(18))),
                                  child: Text(
                                    '${d.repeatCount}',
                                    textDirection: TextDirection.ltr,
                                    style: Fa.text(s(19), weight: FontWeight.w800, color: Colors.white, height: 1),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                    ],
                  ),
          ),
          SizedBox(height: s(16)),
          _block(
            context,
            icon: 'layers',
            title: 'المكونات',
            green: true,
            count: (_lines?.isNotEmpty ?? false) ? _lines!.length : null,
            child: _lines == null
                ? Text('جاري التحميل...', style: muted)
                : groups.isEmpty
                ? Text('لا توجد مكونات.', style: muted)
                : Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      for (final g in groups.entries) ...[
                        Container(
                          height: s(34),
                          margin: EdgeInsets.only(top: s(4), bottom: s(10)),
                          padding: EdgeInsets.symmetric(horizontal: s(14)),
                          decoration: BoxDecoration(color: const Color(0xFFEEF5E2), borderRadius: BorderRadius.circular(s(17))),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              FaIcon('cart', size: s(22), color: const Color(0xFF3E7B22), weight: 2.2),
                              SizedBox(width: s(8)),
                              Text(
                                pricesOn && g.value.any((l) => (l.cost ?? 0) > 0)
                                    ? '${g.key} · ${egp(g.value.fold<double>(0, (a, l) => a + (l.cost ?? 0)))}'
                                    : g.key,
                                style: Fa.text(s(19), weight: FontWeight.w800, color: const Color(0xFF3E7B22), height: 1),
                              ),
                            ],
                          ),
                        ),
                        GridView(
                          padding: EdgeInsets.zero,
                          shrinkWrap: true,
                          physics: const NeverScrollableScrollPhysics(),
                          gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                            crossAxisCount: 2,
                            crossAxisSpacing: s(10),
                            mainAxisSpacing: s(10),
                            mainAxisExtent: pricesOn ? s(78) : s(54),
                          ),
                          children: [
                            for (final l in g.value)
                              Container(
                                padding: EdgeInsets.only(right: s(10), left: s(8)),
                                decoration: BoxDecoration(
                                  color: const Color(0xFFFBF7F2),
                                  borderRadius: BorderRadius.circular(s(16)),
                                  border: Border.all(color: const Color(0xFFF1E6DA), width: s(1.5)),
                                ),
                                child: Row(
                                  children: [
                                    Expanded(
                                      child: pricesOn
                                          ? Column(
                                              mainAxisAlignment: MainAxisAlignment.center,
                                              crossAxisAlignment: CrossAxisAlignment.start,
                                              children: [
                                                Text(l.name ?? '', maxLines: 1, overflow: TextOverflow.ellipsis, style: Fa.text(s(21), height: 1.2)),
                                                SizedBox(height: s(3)),
                                                _money(context, l.cost, size: 17),
                                              ],
                                            )
                                          : Text(l.name ?? '', maxLines: 1, overflow: TextOverflow.ellipsis, style: Fa.text(s(21))),
                                    ),
                                    Container(
                                      height: s(34),
                                      padding: EdgeInsets.symmetric(horizontal: s(10)),
                                      alignment: Alignment.center,
                                      decoration: BoxDecoration(color: const Color(0xFFFDEBDB), borderRadius: BorderRadius.circular(s(17))),
                                      child: Text(
                                        PurchaseRules.text(l),
                                        style: Fa.text(s(19), weight: FontWeight.w800, color: const Color(0xFFE0640A), height: 1),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                          ],
                        ),
                        SizedBox(height: s(8)),
                      ],
                    ],
                  ),
          ),
          SizedBox(height: s(18)),
          Tap(
            onTap: () => context.push('/orders/${o.id}'),
            child: Container(
              height: s(62),
              margin: EdgeInsets.symmetric(horizontal: s(2)),
              decoration: BoxDecoration(color: const Color(0xFFE7F3D6), borderRadius: BorderRadius.circular(s(31))),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  FaIcon('doc', size: s(34), color: Fa.green),
                  SizedBox(width: s(12)),
                  Text(
                    'عرض التفاصيل',
                    style: Fa.text(s(25), weight: FontWeight.w800, color: Fa.green),
                  ),
                ],
              ),
            ),
          ),
          SizedBox(height: s(16)),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    final st = _status;
    final name = (o.name ?? '').isNotEmpty ? o.name! : 'طلب # ${o.code ?? ''}';
    return Container(
      padding: EdgeInsets.only(bottom: _open ? s(20) - s(16) : 0),
      decoration: BoxDecoration(
        color: Fa.card,
        borderRadius: BorderRadius.circular(s(_open ? 30 : 28)),
        border: Border.all(color: _open ? const Color(0xFF6EA43C) : const Color(0xFFF3E8DC), width: s(_open ? 2 : 1.5)),
        boxShadow: [Fa.shadow(context)],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Tap(
            onTap: _toggle,
            child: Container(
              constraints: BoxConstraints(minHeight: s(102)),
              padding: EdgeInsets.fromLTRB(s(20), s(12), s(22), s(12)),
              child: Row(
                children: [
                  FaIcon('receipt', size: s(44), color: Fa.orange),
                  SizedBox(width: s(16)),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        if (_editing)
                          Row(
                            children: [
                              Expanded(
                                child: Container(
                                  height: s(52),
                                  padding: EdgeInsets.symmetric(horizontal: s(14)),
                                  alignment: Alignment.centerRight,
                                  decoration: BoxDecoration(
                                    color: const Color(0xFFFFFDFA),
                                    borderRadius: BorderRadius.circular(s(26)),
                                    border: Border.all(color: const Color(0xFFF7B98A), width: s(2)),
                                  ),
                                  child: TextField(
                                    controller: _name,
                                    autofocus: true,
                                    maxLength: 150,
                                    onSubmitted: (_) => _saveName(),
                                    style: Fa.text(s(24)),
                                    decoration: const InputDecoration(
                                      isCollapsed: true,
                                      counterText: '',
                                      border: InputBorder.none,
                                      enabledBorder: InputBorder.none,
                                      focusedBorder: InputBorder.none,
                                      filled: false,
                                    ),
                                  ),
                                ),
                              ),
                              SizedBox(width: s(8)),
                              _roundBtn(context, 'check', _saveName, green: true),
                              SizedBox(width: s(8)),
                              _roundBtn(context, 'x', () => setState(() => _editing = false)),
                            ],
                          )
                        else
                          Row(
                            children: [
                              Flexible(
                                child: Text(
                                  name,
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  style: Fa.text(s(28), weight: FontWeight.w800),
                                ),
                              ),
                              SizedBox(width: s(10)),
                              _roundBtn(context, 'pencil', () => setState(() => _editing = true), filled: true),
                            ],
                          ),
                        SizedBox(height: s(4)),
                        Text(
                          '${_dateLabel(o.date)} · ${o.dishCount} أكلة · ${o.lineCount} مكون',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: Fa.text(s(21), weight: FontWeight.w500, color: const Color(0xFF6B625C)),
                        ),
                        if (o.total != null) ...[
                          SizedBox(height: s(6)),
                          o.total! > 0 ? FaPriceTag(o.total!, size: 20) : _money(context, null, size: 19),
                        ],
                      ],
                    ),
                  ),
                  SizedBox(width: s(16)),
                  Container(
                    height: s(46),
                    padding: EdgeInsets.symmetric(horizontal: s(18)),
                    decoration: BoxDecoration(color: st.$3, borderRadius: BorderRadius.circular(s(23))),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        FaIcon(st.$2, size: s(26), color: st.$4, weight: 2.6),
                        SizedBox(width: s(8)),
                        Text(
                          st.$1,
                          style: Fa.text(s(21), weight: FontWeight.w800, color: st.$4, height: 1),
                        ),
                      ],
                    ),
                  ),
                  SizedBox(width: s(16)),
                  Container(
                    width: s(50),
                    height: s(50),
                    alignment: Alignment.center,
                    decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0xFFFBF1E4)),
                    child: FaIcon(_open ? 'chevronUp' : 'chevronDown', size: s(30), weight: 2.6),
                  ),
                ],
              ),
            ),
          ),
          if (_open) ...[_inner(context), _quick(context)] else _quick(context),
        ],
      ),
    );
  }

  /// A price in green, or "مفيش سعر لسه" in orange when nothing is priced yet.
  Widget _money(BuildContext context, double? v, {double size = 18}) {
    final s = context.s;
    final has = v != null && v > 0;
    return Text(
      has ? egp(v!) : 'مفيش سعر لسه',
      maxLines: 1,
      overflow: TextOverflow.ellipsis,
      style: Fa.text(s(size), weight: FontWeight.w800, color: has ? const Color(0xFF2F6A16) : const Color(0xFFC45A00), height: 1.1),
    );
  }

  Widget _roundBtn(BuildContext context, String icon, VoidCallback onTap, {bool filled = false, bool green = false}) {
    final s = context.s;
    return Tap(
      onTap: onTap,
      child: Container(
        width: s(44),
        height: s(44),
        alignment: Alignment.center,
        decoration: BoxDecoration(shape: BoxShape.circle, color: green ? const Color(0xFFE4F0D2) : const Color(0xFFFDEBDB)),
        child: FaIcon(icon, size: s(24), color: green ? Fa.green : const Color(0xFFF26A0A), filled: filled, weight: 3),
      ),
    );
  }
}
