import 'dart:async';

import 'package:flutter/services.dart' show FilteringTextInputFormatter;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../app/providers.dart';
import '../../app/widgets/fa_chrome.dart';
import '../../app/widgets/fa_widgets.dart';
import '../../core/ui/fa_kit.dart';
import '../../core/utils/json.dart';
import '../../core/utils/money.dart';
import '../../core/widgets/async_views.dart';
import '../../core/widgets/net_image.dart';
import '../catalog/domain/catalog_models.dart';

/// "120 جنيه" / "12.5 جنيه": money as people write it, in the customer's currency.
String egp(double v) {
  final r = (v * 100).round() / 100;
  final t = r == r.roundToDouble() ? r.toStringAsFixed(0) : r.toStringAsFixed(r * 10 == (r * 10).roundToDouble() ? 1 : 2);
  return '$t $faCurrency';
}

String _qty(double v) => v == v.roundToDouble() ? v.toStringAsFixed(0) : '${(v * 1000).round() / 1000}';

class PriceUnit {
  PriceUnit(this.id, this.name);
  final String id;
  final String? name;
}

/// One ingredient in "أسعار المكونات": the price in use (mine or the app's) and the app price.
class ContentPriceRow {
  ContentPriceRow({
    required this.contentId,
    this.name,
    this.imagePath,
    required this.unitId,
    this.unitName,
    this.price,
    this.quantity = 1,
    required this.priceUnitId,
    this.priceUnitName,
    this.isMine = false,
    this.adminPrice,
    this.adminQuantity = 1,
    this.adminUnitName,
    this.units = const [],
    this.tags = const {},
  });

  /// Advanced-search values of the ingredient: cuisine / category / group / place → id.
  final Map<String, String> tags;

  final String contentId;
  final String? name;
  final String? imagePath;
  final String unitId;
  final String? unitName;
  final double? price;
  final double quantity;
  final String priceUnitId;
  final String? priceUnitName;
  final bool isMine;
  final double? adminPrice;
  final double adminQuantity;
  final String? adminUnitName;
  final List<PriceUnit> units;

  String get label => price == null ? 'مفيش سعر' : '${egp(price!)} لكل ${_qty(quantity)} ${priceUnitName ?? ''}'.trim();
  String? get adminLabel => adminPrice == null ? null : '${egp(adminPrice!)} لكل ${_qty(adminQuantity)} ${adminUnitName ?? ''}'.trim();

  factory ContentPriceRow.fromJson(Map<String, dynamic> j) => ContentPriceRow(
    contentId: strOr(j['contentId']),
    name: str(j['name']),
    imagePath: str(j['imagePath']),
    unitId: strOr(j['unitId']),
    unitName: str(j['unitName']),
    price: dblOrNull(j['price']),
    quantity: dbl(j['quantity'], 1),
    priceUnitId: strOr(j['priceUnitId']),
    priceUnitName: str(j['priceUnitName']),
    isMine: boolOf(j['isMine']),
    adminPrice: dblOrNull(j['adminPrice']),
    adminQuantity: dbl(j['adminQuantity'], 1),
    adminUnitName: str(j['adminUnitName']),
    tags: {
      for (final k in const ['cuisine', 'category', 'group', 'place'])
        if ((str(j['${k}Id']) ?? '').isNotEmpty) k: str(j['${k}Id'])!,
    },
    units: j['units'] is List
        ? (j['units'] as List).whereType<Map>().map((u) => PriceUnit(strOr(u['id']), str(u['name']))).toList()
        : const [],
  );
}

class PricesPageData {
  PricesPageData(this.enabled, this.items, {this.currency, this.countryName, this.filters = const []});
  final bool enabled;
  /// The advanced-search sections (المطبخ / التصنيف / المجموعة / مكان الشراء).
  final List<FaFilterSection> filters;
  final List<ContentPriceRow> items;
  final String? currency;
  final String? countryName;
}

