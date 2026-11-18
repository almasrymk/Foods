import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../app/providers.dart';
import '../../../app/widgets/fa_chrome.dart';
import '../../../app/widgets/fa_top_bar.dart';
import '../../../app/widgets/fa_widgets.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/ui/fa_kit.dart';
import '../../../core/widgets/async_views.dart';
import '../../../core/widgets/net_image.dart';
import '../../cart/presentation/cart_controller.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../catalog/presentation/catalog_providers.dart';
import '../../notifications/presentation/notifications_providers.dart';
import '../../prices/prices.dart';
import '../../account/presentation/settings_page.dart' show familyProvider;

final _suggestedProvider = FutureProvider.autoDispose<List<ItemCard>>(
  (ref) {
    ref.watch(pricesVersionProvider);
    return ref.watch(catalogRepositoryProvider).items(const SuggestedSource(), const ItemQuery(), take: 20);
  },
);

/// Home page, ported from pages/home (all sizes are 841px mockup units).
class HomePage extends ConsumerWidget {
  const HomePage({super.key});

  Future<void> _refresh(WidgetRef ref) async {
    ref.invalidate(mealsProvider);
    ref.invalidate(cuisinesProvider);
    ref.invalidate(categoriesProvider);
    ref.invalidate(groupsProvider);
    ref.invalidate(_suggestedProvider);
    ref.invalidate(unreadCountProvider);
    await ref.read(_suggestedProvider.future).catchError((_) => <ItemCard>[]);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final u = context.u;
    return Directionality(
      textDirection: TextDirection.rtl,
      child: SafeArea(
        bottom: false,
        child: RefreshIndicator(
          color: Fa.orange,
          onRefresh: () => _refresh(ref),
          child: ListView(
            padding: EdgeInsets.only(bottom: faBottomSpace(context, ref)),
            children: [
              const _Header(),
              SizedBox(height: u(18)),
              const _MealsLane(),
              const _PlanBanner(),
              SizedBox(height: u(30)),
              const _BrowseTabs(),
              SizedBox(height: u(44)),
              _SectionHead(title: 'أكلات مقترحة لك', verified: true, onAll: () => context.push('/meals/all?name=${Uri.encodeComponent('الكل')}')),
              SizedBox(height: u(22)),
              const _SuggestedLane(),
            ],
          ),
        ),
      ),
    );
  }
}

class _Header extends ConsumerWidget {
  const _Header();

  void _openSearch(BuildContext context, ItemQuery q) {
    final params = q.toRoute();
    if (params.isEmpty) return;
    params['name'] = 'الكل';
    context.push(Uri(path: '/meals/all', queryParameters: params).toString());
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final u = context.u;
    final s = context.s;
    return SizedBox(
      height: u(410),
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Positioned(
            left: 0,
            top: 0,
            width: u(345),
            height: u(410),
            child: EdgeFade(left: .06, right: .15, bottom: .06, child: Image.asset('assets/images/home/hero.png', fit: BoxFit.fill)),
          ),
          const Positioned(left: 0, right: 0, top: 0, child: FaTopBar()),
          Positioned(
            right: u(66),
            top: u(148),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text('مرحباً بك', style: Fa.text(u(49), weight: FontWeight.w800, height: 1.2)),
                SizedBox(width: u(16)),
                Image.asset('assets/images/home/wave.png', width: u(58), height: u(56)),
              ],
            ),
          ),
          Positioned(
            right: u(66),
            top: u(222),
            child: Text('استمتع بوجبة أسهل وأصح', style: Fa.text(u(29), color: const Color(0xFF4A3324))),
          ),
          Positioned(
            left: 0,
            right: 0,
            top: u(396) - s(94),
            child: FaSearchBar(query: const ItemQuery(), onChanged: (_) {}, onSubmitted: (q) => _openSearch(context, q)),
          ),
        ],
      ),
    );
  }
}

/// A horizontal lane with the web's small orange prev / next arrows.
class _Lane extends StatefulWidget {
  const _Lane({required this.height, required this.itemCount, required this.itemBuilder, required this.gap, this.arrowTop, this.outside = false, this.pad = 0});
  final double height;
  final int itemCount;
  final IndexedWidgetBuilder itemBuilder;
  final double gap;
  final double? arrowTop;
  final bool outside;

