import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/ui/fa_kit.dart';
import '../../core/widgets/async_views.dart';
import '../../features/cart/domain/selected_item.dart';
import '../../features/cart/presentation/cart_controller.dart';

/// Wraps every signed-in screen with the floating tab bar and the
/// "تم اختيار ..." bar, like app.component.ts does on the web.
class FaChrome extends ConsumerStatefulWidget {
  const FaChrome({super.key, required this.location, required this.child});
  final String location;
  final Widget child;

  @override
  ConsumerState<FaChrome> createState() => _FaChromeState();
}

class _FaChromeState extends ConsumerState<FaChrome> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(cartControllerProvider.notifier).boot());
  }

  @override
  Widget build(BuildContext context) {
    final path = widget.location.split('?').first;
    final purchaseOrder = RegExp(r'^/orders/.+').hasMatch(path);
    final showSheet = path != '/settings' && !purchaseOrder;
    // While the keyboard is up the floating bars hide, so they never cover what is being typed.
    final keyboard = MediaQuery.viewInsetsOf(context).bottom > 0;
    return Scaffold(
      backgroundColor: Fa.page,
      body: Stack(
        children: [
          Positioned.fill(child: widget.child),
          if (showSheet && !keyboard) _SelectedSheet(onIngredients: path == '/ingredients', location: path),
          if (!keyboard) FaTabBar(path: path),
        ],
      ),
    );
  }
}

/// Space a scrolling screen leaves at its end so the floating bars never cover it.
double faBottomSpace(BuildContext context, WidgetRef ref) {
  final count = ref.watch(cartControllerProvider.select((c) => c.ready ? c.selectedCount : 0));
  final safe = MediaQuery.viewPaddingOf(context).bottom;
  return context.u(148 * .9 + 30) + safe + (count > 0 ? context.s(130) : 0);
}

const _homeOn =
    'M10 19v-5h4v5c0 .55.45 1 1 1h3c.55 0 1-.45 1-1v-7h1.7c.46 0 .68-.57.33-.87L12.67 3.6c-.38-.34-.96-.34-1.34 0l-8.36 7.53c-.34.3-.13.87.33.87H5v7c0 .55.45 1 1 1h3c.55 0 1-.45 1-1z';
const _homeOff = 'm12 5.69 5 4.5V18h-2v-6H9v6H7v-7.81l5-4.5M12 3 2 12h3v8h6v-6h2v6h6v-8h3L12 3z';
const _favOff =
    'M16.5 3c-1.74 0-3.41.81-4.5 2.09C10.91 3.81 9.24 3 7.5 3 4.42 3 2 5.42 2 8.5c0 3.78 3.4 6.86 8.55 11.54L12 21.35l1.45-1.32C18.6 15.36 22 12.28 22 8.5 22 5.42 19.58 3 16.5 3zm-4.4 15.55-.1.1-.1-.1C7.14 14.24 4 11.39 4 8.5 4 6.5 5.5 5 7.5 5c1.54 0 3.04.99 3.57 2.36h1.87C13.46 5.99 14.96 5 16.5 5c2 0 3.5 1.5 3.5 3.5 0 2.89-3.14 5.74-7.9 10.05z';
const _favOn =
    'M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z';
const _ordOn =
    'M19.5 3.5 18 2l-1.5 1.5L15 2l-1.5 1.5L12 2l-1.5 1.5L9 2 7.5 3.5 6 2v14H3v3c0 1.66 1.34 3 3 3h12c1.660 0 3-1.34 3-3V2l-1.5 1.5zM9 7h6v2H9zm7 0h2v2h-2zm-7 3h6v2H9zm7 0h2v2h-2zM5 18h10v2H6c-.55 0-1-.45-1-1z';
const _ordOff =
    'M19.5 3.5 18 2l-1.5 1.5L15 2l-1.5 1.5L12 2l-1.5 1.5L9 2 7.5 3.5 6 2v14H3v3c0 1.66 1.34 3 3 3h12c1.66 0 3-1.34 3-3V2l-1.5 1.5zM15 20H6c-.55 0-1-.45-1-1v-1h10v2zm4-1c0 .55-.45 1-1 1s-1-.45-1-1v-3H8V5h11v14zM9 7h6v2H9zM16 7h2v2h-2zM9 10h6v2H9zM16 10h2v2h-2z';