class PriceCountryOption {
  PriceCountryOption(this.id, this.name, this.currency);
  final String id;
  final String? name;
  final String? currency;
  factory PriceCountryOption.fromJson(Map<String, dynamic> j) => PriceCountryOption(strOr(j['id']), str(j['name']), str(j['currency']));
}

/// "البلد اللي أنت فيها": prices and currency follow it (default = the customer's own country).
class PriceCountry {
  PriceCountry({this.countryId, this.countryName, this.currency, this.ownCountryId, this.isDefault = true, this.countries = const []});
  final String? countryId;
  final String? countryName;
  final String? currency;
  final String? ownCountryId;
  final bool isDefault;
  final List<PriceCountryOption> countries;
  factory PriceCountry.fromJson(Map<String, dynamic> j) => PriceCountry(
    countryId: str(j['countryId']),
    countryName: str(j['countryName']),
    currency: str(j['currency']),
    ownCountryId: str(j['ownCountryId']),
    isDefault: boolOf(j['isDefault']),
    countries: listOf(j['countries'], PriceCountryOption.fromJson),
  );
}

/// customer/prices calls.
class PricesApi {
  PricesApi(this.ref);
  final Ref ref;

  Future<PricesPageData> list({String text = '', String? contentId}) => ref.read(apiClientProvider).get(
    '/customer/prices',
    query: {'text': text, 'contentId': contentId},
    parse: (j) {
      final m = Map<String, dynamic>.from(j as Map);
      setFaCurrency(str(m['currency']));
      return PricesPageData(
        boolOf(m['enabled']),
        listOf(m['items'], ContentPriceRow.fromJson),
        currency: str(m['currency']),
        countryName: str(m['countryName']),
        filters: [
          if (m['filters'] is List)
            for (final f in (m['filters'] as List).whereType<Map>())
              FaFilterSection(
                strOr(f['key']),
                strOr(f['title']),
                [
                  if (f['options'] is List)
                    for (final o in (f['options'] as List).whereType<Map>()) (strOr(o['id']), strOr(o['name'])),
                ],
              ),
        ],
      );
    },
  );

  Future<PriceCountry> country() => ref.read(apiClientProvider).get('/customer/prices/country', parse: _country);

  /// Null = back to the customer's own country.
  Future<PriceCountry> setCountry(String? countryId) =>
      ref.read(apiClientProvider).put('/customer/prices/country', body: {'countryId': countryId}, parse: _country);

  static PriceCountry _country(dynamic j) {
    final c = PriceCountry.fromJson(Map<String, dynamic>.from(j as Map));
    setFaCurrency(c.currency);
    return c;
  }

  Future<bool> setEnabled(bool on) => ref.read(apiClientProvider).put('/customer/prices/enabled', body: {'enabled': on}, parse: boolOf);

  Future<ContentPriceRow> save(String contentId, double price, double quantity, String? unitId) => ref.read(apiClientProvider).put(
    '/customer/prices/$contentId',
    body: {'price': price, 'quantity': quantity, 'unitId': unitId},
    parse: (j) => ContentPriceRow.fromJson(Map<String, dynamic>.from(j as Map)),
  );

  Future<ContentPriceRow> remove(String contentId) => ref.read(apiClientProvider).delete(
    '/customer/prices/$contentId',
    parse: (j) => ContentPriceRow.fromJson(Map<String, dynamic>.from(j as Map)),
  );
}

final pricesApiProvider = Provider<PricesApi>(PricesApi.new);

/// "البلد اللي أنت فيها" and its currency (also sets the currency shown after amounts).
final priceCountryProvider = FutureProvider.autoDispose<PriceCountry>((ref) {
  ref.watch(pricesVersionProvider);
  return ref.watch(pricesApiProvider).country();
});

/// Bumped after any price change, so screens that show prices reload.
final pricesVersionProvider = NotifierProvider<PricesVersion, int>(PricesVersion.new);

class PricesVersion extends Notifier<int> {
  @override
  int build() => 0;
  void bump() => state++;
}