  /// Space before the first and after the last item (inside the scroll view).
  final double pad;

  @override
  State<_Lane> createState() => _LaneState();
}

class _LaneState extends State<_Lane> {
  final _ctl = ScrollController();
  bool _prev = false, _next = false;

  @override
  void initState() {
    super.initState();
    _ctl.addListener(_sync);
    WidgetsBinding.instance.addPostFrameCallback((_) => _sync());
  }

  @override
  void dispose() {
    _ctl.dispose();
    super.dispose();
  }

  void _sync() {
    if (!mounted || !_ctl.hasClients) return;
    final p = _ctl.position;
    final prev = p.pixels > 4, next = p.pixels < p.maxScrollExtent - 4;
    if (prev != _prev || next != _next)
      setState(() {
        _prev = prev;
        _next = next;
      });
  }

  void _go(int dir) {
    final p = _ctl.position;
    _ctl.animateTo((p.pixels + dir * p.viewportDimension * .8).clamp(0, p.maxScrollExtent), duration: const Duration(milliseconds: 300), curve: Curves.easeOut);
  }

  @override
  Widget build(BuildContext context) {
    final u = context.u;
    Widget arrow(bool next) => Tap(
      onTap: () => _go(next ? 1 : -1),
      child: Opacity(
        opacity: .5,
        child: Container(
          width: u(42),
          height: u(42),
          alignment: Alignment.center,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: Fa.orange,
            boxShadow: [BoxShadow(color: const Color(0x47F37202), offset: Offset(0, u(2)), blurRadius: u(8))],
          ),
          child: FaIcon(next ? 'chevronLeft' : 'chevronRight', size: u(22), color: Colors.white, weight: 2.4),
        ),
      ),
    );
    final top = widget.arrowTop ?? (widget.height - u(42)) / 2;
    final edge = widget.outside ? u(-50) : u(6);
    WidgetsBinding.instance.addPostFrameCallback((_) => _sync());
    return SizedBox(
      height: widget.height,
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Positioned.fill(
            child: ListView.separated(
              controller: _ctl,
              scrollDirection: Axis.horizontal,
              clipBehavior: Clip.none,
              padding: EdgeInsets.symmetric(horizontal: widget.pad),
              itemCount: widget.itemCount,
              separatorBuilder: (_, _) => SizedBox(width: widget.gap),
              itemBuilder: widget.itemBuilder,
            ),
          ),
          if (_prev) Positioned(right: edge, top: top, child: arrow(false)),
          if (_next) Positioned(left: edge, top: top, child: arrow(true)),
        ],
      ),
    );
  }
}

