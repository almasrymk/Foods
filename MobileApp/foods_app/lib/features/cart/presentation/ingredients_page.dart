import 'dart:async';

import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../../app/providers.dart';
import '../../../app/widgets/fa_chrome.dart';
import '../../../app/widgets/fa_widgets.dart';
import '../../../core/ui/fa_kit.dart';
import '../../../core/widgets/net_image.dart';
import '../domain/purchase_rules.dart';
import '../domain/shopping_line.dart';
import 'cart_controller.dart';
import '../../prices/prices.dart';

/// "طلباتي": the chosen dishes and the ingredients to buy for them.
class IngredientsPage extends ConsumerStatefulWidget {
  const IngredientsPage({super.key});
  @override
  ConsumerState<IngredientsPage> createState() => _IngredientsPageState();
}

class _IngredientsPageState extends ConsumerState<IngredientsPage> {
  List<ShoppingLine> _lines = [];
  bool _loading = false;
  Object? _error;
  String _calculatedFor = '';
  Timer? _calc;
  int _token = 0;

  @override
  void initState() {
    super.initState();
    final cart = ref.read(cartControllerProvider.notifier);
    cart.boot();
    WidgetsBinding.instance.addPostFrameCallback((_) => _recalculate(ref.read(cartControllerProvider)));
  }

  @override
  void dispose() {
    _calc?.cancel();
    super.dispose();
  }

  void _recalculate(CartState cart) {
    final key = CartState.fingerprint(cart.items);
    if (key == _calculatedFor) return;
    _calculatedFor = key;
    _calc?.cancel();
    if (cart.items.isEmpty) {
      setState(() {
        _lines = [];
        _loading = false;
      });
      return;
    }
    setState(() => _loading = true);
    final token = ++_token;
    _calc = Timer(const Duration(milliseconds: 300), () async {
      try {
        final rows = await ref.read(ordersRepositoryProvider).calculate(cart.items);
        if (!mounted || token != _token) return;
        final now = ref.read(cartControllerProvider);
        List<ShoppingLine> lines;
        if (now.linesMatchItems) {
          // Saved edits for these exact dishes: show them, the rest as removed (0).
          // Fresh prices from the server for the saved lines too.
          final priceOf = {for (final r in rows) r.key: r.unitPrice};
          final saved = now.lines
              .where((l) => l.quantity > 0)
              .map((l) => ShoppingLine.fromJson({...l.toJson(), 'unitPrice': priceOf.containsKey(l.key) ? priceOf[l.key] : l.unitPrice}))
              .toList();
          final keys = saved.map((l) => l.key).toSet();
          lines = [...saved, ...rows.where((r) => !keys.contains(r.key)).map((r) => r.copyWith(quantity: 0))];
        } else {
          lines = rows;
          ref.read(cartControllerProvider.notifier).setLines(rows, sync: false);
        }
        setState(() {
          _lines = lines;
          _error = null;
        });
      } catch (e) {
        if (mounted && token == _token) setState(() => _error = e);
      } finally {
        if (mounted && token == _token) setState(() => _loading = false);
      }
    });
  }

  void _persist() => ref.read(cartControllerProvider.notifier).setLines(_lines.where((l) => l.quantity > 0).toList());

  void _step(ShoppingLine line, int delta) {
    setState(() => line.quantity = PurchaseRules.stepQuantity(line, delta));
    _persist();
  }

  void _toggle(ShoppingLine line) {
    setState(() {
      line.quantity = line.quantity > 0 ? 0 : PurchaseRules.stepQuantity(line, 1);
    });
    _persist();
  }

