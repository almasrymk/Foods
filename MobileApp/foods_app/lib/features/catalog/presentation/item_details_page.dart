import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../app/providers.dart';
import '../../../app/widgets/fa_chrome.dart';
import '../../../app/widgets/fa_top_bar.dart';
import '../../../app/widgets/fa_widgets.dart';
import '../../../core/config/app_config.dart';
import '../../../core/ui/fa_kit.dart';
import '../../../core/widgets/async_views.dart';
import '../../../core/widgets/net_image.dart';
import '../../account/presentation/settings_page.dart' show familyProvider;
import '../../cart/domain/purchase_rules.dart';
import '../../cart/domain/selected_item.dart';
import '../../cart/presentation/cart_controller.dart';
import '../domain/catalog_models.dart';
import 'catalog_providers.dart';
import '../../prices/prices.dart';

/// "تفاصيل الأكلة" (meal-details.component). This mockup is 958px wide.
extension on BuildContext {
  double m(num n) => n * faWidth / 958;
}

class ItemDetailsPage extends ConsumerStatefulWidget {
  const ItemDetailsPage({super.key, required this.itemId, this.cuisineId, this.countryId});
  final String itemId;

  /// The cuisine it was opened from (that country's version), or an exact country version (a part of a dish).
  final String? cuisineId;
  final String? countryId;

  @override
  ConsumerState<ItemDetailsPage> createState() => _ItemDetailsPageState();
}

class _EditLine {
  _EditLine({required this.contentId, required this.name, this.imagePath, required this.unitId, double? quantity, this.isNew = false})
    : qty = TextEditingController(text: quantity == null ? '' : PurchaseRules.trim(quantity));
  final String contentId;
  final String name;
  final String? imagePath;
  String unitId;
  final TextEditingController qty;
  final bool isNew;
}

class _EditStep {
  _EditStep(this.key, String body) : text = TextEditingController(text: body);
  final int key;
  final TextEditingController text;
}

class _ItemDetailsPageState extends ConsumerState<ItemDetailsPage> {
  ItemKey get _key => (id: widget.itemId, cuisineId: widget.cuisineId, countryId: widget.countryId);

  /// The other country whose version is shown (edits and the order follow it); null = the customer's own.
  String? get _recipeCountry {
    final d = ref.read(itemDetailProvider(_key)).value;
    return d != null && d.isOtherCountry ? d.recipeCountryId : null;
  }

  // Personal version ("تعديل الأكلة ليا"): edits are saved for this customer only.
  bool _editing = false;
  bool _saving = false;
  String _editError = '';
  List<_EditLine> _lines = [];
  List<_EditLine> _removed = [];
  List<_EditStep> _stepsDraft = [];
  int _stepKey = 0;
  List<UnitPick> _units = [];
  bool _pickerOpen = false;
  bool _pickerBusy = false;
  String _pickerText = '';
  List<ContentPick> _pickerResults = [];
  Timer? _searchTimer;
  // Parts (أصناف) while editing.
  List<ItemPart> _partsDraft = [];
  List<ItemPart> _partsRemoved = [];
  bool _partPickerOpen = false;
  bool _partPickerBusy = false;
  String _partPickerText = '';
  List<ItemPart> _partPickerResults = [];
  Timer? _partTimer;

  @override
  void dispose() {
    _searchTimer?.cancel();
    _partTimer?.cancel();
    super.dispose();
  }

  _EditLine _toEdit(ItemIngredient l, bool isNew) => _EditLine(contentId: l.contentId, name: l.name ?? '', imagePath: l.imagePath, unitId: l.unitId, quantity: l.quantity, isNew: isNew);

  Future<void> _startEdit() async {
    if (_saving) return;
    final repo = ref.read(catalogRepositoryProvider);
    setState(() {
      _editError = '';
      _saving = true;
    });
    try {
      final mine = await repo.myItem(widget.itemId, countryId: _recipeCountry);
      if (_units.isEmpty) {
        repo.searchContents('', take: 1).then((r) {
          if (mounted && r.units.isNotEmpty) setState(() => _units = r.units);
        }).ignore();
      }
      if (!mounted) return;
      setState(() {
        _lines = [for (final l in mine.lines) _toEdit(l, l.isAdded)];
        _removed = [for (final l in mine.removed) _toEdit(l, false)];
        _stepsDraft = [for (final st in mine.steps) _EditStep(++_stepKey, st.body ?? '')];
        _partsDraft = mine.parts.where((p) => p.selected).toList();
        _partsRemoved = mine.parts.where((p) => !p.selected).toList();
        _closePicker();
        _closePartPicker();
        _editing = true;
        _saving = false;
      });
    } catch (e) {
      if (mounted) {
        setState(() {
          _editError = '$e'.replaceFirst('Exception: ', '');
          _saving = false;
        });
      }
    }
  }

  void _cancelEdit() => setState(() {
    _editing = false;
    _editError = '';
    _closePicker();
    _closePartPicker();
  });

  void _closePartPicker() {
    _partPickerOpen = false;
    _partPickerResults = [];
    _partTimer?.cancel();
  }

  void _openPartPicker() {
    setState(() {
      _partPickerOpen = true;
      _partPickerText = '';
    });
    _searchParts();
  }

  void _onPartPickerText(String v) {
    _partPickerText = v;
    _partTimer?.cancel();
    _partTimer = Timer(const Duration(milliseconds: 250), _searchParts);
  }

  Future<void> _searchParts() async {
    final text = _partPickerText.trim();
    setState(() => _partPickerBusy = true);
    try {
      final rows = await ref.read(catalogRepositoryProvider).partPicks(widget.itemId, text);
      if (!mounted || text != _partPickerText.trim()) return;
      final taken = {for (final p in _partsDraft) p.itemId};
      setState(() {
        _partPickerResults = rows.where((p) => !taken.contains(p.itemId)).toList();
        _partPickerBusy = false;
      });
    } catch (_) {
      if (mounted) setState(() => _partPickerBusy = false);
    }
  }

  void _pickPart(ItemPart part) => setState(() {
    final restored = _partsRemoved.indexWhere((p) => p.itemId == part.itemId);
    if (restored >= 0) {
      _partsDraft.add(_partsRemoved.removeAt(restored));
    } else if (!_partsDraft.any((p) => p.itemId == part.itemId)) {
      _partsDraft.add(ItemPart(itemId: part.itemId, name: part.name, imagePath: part.imagePath, canOpen: part.canOpen, isAdded: true));
    }
    _closePartPicker();
  });

  void _closePicker() {
    _pickerOpen = false;
    _pickerResults = [];
    _searchTimer?.cancel();
  }

  void _openPicker() {
    setState(() {
      _pickerOpen = true;
      _pickerText = '';
    });
    _search();
  }

  void _onPickerText(String v) {
    _pickerText = v;
    _searchTimer?.cancel();
    _searchTimer = Timer(const Duration(milliseconds: 250), _search);
  }

  Future<void> _search() async {
    final text = _pickerText.trim();
    setState(() => _pickerBusy = true);
    try {
      final r = await ref.read(catalogRepositoryProvider).searchContents(text, take: 12);
      if (!mounted || text != _pickerText.trim()) return;
      final taken = {for (final l in _lines) l.contentId};
      setState(() {
        _pickerResults = r.items.where((i) => !taken.contains(i.id)).toList();
        if (r.units.isNotEmpty) _units = r.units;
        _pickerBusy = false;
      });
    } catch (_) {
      if (mounted) setState(() => _pickerBusy = false);
    }
  }

