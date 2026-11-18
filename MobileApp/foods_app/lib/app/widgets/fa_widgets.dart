import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:speech_to_text/speech_to_text.dart';

import '../../core/config/app_config.dart';
import '../../core/ui/fa_kit.dart';
import '../../core/widgets/async_views.dart';
import '../../core/widgets/net_image.dart';
import '../../features/cart/domain/selected_item.dart';
import '../../features/cart/presentation/cart_controller.dart';
import '../../features/catalog/domain/catalog_models.dart';
import '../../features/catalog/presentation/catalog_providers.dart';
import 'fa_top_bar.dart';
import '../../features/prices/prices.dart';

/// Goes back, or home when there is nothing to go back to.
void faBack(BuildContext context) {
  if (context.canPop()) {
    context.pop();
  } else {
    context.go('/home');
  }
}

/// White rounded square back / share button (`.hd-back`).
class FaSquareButton extends StatelessWidget {
  const FaSquareButton({super.key, required this.icon, required this.onTap, this.weight = 2.6});
  final String icon;
  final VoidCallback onTap;
  final double weight;

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    return Tap(
      onTap: onTap,
      child: Container(
        width: s(72),
        height: s(70),
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: const Color(0xFFFFFCF8),
          borderRadius: BorderRadius.circular(s(20)),
          boxShadow: [Fa.shadow(context, y: 4, blur: 14, color: const Color(0x21784820))],
        ),
        child: FaIcon(icon, size: s(34), weight: weight),
      ),
    );
  }
}

/// Header of the list screens (screen-header.component.ts): the top bar, then either
/// the illustrated variant (art on the left, title with underline, subtitle) or the
/// centered title between two dashes.
class FaScreenHeader extends StatelessWidget {
  const FaScreenHeader({
    super.key,
    required this.title,
    this.subtitle,
    this.art,
    this.titleIcon,
    this.titleFilled = false,
    this.heart = false,
    this.height = 260,
    this.titleY = 180,
    this.titleSize = 50,
    this.backY = 155,
    this.subY = 233,
    this.onShare,
  });

  final String title;
  final String? subtitle;
  final String? art;
  final String? titleIcon;
  final bool titleFilled;
  final bool heart;
  final double height, titleY, titleSize, backY, subY;
  final VoidCallback? onShare;

  @override
  Widget build(BuildContext context) => art != null ? _photo(context) : _dashes(context);