/// Small money badge used on dish rows, details and orders.
class FaPriceTag extends StatelessWidget {
  const FaPriceTag(this.value, {super.key, this.size = 22, this.missing = 0, this.prefix});
  final double value;
  final double size;
  final int missing;
  final String? prefix;

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    return Container(
      padding: EdgeInsets.symmetric(horizontal: s(size * .55), vertical: s(size * .18)),
      decoration: BoxDecoration(
        color: const Color(0xFFEAF4DE),
        borderRadius: BorderRadius.circular(s(size)),
        border: Border.all(color: const Color(0xFFCFE3B6), width: s(1.2)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          FaIcon('tag', size: s(size * 1.05), color: const Color(0xFF3E7B22), weight: 2.2),
          SizedBox(width: s(size * .3)),
          Flexible(
            child: Text(
              '${prefix ?? ''}${egp(value)}${missing > 0 ? ' +' : ''}',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: Fa.text(s(size), weight: FontWeight.w800, color: const Color(0xFF2F6A16), height: 1.2),
            ),
          ),
        ],
      ),
    );
  }
}

/// Edit one ingredient's price for this customer. Returns true when something changed.
Future<bool> showPriceSheet(BuildContext context, WidgetRef ref, String contentId) async {
  final changed = await showModalBottomSheet<bool>(
    context: context,
    useRootNavigator: true,
    isScrollControlled: true,
    barrierColor: const Color(0x6B2E1E12),
    constraints: const BoxConstraints(maxWidth: 560),
    backgroundColor: const Color(0xFFFFFCF8),
    shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(22))),
    builder: (_) => _PriceSheet(contentId: contentId),
  );
  if (changed == true) ref.read(pricesVersionProvider.notifier).bump();
  return changed == true;
}

class _PriceSheet extends ConsumerStatefulWidget {
  const _PriceSheet({required this.contentId});
  final String contentId;
  @override
  ConsumerState<_PriceSheet> createState() => _PriceSheetState();
}