  @override
  Widget build(BuildContext context) {
    ref.listen(cartControllerProvider, (_, next) => _recalculate(next));
    ref.listen(pricesVersionProvider, (_, _) {
      _calculatedFor = '';
      _recalculate(ref.read(cartControllerProvider));
    });
    final priced = _lines.where((l) => l.quantity > 0 && l.unitPrice != null).toList();
    final anyPrice = _lines.any((l) => l.unitPrice != null);
    final total = priced.fold<double>(0, (a, l) => a + (l.cost ?? 0));
    final missing = _lines.where((l) => l.quantity > 0 && l.unitPrice == null).length;
    final cart = ref.watch(cartControllerProvider);
    final s = context.s;
    Widget empty(String t, {bool err = false}) => Padding(
      padding: EdgeInsets.symmetric(vertical: s(48), horizontal: s(28)),
      child: Text(
        t,
        textAlign: TextAlign.center,
        style: Fa.text(s(28), color: err ? const Color(0xFFC0392B) : const Color(0xFF8A6A4A)),
      ),
    );
    final hasMeals = cart.items.any((i) => i.repeatCount > 0);
    return Directionality(
      textDirection: TextDirection.rtl,
      child: SafeArea(
        bottom: false,
        child: ListView(
          padding: EdgeInsets.only(bottom: faBottomSpace(context, ref)),
          children: [
            const FaScreenHeader(
              title: 'طلباتي',
              art: 'assets/screens/art/ingredients.svg',
              titleIcon: 'basket',
              subtitle: 'المكونات المطلوبة للأكلات التي اخترتها',
            ),
            if (_error != null && _lines.isEmpty && hasMeals) empty('$_error', err: true),
            if (!hasMeals) ...[
              empty('لا توجد أكلات مختارة بعد. اختر من الأكلات أو المفضلة.'),
              Center(
                child: SizedBox(
                  width: s(440),
                  child: FaCta(label: 'اختار أكلتك', icon: 'cloche', onTap: () => context.go('/meals')),
                ),
              ),
            ] else if (_loading && _lines.isEmpty)
              empty('جاري حساب المكونات...')
            else if (_lines.isEmpty && _error == null)
              empty('لا توجد مكونات محسوبة لهذه الأكلات.')
            else if (_lines.isNotEmpty) ...[
              Padding(
                padding: EdgeInsets.fromLTRB(s(28), s(24), s(28), s(16)),
                child: Row(
                  children: [
                    Expanded(child: Text('المكونات', style: Fa.text(s(32), weight: FontWeight.w800))),
                    if (anyPrice) FaPriceTag(total, size: 26, prefix: 'الإجمالي ', missing: missing),
                  ],
                ),
              ),
              Padding(
                padding: EdgeInsets.symmetric(horizontal: s(22)),
                child: Column(
                  children: [
                    for (var i = 0; i < _lines.length; i++) ...[
                      if (i > 0) SizedBox(height: s(12)),
                      _LineCard(line: _lines[i], onStep: (d) => _step(_lines[i], d), onToggle: () => _toggle(_lines[i])),
                    ],
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// One ingredient card (`.ig-ing`, phone layout): picture, name, store, unit, stepper, remove.
class _LineCard extends StatelessWidget {
  const _LineCard({required this.line, required this.onStep, required this.onToggle});
  final ShoppingLine line;
  final ValueChanged<int> onStep;
  final VoidCallback onToggle;

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    final off = line.quantity <= 0;
    final muted = const Color(0xFF8A7B6E);
    final qty = PurchaseRules.qtyText(line);
    final place = (line.purchasePlaceName ?? '').trim();
    final shopColor = off ? const Color(0xFF9A7B62) : const Color(0xFFF06A02);
    Widget card = Container(
      padding: EdgeInsets.all(s(14)),
      decoration: BoxDecoration(
        color: off ? const Color(0xFFF5F0EA) : Fa.card,
        borderRadius: BorderRadius.circular(s(28)),
        border: Border.all(color: off ? const Color(0xFFE0D5C8) : const Color(0xFFF3E8DC), width: s(1.5)),
        boxShadow: [Fa.shadow(context, y: 5, blur: 16)],
      ),
      // One line, like the web row: photo | name, place, place chip | divider | unit | counter | trash.
      child: Row(
        children: [
          Opacity(
            opacity: off ? .7 : 1,
            // A slider when the ingredient has more than one photo; hover or long-press zooms it.
            child: FaZoom(
              images: line.images.isNotEmpty ? line.images : [if (line.imagePath != null) line.imagePath!],
              fallbackAsset: 'assets/screens/ingredients/onion.png',
              child: line.images.length > 1
                ? ClipRRect(
                    borderRadius: BorderRadius.circular(s(18)),
                    child: SizedBox(
                      width: s(150),
                      height: s(130),
                      child: PhotoSlider(images: line.images, fallback: line.imagePath, fallbackAsset: 'assets/screens/ingredients/onion.png'),
                    ),
                  )
                : NetImage(
                    line.imagePath,
                    width: s(150),
                    height: s(130),
                    radius: BorderRadius.circular(s(18)),
                    fallbackAsset: 'assets/screens/ingredients/onion.png',
                  ),
            ),
          ),
          SizedBox(width: s(14)),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  line.name ?? '',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: Fa.text(s(32), weight: FontWeight.w800, color: off ? muted : Fa.ink),
                ),
                SizedBox(height: s(4)),
                Text(
                  place.isEmpty ? 'البقالة' : place,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: Fa.text(s(22), weight: FontWeight.w500, color: off ? muted : const Color(0xFF5E5148)),
                ),
                if (!off && line.cost != null) ...[
                  SizedBox(height: s(6)),
                  FaPriceTag(line.cost!, size: 20),
                ],
                SizedBox(height: s(8)),
                Container(
                  height: s(40),
                  padding: EdgeInsets.symmetric(horizontal: s(14)),
                  decoration: BoxDecoration(
                    color: off ? const Color(0xFFEDE6DC) : null,
                    gradient: off ? null : const LinearGradient(colors: [Color(0xFFFFF3E2), Color(0xFFFDE7CC)]),
                    borderRadius: BorderRadius.circular(s(20)),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      FaIcon('cart', size: s(24), color: shopColor, weight: 2.2),
                      SizedBox(width: s(8)),
                      Flexible(
                        child: Text(
                          place.isEmpty ? 'البقالة' : place,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: Fa.text(s(20), weight: FontWeight.w800, color: shopColor, height: 1),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          SizedBox(width: s(10)),
          Container(width: s(2), height: s(64), color: off ? const Color(0xFFD0C6BA) : const Color(0xFFD8CBBE)),
          SizedBox(width: s(10)),
          // Unit: small beige chip.
          Container(
            height: s(48),
            constraints: BoxConstraints(minWidth: s(70)),
            padding: EdgeInsets.symmetric(horizontal: s(12)),
            alignment: Alignment.center,
            decoration: BoxDecoration(color: const Color(0xFFFBF1E4), borderRadius: BorderRadius.circular(s(24))),
            child: Text(PurchaseRules.unitText(line), style: Fa.text(s(22), color: off ? muted : Fa.ink, height: 1)),
          ),
          SizedBox(width: s(10)),
          // The same counter as the meals list.
          SizedBox(width: s(158), child: FaStepper(qty: off ? 0 : 1, label: qty, onStep: onStep)),
          SizedBox(width: s(10)),
          Tap(
            onTap: onToggle,
            child: Container(
              width: s(48),
              height: s(48),
              alignment: Alignment.center,
              decoration: BoxDecoration(shape: BoxShape.circle, color: off ? const Color(0xFFE4F0D2) : const Color(0xFFFDE8E8)),
              child: FaIcon(off ? 'plusCircle' : 'trash', size: s(26), color: off ? Fa.green : const Color(0xFFC0392B)),
            ),
          ),
        ],
      ),
    );
    return card;
  }
}