  Widget _photo(BuildContext context) {
    final s = context.s;
    final top = context.u(128);
    final icon = titleIcon ?? (heart ? 'heart' : 'cloche');
    return SizedBox(
      height: top + s(262),
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Positioned(
            left: 0,
            top: top - s(33),
            width: s(356),
            height: s(292),
            child: EdgeFade(
              right: .22,
              top: .08,
              bottom: .15,
              child: art!.endsWith('.svg') ? SvgPicture.asset(art!, fit: BoxFit.contain) : Image.asset(art!, fit: BoxFit.cover),
            ),
          ),
          const Positioned(left: 0, right: 0, top: 0, child: FaTopBar()),
          Positioned(
            left: s(836),
            top: top + s(42),
            child: FaSquareButton(icon: 'chevronLeft', onTap: () => faBack(context)),
          ),
          Positioned(
            right: s(126),
            top: top + s(77),
            child: FractionalTranslation(
              translation: const Offset(0, -.5),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    textDirection: TextDirection.rtl,
                    children: [
                      FaIcon(icon, size: s(titleFilled ? 58 : 62), color: Fa.orange, weight: 2.2, filled: titleFilled),
                      SizedBox(width: s(16)),
                      ConstrainedBox(
                        // Keep a gap from the art on the left: long titles shrink instead of touching it.
                        constraints: BoxConstraints(maxWidth: s(941 - 126 - 356 - 62 - 16 - 30)),
                        child: FittedBox(
                          fit: BoxFit.scaleDown,
                          alignment: AlignmentDirectional.centerStart,
                          child: Text(
                            title,
                            maxLines: 1,
                            style: Fa.text(s(54), weight: FontWeight.w800, height: 1.15),
                          ),
                        ),
                      ),
                    ],
                  ),
                  Transform.translate(
                    offset: Offset(0, s(-6)),
                    child: Image.asset('assets/screens/plan/underline.png', width: s(68), height: s(22), fit: BoxFit.fill),
                  ),
                ],
              ),
            ),
          ),
          if ((subtitle ?? '').isNotEmpty)
            Positioned(
              right: s(51),
              left: s(330),
              top: top + s(159),
              child: FractionalTranslation(
                translation: const Offset(0, -.5),
                // Shrinks to one line when the text is bigger (font size setting).
                child: Align(
                  alignment: AlignmentDirectional.centerStart,
                  child: FittedBox(
                    fit: BoxFit.scaleDown,
                    child: Text(
                      subtitle!,
                      maxLines: 1,
                      style: Fa.text(s(24), weight: FontWeight.w500, color: Fa.body),
                    ),
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }

  Widget _dashes(BuildContext context) {
    final s = context.s;
    final top = context.u(128);
    final base = top - s(100);
    Widget dash({bool flip = false}) => Transform.flip(
      flipX: flip,
      child: Image.asset('assets/screens/shared/dash.png', width: s(62), height: s(22), fit: BoxFit.fill),
    );
    return SizedBox(
      height: base + s(height),
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          const Positioned(left: 0, right: 0, top: 0, child: FaTopBar()),
          if (onShare != null)
            Positioned(
              left: s(28),
              top: base + s(backY),
              child: FaSquareButton(icon: 'share', weight: 2.4, onTap: onShare!),
            ),
          Positioned(
            left: s(818),
            top: base + s(backY),
            child: FaSquareButton(icon: 'chevronLeft', onTap: () => faBack(context)),
          ),
          Positioned(
            left: 0,
            right: 0,
            top: base + s(titleY),
            child: FractionalTranslation(
              translation: const Offset(0, -.5),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                textDirection: TextDirection.rtl,
                children: [
                  dash(flip: true),
                  SizedBox(width: s(22)),
                  if (heart || titleIcon != null) ...[
                    FaIcon(titleIcon ?? 'heart', size: s(54), color: Fa.orange, weight: 2.2, filled: titleFilled),
                    SizedBox(width: s(18)),
                  ],
                  Flexible(
                    child: Text(
                      title,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Fa.text(s(titleSize), weight: FontWeight.w800, color: Fa.title, height: 1.2),
                    ),
                  ),
                  SizedBox(width: s(22)),
                  dash(),
                ],
              ),
            ),
          ),
          if ((subtitle ?? '').isNotEmpty)
            Positioned(
              left: 0,
              right: 0,
              top: base + s(subY),
              child: FractionalTranslation(
                translation: const Offset(0, -.5),
                child: Text(
                  subtitle!,
                  textAlign: TextAlign.center,
                  style: Fa.text(s(26), weight: FontWeight.w500),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

/// A section of the advanced-search panel when a screen brings its own filters (e.g. the ingredient prices).
class FaFilterSection {
  const FaFilterSection(this.key, this.title, this.options);
  final String key;
  final String title;

  /// (id, name)
  final List<(String, String)> options;
}

/// Shared search bar (search-bar.component.ts): pill with search button, mic, and the
/// green advanced-search button that opens the category + ingredient panel.
class FaSearchBar extends ConsumerStatefulWidget {
  const FaSearchBar({
    super.key,
    required this.query,
    required this.onChanged,
    this.onSubmitted,
    this.hint = 'ابحث عن أكلة، وصفة، مكون ...',
    this.showAdvanced = true,
    this.onAdvanced,
    this.active = false,
    this.left = 45,
    this.right = 33,
    this.filters,
    this.picked = const {},
    this.onPicked,
  });

  /// The screen's own advanced-search sections (in place of the dish ones); [picked] holds key → id.
  final List<FaFilterSection>? filters;
  final Map<String, String> picked;
  final ValueChanged<Map<String, String>>? onPicked;

  final double left, right;
  final ItemQuery query;
  final ValueChanged<ItemQuery> onChanged;
  final ValueChanged<ItemQuery>? onSubmitted;
  final String hint;
  final bool showAdvanced;

  /// When set, the green button calls this instead of opening the panel.
  final VoidCallback? onAdvanced;
  final bool active;

  @override
  ConsumerState<FaSearchBar> createState() => _FaSearchBarState();
}

class _FaSearchBarState extends ConsumerState<FaSearchBar> {
  late final _text = TextEditingController(text: widget.query.text);
  late final _ingredient = TextEditingController(text: widget.query.ingredient ?? '');
  final _link = LayerLink();
  final _portal = OverlayPortalController();
  Timer? _debounce;
  late ItemQuery _q = widget.query;
  static final _speech = SpeechToText();
  static bool _speechReady = false;

  /// The engine is set up once for the whole app, so its callbacks go to the bar that is listening now
  /// (not the first bar that set it up, which may be gone).
  static _FaSearchBarState? _active;
  bool _listening = false;

  /// Speaking ended: close the mic and run the search once.
  void _voiceDone() {
    if (!mounted || !_listening) return;
    setState(() => _listening = false);
    if (identical(_active, this)) _active = null;
    if (_text.text.trim().isNotEmpty) _submit();
  }

  void _voiceError(String errorMsg) {
    if (!mounted) return;
    setState(() => _listening = false);
    if (identical(_active, this)) _active = null;
    final msg = errorMsg.contains('no_match') || errorMsg.contains('speech_timeout')
        ? 'مسمعتش حاجة، جرّب تاني'
        : (errorMsg.contains('permission') ? 'اسمح للتطبيق يستخدم المايك عشان البحث بالصوت' : 'حصلت مشكلة في البحث بالصوت');
    showMessage(context, msg);
  }

  /// Voice search like the web (Web Speech API, ar-EG): words fill the box while
  /// speaking, then the search runs when listening stops.
  Future<void> _toggleVoice() async {
    if (_listening) {
      await _speech.stop();
      return;
    }
    try {
      if (!_speechReady) {
        _speechReady = await _speech.initialize(
          onStatus: (s) {
            if (s == 'done' || s == 'notListening') _active?._voiceDone();
          },
          onError: (e) => _active?._voiceError(e.errorMsg),
        );
      }
      if (!_speechReady) {
        if (mounted) showMessage(context, 'البحث بالصوت مش متاح على الجهاز ده، أو محتاج إذن المايك');
        return;
      }
      _active = this;
      setState(() => _listening = true);
      await _speech.listen(
        listenOptions: SpeechListenOptions(localeId: 'ar_EG', partialResults: true, cancelOnError: true, listenMode: ListenMode.search),
        onResult: (r) {
          if (!mounted) return;
          final t = r.recognizedWords.trim().replaceAll(RegExp(r'[.,!?؟،؛:;]+$'), '').trim();
          _text.value = TextEditingValue(
            text: t,
            selection: TextSelection.collapsed(offset: t.length),
          );
          _emit(_q.copyWith(text: t));
          // The last words: stop listening and search now (some phones never send "done").
          if (r.finalResult) {
            _speech.stop();
            _voiceDone();
          }
        },
      );
    } catch (_) {
      if (mounted) {
        setState(() => _listening = false);
        showMessage(context, 'حصلت مشكلة في البحث بالصوت');
      }
    }
  }

  @override
  void didUpdateWidget(covariant FaSearchBar old) {
    super.didUpdateWidget(old);
    _q = widget.query.copyWith(text: _text.text);
  }

  @override
  void dispose() {
    if (identical(_active, this)) {
      _active = null;
      if (_listening) _speech.stop();
    }
    _debounce?.cancel();
    _text.dispose();
    _ingredient.dispose();
    super.dispose();
  }

  void _emit(ItemQuery q, {int delay = 400}) {
    _q = q;
    _debounce?.cancel();
    _debounce = Timer(Duration(milliseconds: delay), () => widget.onChanged(q));
    setState(() {});
  }

  void _submit() {
    _debounce?.cancel();
    final q = _q.copyWith(text: _text.text);
    _portal.hide();
    setState(() {});
    (widget.onSubmitted ?? widget.onChanged)(q);
  }

  bool get _hasExtra => widget.filters != null ? widget.picked.isNotEmpty : _q.hasExtra;

  void _refresh() => setState(() {});

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    final on = _portal.isShowing || widget.active || _hasExtra;
    return CompositedTransformTarget(
      link: _link,
      child: OverlayPortal(
        controller: _portal,
        overlayChildBuilder: _panel,
        child: Padding(
          padding: EdgeInsets.only(left: s(widget.left), right: s(widget.right)),
          child: Row(
            textDirection: TextDirection.rtl,
            children: [
              Expanded(
                child: Container(
                  height: s(94),
                  padding: EdgeInsets.only(right: s(6), left: s(26)),
                  decoration: BoxDecoration(
                    color: Fa.card,
                    borderRadius: BorderRadius.circular(s(47)),
                    boxShadow: [Fa.shadow(context, y: 6, blur: 20, color: const Color(0x1F784820))],
                  ),
                  child: Row(
                    textDirection: TextDirection.rtl,
                    children: [
                      Tap(
                        onTap: _submit,
                        child: Container(
                          width: s(82),
                          height: s(82),
                          alignment: Alignment.center,
                          decoration: const BoxDecoration(shape: BoxShape.circle, gradient: Fa.orangeDiag),
                          child: FaPath(FaPaths.search, size: s(48), color: Colors.white),
                        ),
                      ),
                      SizedBox(width: s(20)),
                      Expanded(
                        // Tajawal's Arabic glyphs sit above the line centre; nudge down so text looks centred.
                        child: Transform.translate(
                          offset: Offset(0, s(9)),
                          child: TextField(
                            controller: _text,
                            textInputAction: TextInputAction.search,
                            onChanged: (v) => _emit(_q.copyWith(text: v)),
                            onSubmitted: (_) => _submit(),
                            style: Fa.text(s(27), weight: FontWeight.w500, color: const Color(0xFF3B2A1E)),
                            cursorColor: Fa.orange,
                            decoration: InputDecoration(
                              isCollapsed: true,
                              filled: false,
                              border: InputBorder.none,
                              enabledBorder: InputBorder.none,
                              focusedBorder: InputBorder.none,
                              hintText: widget.hint,
                              hintMaxLines: 1,
                              hintStyle: Fa.text(s(27), weight: FontWeight.w500, color: const Color(0xFF8C8079)),
                            ),
                          ),
                        ),
                      ),
                      if (_text.text.isNotEmpty)
                        Tap(
                          onTap: () {
                            _text.clear();
                            _emit(_q.copyWith(text: ''), delay: 0);
                          },
                          child: Padding(
                            padding: EdgeInsets.symmetric(horizontal: s(10)),
                            child: Text(
                              '×',
                              style: Fa.text(s(40), weight: FontWeight.w400, color: const Color(0xFFA08D7E), height: 1),
                            ),
                          ),
                        ),
                      Tap(
                        onTap: _toggleVoice,
                        child: Container(
                          width: s(56),
                          height: s(56),
                          decoration: BoxDecoration(shape: BoxShape.circle, color: _listening ? const Color(0xFFE5262A) : null),
                          child: Center(
                            child: FaPath(FaPaths.mic, size: s(46), color: _listening ? Colors.white : const Color(0xFF4A3426)),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              if (widget.showAdvanced) ...[
                SizedBox(width: s(17)),
                Tap(
                  onTap: () {
                    if (widget.onAdvanced != null) return widget.onAdvanced!();
                    _portal.toggle();
                    setState(() {});
                  },
                  child: Container(
                    width: s(94),
                    height: s(90),
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(s(30)),
                      gradient: on ? Fa.orangeDiag : Fa.greenRadial,
                      boxShadow: [Fa.shadow(context, y: 6, blur: 18, color: on ? const Color(0x59F36202) : const Color(0x592F7514))],
                    ),
                    child: Stack(
                      alignment: Alignment.center,
                      children: [
                        FaIcon('tune', size: s(46), color: Colors.white, weight: 2.2),
                        if (_hasExtra)
                          Positioned(
                            top: s(14),
                            right: s(14),
                            child: Container(
                              width: s(14),
                              height: s(14),
                              decoration: const BoxDecoration(color: Colors.white, shape: BoxShape.circle),
                            ),
                          ),
                      ],
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  /// Room under the search bar down to the keyboard (or 62% of the screen), so the panel scrolls instead of being cut.
  double _panelMaxHeight(BuildContext overlay) {
    final mq = MediaQueryData.fromView(View.of(overlay));
    final screen = mq.size.height;
    final box = context.findRenderObject() as RenderBox?;
    final top = box != null && box.hasSize ? box.localToGlobal(Offset.zero).dy + box.size.height + context.s(10) : screen * .3;
    final room = screen - mq.viewInsets.bottom - top - 12;
    return room.clamp(120.0, screen * .62);
  }

  Widget _panel(BuildContext context) {
    final s = context.s;
    if (widget.filters != null) return _panelFrame(context, _ownFilters(context));
    final cats = ref.watch(categoriesProvider).value ?? const <FilterChipData>[];
    final kitchens = ref.watch(cuisinesProvider).value ?? const <FilterChipData>[];
    final groups = ref.watch(groupsProvider).value ?? const <FilterChipData>[];
    final meals = ref.watch(mealsProvider).value ?? const <MealCard>[];
    Widget chip(String label, bool sel, VoidCallback onTap) => Tap(
      onTap: onTap,
      child: Container(
        height: s(58),
        padding: EdgeInsets.symmetric(horizontal: s(24)),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(s(30)),
          color: sel ? null : const Color(0xFFFBF1E4),
          gradient: sel ? Fa.orangeDiag : null,
        ),
        child: Center(
          widthFactor: 1,
          child: Text(
            label,
            style: Fa.text(s(23), weight: FontWeight.w600, color: sel ? Colors.white : const Color(0xFF6B4A2E), height: 1),
          ),
        ),
      ),
    );
    void pick(ItemQuery q) {
      _emit(q.copyWith(text: _text.text), delay: 0);
      _portal.hide();
      _portal.show();
    }

    /// [multi]: more than one can be picked; [selected] then holds the ids joined by commas.
    List<Widget> section(String title, List<(String, String)> options, String? selected, ItemQuery Function(String?) apply, {bool multi = false}) {
      final picked = (selected ?? '').split(',').where((e) => e.isNotEmpty).toList();
      String? toggled(String id) {
        final next = picked.contains(id) ? picked.where((e) => e != id).toList() : [...picked, id];
        return next.isEmpty ? null : next.join(',');
      }

      return [
        Text.rich(
          TextSpan(
            text: title,
            children: [if (multi) TextSpan(text: '  (تقدر تختار أكتر من واحد)', style: Fa.text(s(19), weight: FontWeight.w600, color: const Color(0xFF8A6E58)))],
          ),
          style: Fa.text(s(26), color: const Color(0xFF3B2A1E)),
        ),
        SizedBox(height: s(14)),
        Wrap(
          spacing: s(12),
          runSpacing: s(12),
          children: [
            chip('الكل', selected == null, () => pick(apply(null))),
            for (final o in options)
              multi ? chip(o.$2, picked.contains(o.$1), () => pick(apply(toggled(o.$1)))) : chip(o.$2, selected == o.$1, () => pick(apply(o.$1))),
          ],
        ),
        SizedBox(height: s(22)),
      ];
    }

    return Stack(
      children: [
        Positioned.fill(
          child: Tap(
            onTap: () {
              _portal.hide();
              setState(() {});
            },
            child: const SizedBox.expand(),
          ),
        ),
        CompositedTransformFollower(
          link: _link,
          targetAnchor: Alignment.bottomLeft,
          offset: Offset(s(widget.left), s(10)),
          child: Directionality(
            textDirection: TextDirection.rtl,
            child: Align(
              alignment: Alignment.topLeft,
              child: Material(
                type: MaterialType.transparency,
                child: Container(
                  width: context.faWidth - s(widget.left + widget.right),
                  padding: EdgeInsets.all(s(24)),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(s(30)),
                    boxShadow: [BoxShadow(color: const Color(0x38502D0F), offset: Offset(0, s(18)), blurRadius: s(50))],
                  ),
                  constraints: BoxConstraints(maxHeight: _panelMaxHeight(context)),
                  child: SingleChildScrollView(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        ...section('الوجبة', [for (final m in meals) (m.id, m.name ?? '')], _q.mealId, (v) => _q.copyWith(mealId: () => v)),
                        ...section(
                          'المطبخ',
                          [
                            for (final c in kitchens)
                              if (c.id != null) (c.id!, c.name),
                          ],
                          _q.kitchenId,
                          (v) => _q.copyWith(kitchenId: () => v),
                        ),
                        ...section(
                          'المجموعة',
                          [
                            for (final c in groups)
                              if (c.id != null) (c.id!, c.name),
                          ],
                          _q.groupId,
                          (v) => _q.copyWith(groupId: () => v),
                        ),
                        ...section(
                          'التصنيف',
                          [
                            for (final c in cats)
                              if (c.id != null) (c.id!, c.name),
                          ],
                          _q.categoryId,
                          (v) => _q.copyWith(categoryId: () => v),
                          multi: true,
                        ),
                        Text(
                          'فيها مكوّن (ممكن أكتر من واحد بينهم فاصلة)',
                          style: Fa.text(s(23), weight: FontWeight.w600, color: const Color(0xFF6B4A2E)),
                        ),
                        SizedBox(height: s(10)),
                        Container(
                          height: s(70),
                          padding: EdgeInsets.symmetric(horizontal: s(22)),
                          alignment: Alignment.centerRight,
                          decoration: BoxDecoration(
                            color: Fa.card,
                            borderRadius: BorderRadius.circular(s(22)),
                            border: Border.all(color: const Color(0xFFF1E3D3), width: s(2)),
                          ),
                          child: Row(
                            children: [
                              Expanded(
                                // The field fills the box with its line at the top; move the line to the middle.
                                child: Transform.translate(
                                  offset: Offset(0, s(20)),
                                  child: TextField(
                                    controller: _ingredient,
                                    maxLines: 1,
                                    textAlignVertical: TextAlignVertical.center,
                                    keyboardType: TextInputType.text,
                                    textInputAction: TextInputAction.search,
                                    onSubmitted: (_) => _submit(),
                                    onChanged: (v) => _emit(_q.copyWith(text: _text.text, ingredient: () => v.trim().isEmpty ? null : v.trim())),
                                    style: Fa.text(s(24), weight: FontWeight.w500, color: const Color(0xFF3B2A1E), height: 1.2),
                                    decoration: InputDecoration(
                                      isCollapsed: true,
                                      filled: false,
                                      border: InputBorder.none,
                                      enabledBorder: InputBorder.none,
                                      focusedBorder: InputBorder.none,
                                      hintText: 'مثلاً: فراخ، رز',
                                      hintMaxLines: 1,
                                      // Shrink to the text line so Center can centre it in the box.
                                      constraints: const BoxConstraints(),
                                      hintStyle: Fa.text(s(24), weight: FontWeight.w500, color: const Color(0xFF8C8079), height: 1.2),
                                    ),
                                  ),
                                ),
                              ),
                              // × shows only while there is text.
                              ValueListenableBuilder<TextEditingValue>(
                                valueListenable: _ingredient,
                                builder: (_, v, _) => v.text.isEmpty
                                    ? const SizedBox.shrink()
                                    : Tap(
                                        onTap: () {
                                          _ingredient.clear();
                                          _emit(_q.copyWith(text: _text.text, ingredient: () => null), delay: 0);
                                          _portal.hide();
                                          _portal.show();
                                        },
                                        child: Padding(
                                          padding: EdgeInsets.only(right: s(10)),
                                          child: Text(
                                            '×',
                                            style: Fa.text(s(40), weight: FontWeight.w400, color: const Color(0xFFA08D7E), height: 1),
                                          ),
                                        ),
                                      ),
                              ),
                            ],
                          ),
                        ),
                        SizedBox(height: s(22)),
                        Row(
                          children: [
                            Expanded(
                              child: Tap(
                                onTap: _submit,
                                child: Container(
                                  height: s(70),
                                  alignment: Alignment.center,
                                  decoration: BoxDecoration(gradient: Fa.orangeDiag, borderRadius: BorderRadius.circular(s(35))),
                                  child: Text('بحث', style: Fa.text(s(25), color: Colors.white, height: 1)),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

extension on _FaSearchBarState {
  /// The screen's own sections (one pick each), then مسح / بحث.
  List<Widget> _ownFilters(BuildContext context) {
    final s = context.s;
    final picked = widget.picked;
    Widget chip(String label, bool sel, VoidCallback onTap) => Tap(
      onTap: onTap,
      child: Container(
        height: s(58),
        padding: EdgeInsets.symmetric(horizontal: s(24)),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(s(30)),
          color: sel ? null : const Color(0xFFFBF1E4),
          gradient: sel ? Fa.orangeDiag : null,
        ),
        child: Center(
          widthFactor: 1,
          child: Text(label, style: Fa.text(s(23), weight: FontWeight.w600, color: sel ? Colors.white : const Color(0xFF6B4A2E), height: 1)),
        ),
      ),
    );
    void pick(String key, String? id) {
      final next = Map<String, String>.of(picked);
      if (id == null) {
        next.remove(key);
      } else {
        next[key] = id;
      }
      widget.onPicked?.call(next);
      _portal.hide();
      _portal.show();
    }

    return [
      for (final f in widget.filters!) ...[
        Text(f.title, style: Fa.text(s(26), color: const Color(0xFF3B2A1E))),
        SizedBox(height: s(14)),
        Wrap(
          spacing: s(12),
          runSpacing: s(12),
          children: [
            chip('الكل', picked[f.key] == null, () => pick(f.key, null)),
            for (final o in f.options) chip(o.$2, picked[f.key] == o.$1, () => pick(f.key, o.$1)),
          ],
        ),
        SizedBox(height: s(22)),
      ],
      if (widget.filters!.isEmpty)
        Padding(
          padding: EdgeInsets.only(bottom: s(22)),
          child: Text('مفيش اختيارات للبحث المتقدم دلوقتي', style: Fa.text(s(23), weight: FontWeight.w500, color: const Color(0xFF8A6E58))),
        ),
      Row(
        children: [
          Expanded(
            child: Tap(
              onTap: _submit,
              child: Container(
                height: s(70),
                alignment: Alignment.center,
                decoration: BoxDecoration(gradient: Fa.orangeDiag, borderRadius: BorderRadius.circular(s(35))),
                child: Text('بحث', style: Fa.text(s(25), color: Colors.white, height: 1)),
              ),
            ),
          ),
          SizedBox(width: s(14)),
          Tap(
            onTap: () {
              widget.onPicked?.call(const {});
              _portal.hide();
              _portal.show();
            },
            child: Container(
              height: s(70),
              padding: EdgeInsets.symmetric(horizontal: s(36)),
              alignment: Alignment.center,
              decoration: BoxDecoration(color: const Color(0xFFFBF1E4), borderRadius: BorderRadius.circular(s(35))),
              child: Text('مسح', style: Fa.text(s(25), color: const Color(0xFF6B4A2E), height: 1)),
            ),
          ),
        ],
      ),
    ];
  }

  /// The white dropdown under the bar (closes on a tap outside), with the given content.
  Widget _panelFrame(BuildContext context, List<Widget> children) {
    final s = context.s;
    return Stack(
      children: [
        Positioned.fill(
          child: Tap(
            onTap: () {
              _portal.hide();
              _refresh();
            },
            child: const SizedBox.expand(),
          ),
        ),
        CompositedTransformFollower(
          link: _link,
          targetAnchor: Alignment.bottomLeft,
          offset: Offset(s(widget.left), s(10)),
          child: Directionality(
            textDirection: TextDirection.rtl,
            child: Align(
              alignment: Alignment.topLeft,
              child: Material(
                type: MaterialType.transparency,
                child: Container(
                  width: context.faWidth - s(widget.left + widget.right),
                  padding: EdgeInsets.all(s(24)),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(s(30)),
                    boxShadow: [BoxShadow(color: const Color(0x38502D0F), offset: Offset(0, s(18)), blurRadius: s(50))],
                  ),
                  constraints: BoxConstraints(maxHeight: _panelMaxHeight(context)),
                  child: SingleChildScrollView(
                    child: Column(crossAxisAlignment: CrossAxisAlignment.start, mainAxisSize: MainAxisSize.min, children: children),
                  ),
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

/// Icon name for a cuisine / category chip (meal-type.component.ts `iconName`).
String faChipIcon(String? name) {
  final v = (name ?? '').trim();
  if (v == 'الكل') return 'grid';
  if (RegExp('نباتي|نبات|فيجان|vegan').hasMatch(v)) return 'vegan';
  if (RegExp('صحي|سلطة|دايت').hasMatch(v)) return 'leaf';
  if (RegExp('مشوي|كباب|شيش|ستيك|مشويات').hasMatch(v)) return 'grill';
  if (RegExp('غربي|باستا|بيتزا|برجر|خليجي').hasMatch(v)) return 'chef';
  return 'cloche';
}

/// One `.fa-chip`.
class FaChip extends StatelessWidget {
  const FaChip({
    super.key,
    required this.label,
    required this.on,
    required this.onTap,
    this.icon,
    this.image,
    this.fontSize = 24,
    this.pad = 22,
    this.height = 67,
    this.badge,
    this.countCircle = false,
  });
  final String label;

  /// Show [badge] as a soft round count (cuisine dish counts) instead of the red alert badge.
  final bool countCircle;
  final bool on;
  final VoidCallback onTap;
  final String? icon;
  final String? image;
  final double fontSize;
  final double pad;
  final double height;
  final int? badge;

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    final color = on ? Colors.white : Fa.green;
    return Tap(
      onTap: onTap,
      child: Container(
        height: s(height),
        padding: EdgeInsets.symmetric(horizontal: s(pad)),
        decoration: BoxDecoration(
          color: on ? null : Fa.card,
          gradient: on ? Fa.chipGradient : null,
          borderRadius: BorderRadius.circular(s(34)),
          boxShadow: [
            on ? Fa.shadow(context, y: 6, blur: 14, color: const Color(0x4DF36202)) : Fa.shadow(context, y: 4, blur: 12, color: const Color(0x1A784820)),
          ],
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          textDirection: TextDirection.rtl,
          children: [
            if ((image ?? '').isNotEmpty)
              NetImage(image, width: s(34), height: s(34), fit: BoxFit.contain)
            else if (icon != null)
              FaIcon(icon!, size: s(34), color: color, weight: 1.8),
            if ((image ?? '').isNotEmpty || icon != null) SizedBox(width: s(10)),
            Text(label, style: Fa.text(s(fontSize), color: color, height: 1)),
            if (badge != null) ...[
              SizedBox(width: s(8)),
              if (countCircle)
                Container(
                  constraints: BoxConstraints(minWidth: s(40)),
                  height: s(40),
                  padding: EdgeInsets.symmetric(horizontal: s(6)),
                  alignment: Alignment.center,
                  decoration: BoxDecoration(color: on ? Colors.white : const Color(0xFFE6F0D3), borderRadius: BorderRadius.circular(s(20))),
                  child: FittedBox(
                    fit: BoxFit.scaleDown,
                    child: Text(
                      '$badge',
                      style: Fa.text(s(19), weight: FontWeight.w800, color: on ? const Color(0xFFF26A0A) : Fa.green, height: 1),
                    ),
                  ),
                )
              else
                Container(
                  constraints: BoxConstraints(minWidth: s(32)),
                  height: s(32),
                  padding: EdgeInsets.symmetric(horizontal: s(8)),
                  alignment: Alignment.center,
                  decoration: BoxDecoration(color: const Color(0xFFE5262A), borderRadius: BorderRadius.circular(s(16))),
                  child: Text(
                    '$badge',
                    style: Fa.text(s(19), weight: FontWeight.w800, color: Colors.white, height: 1),
                  ),
                ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Horizontal row of chips (`.fa-chips`), spread out when they fit, scrolling when not.
class FaChipsRow extends StatefulWidget {
  const FaChipsRow({super.key, required this.children, this.left = 27, this.right = 25, this.selected});
  final List<Widget> children;
  final double left, right;

  /// Index of the selected chip; the row scrolls so it is visible.
  final int? selected;

  @override
  State<FaChipsRow> createState() => _FaChipsRowState();
}

class _FaChipsRowState extends State<FaChipsRow> {
  final _keys = <GlobalKey>[];
  int? _shown;

  void _reveal() {
    final i = widget.selected;
    if (i == null || i == _shown || i < 0 || i >= _keys.length) return;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final ctx = _keys[i].currentContext;
      if (ctx == null || !mounted) return;
      _shown = i;
      Scrollable.ensureVisible(ctx, alignment: .5, duration: Duration.zero);
    });
  }

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    while (_keys.length < widget.children.length) {
      _keys.add(GlobalKey());
    }
    _reveal();
    // The first chip sits at the right edge (start of an RTL row), so the row starts scrolled there.
    final rtl = Directionality.of(context) == TextDirection.rtl;
    return SizedBox(
      height: s(67 + 20),
      child: LayoutBuilder(
        builder: (context, box) => SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          reverse: !rtl,
          padding: EdgeInsets.only(left: s(widget.left), right: s(widget.right), top: s(4), bottom: s(16)),
          child: ConstrainedBox(
            constraints: BoxConstraints(minWidth: box.maxWidth - s(widget.left + widget.right)),
            child: Row(
              textDirection: TextDirection.rtl,
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                for (var i = 0; i < widget.children.length; i++) ...[if (i > 0) SizedBox(width: s(10)), KeyedSubtree(key: _keys[i], child: widget.children[i])],
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// The dish details link; a dish shown in another country's version opens in that version.
String faItemLink(ItemCard item) =>
    item.recipeCountryId == null ? '/items/${item.id}' : '/items/${item.id}?country=${item.recipeCountryId}';

SelectedItem faSelectionOf(ItemCard item) =>
    SelectedItem(itemId: item.id, name: item.name ?? '', imagePath: item.imagePath, repeatCount: 1, countryId: item.recipeCountryId);

/// Selectable dish row (food-row.component.ts / `.fa-row`): photo, tag, title,
/// description, checkbox, heart and the − n + stepper.
class FaFoodRow extends ConsumerWidget {
  const FaFoodRow({super.key, required this.item, this.redHeart = false, this.greenSelection = true});
  final ItemCard item;
  final bool redHeart;
  final bool greenSelection;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final s = context.s;
    final qty = ref.watch(cartControllerProvider.select((c) => c.count(item.id)));
    final fav = ref.watch(favoriteOverridesProvider)[item.id] ?? item.isFavorite;
    final selected = qty > 0;
    final green = greenSelection && selected;
    final ctl = ref.read(cartControllerProvider.notifier);
    void setQty(int q) => ctl.setQty(faSelectionOf(item), q < 0 ? 0 : q);
    void open() => context.push(faItemLink(item));

    return Container(
      constraints: BoxConstraints(minHeight: s(184)),
      decoration: BoxDecoration(
        color: green ? null : Fa.card,
        gradient: green ? Fa.selGreen : null,
        borderRadius: BorderRadius.circular(s(28)),
        border: Border.all(color: green ? const Color(0xFF6EA43C) : const Color(0xFFF3E8DC), width: s(green ? 2.5 : 1.5)),
        boxShadow: [Fa.shadow(context, y: 5, blur: 16)],
      ),
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Positioned(
            right: s(10),
            top: s(10),
            bottom: s(10),
            width: s(222),
            child: Tap(
              onTap: open,
              child: Stack(
                fit: StackFit.expand,
                children: [
                  FaZoom(
                    images: item.images.isNotEmpty ? item.images : [if (item.imagePath != null) item.imagePath!],
                    fallbackAsset: 'assets/screens/foods/peppers.png',
                    child: NetImage(item.imagePath, radius: BorderRadius.circular(s(20)), fallbackAsset: 'assets/screens/foods/peppers.png'),
                  ),
                  if (selected)
                    Positioned(
                      right: s(4),
                      top: s(4),
                      child: Container(
                        width: s(46),
                        height: s(46),
                        alignment: Alignment.center,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: const Color(0xFF3E8E1E),
                          border: Border.all(color: Colors.white, width: s(3)),
                        ),
                        child: FaIcon('check', size: s(28), color: Colors.white, weight: 3),
                      ),
                    ),
                ],
              ),
            ),
          ),
          // Text column (in the flow, so the row grows with bigger text): المطبخ · التصنيف · المجموعة, name, description.
          Padding(
            padding: EdgeInsets.fromLTRB(s(234), s(10), s(250), s(14)),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                // المجموعة then التصنيف on top; the cuisine goes under the name.
                FaItemTags(category: item.categoryName, group: item.groupName),
                SizedBox(height: s(6)),
                Tap(
                  onTap: open,
                  child: Text(
                    item.name ?? '',
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    textDirection: TextDirection.rtl,
                    style: Fa.text(s(30), weight: FontWeight.w800, color: Fa.title, height: 1.2),
                  ),
                ),
                if ((item.cuisineName ?? '').trim().isNotEmpty) ...[
                  SizedBox(height: s(6)),
                  FaItemTags(cuisine: item.cuisineName),
                ],
                if ((item.summary ?? '').trim().isNotEmpty) ...[
                  SizedBox(height: s(4)),
                  Text(
                    item.summary!.trim(),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    textDirection: TextDirection.rtl,
                    style: Fa.text(s(20), weight: FontWeight.w500, color: Fa.body, height: 1.25),
                  ),
                ],
                // سعر الأكلة للأسرة (only when the customer works with prices).
                if (item.price != null) ...[
                  SizedBox(height: s(8)),
                  if (item.price! > 0)
                    FaPriceTag(item.price!, size: 21, missing: item.priceMissing)
                  else
                    // Prices are on but none of the dish's ingredients has a price yet.
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: s(12), vertical: s(4)),
                      decoration: BoxDecoration(
                        color: const Color(0xFFFFF1E2),
                        borderRadius: BorderRadius.circular(s(21)),
                        border: Border.all(color: const Color(0xFFF9CF9F), width: s(1.2)),
                      ),
                      child: Text('مفيش سعر لسه', style: Fa.text(s(19), weight: FontWeight.w800, color: const Color(0xFFC45A00), height: 1.2)),
                    ),
                ],
              ],
            ),
          ),
          Positioned(
            left: s(220),
            top: s(36),
            height: s(104),
            width: s(2),
            child: const ColoredBox(color: Color(0xFFEEDFCD)),
          ),
          Positioned(
            left: s(22),
            top: s(24),
            child: Tap(
              onTap: () => setQty(selected ? 0 : 1),
              child: Container(
                width: s(46),
                height: s(46),
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: selected ? null : Colors.white,
                  gradient: selected
                      ? const LinearGradient(colors: [Color(0xFF5AA22B), Color(0xFF3A8418)], begin: Alignment.topCenter, end: Alignment.bottomCenter)
                      : null,
                  borderRadius: BorderRadius.circular(s(10)),
                  border: Border.all(color: selected ? const Color(0xFF3A8418) : const Color(0xFFB07A4F), width: s(3)),
                ),
                child: selected ? FaIcon('check', size: s(30), color: Colors.white, weight: 3.2) : null,
              ),
            ),
          ),
          Positioned(
            left: s(116),
            top: s(22),
            child: Tap(
              onTap: () async {
                try {
                  await ref.read(favoriteOverridesProvider.notifier).toggle(item.id, fav);
                } catch (e) {
                  if (context.mounted) showMessage(context, e, error: true);
                }
              },
              child: FaIcon('heart', size: s(48), filled: fav, weight: 1.8, color: redHeart ? const Color(0xFFE8222E) : const Color(0xFFF07A2A)),
            ),
          ),
          // The counter stays at the bottom of the card, with or without a price.
          Positioned(
            left: s(24),
            bottom: s(34),
            width: s(158),
            child: FaStepper(qty: qty, onStep: (d) => setQty(qty + d)),
          ),
        ],
      ),
    );
  }
}

/// The `− [n] +` stepper (`.fa-stepper`).
class FaStepper extends StatefulWidget {
  const FaStepper({super.key, required this.qty, required this.onStep, this.label});
  final int qty;
  final ValueChanged<int> onStep;
  final String? label;

  @override
  State<FaStepper> createState() => _FaStepperState();
}

/// − [n] + like the web `.fa-stepper`: tapping a side bumps the pill toward it, and the
/// pill can be dragged left/right past a threshold to step; it turns orange above 0.
class _FaStepperState extends State<FaStepper> {
  double _dragX = 0;
  bool _dragging = false;
  int _pull = 0; // 1 toward +, -1 toward −
  Timer? _pullTimer;
  double _room = 24;

  @override
  void dispose() {
    _pullTimer?.cancel();
    super.dispose();
  }

  void _step(int d) {
    widget.onStep(d);
    _pullTimer?.cancel();
    setState(() => _pull = d > 0 ? 1 : -1);
    _pullTimer = Timer(const Duration(milliseconds: 220), () {
      if (mounted) setState(() => _pull = 0);
    });
  }

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    final on = widget.qty > 0;
    final text = widget.label ?? '${widget.qty}';
    Widget btn(String t, int d) => Tap(
      onTap: () => _step(d),
      child: SizedBox(
        width: s(30),
        height: s(54),
        child: Center(
          child: Text(
            t,
            style: Fa.text(s(40), weight: FontWeight.w900, color: const Color(0xFF2C7A12), height: 1),
          ),
        ),
      ),
    );
    final offset = _dragging ? _dragX : _pull * s(46);
    return LayoutBuilder(
      builder: (context, c) {
        if (c.maxWidth.isFinite) _room = ((c.maxWidth - s(20) - s(66)) / 2).clamp(24.0, 400.0);
        return Container(
          height: s(54),
          padding: EdgeInsets.symmetric(horizontal: s(10)),
          decoration: BoxDecoration(
            color: const Color(0xFFFBF1E4),
            borderRadius: BorderRadius.circular(s(27)),
            border: Border.all(color: const Color(0xFFF4E6D3), width: s(1)),
            boxShadow: [Fa.shadow(context, y: 3, blur: 8, color: const Color(0x0F784820))],
          ),
          // The value pill is painted last so it always stays above − and + while it moves.
          child: Stack(
            alignment: Alignment.center,
            clipBehavior: Clip.none,
            children: [
              Row(textDirection: TextDirection.ltr, mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [btn('−', -1), btn('+', 1)]),
              // The pill is taller than the bar (68 vs 54) and overflows it, like the web.
              // Unconstrained both ways so the pill keeps its own size (not the bar's width).
              UnconstrainedBox(
                clipBehavior: Clip.none,
                child: GestureDetector(
                  behavior: HitTestBehavior.opaque,
                  onHorizontalDragStart: (_) => setState(() {
                    _dragging = true;
                    _dragX = 0;
                    _pull = 0;
                  }),
                  onHorizontalDragUpdate: (d) => setState(() => _dragX = (_dragX + d.delta.dx).clamp(-_room, _room)),
                  onHorizontalDragEnd: (_) {
                    final dx = _dragX;
                    setState(() {
                      _dragging = false;
                      _dragX = 0;
                    });
                    if (dx >= _room * .55) {
                      _step(1);
                    } else if (dx <= -_room * .55) {
                      _step(-1);
                    }
                  },
                  child: TweenAnimationBuilder<double>(
                    tween: Tween(end: offset),
                    duration: _dragging ? Duration.zero : const Duration(milliseconds: 200),
                    curve: Curves.ease,
                    builder: (_, x, child) => Transform.translate(offset: Offset(x, 0), child: child),
                    child: AnimatedContainer(
                      duration: const Duration(milliseconds: 160),
                      constraints: BoxConstraints(minWidth: s(66)),
                      height: s(68),
                      padding: EdgeInsets.symmetric(horizontal: s(10)),
                      alignment: Alignment.center,
                      decoration: BoxDecoration(
                        color: on ? null : Colors.white,
                        gradient: on ? Fa.orangeV : null,
                        borderRadius: BorderRadius.circular(s(34)),
                        boxShadow: [
                          on
                              ? Fa.shadow(context, y: 4, blur: 10, color: const Color(0x47F37202))
                              : Fa.shadow(context, y: 2, blur: 6, color: const Color(0x14784820)),
                        ],
                      ),
                      child: Text(
                        text,
                        textDirection: TextDirection.ltr,
                        style: Fa.text(s(text.length > 2 ? 24 : 30), weight: FontWeight.w800, color: on ? Colors.white : Fa.orange, height: 1),
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

/// A full-width loading / empty line (`.mt-loading`).
class FaNote extends StatelessWidget {
  const FaNote(this.text, {super.key});
  final String text;

  @override
  Widget build(BuildContext context) => Padding(
    padding: EdgeInsets.all(context.s(24)),
    child: Text(
      text,
      textAlign: TextAlign.center,
      style: Fa.text(context.s(26), color: const Color(0xFF8A6A4A)),
    ),
  );
}

String faMedia(String? path) => AppConfig.mediaUrl(path);

/// المطبخ · التصنيف · المجموعة of a dish as small coloured tags on one line (the row shortens them when tight).
class FaItemTags extends StatelessWidget {
  const FaItemTags({super.key, this.cuisine, this.category, this.group});
  final String? cuisine;
  final String? category;
  final String? group;

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    Widget tag(String icon, String text, Color bg, Color fg) => Container(
      constraints: BoxConstraints(minHeight: s(34)),
      padding: EdgeInsets.symmetric(horizontal: s(12), vertical: s(3)),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(s(17))),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        textDirection: TextDirection.rtl,
        children: [
          FaIcon(icon, size: s(22), color: fg, weight: 1.8),
          SizedBox(width: s(6)),
          Flexible(
            child: Text(text, maxLines: 1, overflow: TextOverflow.ellipsis, style: Fa.text(s(19), weight: FontWeight.w800, color: fg, height: 1.1)),
          ),
        ],
      ),
    );
    final tags = <Widget>[
      if ((cuisine ?? '').trim().isNotEmpty) tag(faChipIcon(cuisine), cuisine!.trim(), const Color(0xFFEAF4D9), Fa.green),
      if ((group ?? '').trim().isNotEmpty) tag('layers', group!.trim(), const Color(0xFFEDE7FA), const Color(0xFF5B3FA6)),
      if ((category ?? '').trim().isNotEmpty) tag('grid', category!.trim(), const Color(0xFFFFEEDC), const Color(0xFFC75A06)),
    ];
    if (tags.isEmpty) tags.add(tag('cloche', 'أكلة', const Color(0xFFEAF4D9), Fa.green));
    return Wrap(
      textDirection: TextDirection.rtl,
      spacing: s(6),
      runSpacing: s(6),
      children: tags,
    );
  }
}