class _PriceSheetState extends ConsumerState<_PriceSheet> {
  ContentPriceRow? _row;
  Object? _error;
  bool _busy = false;
  final _price = TextEditingController();
  final _qtyC = TextEditingController();
  String? _unit;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final data = await ref.read(pricesApiProvider).list(contentId: widget.contentId);
      if (!mounted) return;
      final row = data.items.isEmpty ? null : data.items.first;
      setState(() {
        _row = row;
        if (row != null) {
          _price.text = row.price == null ? '' : _qty(row.price!);
          _qtyC.text = _qty(row.quantity);
          _unit = row.priceUnitId;
        }
      });
    } catch (e) {
      if (mounted) setState(() => _error = e);
    }
  }

  Future<void> _save() async {
    final price = double.tryParse(_price.text.trim()) ?? 0;
    final qty = double.tryParse(_qtyC.text.trim()) ?? 1;
    if (price <= 0) {
      showMessage(context, 'اكتب السعر', error: true);
      return;
    }
    setState(() => _busy = true);
    try {
      await ref.read(pricesApiProvider).save(widget.contentId, price, qty <= 0 ? 1 : qty, _unit);
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) {
        setState(() => _busy = false);
        showMessage(context, e, error: true);
      }
    }
  }

  Future<void> _reset() async {
    setState(() => _busy = true);
    try {
      await ref.read(pricesApiProvider).remove(widget.contentId);
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) {
        setState(() => _busy = false);
        showMessage(context, e, error: true);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    final row = _row;
    InputDecoration deco(String hint) => InputDecoration(
      hintText: hint,
      isDense: true,
      filled: true,
      fillColor: Colors.white,
      contentPadding: EdgeInsets.symmetric(horizontal: s(20), vertical: s(16)),
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(s(16)), borderSide: const BorderSide(color: Color(0xFFE8D8C5))),
      enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(s(16)), borderSide: const BorderSide(color: Color(0xFFE8D8C5))),
      focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(s(16)), borderSide: const BorderSide(color: Fa.orange, width: 1.5)),
    );
    Widget label(String t) => Padding(
      padding: EdgeInsets.only(bottom: s(8), top: s(18)),
      child: Text(t, style: Fa.text(s(24), weight: FontWeight.w800, color: Fa.ink)),
    );
    final numbers = [FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))];

    return Directionality(
      textDirection: TextDirection.rtl,
      child: Padding(
        padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
        child: SafeArea(
          top: false,
          child: Padding(
            padding: EdgeInsets.fromLTRB(s(40), s(30), s(40), s(30)),
            child: row == null
                ? SizedBox(
                    height: s(260),
                    child: _error == null ? const LoadingView() : MessageView(message: '$_error'.replaceFirst('Exception: ', '')),
                  )
                : Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Row(
                        children: [
                          ClipRRect(
                            borderRadius: BorderRadius.circular(s(16)),
                            child: NetImage(row.imagePath, width: s(84), height: s(84)),
                          ),
                          SizedBox(width: s(20)),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('سعر ${row.name ?? ''}', style: Fa.text(s(32), weight: FontWeight.w800, color: Fa.ink, height: 1.25)),
                                Text(
                                  row.adminLabel == null ? 'التطبيق مش حاطط سعر للمكون ده' : 'سعر التطبيق: ${row.adminLabel}',
                                  style: Fa.text(s(21), weight: FontWeight.w500, color: Fa.muted, height: 1.3),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      label('السعر ($faCurrency)'),
                      TextField(
                        controller: _price,
                        keyboardType: const TextInputType.numberWithOptions(decimal: true),
                        inputFormatters: numbers,
                        textDirection: TextDirection.ltr,
                        textAlign: TextAlign.right,
                        style: Fa.text(s(28), color: Fa.ink),
                        decoration: deco('مثلاً 120'),
                      ),
                      label('لكل'),
                      Row(
                        children: [
                          SizedBox(
                            width: s(200),
                            child: TextField(
                              controller: _qtyC,
                              keyboardType: const TextInputType.numberWithOptions(decimal: true),
                              inputFormatters: numbers,
                              textDirection: TextDirection.ltr,
                              textAlign: TextAlign.right,
                              style: Fa.text(s(28), color: Fa.ink),
                              decoration: deco('1000'),
                            ),
                          ),
                          SizedBox(width: s(16)),
                          Expanded(
                            child: Container(
                              padding: EdgeInsets.symmetric(horizontal: s(18)),
                              decoration: BoxDecoration(
                                color: Colors.white,
                                borderRadius: BorderRadius.circular(s(16)),
                                border: Border.all(color: const Color(0xFFE8D8C5)),
                              ),
                              child: DropdownButtonHideUnderline(
                                child: DropdownButton<String>(
                                  value: row.units.any((u) => u.id == _unit) ? _unit : null,
                                  isExpanded: true,
                                  hint: Text(row.unitName ?? '', style: Fa.text(s(26), color: Fa.ink)),
                                  style: Fa.text(s(26), color: Fa.ink),
                                  items: [
                                    for (final u in row.units)
                                      DropdownMenuItem(value: u.id, child: Text(u.name ?? '', style: Fa.text(s(26), color: Fa.ink))),
                                  ],
                                  onChanged: (v) => setState(() => _unit = v),
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                      SizedBox(height: s(30)),
                      FaCta(label: 'حفظ السعر', icon: 'check', busy: _busy, onTap: _busy ? null : _save, height: 72),
                      if (row.isMine) ...[
                        SizedBox(height: s(14)),
                        Tap(
                          onTap: _busy ? null : _reset,
                          child: Padding(
                            padding: EdgeInsets.all(s(10)),
                            child: Text(
                              row.adminLabel == null ? 'امسح سعري' : 'رجّع سعر التطبيق',
                              textAlign: TextAlign.center,
                              style: Fa.text(s(24), weight: FontWeight.w800, color: const Color(0xFFC45A00)),
                            ),
                          ),
                        ),
                      ],
                    ],
                  ),
          ),
        ),
      ),
    );
  }
}

/// Settings → أسعار المكونات: every ingredient with its price; tap one to change it.
class PricesPage extends ConsumerStatefulWidget {
  const PricesPage({super.key});
  @override
  ConsumerState<PricesPage> createState() => _PricesPageState();
}

class _PricesPageState extends ConsumerState<PricesPage> {
  PricesPageData? _data;
  Object? _error;
  String _text = '';
  bool _mineOnly = false;
  /// Advanced search picks: key → id.
  Map<String, String> _picked = const {};
  Timer? _debounce;
  int _token = 0;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }

  Future<void> _load() async {
    final token = ++_token;
    try {
      final data = await ref.read(pricesApiProvider).list(text: _text);
      if (mounted && token == _token) setState(() { _data = data; _error = null; });
    } catch (e) {
      if (mounted && token == _token) setState(() => _error = e);
    }
  }

  Future<void> _toggle(bool on) async {
    final before = _data;
    if (before == null) return;
    setState(() => _data = PricesPageData(on, before.items, currency: before.currency, countryName: before.countryName, filters: before.filters));
    try {
      await ref.read(pricesApiProvider).setEnabled(on);
      ref.read(pricesVersionProvider.notifier).bump();
    } catch (e) {
      if (mounted) {
        setState(() => _data = before);
        showMessage(context, e, error: true);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    final data = _data;
    final rows = (data?.items ?? const <ContentPriceRow>[])
        .where((r) => (!_mineOnly || r.isMine) && _picked.entries.every((e) => r.tags[e.key] == e.value))
        .toList();

    Widget row(ContentPriceRow r) => Tap(
      onTap: () async {
        if (await showPriceSheet(context, ref, r.contentId)) _load();
      },
      child: Container(
        padding: EdgeInsets.symmetric(vertical: s(16), horizontal: s(20)),
        decoration: BoxDecoration(
          color: const Color(0xFFFFFCF8),
          borderRadius: BorderRadius.circular(s(22)),
          border: Border.all(color: r.isMine ? const Color(0xFFF9CF9F) : const Color(0xFFF2E6D8), width: s(1.5)),
          boxShadow: [BoxShadow(color: const Color(0x12784820), offset: Offset(0, s(4)), blurRadius: s(14))],
        ),
        child: Row(
          children: [
            ClipRRect(borderRadius: BorderRadius.circular(s(14)), child: NetImage(r.imagePath, width: s(78), height: s(78))),
            SizedBox(width: s(20)),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(r.name ?? '', style: Fa.text(s(28), weight: FontWeight.w800, color: Fa.ink, height: 1.25)),
                  SizedBox(height: s(4)),
                  Text(
                    r.label,
                    style: Fa.text(s(23), weight: FontWeight.w700, color: r.price == null ? const Color(0xFF9A8B7E) : const Color(0xFF2F6A16), height: 1.3),
                  ),
                  if (r.isMine)
                    Text(
                      r.adminLabel == null ? 'سعرك انت' : 'سعرك انت · التطبيق: ${r.adminLabel}',
                      style: Fa.text(s(19), weight: FontWeight.w500, color: const Color(0xFFC45A00), height: 1.3),
                    ),
                ],
              ),
            ),
            SizedBox(width: s(10)),
            FaIcon('pencil', size: s(30), color: Fa.orange),
          ],
        ),
      ),
    );

    return Directionality(
      textDirection: TextDirection.rtl,
      child: ColoredBox(
        color: Fa.page,
        child: SafeArea(
          bottom: false,
          child: RefreshIndicator(
            onRefresh: _load,
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: EdgeInsets.only(bottom: faBottomSpace(context, ref)),
              children: [
                const FaScreenHeader(
                  title: 'أسعار المكونات',
                  titleIcon: 'tag',
                  subtitle: 'سعر الأكلة والطلب بيتحسب من أسعار مكوناتها',
                  height: 210,
                  titleY: 157,
                  backY: 120,
                ),
                Padding(
                  padding: EdgeInsets.fromLTRB(s(36), s(40), s(36), 0),
                  child: Container(
                    padding: EdgeInsets.symmetric(horizontal: s(24), vertical: s(16)),
                    decoration: BoxDecoration(
                      color: const Color(0xFFFFF4E6),
                      borderRadius: BorderRadius.circular(s(22)),
                      border: Border.all(color: const Color(0xFFF9CF9F), width: s(1.5)),
                    ),
                    child: Row(
                      children: [
                        FaIcon('coins', size: s(40), color: Fa.orange),
                        SizedBox(width: s(18)),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text('التعامل بالأسعار', style: Fa.text(s(28), weight: FontWeight.w800, color: Fa.ink, height: 1.25)),
                              Text(
                                'يظهر سعر الأكلات والطلبات',
                                style: Fa.text(s(21), weight: FontWeight.w500, color: Fa.muted, height: 1.3),
                              ),
                            ],
                          ),
                        ),
                        if (data != null) PriceSwitch(on: data.enabled, onTap: () => _toggle(!data.enabled)),
                      ],
                    ),
                  ),
                ),
                // Same search as the dishes: text, voice and the advanced-search panel.
                Padding(
                  padding: EdgeInsets.only(top: s(24)),
                  child: FaSearchBar(
                    query: ItemQuery(text: _text),
                    hint: 'ابحث عن مكون ...',
                    left: 36,
                    right: 36,
                    onChanged: (q) {
                      final t = q.text.trim();
                      if (t == _text) return;
                      _text = t;
                      _load();
                    },
                    filters: data?.filters ?? const [],
                    picked: _picked,
                    onPicked: (m) => setState(() => _picked = m),
                  ),
                ),
                Padding(
                  padding: EdgeInsets.fromLTRB(s(36), s(20), s(36), 0),
                  child: Row(
                    children: [
                      FaChip(label: 'الكل', on: !_mineOnly, onTap: () => setState(() => _mineOnly = false), height: 56),
                      SizedBox(width: s(14)),
                      FaChip(label: 'أسعاري', on: _mineOnly, onTap: () => setState(() => _mineOnly = true), height: 56),
                    ],
                  ),
                ),
                Padding(
                  padding: EdgeInsets.fromLTRB(s(36), s(22), s(36), 0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      if (data == null && _error == null) Padding(padding: EdgeInsets.all(s(40)), child: const LoadingView()),
                      if (_error != null)
                        Text('$_error'.replaceFirst('Exception: ', ''), textAlign: TextAlign.center, style: Fa.text(s(24), color: const Color(0xFFB71C1C))),
                      for (final r in rows) ...[row(r), SizedBox(height: s(14))],
                      if (data != null && rows.isEmpty)
                        Padding(
                          padding: EdgeInsets.symmetric(vertical: s(60)),
                          child: Text(
                            _mineOnly ? 'لسه محطتش أسعار ليك' : 'مفيش مكونات',
                            textAlign: TextAlign.center,
                            style: Fa.text(s(26), color: const Color(0xFF8A7A6E)),
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

/// The orange on/off switch of the settings screen.
class PriceSwitch extends StatelessWidget {
  const PriceSwitch({super.key, required this.on, required this.onTap});
  final bool on;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    return Tap(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        width: s(78),
        height: s(42),
        decoration: BoxDecoration(
          color: on ? null : const Color(0xFFE4D9CC),
          gradient: on ? const LinearGradient(colors: [Color(0xFFFF8A12), Color(0xFFF36202)]) : null,
          borderRadius: BorderRadius.circular(s(21)),
        ),
        child: AnimatedAlign(
          duration: const Duration(milliseconds: 200),
          alignment: on ? Alignment.centerLeft : Alignment.centerRight,
          child: Container(
            width: s(34),
            height: s(34),
            margin: EdgeInsets.all(s(4)),
            decoration: const BoxDecoration(shape: BoxShape.circle, color: Colors.white),
          ),
        ),
      ),
    );
  }
}