  void _pick(ContentPick item) => setState(() {
    final restored = _removed.indexWhere((l) => l.contentId == item.id);
    if (restored >= 0) {
      _lines.add(_removed.removeAt(restored));
    } else {
      _lines.add(_EditLine(contentId: item.id, name: item.name ?? '', imagePath: item.imagePath, unitId: item.unitId, quantity: item.miniQuantity > 0 ? item.miniQuantity : 1, isNew: true));
    }
    _closePicker();
  });

  Future<void> _afterChange() async {
    ref.read(cartControllerProvider.notifier).recipeChanged(widget.itemId);
    ref.invalidate(itemDetailProvider(_key));
    try {
      await ref.read(itemDetailProvider(_key).future);
    } catch (_) {}
    if (mounted) {
      setState(() {
        _editing = false;
        _saving = false;
      });
    }
  }

  Future<void> _save() async {
    if (_saving) return;
    for (final l in _lines) {
      final q = double.tryParse(l.qty.text.trim().replaceAll(',', '.'));
      if (q == null || q <= 0 || l.unitId.isEmpty) {
        setState(() => _editError = 'اكتب كمية ووحدة لـ ${l.name}');
        return;
      }
    }
    final lines = [
      for (final l in _lines) {'contentId': l.contentId, 'unitId': l.unitId, 'quantity': double.parse(l.qty.text.trim().replaceAll(',', '.'))},
    ];
    final bodies = _stepsDraft.map((st) => st.text.text.trim()).where((b) => b.isNotEmpty).toList();
    final steps = [
      for (var i = 0; i < bodies.length; i++) {'sort': i + 1, 'body': bodies[i]},
    ];
    setState(() {
      _editError = '';
      _saving = true;
    });
    try {
      await ref.read(catalogRepositoryProvider).saveMyItem(widget.itemId, lines, steps, parts: [for (final p in _partsDraft) p.itemId], countryId: _recipeCountry);
      await _afterChange();
    } catch (e) {
      if (mounted) {
        setState(() {
          _editError = '$e'.replaceFirst('Exception: ', '');
          _saving = false;
        });
      }
    }
  }