const _setOn =
    'M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58a.49.49 0 0 0 .12-.61l-1.92-3.32a.49.49 0 0 0-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54a.48.48 0 0 0-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.540c-.59.24-1.13.57-1.62.94l-2.39-.96a.49.49 0 0 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58a.49.49 0 0 0-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32a.49.49 0 0 0-.12-.61l-2.01-1.58zM12 15.6a3.6 3.6 0 1 1 0-7.2 3.6 3.6 0 0 1 0 7.2z';
const _setOff =
    'M19.43 12.98c.04-.32.07-.64.07-.98 0-.34-.03-.66-.07-.98l2.11-1.65c.19-.15.24-.42.12-.64l-2-3.46a.5.5 0 0 0-.61-.22l-2.49 1c-.52-.4-1.08-.73-1.69-.98l-.38-2.65A.49.49 0 0 0 14 2h-4c-.25 0-.46.18-.49.42l-.38 2.65c-.61.25-1.17.59-1.69.98l-2.49-1a.57.57 0 0 0-.18-.03c-.17 0-.34.09-.43.25l-2 3.46c-.13.22-.07.49.12.64l2.11 1.65c-.04.32-.07.65-.07.98 0 .33.03.66.07.98l-2.11 1.65c-.19.15-.24.42-.12.64l2 3.46a.5.5 0 0 0 .61.22l2.49-1c.52.4 1.08.73 1.69.98l.38 2.65c.03.24.24.42.49.42h4c.25 0 .46-.18.49-.42l.38-2.65c.61-.25 1.17-.59 1.69-.98l2.49 1c.06.02.12.03.18.03.17 0 .34-.09.43-.25l2-3.46c.12-.22.07-.49-.12-.64l-2.11-1.65zm-1.98-1.71c.04.31.05.52.05.73 0 .21-.02.43-.05.73l-.14 1.13.89.7 1.08.84-.7 1.21-1.27-.51-1.04-.42-.9.68c-.43.32-.84.56-1.250.73l-1.06.43-.16 1.13-.2 1.35h-1.4l-.19-1.35-.16-1.13-1.06-.43c-.43-.18-.83-.41-1.23-.71l-.91-.7-1.06.43-1.27.51-.7-1.21 1.08-.84.89-.7-.14-1.13c-.03-.31-.05-.54-.05-.74s.02-.43.05-.73l.14-1.13-.89-.7-1.08-.84.7-1.21 1.27.51 1.04.42.9-.68c.43-.32.84-.56 1.25-.73l1.06-.43.16-1.13.2-1.35h1.39l.19 1.35.16 1.13 1.06.43c.43.18.83.41 1.23.71l.91.7 1.06-.43 1.27-.51.7 1.21-1.07.85-.89.7.14 1.13zM12 8c-2.21 0-4 1.790-4 4s1.79 4 4 4 4-1.79 4-4-1.79-4-4-4zm0 6c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2z';

/// The floating bottom navigation (tab-bar.component.ts), drawn at 90% of the page scale.
class FaTabBar extends StatelessWidget {
  const FaTabBar({super.key, required this.path});
  final String path;