class _MealsLane extends ConsumerWidget {
  const _MealsLane();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final u = context.u;
    final meals = ref.watch(mealsProvider);
    return Padding(
      padding: EdgeInsets.only(right: u(33), left: u(30)),
      child: meals.when(
        loading: () => SizedBox(
          height: u(206),
          child: const LoadingView(padding: EdgeInsets.zero),
        ),
        error: (e, _) => SizedBox(
          height: u(206),
          child: Center(
            child: TextButton(onPressed: () => ref.invalidate(mealsProvider), child: const Text('حاول تاني')),
          ),
        ),
        data: (rows) => _Lane(
          // Grows with the chosen font size so the count never drops out of the tile.
          height: u(206) + u(64) * ((MediaQuery.textScalerOf(context).scale(10) / 10) - 1).clamp(0.0, 1.0),
          gap: u(6),
          itemCount: rows.length,
          itemBuilder: (_, i) {
            final m = rows[i];
            final tone = AppColors.mealTones[i % AppColors.mealTones.length];
            final bg = AppColors.parse(m.color) ?? tone.$1;
            final fg = AppColors.parse(m.textColor) ?? tone.$2;
            final name = m.name ?? '';
            return Tap(
              onTap: () => context.push('/meals/${m.id}?name=${Uri.encodeComponent(name)}'),
              child: Container(
                width: u(150),
                padding: EdgeInsets.fromLTRB(u(8), u(6), u(8), u(10)),
                decoration: BoxDecoration(
                  color: bg,
                  borderRadius: BorderRadius.circular(u(19)),
                  boxShadow: [BoxShadow(color: const Color(0x0F000000), offset: Offset(0, u(3)), blurRadius: u(8))],
                ),
                child: Column(
                  children: [
                    SizedBox(
                      height: u(82),
                      width: double.infinity,
                      child: NetImage(m.imagePath, fit: BoxFit.contain, fallbackAsset: 'assets/images/home/cat_lunch.png'),
                    ),
                    if ((m.iconPath ?? '').isNotEmpty)
                      SizedBox(
                        width: u(42),
                        height: u(42),
                        child: NetImage(m.iconPath, fit: BoxFit.contain),
                      ),
                    SizedBox(height: u(2)),
                    FittedBox(
                      fit: BoxFit.scaleDown,
                      child: Text(
                        name,
                        maxLines: 1,
                        softWrap: false,
                        style: Fa.text(u(name.length > 8 ? 22 : 28), weight: FontWeight.w800, color: fg),
                      ),
                    ),
                    // The count sits on the tile's bottom line, the same height in every tile.
                    const Spacer(),
                    Text(
                      '${m.count}',
                      style: Fa.text(u(19), weight: FontWeight.w800, color: _darken(m.color ?? '#F37202', .35), height: 1),
                    ),
                  ],
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}

class _PlanBanner extends StatelessWidget {
  const _PlanBanner();

  @override
  Widget build(BuildContext context) {
    final u = context.u;
    return Container(
      height: u(293),
      margin: EdgeInsets.only(top: u(27), right: u(26), left: u(25)),
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(u(30)),
        gradient: const LinearGradient(colors: [Color(0xFF3E6B2A), Color(0xFF2F4D28)]),
        boxShadow: [BoxShadow(color: const Color(0x1F000000), offset: Offset(0, u(6)), blurRadius: u(14))],
      ),
      child: Stack(
        children: [
          Positioned(
            left: 0,
            top: 0,
            bottom: 0,
            width: u(395),
            child: EdgeFade(right: .2, child: Image.asset('assets/images/home/banner_art.png', fit: BoxFit.fill)),
          ),
          Positioned(
            right: u(42),
            top: u(36),
            child: Text(
              'خطط لأكل الشهر',
              style: Fa.text(u(40), weight: FontWeight.w800, color: Colors.white),
            ),
          ),
          Positioned(
            right: u(42),
            left: u(330),
            top: u(100),
            height: u(82),
            // Shrinks to fit beside the picture when the text is bigger (font size setting).
            child: Align(
              alignment: AlignmentDirectional.topStart,
              child: FittedBox(
                fit: BoxFit.scaleDown,
                alignment: AlignmentDirectional.topStart,
                child: Text(
                  'اختر أكلاتك، وحدد عدد التكرار،\nونحن نحسب لك المكونات تلقائياً',
                  style: Fa.text(u(25), weight: FontWeight.w500, color: Colors.white, height: 1.5),
                ),
              ),
            ),
          ),
          Positioned(
            left: u(402),
            right: u(25),
            top: u(195),
            height: u(70),
            child: Tap(
              onTap: () => context.push('/meals'),
              child: Container(
                padding: EdgeInsets.only(right: u(24), left: u(38)),
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(u(35)),
                  gradient: const LinearGradient(colors: [Fa.orangeLight, Fa.orange]),
                  boxShadow: [BoxShadow(color: const Color(0x26000000), offset: Offset(0, u(4)), blurRadius: u(10))],
                ),
                child: Row(
                  children: [
                    FaPath(FaPaths.calendar, size: u(44), color: Colors.white),
                    SizedBox(width: u(14)),
                    Expanded(
                      child: FittedBox(
                        fit: BoxFit.scaleDown,
                        alignment: AlignmentDirectional.centerStart,
                        child: Text(
                          'ابدأ التخطيط الآن',
                          maxLines: 1,
                          softWrap: false,
                          style: Fa.text(u(26), weight: FontWeight.w800, color: Colors.white),
                        ),
                      ),
                    ),
                    FaPath(FaPaths.chevStart, size: u(24), color: Colors.white70),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// "تصفح": المطبخ / التصنيف / المجموعة in one place, as three fixed tabs over one lane.
class _BrowseTabs extends StatefulWidget {
  const _BrowseTabs();

  @override
  State<_BrowseTabs> createState() => _BrowseTabsState();
}

class _BrowseTabsState extends State<_BrowseTabs> {
  BrowseKind _kind = BrowseKind.cuisine;

  static const _tabs = [(BrowseKind.cuisine, 'المطبخ', '/cuisines'), (BrowseKind.category, 'التصنيف', '/categories'), (BrowseKind.group, 'المجموعة', '/groups')];

  @override
  Widget build(BuildContext context) {
    final u = context.u;
    final all = _tabs.firstWhere((t) => t.$1 == _kind).$3;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _SectionHead(title: 'تصفح حسب', icon: FaPaths.browse, onAll: () => context.push(all)),
        SizedBox(height: u(14)),
        Padding(
          padding: EdgeInsets.only(right: u(30), left: u(30)),
          child: Container(
            height: u(66),
            padding: EdgeInsets.all(u(5)),
            decoration: BoxDecoration(
              color: const Color(0xFFFBEFE2),
              borderRadius: BorderRadius.circular(u(33)),
              border: Border.all(color: const Color(0xFFF3E1CC), width: u(1.5)),
            ),
            child: Row(
              children: [
                for (var i = 0; i < _tabs.length; i++) ...[
                  if (i > 0)
                    Container(
                      width: u(1.5),
                      height: u(30),
                      color: _tabs[i].$1 == _kind || _tabs[i - 1].$1 == _kind ? Colors.transparent : const Color(0xFFE7CDB0),
                    ),
                  Expanded(
                    child: Tap(
                      onTap: () => setState(() => _kind = _tabs[i].$1),
                      child: AnimatedContainer(
                        duration: const Duration(milliseconds: 180),
                        alignment: Alignment.center,
                        decoration: BoxDecoration(
                          gradient: _tabs[i].$1 == _kind ? const LinearGradient(colors: [Color(0xFFFF9A2E), Color(0xFFF37202)], begin: Alignment.topCenter, end: Alignment.bottomCenter) : null,
                          borderRadius: BorderRadius.circular(u(28)),
                          boxShadow: _tabs[i].$1 == _kind ? [BoxShadow(color: const Color(0x40F37202), offset: Offset(0, u(3)), blurRadius: u(8))] : null,
                        ),
                        child: Text(
                          _tabs[i].$2,
                          style: Fa.text(u(26), weight: FontWeight.w800, color: _tabs[i].$1 == _kind ? Colors.white : const Color(0xFF8A6E58)),
                        ),
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
        SizedBox(height: u(16)),
        _CuisinesLane(key: ValueKey(_kind), kind: _kind),
      ],
    );
  }
}

class _SectionHead extends StatelessWidget {
  const _SectionHead({required this.title, required this.onAll, this.verified = false, this.icon});
  final String title;
  final VoidCallback onAll;
  final bool verified;
  /// An orange icon before the title, like the verified badge (FaPaths shape).
  final String? icon;

  @override
  Widget build(BuildContext context) {
    final u = context.u;
    return Padding(
      padding: EdgeInsets.only(right: u(32), left: u(28)),
      child: Row(
        children: [
          if (verified || icon != null) ...[FaPath(icon ?? FaPaths.verified, size: u(42), color: Fa.orange), SizedBox(width: u(12))],
          Text(title, style: Fa.text(u(35), weight: FontWeight.w800)),
          const Spacer(),
          Tap(
            onTap: onAll,
            child: Row(
              children: [
                Text(
                  'عرض الكل',
                  style: Fa.text(u(25), weight: FontWeight.w800, color: Fa.orange),
                ),
                SizedBox(width: u(8)),
                FaPath(FaPaths.chevBack, size: u(20), color: Fa.orange),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

Color _darken(String? hex, double amount) {
  final c = AppColors.parse(hex);
  if (c == null) return const Color(0xFFC45A00);
  int ch(double v) => (v * 255 * (1 - amount)).round().clamp(0, 255);
  return Color.fromARGB(255, ch(c.r), ch(c.g), ch(c.b));
}

/// A lane of cuisine / category / group tiles (same look); a tap opens their dishes.
class _CuisinesLane extends ConsumerWidget {
  const _CuisinesLane({super.key, this.kind = BrowseKind.cuisine});
  final BrowseKind kind;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final u = context.u;
    final provider = kind.provider;
    final cuisines = ref.watch(provider);
    // Full width, first cuisine lined up with the page margin (no inset gap at the start).
    return Padding(
      padding: EdgeInsets.zero,
      child: cuisines.when(
        loading: () => SizedBox(
          height: u(126),
          child: const LoadingView(padding: EdgeInsets.zero),
        ),
        error: (e, _) => SizedBox(
          height: u(126),
          child: Center(
            child: TextButton(onPressed: () => ref.invalidate(provider), child: const Text('حاول تاني')),
          ),
        ),
        data: (all) {
          // Categories and groups get a "الكل" tile first, like the cuisines.
          final rows = kind == BrowseKind.cuisine
              ? all
              : [
                  FilterChipData(name: 'الكل', count: ref.watch(browseTotalProvider).value ?? 0),
                  ...all.where((c) => (c.id ?? '').isNotEmpty),
                ];
          // Taller tiles when the text is bigger (font size setting).
          final grow = (MediaQuery.textScalerOf(context).scale(10) / 12.5).clamp(1.0, 1.4);
          // A name too long for one line (e.g. "حلويات شرقية") keeps its size and wraps to a second line;
          // then every tile of the lane gets taller together so the row stays even.
          final nameStyle = Fa.text(u(21), weight: FontWeight.w800, height: 1.1);
          final scaler = MediaQuery.textScalerOf(context);
          final lineH = scaler.scale(u(21)) * 1.1;
          final twoLines = rows.any((c) {
            final painter = TextPainter(
              text: TextSpan(text: c.name, style: nameStyle),
              textScaler: scaler,
              textDirection: TextDirection.rtl,
              maxLines: 1,
            )..layout(maxWidth: u(143) - u(12));
            final over = painter.didExceedMaxLines;
            painter.dispose();
            return over;
          });
          final nameBox = twoLines ? lineH * 2 + u(2) : lineH + u(2);
          return _Lane(
          height: u(126) * grow + (twoLines ? lineH : 0),
          gap: u(13),
          pad: u(30),
          itemCount: rows.length,
          itemBuilder: (_, i) {
            final c = rows[i];
            final text = AppColors.parse(c.textColor) ?? Fa.ink;
            return Tap(
              onTap: () => context.push(Uri(path: '/meals/all', queryParameters: {'name': (c.id ?? '').isEmpty ? 'الكل' : c.name, if (c.id != null) kind.param: c.id!}).toString()),
              child: Container(
                width: u(143),
                padding: EdgeInsets.only(bottom: u(8)),
                decoration: BoxDecoration(
                  color: AppColors.parse(c.color) ?? const Color(0xFFFFF3E6),
                  borderRadius: BorderRadius.circular(u(19)),
                  boxShadow: [BoxShadow(color: const Color(0x0F000000), offset: Offset(0, u(3)), blurRadius: u(8))],
                ),
                child: Column(
                  children: [
                    SizedBox(
                      height: u(61),
                      child: Center(
                        child: (c.iconPath ?? '').contains('/') || (c.iconPath ?? '').startsWith('http')
                            ? NetImage(c.iconPath, width: u(46), height: u(46), fit: BoxFit.contain)
                            : FaIcon((c.iconPath ?? '').trim().isNotEmpty ? c.iconPath!.trim() : _cuisineIcon(c.name), size: u(38), color: text, weight: 1.6),
                      ),
                    ),
                    Transform.translate(
                      offset: Offset(0, u(-3)),
                      child: Padding(
                        padding: EdgeInsets.symmetric(horizontal: u(6)),
                        child: SizedBox(
                          height: nameBox,
                          child: Center(
                            child: Text(
                              c.name,
                              maxLines: 2,
                              textAlign: TextAlign.center,
                              overflow: TextOverflow.ellipsis,
                              style: nameStyle.copyWith(color: text),
                            ),
                          ),
                        ),
                      ),
                    ),
                    SizedBox(height: u(9)),
                    Text(
                      '${c.count}',
                      style: Fa.text(u(19), weight: FontWeight.w800, color: _darken(c.color ?? c.textColor ?? '#F37202', .35), height: 1),
                    ),
                  ],
                ),
              ),
            );
          },
        );
        },
      ),
    );
  }
}

class _SuggestedLane extends ConsumerWidget {
  const _SuggestedLane();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final u = context.u;
    final items = ref.watch(_suggestedProvider);
    return Padding(
      padding: EdgeInsets.symmetric(horizontal: u(27)),
      child: items.when(
        loading: () => SizedBox(
          height: u(420),
          child: const LoadingView(padding: EdgeInsets.zero),
        ),
        error: (e, _) => SizedBox(
          height: u(420),
          child: MessageView.error(e, onRetry: () => ref.invalidate(_suggestedProvider)),
        ),
        data: (rows) => rows.isEmpty
            ? SizedBox(
                height: u(200),
                child: const MessageView(message: 'مفيش اقتراحات دلوقتي'),
              )
            : _Lane(
                height: u(420) + _cardGrow(context),
                gap: u(23),
                arrowTop: u(200),
                itemCount: rows.length,
                itemBuilder: (_, i) => Padding(
                  padding: EdgeInsets.only(bottom: u(22)),
                  child: _MealCard(item: rows[i]),
                ),
              ),
      ),
    );
  }
}

/// A suggested dish's name gets two full lines at the chosen font size; the card (and its lane) grow by what
/// the bigger text needs, so the second line is never cut off.
TextStyle _nameStyle(BuildContext context) => Fa.text(context.u(25), weight: FontWeight.w800, height: 1.3);

/// Every line of the name is exactly 1.3 × the font size (Arabic glyphs can't push it taller).
StrutStyle _nameStrut(BuildContext context) => StrutStyle(
  fontFamily: 'Tajawal',
  fontSize: context.u(25),
  height: 1.3,
  forceStrutHeight: true,
  leadingDistribution: TextLeadingDistribution.even,
);

/// Two lines of the name measured with the real font and the font-size setting.
double _nameHeight(BuildContext context) {
  final painter = TextPainter(
    text: TextSpan(text: 'أم علي\nباللبن', style: _nameStyle(context)),
    strutStyle: _nameStrut(context),
    textScaler: MediaQuery.textScalerOf(context),
    textDirection: TextDirection.rtl,
    maxLines: 2,
  )..layout();
  final h = painter.height + context.u(8);
  painter.dispose();
  return h < context.u(68) ? context.u(68) : h;
}

/// How much taller the card gets than the design: the name's extra height and the bigger meta line.
double _cardGrow(BuildContext context) {
  final scale = MediaQuery.textScalerOf(context).scale(10) / 10;
  final grow = _nameHeight(context) - context.u(68) + context.u(25) * 1.3 * (scale - 1);
  return grow > 0 ? grow : 0;
}

class _MealCard extends ConsumerWidget {
  const _MealCard({required this.item});
  final ItemCard item;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final u = context.u;
    final qty = ref.watch(cartControllerProvider.select((c) => c.count(item.id)));
    final fav = ref.watch(favoriteOverridesProvider)[item.id] ?? item.isFavorite;
    // The family size, the same number the dish details page shows.
    final members = ref.watch(familyProvider).value?.length ?? 0;
    final people = members > 0 ? members : 1;
    final ctl = ref.read(cartControllerProvider.notifier);
    final sel = qty > 0;
    Widget round(bool plus) => Tap(
      onTap: () => ctl.setQty(faSelectionOf(item), (qty + (plus ? 1 : -1)).clamp(0, 999)),
      child: Container(
        width: u(50),
        height: u(50),
        alignment: Alignment.center,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          color: plus ? null : Colors.white,
          gradient: plus ? const LinearGradient(colors: [Fa.orangeLight, Fa.orange], begin: Alignment.topLeft, end: Alignment.bottomRight) : null,
          boxShadow: [BoxShadow(color: Color(plus ? 0x26000000 : 0x1A000000), offset: Offset(0, u(2)), blurRadius: u(6))],
        ),
        child: FaPath(plus ? FaPaths.plus : FaPaths.minus, size: u(36), color: plus ? Colors.white : const Color(0xFFC0392B)),
      ),
    );
    return Tap(
      onTap: () => context.push(faItemLink(item)),
      child: Container(
        width: u(244),
        height: u(398) + _cardGrow(context),
        clipBehavior: Clip.antiAlias,
        decoration: BoxDecoration(
          color: sel ? null : const Color(0xFFFFFBF6),
          gradient: sel ? Fa.selGreen : null,
          borderRadius: BorderRadius.circular(u(26)),
          border: Border.all(color: sel ? const Color(0xFF6EA43C) : Colors.transparent, width: u(1.5)),
          boxShadow: [BoxShadow(color: const Color(0x1A8A5A2B), offset: Offset(0, u(6)), blurRadius: u(16))],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            SizedBox(
              height: u(176),
              child: Stack(
                fit: StackFit.expand,
                children: [
                  FaZoom(
                    images: item.images.isNotEmpty ? item.images : [if (item.imagePath != null) item.imagePath!],
                    fallbackAsset: 'assets/screens/foods/peppers.png',
                    hoverScale: 1.08,
                    child: NetImage(item.imagePath, fallbackAsset: 'assets/screens/foods/peppers.png'),
                  ),
                  if (sel)
                    Positioned(
                      left: u(10),
                      top: u(10),
                      child: Container(
                        width: u(42),
                        height: u(42),
                        alignment: Alignment.center,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: const Color(0xFF3E8E1E),
                          border: Border.all(color: Colors.white, width: u(3)),
                        ),
                        child: FaPath(FaPaths.check, size: u(24), color: Colors.white),
                      ),
                    ),
                  Positioned(
                    right: u(10),
                    top: u(10),
                    child: Tap(
                      onTap: () async {
                        try {
                          await ref.read(favoriteOverridesProvider.notifier).toggle(item.id, fav);
                        } catch (e) {
                          if (context.mounted) showMessage(context, e, error: true);
                        }
                      },
                      child: Container(
                        width: u(54),
                        height: u(54),
                        alignment: Alignment.center,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: Colors.white,
                          boxShadow: [BoxShadow(color: const Color(0x1A000000), blurRadius: u(6))],
                        ),
                        child: FaPath(fav ? FaPaths.heartOn : FaPaths.heartOff, size: u(34), color: fav ? const Color(0xFFE8222E) : Fa.ink),
                      ),
                    ),
                  ),
                ],
              ),
            ),
            SizedBox(height: u(14)),
            SizedBox(
              height: _nameHeight(context),
              child: Padding(
                padding: EdgeInsets.symmetric(horizontal: u(14)),
                child: Text(
                  item.name ?? '',
                  maxLines: 2,
                  textAlign: TextAlign.center,
                  overflow: TextOverflow.ellipsis,
                  strutStyle: _nameStrut(context),
                  style: _nameStyle(context),
                ),
              ),
            ),
            SizedBox(height: u(6)),
            Padding(
              padding: EdgeInsets.zero,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  FaPath(FaPaths.clock, size: u(27), color: const Color(0xFF5B4636)),
                  SizedBox(width: u(6)),
                  Text('30 د', style: Fa.text(u(25), color: const Color(0xFF5B4636))),
                  SizedBox(width: u(46)),
                  Text('$people', style: Fa.text(u(25), color: const Color(0xFF5B4636))),
                  SizedBox(width: u(6)),
                  FaPath(FaPaths.personSmall, size: u(30), color: const Color(0xFF5B4636)),
                ],
              ),
            ),
            const Spacer(),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                round(true),
                if (sel) ...[
                  SizedBox(width: u(24)),
                  SizedBox(
                    width: u(30),
                    child: Text('$qty', textAlign: TextAlign.center, style: Fa.text(u(28))),
                  ),
                  SizedBox(width: u(24)),
                  round(false),
                ],
              ],
            ),
            SizedBox(height: u(32)),
          ],
        ),
      ),
    );
  }
}

String _cuisineIcon(String name) {
  if (name.trim() == 'الكل') return 'grid';
  if (RegExp('نباتي|نبات|فيجان|vegan').hasMatch(name)) return 'vegan';
  if (RegExp('صحي|سلطة|دايت').hasMatch(name)) return 'leaf';
  if (RegExp('مشوي|كباب|شيش|ستيك|مشويات').hasMatch(name)) return 'grill';
  if (RegExp('غربي|باستا|بيتزا|برجر').hasMatch(name)) return 'chef';
  return 'cloche';
}