  Future<void> _reset() async {
    if (_saving) return;
    final ok = await showDialog<bool>(
      context: context,
      builder: (c) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          backgroundColor: const Color(0xFFFFFCF8),
          title: Text('ترجع الأكلة لأصلها وتمسح تعديلاتك؟', style: Fa.text(18, weight: FontWeight.w800)),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(c, false),
              child: Text('إلغاء', style: Fa.text(15)),
            ),
            TextButton(
              onPressed: () => Navigator.pop(c, true),
              child: Text('رجوع للأصل', style: Fa.text(15, color: const Color(0xFFC0392B))),
            ),
          ],
        ),
      ),
    );
    if (ok != true) return;
    setState(() => _saving = true);
    try {
      await ref.read(catalogRepositoryProvider).resetMyItem(widget.itemId, countryId: _recipeCountry);
      await _afterChange();
    } catch (e) {
      if (mounted) {
        setState(() {
          _editError = '$e'.replaceFirst('Exception: ', '');
          _saving = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final detail = ref.watch(itemDetailProvider(_key));
    final m = context.m;
    final item = detail.value;
    final inCart = ref.watch(cartControllerProvider.select((c) => c.count(widget.itemId)));
    return Directionality(
      textDirection: TextDirection.rtl,
      child: ColoredBox(
        color: const Color(0xFFFDF6F1),
        child: SafeArea(
          bottom: false,
          child: ListView(
            padding: EdgeInsets.only(bottom: faBottomSpace(context, ref)),
            children: [
              const FaTopBar(),
              if (item == null)
                Padding(
                  padding: EdgeInsets.only(top: m(200)),
                  child: detail.hasError ? MessageView.error(detail.error!, onRetry: () => ref.invalidate(itemDetailProvider(_key))) : const LoadingView(),
                )
              else
                Transform.translate(
                  offset: Offset(0, m(-85)),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      _hero(context, item, inCart),
                      _ingredients(context, item),
                      if (item.parts.isNotEmpty || _editing) ...[SizedBox(height: m(20)), _editing ? _partsEdit(context) : _parts(context, item)],
                      if (_editing || item.steps.any((s) => (s.body ?? '').trim().isNotEmpty)) ...[SizedBox(height: m(20)), _steps(context, item)],
                      if (item.videos.isNotEmpty) ...[SizedBox(height: m(17)), _videos(context, item)],
                      SizedBox(height: m(22)),
                    ],
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _hero(BuildContext context, ItemDetail item, int inCart) {
    final sel = SelectedItem(itemId: item.id, name: item.name ?? '', imagePath: item.imagePath, repeatCount: inCart, countryId: item.isOtherCountry ? item.recipeCountryId : null);
    final m = context.m;
    final fav = ref.watch(favoriteOverridesProvider)[item.id] ?? item.isFavorite;
    Widget dash({bool flip = false}) => Transform.flip(
      flipX: flip,
      child: Image.asset('assets/screens/shared/dash.png', width: m(60), height: m(20), fit: BoxFit.fill),
    );
    final members = ref.watch(familyProvider).value?.length ?? 0;
    final people = members > 0 ? members : 1;
    Widget fact(String icon, String label, {bool filled = false}) => Container(
      height: m(52),
      padding: EdgeInsets.symmetric(horizontal: m(18)),
      decoration: BoxDecoration(
        color: Fa.card,
        borderRadius: BorderRadius.circular(m(26)),
        boxShadow: [BoxShadow(color: const Color(0x1A784820), offset: Offset(0, m(3)), blurRadius: m(10))],
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          FaIcon(icon, size: m(36), color: Fa.orange, filled: filled, weight: 1.8),
          SizedBox(width: m(10)),
          ConstrainedBox(
            constraints: BoxConstraints(maxWidth: m(600)),
            child: Text(
              label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: Fa.text(m(23), weight: FontWeight.w800),
            ),
          ),
        ],
      ),
    );
    final images = item.images.isNotEmpty ? item.images : [if (item.imagePath != null) item.imagePath!];
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SizedBox(
          height: m(165),
          child: Stack(
            clipBehavior: Clip.none,
            children: [
          Positioned(
            left: 0,
            right: 0,
            top: m(130),
            child: FractionalTranslation(
              translation: const Offset(0, -.5),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  dash(flip: true),
                  SizedBox(width: m(16)),
                  Text(
                    'تفاصيل الأكلة',
                    style: Fa.text(m(40), weight: FontWeight.w800, color: Fa.title),
                  ),
                  SizedBox(width: m(16)),
                  dash(),
                ],
              ),
            ),
          ),
          Positioned(
            left: m(850),
            top: m(98),
            child: Tap(
              onTap: () => faBack(context),
              child: Container(
                width: m(65),
                height: m(65),
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: const Color(0xFFFFFCF8),
                  borderRadius: BorderRadius.circular(m(18)),
                  boxShadow: [BoxShadow(color: const Color(0x21784820), offset: Offset(0, m(4)), blurRadius: m(14))],
                ),
                child: FaIcon('chevronLeft', size: m(36), weight: 2.6),
              ),
            ),
          ),
            ],
          ),
        ),
        // The cuisine tag, the name, its description, and the price / other country's version above the photo.
        Padding(
          padding: EdgeInsets.only(right: m(38), left: m(30)),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                height: m(36),
                padding: EdgeInsets.symmetric(horizontal: m(18)),
                decoration: BoxDecoration(color: const Color(0xFFE6F2D6), borderRadius: BorderRadius.circular(m(18))),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    FaIcon('cloche', size: m(32), color: Fa.green, weight: 1.8),
                    SizedBox(width: m(10)),
                    Text(
                      (item.countryName ?? '').isNotEmpty ? item.countryName! : 'شرقي',
                      style: Fa.text(m(25), weight: FontWeight.w800, color: Fa.green, height: 1),
                    ),
                  ],
                ),
              ),
              SizedBox(height: m(10)),
              Text(item.name ?? '', style: Fa.text(m(40), weight: FontWeight.w800, color: const Color(0xFF1F140C), height: 1.2)),
              if ((item.summary ?? '').trim().isNotEmpty) ...[
                SizedBox(height: m(6)),
                Text(item.summary!.trim(), style: Fa.text(m(25), color: const Color(0xFF6E6A66), height: 1.3)),
              ],
              if (item.price != null || (item.isOtherCountry && (item.recipeCountryName ?? '').isNotEmpty)) ...[
                SizedBox(height: m(14)),
                Wrap(
                  spacing: m(14),
                  runSpacing: m(12),
                  children: [
                    if (item.isOtherCountry && (item.recipeCountryName ?? '').isNotEmpty) fact('pin', 'نسخة ${item.recipeCountryName}'),
                    // سعر الأكلة للأسرة من أسعار مكوناتها (وأصنافها).
                    if (item.price != null)
                      fact(
                        'tag',
                        item.price!.total > 0
                            ? 'السعر ${egp(item.price!.total)}${item.price!.people > 1 ? ' · للفرد ${egp(item.price!.perPerson)}' : ''}'
                            : 'مفيش سعر لسه',
                      ),
                  ],
                ),
              ],
              if (item.price != null && item.price!.missing > 0) ...[
                SizedBox(height: m(10)),
                Text(
                  item.price!.missing == 1 ? 'في مكون من غير سعر، دوس على "حط سعر" جنبه' : 'في ${item.price!.missing} مكونات من غير سعر، دوس على "حط سعر" جنبهم',
                  style: Fa.text(m(20), weight: FontWeight.w500, color: const Color(0xFFC45A00), height: 1.3),
                ),
              ],
            ],
          ),
        ),
        SizedBox(height: m(18)),
        // The dish photo across the page, the whole picture (never cropped), not taller than m(430); tap to open it.
        Padding(
          padding: EdgeInsets.only(right: m(23), left: m(25)),
          child: FaZoom(
            images: images,
            fallbackAsset: 'assets/screens/details/video-main.png',
            tapToOpen: true,
            hoverScale: 1,
            child: NaturalPhoto(images: images, fallbackAsset: 'assets/screens/details/video-main.png', radius: BorderRadius.circular(m(22)), maxHeight: m(430), whole: true),
          ),
        ),
        SizedBox(height: m(18)),
        // Under the photo: only the group, the category and the family size.
        Padding(
          padding: EdgeInsets.only(right: m(38), left: m(30)),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Wrap(
                spacing: m(14),
                runSpacing: m(12),
                children: [
                  if ((item.groupName ?? '').isNotEmpty) fact('layers', item.groupName!),
                  if ((item.categoryName ?? '').isNotEmpty) fact('grid', item.categoryName!),
                  fact('person', peopleLabel(people), filled: true),
                ],
              ),
            ],
          ),
        ),
        SizedBox(height: m(26)),
        Padding(
          padding: EdgeInsets.only(left: m(25), right: m(23)),
          child: SizedBox(
            height: m(88),
            child: Container(
              decoration: BoxDecoration(
                color: Fa.card,
                borderRadius: BorderRadius.circular(m(44)),
                boxShadow: [BoxShadow(color: const Color(0x1A784820), offset: Offset(0, m(5)), blurRadius: m(18))],
              ),
              child: Stack(
                children: [
                  Positioned(
                    right: m(17),
                    top: m(15),
                    width: m(320),
                    height: m(58),
                    child: Tap(
                      onTap: () {
                        final n = inCart > 0 ? inCart : 1;
                        ref.read(cartControllerProvider.notifier).setQty(sel, n);
                        showMessage(context, 'اتضافت للخطة ($n مرة)');
                      },
                      child: Container(
                        padding: EdgeInsets.symmetric(horizontal: m(20)),
                        decoration: BoxDecoration(
                          gradient: Fa.ctaGradient,
                          borderRadius: BorderRadius.circular(m(29)),
                          boxShadow: [BoxShadow(color: const Color(0x4DF35E02), offset: Offset(0, m(4)), blurRadius: m(10))],
                        ),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            FaIcon('calendarDays', size: m(42), color: Colors.white, weight: 1.8),
                            SizedBox(width: m(12)),
                            Flexible(
                              child: FittedBox(
                                fit: BoxFit.scaleDown,
                                child: Text(
                                  'إضافة إلى الخطة',
                                  maxLines: 1,
                                  style: Fa.text(m(28), weight: FontWeight.w800, color: Colors.white),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                  Positioned(
                    // Same width as the stepper on the meals list (158 of 941).
                    left: m(352),
                    top: m(15),
                    width: m(161),
                    height: m(58),
                    child: Center(
                      child: FaStepper(qty: inCart, onStep: (d) => ref.read(cartControllerProvider.notifier).setQty(sel, (inCart + d).clamp(0, 99))),
                    ),
                  ),
                  Positioned(
                    left: m(286),
                    top: m(22),
                    width: m(2),
                    height: m(44),
                    child: const ColoredBox(color: Color(0xFFE3D6C8)),
                  ),
                  Positioned(
                    left: m(24),
                    top: m(15),
                    width: m(250),
                    height: m(58),
                    child: Tap(
                      onTap: () async {
                        try {
                          await ref.read(favoriteOverridesProvider.notifier).toggle(item.id, fav);
                        } catch (e) {
                          if (context.mounted) showMessage(context, e, error: true);
                        }
                      },
                      child: Row(
                        children: [
                          Flexible(
                            child: FittedBox(
                              fit: BoxFit.scaleDown,
                              child: Text(fav ? 'في المفضلة' : 'إضافة للمفضلة', maxLines: 1, style: Fa.text(m(26), weight: FontWeight.w800)),
                            ),
                          ),
                          SizedBox(width: m(14)),
                          FaIcon('heart', size: m(44), color: Fa.orange, filled: fav, weight: 2),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
        SizedBox(height: m(20)),
      ],
    );
  }

  Widget _card(BuildContext context, {required Widget child, double? height, EdgeInsets? padding}) {
    final m = context.m;
    return Container(
      height: height,
      margin: EdgeInsets.only(right: m(23), left: m(25)),
      padding: padding,
      decoration: BoxDecoration(
        color: const Color(0xD9FFFCF9),
        borderRadius: BorderRadius.circular(m(30)),
        boxShadow: [BoxShadow(color: const Color(0x14784820), offset: Offset(0, m(5)), blurRadius: m(18))],
      ),
      child: child,
    );
  }

  /// The card's orange title pill with its dash; it grows with the text (font size setting).
  List<Widget> _pill(BuildContext context, String icon, String title, double width, double top, double dashLeft) {
    final m = context.m;
    return [
      Positioned(
        right: m(25),
        top: m(top),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              constraints: BoxConstraints(minHeight: m(48), minWidth: m(width * .8)),
              padding: EdgeInsets.symmetric(horizontal: m(26), vertical: m(5)),
              decoration: BoxDecoration(gradient: Fa.ctaGradient, borderRadius: BorderRadius.circular(m(24))),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  FaIcon(icon, size: m(38), color: Colors.white, weight: 1.9),
                  SizedBox(width: m(14)),
                  Text(title, style: Fa.text(m(30), weight: FontWeight.w800, color: Colors.white, height: 1)),
                ],
              ),
            ),
            SizedBox(width: m(16)),
            Image.asset('assets/screens/shared/dash.png', width: m(66), height: m(20), fit: BoxFit.fill),
          ],
        ),
      ),
    ];
  }

  /// "تعديل الأكلة ليا" with the "نسختك الخاصة" badge; sits on the ingredients card, or alone when it is hidden.
  Widget _mineButton(BuildContext context, ItemDetail item) {
    final m = context.m;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (item.isCustomized) ...[
          Container(
            height: m(34),
            padding: EdgeInsets.symmetric(horizontal: m(12)),
            alignment: Alignment.center,
            decoration: BoxDecoration(color: const Color(0xFFEAF4D9), borderRadius: BorderRadius.circular(m(17))),
            child: Text('نسختك الخاصة', style: Fa.text(m(17), weight: FontWeight.w800, color: const Color(0xFF2F7D1E), height: 1)),
          ),
          SizedBox(width: m(10)),
        ],
        Tap(
          onTap: _saving ? null : _startEdit,
          child: Opacity(
            opacity: _saving ? .6 : 1,
            child: Container(
              height: m(40),
              padding: EdgeInsets.symmetric(horizontal: m(16)),
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: const Color(0xFFFFF1E3),
                borderRadius: BorderRadius.circular(m(20)),
                border: Border.all(color: const Color(0xFFF7C79B)),
              ),
              child: Text('تعديل الأكلة ليا', style: Fa.text(m(19), weight: FontWeight.w800, color: const Color(0xFFE25F02), height: 1)),
            ),
          ),
        ),
      ],
    );
  }

  /// The ingredient's cost (per person); tap to change its price for this customer.
  Widget _costTag(BuildContext context, ItemIngredient ing) {
    final m = context.m;
    final has = ing.cost != null;
    return Tap(
      onTap: () => showPriceSheet(context, ref, ing.contentId),
      child: Container(
        padding: EdgeInsets.symmetric(horizontal: m(10), vertical: m(3)),
        decoration: BoxDecoration(
          color: has ? const Color(0xFFEAF4DE) : const Color(0xFFFFF1E2),
          borderRadius: BorderRadius.circular(m(14)),
          border: Border.all(color: has ? const Color(0xFFCFE3B6) : const Color(0xFFF9CF9F), width: m(1.2)),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              has ? egp(ing.cost!) : 'حط سعر',
              style: Fa.text(m(18), weight: FontWeight.w800, color: has ? const Color(0xFF2F6A16) : const Color(0xFFC45A00), height: 1.1),
            ),
            SizedBox(width: m(5)),
            FaIcon('pencil', size: m(18), color: has ? const Color(0xFF2F6A16) : const Color(0xFFC45A00)),
          ],
        ),
      ),
    );
  }

  Widget _ingredients(BuildContext context, ItemDetail item) {
    final m = context.m;
    if (_editing) return _ingredientsEdit(context);
    final rows = item.ingredients;
    // No ingredients: the card is hidden and only the edit button stays.
    if (rows.isEmpty) {
      return Padding(
        padding: EdgeInsets.only(right: m(23), left: m(25)),
        child: Align(alignment: AlignmentDirectional.centerEnd, child: _mineButton(context, item)),
      );
    }
    final half = (rows.length / 2).ceil();
    final cols = [rows.take(half).toList(), rows.skip(half).toList()];

    Widget row(ItemIngredient ing) {
      final mark = ing.isAdded || ing.isChanged;
      final off = !ing.selected;
      final qty = '${PurchaseRules.trim(ing.quantity)} ${(ing.unitName ?? '').trim()}'.trim();
      final images = ing.images.isNotEmpty ? ing.images : [if (ing.imagePath != null) ing.imagePath!];
      return Container(
        constraints: BoxConstraints(minHeight: m(72)),
        padding: EdgeInsetsDirectional.only(start: m(10), end: m(10), top: m(5), bottom: m(5)),
        decoration: BoxDecoration(
          color: off ? const Color(0xFFF6F0EA) : Fa.card,
          borderRadius: BorderRadius.circular(m(36)),
          border: mark ? Border.all(color: const Color(0xFFBFE29B), width: m(2)) : null,
          boxShadow: [BoxShadow(color: const Color(0x14784820), offset: Offset(0, m(3)), blurRadius: m(10))],
        ),
        child: Row(
          children: [
            Opacity(
              opacity: off ? .45 : 1,
              child: FaZoom(
                images: images,
                fallbackAsset: 'assets/screens/ingredients/onion.png',
                hoverScale: 1.35,
                child: SizedBox(
                  width: m(100),
                  height: m(62),
                  child: PhotoSlider(images: images, fit: BoxFit.contain, fallbackAsset: 'assets/screens/ingredients/onion.png'),
                ),
              ),
            ),
            SizedBox(width: m(12)),
            // Name, then the quantity and unit under it.
            Expanded(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    ing.name ?? '',
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: Fa.text(m(23), weight: FontWeight.w800, color: off ? const Color(0xFFA89A8C) : Fa.ink, height: 1.15),
                  ),
                  SizedBox(height: m(4)),
                  Wrap(
                    crossAxisAlignment: WrapCrossAlignment.center,
                    spacing: m(8),
                    runSpacing: m(4),
                    children: [
                      if (ing.isOptional) _optionalTag(context),
                      if (item.price != null) _costTag(context, ing),
                      Text(
                        qty,
                        style: Fa.text(
                          m(20),
                          weight: FontWeight.w800,
                          color: off ? const Color(0xFF9A8B7D) : const Color(0xFF2F8A1E),
                          height: 1.1,
                        ).copyWith(decoration: off ? TextDecoration.lineThrough : null),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            if (ing.isOptional) ...[
              SizedBox(width: m(6)),
              _pickButton(context, ing.selected, () => _choose(item, 0, ing.contentId, ing.name ?? '', ing.selected, (v) => ing.selected = v)),
            ],
          ],
        ),
      );
    }

    Widget column(List<ItemIngredient> list) => Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (var i = 0; i < list.length; i++) ...[if (i > 0) SizedBox(height: m(8)), row(list[i])],
      ],
    );

    return _card(
      context,
      padding: EdgeInsets.fromLTRB(m(23), m(17), m(25), m(22)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _cardHead(context, 'basket', 'المكونات', trailing: _mineButton(context, item)),
          SizedBox(height: m(20)),
          // On a narrow screen or with big text the two columns become one list.
          LayoutBuilder(
            builder: (context, box) {
              final scale = MediaQuery.textScalerOf(context).scale(10) / 10;
              final twoCols = box.maxWidth / scale > 280;
              if (!twoCols) return column(rows);
              return IntrinsicHeight(
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(child: column(cols[0])),
                    Container(width: m(1.5), margin: EdgeInsets.symmetric(horizontal: m(14)), color: const Color(0xFFE8DCCF)),
                    Expanded(child: column(cols[1])),
                  ],
                ),
              );
            },
          ),
        ],
      ),
    );
  }

  /// Orange title pill with the little dash, and an optional widget at the other end (flow layout).
  Widget _cardHead(BuildContext context, String icon, String title, {Widget? trailing}) {
    final m = context.m;
    return Row(
      children: [
        Container(
          constraints: BoxConstraints(minHeight: m(48)),
          padding: EdgeInsets.symmetric(horizontal: m(26), vertical: m(6)),
          decoration: BoxDecoration(gradient: Fa.ctaGradient, borderRadius: BorderRadius.circular(m(24))),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              FaIcon(icon, size: m(38), color: Colors.white, weight: 1.9),
              SizedBox(width: m(14)),
              Text(title, style: Fa.text(m(30), weight: FontWeight.w800, color: Colors.white, height: 1)),
            ],
          ),
        ),
        SizedBox(width: m(14)),
        Image.asset('assets/screens/shared/dash.png', width: m(66), height: m(20), fit: BoxFit.fill),
        if (trailing == null)
          const Spacer()
        else
          Expanded(
            child: Align(
              alignment: AlignmentDirectional.centerEnd,
              child: FittedBox(fit: BoxFit.scaleDown, child: trailing),
            ),
          ),
      ],
    );
  }

  bool _choiceBusy = false;

  /// Picks or drops an optional ingredient (kind 0) or part (kind 1) for this customer; طلباتي follows.
  Future<void> _choose(ItemDetail item, int kind, String refId, String name, bool current, void Function(bool) set) async {
    if (_choiceBusy) return;
    final next = !current;
    setState(() {
      set(next);
      _choiceBusy = true;
    });
    try {
      await ref.read(catalogRepositoryProvider).setItemChoice(item.id, kind, refId, next, countryId: item.isOtherCountry ? item.recipeCountryId : null);
      ref.read(cartControllerProvider.notifier).recipeChanged(item.id);
      if (mounted) showMessage(context, next ? 'اتضاف $name' : 'اتشال $name');
    } catch (e) {
      if (mounted) {
        setState(() => set(current));
        showMessage(context, e, error: true);
      }
    } finally {
      if (mounted) setState(() => _choiceBusy = false);
    }
  }

  Widget _optionalTag(BuildContext context) {
    final m = context.m;
    return Container(
      padding: EdgeInsets.symmetric(horizontal: m(8), vertical: m(2)),
      decoration: BoxDecoration(color: const Color(0xFFFFF1E3), borderRadius: BorderRadius.circular(m(10))),
      child: Text('اختياري', style: Fa.text(m(16), weight: FontWeight.w800, color: const Color(0xFFE25F02), height: 1.1)),
    );
  }

  /// Round ✓ / + button for an optional ingredient or part.
  Widget _pickButton(BuildContext context, bool on, VoidCallback onTap) {
    final m = context.m;
    return Tap(
      onTap: _choiceBusy ? null : onTap,
      child: Opacity(
        opacity: _choiceBusy ? .6 : 1,
        child: Container(
          width: m(46),
          height: m(46),
          alignment: Alignment.center,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: on ? null : Colors.white,
            gradient: on ? const LinearGradient(begin: Alignment.topCenter, end: Alignment.bottomCenter, colors: [Color(0xFFFF9A1F), Color(0xFFF36202)]) : null,
            border: on ? null : Border.all(color: const Color(0xFFF7C79B), width: m(2)),
            boxShadow: on ? [BoxShadow(color: const Color(0x4DF35E02), offset: Offset(0, m(3)), blurRadius: m(8))] : null,
          ),
          child: on
              ? FaIcon('check', size: m(26), color: Colors.white, weight: 3)
              // Drawn bars, not the "+" letter: a font glyph sits off-centre and grows with the font setting.
              : Stack(
                  alignment: Alignment.center,
                  children: [
                    Container(width: m(20), height: m(3.5), decoration: BoxDecoration(color: const Color(0xFFE25F02), borderRadius: BorderRadius.circular(m(2)))),
                    Container(width: m(3.5), height: m(20), decoration: BoxDecoration(color: const Color(0xFFE25F02), borderRadius: BorderRadius.circular(m(2)))),
                  ],
                ),
        ),
      ),
    );
  }

  /// أصناف: other dishes inside this dish (required, or optional with a ✓ / + button).
  Widget _parts(BuildContext context, ItemDetail item) {
    final m = context.m;
    Widget row(ItemPart p) {
      final off = !p.selected;
      final sub = '${p.isAdded ? 'إضافتك' : p.isOptional ? 'اختياري' : 'أساسي'}${p.ingredientCount > 0 ? ' · ${p.ingredientCount} مكون' : ''}';
      return Container(
        constraints: BoxConstraints(minHeight: m(76)),
        padding: EdgeInsets.fromLTRB(m(14), m(6), m(10), m(6)),
        decoration: BoxDecoration(
          color: off ? const Color(0xFFF6F0EA) : Fa.card,
          borderRadius: BorderRadius.circular(m(38)),
          boxShadow: [BoxShadow(color: const Color(0x14784820), offset: Offset(0, m(3)), blurRadius: m(10))],
        ),
        child: Row(
          children: [
            Expanded(
              child: Tap(
                onTap: p.canOpen ? () => context.push(_recipeCountry == null ? '/items/${p.itemId}' : '/items/${p.itemId}?country=$_recipeCountry') : null,
                child: Opacity(
                  opacity: off ? .5 : 1,
                  child: Row(
                    children: [
                      FaZoom(
                        images: [if (p.imagePath != null) p.imagePath!],
                        hoverScale: 1.25,
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(m(16)),
                          child: SizedBox(width: m(110), height: m(74), child: NetImage(p.imagePath, fit: BoxFit.cover)),
                        ),
                      ),
                      SizedBox(width: m(14)),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text(p.name ?? '', maxLines: 2, overflow: TextOverflow.ellipsis, style: Fa.text(m(24), weight: FontWeight.w800, height: 1.15)),
                            SizedBox(height: m(2)),
                            Text(sub, maxLines: 2, style: Fa.text(m(19), weight: FontWeight.w700, color: const Color(0xFF2F8A1E))),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            if (p.isOptional) ...[
              SizedBox(width: m(12)),
              _pickButton(context, p.selected, () => _choose(item, 1, p.itemId, p.name ?? '', p.selected, (v) => p.selected = v)),
            ],
          ],
        ),
      );
    }

    return _card(
      context,
      child: Stack(
        children: [
          ..._pill(context, 'cloche', 'الأصناف', 230, 17, 567),
          Padding(
            padding: EdgeInsets.fromLTRB(m(23), m(85), m(25), m(20)),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                for (var i = 0; i < item.parts.length; i++) ...[
                  if (i > 0) SizedBox(height: m(10)),
                  row(item.parts[i]),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  /// أصناف in "تعديل الأكلة ليا": drop any part, bring it back, or add another dish.
  Widget _partsEdit(BuildContext context) {
    final m = context.m;
    Widget prow(int i) {
      final p = _partsDraft[i];
      return Container(
        constraints: BoxConstraints(minHeight: m(76)),
        padding: EdgeInsets.fromLTRB(m(12), m(6), m(10), m(6)),
        decoration: BoxDecoration(
          color: Fa.card,
          borderRadius: BorderRadius.circular(m(38)),
          border: p.isAdded ? Border.all(color: const Color(0xFFBFE29B), width: m(2)) : null,
          boxShadow: [BoxShadow(color: const Color(0x14784820), offset: Offset(0, m(3)), blurRadius: m(10))],
        ),
        child: Row(
          children: [
            ClipRRect(
              borderRadius: BorderRadius.circular(m(16)),
              child: SizedBox(width: m(96), height: m(64), child: NetImage(p.imagePath, fit: BoxFit.cover)),
            ),
            SizedBox(width: m(14)),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(p.name ?? '', maxLines: 1, overflow: TextOverflow.ellipsis, style: Fa.text(m(24), weight: FontWeight.w800)),
                  SizedBox(height: m(2)),
                  Text(
                    p.isAdded ? 'إضافتك' : p.isOptional ? 'اختياري' : 'أساسي',
                    style: Fa.text(m(19), weight: FontWeight.w700, color: const Color(0xFF2F8A1E)),
                  ),
                ],
              ),
            ),
            _trash(
              context,
              () => setState(() {
                final part = _partsDraft.removeAt(i);
                if (!part.isAdded) _partsRemoved.add(part);
              }),
            ),
          ],
        ),
      );
    }

    return _card(
      context,
      child: Stack(
        children: [
          ..._pill(context, 'cloche', 'الأصناف', 230, 17, 567),
          Padding(
            padding: EdgeInsets.fromLTRB(m(23), m(85), m(25), m(20)),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                for (var i = 0; i < _partsDraft.length; i++) ...[if (i > 0) SizedBox(height: m(10)), prow(i)],
                if (_partsDraft.isEmpty)
                  Padding(
                    padding: EdgeInsets.symmetric(vertical: m(6)),
                    child: Text(
                      'مفيش أصناف. تقدر تضيف أكلة تانية مع الأكلة دي.',
                      textAlign: TextAlign.center,
                      style: Fa.text(m(21), color: const Color(0xFF8B7B6C)),
                    ),
                  ),
                if (_partsRemoved.isNotEmpty) ...[
                  SizedBox(height: m(10)),
                  Wrap(
                    spacing: m(8),
                    runSpacing: m(8),
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      Text('اتشالت:', style: Fa.text(m(19), color: const Color(0xFF8B7B6C))),
                      for (var i = 0; i < _partsRemoved.length; i++)
                        Tap(
                          onTap: () => setState(() => _partsDraft.add(_partsRemoved.removeAt(i))),
                          child: Container(
                            padding: EdgeInsets.symmetric(horizontal: m(12), vertical: m(9)),
                            decoration: BoxDecoration(color: const Color(0xFFF4ECE3), borderRadius: BorderRadius.circular(m(18))),
                            child: Text('+ ${_partsRemoved[i].name ?? ''}', style: Fa.text(m(18), color: const Color(0xFF6B5646), height: 1)),
                          ),
                        ),
                    ],
                  ),
                ],
                SizedBox(height: m(10)),
                if (!_partPickerOpen)
                  _greenBtn(context, '+ إضافة صنف', _openPartPicker)
                else
                  Container(
                    padding: EdgeInsets.all(m(12)),
                    decoration: BoxDecoration(
                      color: const Color(0xFFFFF8F0),
                      borderRadius: BorderRadius.circular(m(22)),
                      border: Border.all(color: const Color(0xFFF2DCC4)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Row(
                          children: [
                            Expanded(
                              child: TextField(
                                autofocus: true,
                                onChanged: _onPartPickerText,
                                style: Fa.text(m(21)),
                                decoration: _field(context, hint: 'دوّر على أكلة...'),
                              ),
                            ),
                            SizedBox(width: m(10)),
                            Tap(
                              onTap: () => setState(_closePartPicker),
                              child: Text('إغلاق', style: Fa.text(m(20), weight: FontWeight.w800, color: const Color(0xFFE25F02))),
                            ),
                          ],
                        ),
                        SizedBox(height: m(8)),
                        ConstrainedBox(
                          constraints: BoxConstraints(maxHeight: m(320)),
                          child: ListView(
                            shrinkWrap: true,
                            padding: EdgeInsets.zero,
                            children: [
                              for (final it in _partPickerResults)
                                Padding(
                                  padding: EdgeInsets.only(bottom: m(6)),
                                  child: Tap(
                                    onTap: () => _pickPart(it),
                                    child: Container(
                                      constraints: BoxConstraints(minHeight: m(48)),
                                      padding: EdgeInsets.symmetric(horizontal: m(16)),
                                      alignment: AlignmentDirectional.centerStart,
                                      decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(m(16))),
                                      child: Text(it.name ?? '', style: Fa.text(m(21))),
                                    ),
                                  ),
                                ),
                              if (!_partPickerBusy && _partPickerResults.isEmpty)
                                Padding(
                                  padding: EdgeInsets.symmetric(vertical: m(10)),
                                  child: Text('مفيش نتايج.', textAlign: TextAlign.center, style: Fa.text(m(22), color: const Color(0xFF8B7B6C))),
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
        ],
      ),
    );
  }

  Widget _steps(BuildContext context, ItemDetail item) {
    final m = context.m;
    if (_editing) return _stepsEdit(context, item);
    final steps = item.steps.map((s) => (s.body ?? '').trim()).where((s) => s.isNotEmpty).toList();
    Widget li(int? n, String text) => Container(
      constraints: BoxConstraints(minHeight: m(44)),
      padding: EdgeInsets.fromLTRB(m(10), m(3), n == null ? m(20) : m(105), m(3)),
      decoration: BoxDecoration(
        color: Fa.card,
        borderRadius: BorderRadius.circular(m(23)),
        boxShadow: [BoxShadow(color: const Color(0x12784820), offset: Offset(0, m(3)), blurRadius: m(10))],
      ),
      child: Stack(
        clipBehavior: Clip.none,
        alignment: AlignmentDirectional.centerStart,
        children: [
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: Text(
              text,
              style: Fa.text(m(20), weight: FontWeight.w800, color: const Color(0xFF1F140C), height: 1.25),
            ),
          ),
          if (n != null)
            PositionedDirectional(
              start: m(20) - m(105),
              child: Container(
                width: m(42),
                height: m(42),
                alignment: Alignment.center,
                decoration: const BoxDecoration(
                  shape: BoxShape.circle,
                  gradient: LinearGradient(colors: [Fa.orangeLight, Color(0xFFF36202)], begin: Alignment.topCenter, end: Alignment.bottomCenter),
                ),
                child: Text(
                  '$n',
                  style: Fa.text(m(26), weight: FontWeight.w800, color: Colors.white, height: 1),
                ),
              ),
            ),
        ],
      ),
    );
    return _card(
      context,
      child: ConstrainedBox(
        constraints: const BoxConstraints(),
        child: Stack(
          children: [
            ..._pill(context, 'chef', 'طريقة التحضير', 317, 27, 487),
            Padding(
              padding: EdgeInsets.fromLTRB(m(23), m(85), m(25), m(20)),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  for (var i = 0; i < steps.length; i++) ...[if (i > 0) SizedBox(height: m(4)), li(i + 1, steps[i])],
                  if (steps.isEmpty) li(null, 'مفيش خطوات مكتوبة للأكلة دي.'),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  InputDecoration _field(BuildContext context, {String? hint}) {
    final m = context.m;
    final b = OutlineInputBorder(
      borderRadius: BorderRadius.circular(m(14)),
      borderSide: const BorderSide(color: Color(0xFFEADBC9)),
    );
    return InputDecoration(
      hintText: hint,
      hintStyle: Fa.text(m(21), color: const Color(0xFFA89A8E)),
      isDense: true,
      filled: true,
      fillColor: Colors.white,
      contentPadding: EdgeInsets.symmetric(horizontal: m(12), vertical: m(12)),
      border: b,
      enabledBorder: b,
      focusedBorder: b.copyWith(borderSide: const BorderSide(color: Color(0xFFF59A3C))),
    );
  }

  Widget _trash(BuildContext context, VoidCallback onTap, {double size = 48}) {
    final m = context.m;
    return Tap(
      onTap: onTap,
      child: Container(
        width: m(size),
        height: m(size),
        alignment: Alignment.center,
        decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0xFFFDE8E8)),
        child: FaIcon('trash', size: m(24), color: const Color(0xFFC0392B)),
      ),
    );
  }

  Widget _greenBtn(BuildContext context, String label, VoidCallback onTap) {
    final m = context.m;
    return Align(
      alignment: AlignmentDirectional.centerStart,
      child: Tap(
        onTap: onTap,
        child: Container(
          height: m(46),
          padding: EdgeInsets.symmetric(horizontal: m(20)),
          decoration: BoxDecoration(color: const Color(0xFFEAF4D9), borderRadius: BorderRadius.circular(m(23))),
          child: Center(
            widthFactor: 1,
            child: Text(
              label,
              style: Fa.text(m(21), weight: FontWeight.w800, color: const Color(0xFF2F7D1E), height: 1),
            ),
          ),
        ),
      ),
    );
  }

  Widget _ingredientsEdit(BuildContext context) {
    final m = context.m;
    final unitIds = {for (final u in _units) u.id};
    Widget erow(int i) {
      final l = _lines[i];
      return Container(
        constraints: BoxConstraints(minHeight: m(66)),
        padding: EdgeInsets.symmetric(horizontal: m(12), vertical: m(4)),
        decoration: BoxDecoration(
          color: Fa.card,
          borderRadius: BorderRadius.circular(m(33)),
          border: l.isNew ? Border.all(color: const Color(0xFFBFE29B), width: m(2)) : null,
          boxShadow: [BoxShadow(color: const Color(0x14784820), offset: Offset(0, m(3)), blurRadius: m(10))],
        ),
        child: Row(
          children: [
            SizedBox(
              width: m(80),
              height: m(56),
              child: NetImage(l.imagePath, fit: BoxFit.contain, fallbackAsset: 'assets/screens/ingredients/onion.png'),
            ),
            SizedBox(width: m(12)),
            Expanded(
              child: Text(
                l.name,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: Fa.text(m(23), weight: FontWeight.w800),
              ),
            ),
            SizedBox(width: m(12)),
            SizedBox(
              width: m(130),
              child: TextField(
                controller: l.qty,
                textAlign: TextAlign.center,
                textDirection: TextDirection.ltr,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                style: Fa.text(m(21)),
                decoration: _field(context),
              ),
            ),
            SizedBox(width: m(12)),
            Container(
              width: m(150),
              height: m(48),
              padding: EdgeInsets.symmetric(horizontal: m(8)),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(m(14)),
                border: Border.all(color: const Color(0xFFEADBC9)),
              ),
              child: DropdownButtonHideUnderline(
                child: DropdownButton<String>(
                  value: l.unitId.isEmpty ? null : l.unitId,
                  isExpanded: true,
                  iconSize: m(28),
                  style: Fa.text(m(21)),
                  items: [
                    if (l.unitId.isNotEmpty && !unitIds.contains(l.unitId)) DropdownMenuItem(value: l.unitId, child: const Text('—')),
                    for (final u in _units)
                      DropdownMenuItem(
                        value: u.id,
                        child: Text(u.name ?? '', overflow: TextOverflow.ellipsis),
                      ),
                  ],
                  onChanged: (v) => setState(() => l.unitId = v ?? l.unitId),
                ),
              ),
            ),
            SizedBox(width: m(12)),
            _trash(
              context,
              () => setState(() {
                final line = _lines.removeAt(i);
                if (!line.isNew) _removed.add(line);
              }),
            ),
          ],
        ),
      );
    }

    return _card(
      context,
      child: Stack(
        children: [
          ..._pill(context, 'basket', 'المكونات', 270, 17, 527),
          Padding(
            padding: EdgeInsets.fromLTRB(m(25), m(85), m(23), m(22)),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                for (var i = 0; i < _lines.length; i++) ...[if (i > 0) SizedBox(height: m(10)), erow(i)],
                if (_removed.isNotEmpty) ...[
                  SizedBox(height: m(10)),
                  Wrap(
                    spacing: m(8),
                    runSpacing: m(8),
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      Text('اتشالت:', style: Fa.text(m(19), color: const Color(0xFF8B7B6C))),
                      for (var i = 0; i < _removed.length; i++)
                        Tap(
                          onTap: () => setState(() => _lines.add(_removed.removeAt(i))),
                          child: Container(
                            height: m(36),
                            padding: EdgeInsets.symmetric(horizontal: m(12)),
                            alignment: Alignment.center,
                            decoration: BoxDecoration(color: const Color(0xFFF4ECE3), borderRadius: BorderRadius.circular(m(18))),
                            child: Text('${_removed[i].name} ↺', style: Fa.text(m(18), color: const Color(0xFF6B5646), height: 1)),
                          ),
                        ),
                    ],
                  ),
                ],
                SizedBox(height: m(10)),
                if (!_pickerOpen)
                  _greenBtn(context, '+ إضافة مكوّن', _openPicker)
                else
                  Container(
                    padding: EdgeInsets.all(m(12)),
                    decoration: BoxDecoration(
                      color: const Color(0xFFFFF8F0),
                      borderRadius: BorderRadius.circular(m(22)),
                      border: Border.all(color: const Color(0xFFF2DCC4)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Row(
                          children: [
                            Expanded(
                              child: TextField(
                                autofocus: true,
                                onChanged: _onPickerText,
                                style: Fa.text(m(21)),
                                decoration: _field(context, hint: 'دوّر على مكوّن...'),
                              ),
                            ),
                            SizedBox(width: m(10)),
                            Tap(
                              onTap: () => setState(_closePicker),
                              child: Text(
                                'إغلاق',
                                style: Fa.text(m(20), weight: FontWeight.w800, color: const Color(0xFFE25F02)),
                              ),
                            ),
                          ],
                        ),
                        SizedBox(height: m(8)),
                        ConstrainedBox(
                          constraints: BoxConstraints(maxHeight: m(320)),
                          child: ListView(
                            shrinkWrap: true,
                            padding: EdgeInsets.zero,
                            children: [
                              for (final it in _pickerResults)
                                Padding(
                                  padding: EdgeInsets.only(bottom: m(6)),
                                  child: Tap(
                                    onTap: () => _pick(it),
                                    child: Container(
                                      constraints: BoxConstraints(minHeight: m(48)),
                                      padding: EdgeInsets.symmetric(horizontal: m(16)),
                                      decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(m(16))),
                                      child: Row(
                                        children: [
                                          Expanded(child: Text(it.name ?? '', style: Fa.text(m(21)))),
                                          Text(
                                            it.unitName ?? '',
                                            style: Fa.text(m(18), weight: FontWeight.w500, color: const Color(0xFF8B7B6C)),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ),
                                ),
                              if (!_pickerBusy && _pickerResults.isEmpty)
                                Padding(
                                  padding: EdgeInsets.symmetric(vertical: m(10)),
                                  child: Text(
                                    'مفيش نتايج.',
                                    textAlign: TextAlign.center,
                                    style: Fa.text(m(22), color: const Color(0xFF8B7B6C)),
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
        ],
      ),
    );
  }

  Widget _stepsEdit(BuildContext context, ItemDetail item) {
    final m = context.m;
    Widget tool(String icon, VoidCallback? onTap) => Opacity(
      opacity: onTap == null ? .35 : 1,
      child: Tap(
        onTap: onTap,
        child: Container(
          width: m(42),
          height: m(42),
          alignment: Alignment.center,
          decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0xFFF4ECE3)),
          child: FaIcon(icon, size: m(24), color: const Color(0xFF6B5646), weight: 2.4),
        ),
      ),
    );
    Widget action(String label, VoidCallback onTap, int kind) => Opacity(
      opacity: _saving ? .6 : 1,
      child: Tap(
        onTap: _saving ? null : onTap,
        child: Container(
          height: m(54),
          padding: EdgeInsets.symmetric(horizontal: m(26)),
          alignment: Alignment.center,
          decoration: BoxDecoration(
            gradient: kind == 0 ? Fa.orangeV : null,
            color: kind == 1 ? const Color(0xFFF4ECE3) : null,
            borderRadius: BorderRadius.circular(m(27)),
            border: kind == 2 ? Border.all(color: const Color(0xFFE8B4AD)) : null,
          ),
          child: Text(
            label,
            style: Fa.text(m(23), weight: FontWeight.w800, color: kind == 0 ? Colors.white : (kind == 1 ? const Color(0xFF6B5646) : const Color(0xFFC0392B)), height: 1),
          ),
        ),
      ),
    );
    return _card(
      context,
      child: Stack(
        children: [
          ..._pill(context, 'chef', 'طريقة التحضير', 317, 27, 487),
          Padding(
            padding: EdgeInsets.fromLTRB(m(23), m(85), m(25), m(20)),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                for (var i = 0; i < _stepsDraft.length; i++) ...[
                  if (i > 0) SizedBox(height: m(8)),
                  Row(
                    key: ValueKey(_stepsDraft[i].key),
                    children: [
                      Container(
                        width: m(42),
                        height: m(42),
                        alignment: Alignment.center,
                        decoration: const BoxDecoration(shape: BoxShape.circle, gradient: Fa.orangeV),
                        child: Text(
                          '${i + 1}',
                          style: Fa.text(m(24), weight: FontWeight.w800, color: Colors.white, height: 1),
                        ),
                      ),
                      SizedBox(width: m(10)),
                      Expanded(
                        child: TextField(
                          controller: _stepsDraft[i].text,
                          minLines: 2,
                          maxLines: 6,
                          style: Fa.text(m(21), height: 1.35),
                          decoration: _field(context, hint: 'اكتب الخطوة'),
                        ),
                      ),
                      SizedBox(width: m(10)),
                      tool('chevronUp', i == 0 ? null : () => setState(() => _stepsDraft.insert(i - 1, _stepsDraft.removeAt(i)))),
                      SizedBox(width: m(6)),
                      tool('chevronDown', i == _stepsDraft.length - 1 ? null : () => setState(() => _stepsDraft.insert(i + 1, _stepsDraft.removeAt(i)))),
                      SizedBox(width: m(6)),
                      _trash(context, () => setState(() => _stepsDraft.removeAt(i)), size: 42),
                    ],
                  ),
                ],
                SizedBox(height: m(8)),
                _greenBtn(context, '+ خطوة', () => setState(() => _stepsDraft.add(_EditStep(++_stepKey, '')))),
                if (_editError.isNotEmpty)
                  Padding(
                    padding: EdgeInsets.only(top: m(12)),
                    child: Text(_editError, style: Fa.text(m(20), color: const Color(0xFFC0392B))),
                  ),
                SizedBox(height: m(18)),
                Row(
                  children: [
                    action(_saving ? 'جاري الحفظ...' : 'حفظ', _save, 0),
                    SizedBox(width: m(12)),
                    action('إلغاء', _cancelEdit, 1),
                    const Spacer(),
                    if (item.isCustomized) action('رجوع للأصل', _reset, 2),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _videos(BuildContext context, ItemDetail item) {
    final m = context.m;
    final videos = item.videos.take(4).toList();
    return _card(
      context,
      child: Stack(
        children: [
          ..._pill(context, 'play', 'فيديوهات طريقة العمل', 422, 16, 378),
          Padding(
            padding: EdgeInsets.fromLTRB(m(16), m(80), m(20), m(20)),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                for (var i = 0; i < 4; i++) ...[
                  if (i > 0) SizedBox(width: m(16)),
                  Expanded(
                    child: i >= videos.length
                        ? const SizedBox.shrink()
                        : Tap(
                            onTap: (videos[i].url ?? '').isEmpty ? null : () => launchUrl(Uri.parse(AppConfig.mediaUrl(videos[i].url)), mode: LaunchMode.externalApplication),
                            child: Column(
                              children: [
                                ClipRRect(
                                  borderRadius: BorderRadius.circular(m(16)),
                                  child: Image.asset('assets/screens/details/vid-${i + 1}.png', height: m(121), width: double.infinity, fit: BoxFit.cover),
                                ),
                                SizedBox(height: m(10)),
                                Text(
                                  videos[i].title ?? 'فيديو ${i + 1}',
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  style: Fa.text(m(23), weight: FontWeight.w800, color: const Color(0xFF1F140C)),
                                ),
                              ],
                            ),
                          ),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// "4 أفراد" from the family size (Arabic counting).
String peopleLabel(int n) => n == 1
    ? 'فرد واحد'
    : n == 2
    ? 'فردين'
    : (n <= 10 ? '$n أفراد' : '$n فرد');