  @override
  Widget build(BuildContext context) {
    double u(num n) => context.u(n) * .9;
    final safe = MediaQuery.viewPaddingOf(context).bottom;
    final width = context.faWidth - 16;
    final homeActive = path == '/home' || path.startsWith('/meals') || path == '/cuisines' || path.startsWith('/items');

    Widget tab(String to, bool active, String on, String off, String label) => Expanded(
      child: Tap(
        onTap: () => context.go(to),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            FaPath(active ? on : off, size: u(50), color: active ? Fa.orange : const Color(0xFF5B4636)),
            SizedBox(height: u(6)),
            Padding(
              padding: EdgeInsets.symmetric(horizontal: u(4)),
              child: FittedBox(
                fit: BoxFit.scaleDown,
                child: Text(
                  label,
                  maxLines: 1,
                  softWrap: false,
                  style: Fa.text(
                    u(25),
                    weight: active ? FontWeight.w800 : FontWeight.w700,
                    color: active ? Fa.orange : const Color(0xFF5B4636),
                    height: 1.15,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );

    final basketActive = path == '/ingredients';
    return Stack(
      children: [
        // Page-colored strip under the bar so content never shows through the gap.
        Positioned(
          left: 0,
          right: 0,
          bottom: 0,
          height: u(22 + 126) + safe,
          child: const IgnorePointer(
            child: DecoratedBox(
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [Color(0x00FDF8F3), Fa.page],
                  stops: [0, .22],
                ),
              ),
            ),
          ),
        ),
        Positioned(
          left: 8,
          width: width,
          bottom: u(22) + safe,
          height: u(126),
          child: Stack(
            clipBehavior: Clip.none,
            alignment: Alignment.topCenter,
            children: [
              Container(
                decoration: BoxDecoration(
                  color: const Color(0xFFFFFAF3),
                  borderRadius: BorderRadius.circular(u(63)),
                  boxShadow: [BoxShadow(color: const Color(0x248A5A2B), offset: Offset(0, u(6)), blurRadius: u(24))],
                ),
                child: Row(
                  textDirection: TextDirection.rtl,
                  children: [
                    tab('/home', homeActive, _homeOn, _homeOff, 'الرئيسية'),
                    tab('/favorites', path == '/favorites', _favOn, _favOff, 'أكلاتي المفضلة'),
                    SizedBox(width: u(184)),
                    tab('/orders', path == '/orders', _ordOn, _ordOff, 'طلبات قديمة'),
                    tab('/settings', path == '/settings', _setOn, _setOff, 'الإعدادات'),
                  ],
                ),
              ),
              Positioned(
                top: u(-28),
                child: Tap(
                  onTap: () => context.go('/ingredients'),
                  child: Container(
                    width: u(160),
                    height: u(160),
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      border: Border.all(color: const Color(0xFFFFFAF3), width: u(4)),
                      gradient: Fa.orangeV,
                      boxShadow: [
                        if (basketActive) BoxShadow(color: const Color(0x38F37202), spreadRadius: u(5)),
                        BoxShadow(color: const Color(0x59F37202), offset: Offset(0, u(6)), blurRadius: u(16)),
                      ],
                    ),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        FaIcon('basket', size: u(64), color: Colors.white, weight: 1.9),
                        SizedBox(height: u(4)),
                        // Room on both sides so the label never touches the circle.
                        Padding(
                          padding: EdgeInsets.symmetric(horizontal: u(28)),
                          child: FittedBox(
                            fit: BoxFit.scaleDown,
                            child: Text('طلباتي', style: Fa.text(u(24), color: Colors.white, height: 1.1)),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

String _countLabel(int count) {
  if (count == 1) return 'تم اختيار أكلة واحدة';
  if (count == 2) return 'تم اختيار أكلتين';
  if (count >= 3 && count <= 10) return 'تم اختيار $count أكلات';
  return 'تم اختيار $count أكلة';
}

/// "تم اختيار ..." bar above the tab bar, opening into the list of picked dishes
/// (selected-sheet.component.ts).
class _SelectedSheet extends ConsumerStatefulWidget {
  const _SelectedSheet({required this.onIngredients, required this.location});
  final bool onIngredients;
  final String location;

  @override
  ConsumerState<_SelectedSheet> createState() => _SelectedSheetState();
}

class _SelectedSheetState extends ConsumerState<_SelectedSheet> {
  bool _expanded = false;
  bool _saving = false;

  @override
  void didUpdateWidget(covariant _SelectedSheet old) {
    super.didUpdateWidget(old);
    if (old.location != widget.location) _expanded = false;
  }

  Future<void> _save() async {
    if (_saving) return;
    setState(() => _saving = true);
    try {
      final id = await ref.read(cartControllerProvider.notifier).confirm();
      if (!mounted) return;
      setState(() => _expanded = false);
      context.push('/orders/$id');
    } catch (e) {
      if (mounted) showMessage(context, e, error: true);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  void _continue() {
    setState(() => _expanded = false);
    context.go('/ingredients');
  }

  @override
  Widget build(BuildContext context) {
    final cart = ref.watch(cartControllerProvider);
    final count = cart.ready ? cart.selectedCount : 0;
    if (count == 0 && !cart.syncing) {
      _expanded = false;
      return const SizedBox.shrink();
    }
    if (count == 0) return const SizedBox.shrink();
    final s = context.s;
    final safe = MediaQuery.viewPaddingOf(context).bottom;
    final width = (s(920)).clamp(0.0, context.faWidth - 24);
    final busy = cart.syncing || _saving;

    final Widget body = _expanded ? _panel(context, cart, count, busy) : _bar(context, count, busy);
    return Stack(
      children: [
        if (_expanded)
          Positioned.fill(
            child: Tap(onTap: () => setState(() => _expanded = false), child: const SizedBox.expand()),
          ),
        if (!_expanded)
          Positioned(
            left: 0,
            right: 0,
            bottom: s(178) + safe - s(28.9),
            height: s(100 + 14 + 28.9),
            child: const IgnorePointer(
              child: DecoratedBox(
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topCenter,
                    end: Alignment.bottomCenter,
                    colors: [Color(0x00FDF8F3), Fa.page],
                    stops: [0, .1],
                  ),
                ),
              ),
            ),
          ),
        Positioned(
          left: (context.faWidth - width) / 2,
          width: width,
          bottom: s(178) + safe,
          child: Directionality(textDirection: TextDirection.rtl, child: body),
        ),
      ],
    );
  }

  Widget _bar(BuildContext context, int count, bool busy) {
    final s = context.s;
    return Container(
      constraints: BoxConstraints(minHeight: s(100)),
      padding: EdgeInsets.symmetric(horizontal: s(18), vertical: s(12)),
      decoration: BoxDecoration(
        color: Fa.card,
        borderRadius: BorderRadius.circular(s(50)),
        boxShadow: [Fa.shadow(context, y: 6, blur: 22, color: const Color(0x1F784820))],
      ),
      child: Row(
        children: [
          Expanded(
            child: Tap(
              onTap: () => setState(() => _expanded = true),
              child: Row(
                children: [
                  Container(
                    width: s(52),
                    height: s(52),
                    alignment: Alignment.center,
                    decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0xFFE4F0D2)),
                    child: FaIcon('check', size: s(28), color: Fa.green, weight: 3),
                  ),
                  SizedBox(width: s(14)),
                  Flexible(
                    child: Text(
                      _countLabel(count),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Fa.text(s(30), weight: FontWeight.w800, color: Fa.green),
                    ),
                  ),
                ],
              ),
            ),
          ),
          SizedBox(width: s(12)),
          Container(width: s(2), height: s(48), color: const Color(0xFFE9DCCB)),
          SizedBox(width: s(12)),
          SizedBox(
            width: s(260),
            child: widget.onIngredients
                ? FaCta(label: _saving ? 'جاري الحفظ...' : 'حفظ الطلب', icon: 'basket', height: 72, busy: busy, onTap: _save)
                : FaCta(label: 'متابعة', icon: 'arrowLeft', height: 72, busy: busy, onTap: _continue),
          ),
        ],
      ),
    );
  }

  Widget _panel(BuildContext context, CartState cart, int count, bool busy) {
    final s = context.s;
    final ctl = ref.read(cartControllerProvider.notifier);
    final rows = <SelectedItem>[];
    final seen = <String>{};
    for (final i in cart.items) {
      if (i.itemId.isEmpty || i.repeatCount <= 0 || !seen.add(i.itemId.toLowerCase())) continue;
      rows.add(i);
    }
    Widget round(String path, bool plus, VoidCallback onTap) => Tap(
      onTap: busy ? null : onTap,
      child: Container(
        width: s(48),
        height: s(48),
        alignment: Alignment.center,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          color: plus ? null : Colors.white,
          gradient: plus ? Fa.orangeDiag : null,
          boxShadow: [BoxShadow(color: Color(plus ? 0x26000000 : 0x1A000000), offset: const Offset(0, 2), blurRadius: 6)],
        ),
        child: FaPath(path, size: s(34), color: plus ? Colors.white : const Color(0xFFC0392B)),
      ),
    );
    return Container(
      constraints: BoxConstraints(maxHeight: s(620)),
      padding: EdgeInsets.fromLTRB(s(22), s(20), s(22), s(22)),
      decoration: BoxDecoration(
        color: Fa.card,
        borderRadius: BorderRadius.vertical(top: Radius.circular(s(36)), bottom: Radius.circular(s(42))),
        boxShadow: [BoxShadow(color: const Color(0x29784820), offset: Offset(0, s(-8)), blurRadius: s(28))],
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            children: [
              Tap(
                onTap: busy ? null : () => setState(() => _expanded = false),
                child: Container(
                  width: s(48),
                  height: s(48),
                  alignment: Alignment.center,
                  decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0xFFF5EEE4)),
                  child: FaIcon('x', size: s(24), weight: 2.6),
                ),
              ),
              Expanded(
                child: Text(
                  'الأكلات المختارة ($count)',
                  textAlign: TextAlign.center,
                  style: Fa.text(s(30), weight: FontWeight.w800),
                ),
              ),
              SizedBox(width: s(48)),
            ],
          ),
          SizedBox(height: s(14)),
          Flexible(
            child: ConstrainedBox(
              constraints: BoxConstraints(maxHeight: s(360)),
              child: ListView.separated(
                shrinkWrap: true,
                padding: EdgeInsets.zero,
                itemCount: rows.length,
                separatorBuilder: (_, _) => SizedBox(height: s(12)),
                itemBuilder: (_, i) {
                  final m = rows[i];
                  return Container(
                    constraints: BoxConstraints(minHeight: s(82)),
                    padding: EdgeInsets.symmetric(horizontal: s(12), vertical: s(10)),
                    decoration: BoxDecoration(
                      color: const Color(0xFFFFF8F1),
                      borderRadius: BorderRadius.circular(s(22)),
                      border: Border.all(color: const Color(0xFFF0E4D6)),
                    ),
                    child: Row(
                      children: [
                        Tap(
                          onTap: busy ? null : () => ctl.remove(m.itemId),
                          child: Container(
                            width: s(46),
                            height: s(46),
                            alignment: Alignment.center,
                            decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0xFFFDE8E8)),
                            child: FaIcon('trash', size: s(24), color: const Color(0xFFC0392B)),
                          ),
                        ),
                        SizedBox(width: s(12)),
                        Expanded(
                          child: Text(
                            m.name,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: Fa.text(s(26), weight: FontWeight.w800, height: 1.3),
                          ),
                        ),
                        SizedBox(width: s(12)),
                        SizedBox(
                          width: s(150),
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              round(FaPaths.plus, true, () => ctl.step(m, 1)),
                              SizedBox(width: s(18)),
                              SizedBox(
                                width: s(28),
                                child: Text('${m.repeatCount}', textAlign: TextAlign.center, style: Fa.text(s(28), height: 1.3)),
                              ),
                              SizedBox(width: s(18)),
                              round(FaPaths.minus, false, () => m.repeatCount <= 1 ? ctl.remove(m.itemId) : ctl.step(m, -1)),
                            ],
                          ),
                        ),
                      ],
                    ),
                  );
                },
              ),
            ),
          ),
          SizedBox(height: s(18)),
          SizedBox(
            width: double.infinity,
            child: widget.onIngredients
                ? FaCta(label: _saving ? 'جاري الحفظ...' : 'حفظ الطلب', height: 72, busy: busy, onTap: _save)
                : FaCta(label: 'متابعة', height: 72, busy: busy, onTap: _continue),
          ),
        ],
      ),
    );
  }
}
