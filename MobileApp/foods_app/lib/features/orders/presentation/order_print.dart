import 'dart:typed_data';

import 'package:flutter/services.dart' show rootBundle;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import 'package:printing/printing.dart';
import 'package:share_plus/share_plus.dart';

import '../../../app/providers.dart';
import '../../../core/config/app_config.dart';
import '../../cart/domain/purchase_rules.dart';
import '../../cart/domain/shopping_line.dart';
import '../domain/order_models.dart';
import 'order_share.dart';

// Same art as the "طلب الشراء" screen (order_details_page.dart).
const _leaf =
    'M6.05 8.05a7.001 7.001 0 0 0-.02 9.88c1.47-3.4 4.09-6.24 7.36-7.93A15.952 15.952 0 0 0 8 19.32c2.6 1.23 5.8.78 7.950-1.37C19.43 14.47 20 4 20 4S9.53 4.57 6.05 8.05z';
const _basket =
    'M17.21 9l-4.38-6.56a.993.993 0 0 0-.83-.42c-.32 0-.64.14-.83.43L6.79 9H2c-.55 0-1 .45-1 1 0 .09.01.18.04.27l2.54 9.27c.23.84 1 1.460 1.920 1.460h13c.92 0 1.69-.62 1.93-1.46l2.54-9.27L23 10c0-.55-.45-1-1-1h-4.79zM9 9l3-4.4L15 9H9z';
const _group =
    'M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.670-3.5-7-3.5z';
const _cloche = 'M12 6c-4.4 0-8 3.1-8.7 7h17.4C20 9.1 16.4 6 12 6zm0-1a1 1 0 1 0 0-2 1 1 0 0 0 0 2zM2 15h20v2H2z';

const _ink = PdfColor.fromInt(0xFF1F140C);
const _orange = PdfColor.fromInt(0xFFF26A0A);
const _qty = PdfColor.fromInt(0xFFF06A08);
const _green = PdfColor.fromInt(0xFF3E9A2C);
const _line = PdfColor.fromInt(0xFFEFE4D8);
const _cardBorder = PdfColor.fromInt(0xFFF2E8DD);

/// Rows per card block, so a long place still fits on one printed page.
const _rowsPerBlock = 16;

List<(String, List<ShoppingLine>)> _groups(OrderDetail o) {
  final map = <String, List<ShoppingLine>>{};
  for (final l in o.lines) {
    if (!(l.quantity > 0)) continue;
    final p = (l.purchasePlaceName ?? '').trim();
    map.putIfAbsent(p.isEmpty ? 'البقالة' : p, () => []).add(l);
  }
  return [for (final e in map.entries) (e.key, e.value)];
}

pw.Widget _icon(String d, double size, PdfColor color) =>
    pw.SvgImage(svg: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="${_hex(color)}" d="$d"/></svg>', width: size, height: size);

String _hex(PdfColor c) => '#${(c.toInt() & 0xFFFFFF).toRadixString(16).padLeft(6, '0')}';

class _Assets {
  _Assets(this.regular, this.bold, this.band, this.logo, this.ingFallback, this.dishFallback);
  final pw.Font regular, bold;
  final pw.ImageProvider band, logo, ingFallback, dishFallback;
  final images = <String, pw.ImageProvider?>{};

  static Future<_Assets> load() async {
    Future<pw.ImageProvider> asset(String p) async => pw.MemoryImage((await rootBundle.load(p)).buffer.asUint8List());
    return _Assets(
      pw.Font.ttf(await rootBundle.load('assets/fonts/Tajawal-Medium.ttf')),
      pw.Font.ttf(await rootBundle.load('assets/fonts/Tajawal-ExtraBold.ttf')),
      await asset('assets/screens/purchase/hero-band.png'),
      await asset('assets/screens/shared/logo-transparent.png'),
      await asset('assets/screens/ingredients/onion.png'),
      await asset('assets/screens/foods/koshari.png'),
    );
  }

  /// Downloads every photo once (in parallel); a photo that fails uses the fallback art.
  Future<void> fetch(Iterable<String?> paths) async {
    final urls = {for (final p in paths) AppConfig.mediaUrl(p)}..remove('');
    await Future.wait([
      for (final u in urls.where((u) => !images.containsKey(u)))
        networkImage(u).timeout(const Duration(seconds: 8)).then<pw.ImageProvider?>((v) => v).catchError((_) => null).then((v) => images[u] = v),
    ]);
  }

  pw.ImageProvider image(String? path, {bool dish = false}) => images[AppConfig.mediaUrl(path)] ?? (dish ? dishFallback : ingFallback);
}

/// The order laid out like the screen: header, "عدد الطلبات", a card per purchase place
/// (with its own count) and, optionally, the chosen dishes.
List<pw.Widget> _content(OrderDetail o, _Assets a, {required bool withDishes}) {
  final groups = _groups(o);
  final count = o.lines.where((l) => l.quantity > 0).length;

  pw.Widget badge(int n, PdfColor color) => pw.Container(
    constraints: const pw.BoxConstraints(minWidth: 22),
    height: 18,
    padding: const pw.EdgeInsets.symmetric(horizontal: 6),
    alignment: pw.Alignment.center,
    decoration: pw.BoxDecoration(color: color, borderRadius: pw.BorderRadius.circular(9)),
    child: pw.Text('$n', style: pw.TextStyle(font: a.bold, fontSize: 10.5, color: PdfColors.white)),
  );

  pw.Widget row(String? image, String name, String qty, {bool last = false, bool dish = false}) => pw.Container(
    height: 33,
    margin: const pw.EdgeInsets.symmetric(horizontal: 14),
    decoration: pw.BoxDecoration(
      border: last ? null : const pw.Border(bottom: pw.BorderSide(color: _line, width: .75)),
    ),
    child: pw.Row(
      children: [
        pw.ClipRRect(
          horizontalRadius: 8,
          verticalRadius: 8,
          child: pw.Image(a.image(image, dish: dish), width: 29, height: 25, fit: dish ? pw.BoxFit.cover : pw.BoxFit.contain),
        ),
        pw.SizedBox(width: 10),
        pw.Expanded(
          child: pw.Text(name, maxLines: 1, style: pw.TextStyle(font: a.bold, fontSize: 12.5, color: _ink)),
        ),
        pw.Container(
          constraints: pw.BoxConstraints(minWidth: dish ? 0 : 75),
          height: 21,
          padding: const pw.EdgeInsets.symmetric(horizontal: 10),
          alignment: pw.Alignment.center,
          decoration: pw.BoxDecoration(color: const PdfColor.fromInt(0xFFFDEEE1), borderRadius: pw.BorderRadius.circular(12.5)),
          child: pw.Text(qty, style: pw.TextStyle(font: a.bold, fontSize: 11, color: _qty)),
        ),
      ],
    ),
  );

  pw.Widget card(String title, String icon, List<pw.Widget> rows, {bool dishes = false, int? count, bool more = false}) => pw.Container(
    margin: const pw.EdgeInsets.only(top: 9, left: 6, right: 6),
    padding: const pw.EdgeInsets.only(bottom: 3),
    decoration: pw.BoxDecoration(
      color: const PdfColor.fromInt(0xFFFFFCF8),
      borderRadius: pw.BorderRadius.circular(16),
      border: pw.Border.all(color: _cardBorder, width: .75),
    ),
    child: pw.Column(
      children: [
        pw.Container(
          height: 28,
          padding: const pw.EdgeInsets.symmetric(horizontal: 14),
          decoration: pw.BoxDecoration(
            borderRadius: const pw.BorderRadius.only(topLeft: pw.Radius.circular(16), topRight: pw.Radius.circular(16)),
            gradient: pw.LinearGradient(
              colors: dishes
                  ? const [PdfColor.fromInt(0xFFFFF3E6), PdfColor.fromInt(0xFFFDEBD8)]
                  : const [PdfColor.fromInt(0xFFEEF4E0), PdfColor.fromInt(0xFFE6F0D3)],
            ),
          ),
          child: pw.Row(
            children: [
              _icon(icon, 21, dishes ? _orange : _green),
              pw.SizedBox(width: 9),
              pw.Text(more ? '$title (تابع)' : title, style: pw.TextStyle(font: a.bold, fontSize: 12.5, color: _ink)),
              pw.Spacer(),
              if (count != null) badge(count, _orange),
            ],
          ),
        ),
        ...rows,
      ],
    ),
  );

  List<pw.Widget> cards(String title, String icon, List<pw.Widget> rows, {bool dishes = false}) => [
    for (var i = 0; i < rows.length; i += _rowsPerBlock)
      card(
        title,
        icon,
        rows.sublist(i, (i + _rowsPerBlock).clamp(0, rows.length)),
        dishes: dishes,
        count: i == 0 ? rows.length : null,
        more: i > 0,
      ),
  ];

  final name = (o.name ?? '').isEmpty ? 'طلب الشراء' : o.name!;
  return [
    pw.Row(
      mainAxisAlignment: pw.MainAxisAlignment.center,
      children: [
        pw.Text('طلب الشراء', style: pw.TextStyle(font: a.bold, fontSize: 17, color: _ink)),
        pw.SizedBox(width: 8),
        _icon(_basket, 26, const PdfColor.fromInt(0xFFF7931E)),
      ],
    ),
    pw.SizedBox(height: 8),
    pw.Image(a.band, height: 52, fit: pw.BoxFit.cover),
    pw.SizedBox(height: 8),
    pw.Center(child: pw.Text(name, textAlign: pw.TextAlign.center, style: pw.TextStyle(font: a.bold, fontSize: 14, color: _ink))),
    pw.SizedBox(height: 3),
    pw.Center(child: pw.Text(formatDay(o.date), style: pw.TextStyle(font: a.regular, fontSize: 10.5, color: const PdfColor.fromInt(0xFF8A6A4A)))),
    pw.SizedBox(height: 8),
    pw.Container(
      height: 38,
      padding: const pw.EdgeInsets.symmetric(horizontal: 14),
      decoration: pw.BoxDecoration(
        borderRadius: pw.BorderRadius.circular(21),
        gradient: const pw.LinearGradient(colors: [PdfColor.fromInt(0xFFFDF1E6), PdfColor.fromInt(0xFFFDE8D4)]),
      ),
      child: pw.Row(
        children: [
          _icon(_group, 23, const PdfColor.fromInt(0xFFF57C1F)),
          pw.SizedBox(width: 7),
          pw.Text('عدد الطلبات', style: pw.TextStyle(font: a.bold, fontSize: 12.5, color: _ink)),
          pw.Spacer(),
          pw.Container(
            constraints: const pw.BoxConstraints(minWidth: 44),
            height: 26,
            padding: const pw.EdgeInsets.symmetric(horizontal: 9),
            alignment: pw.Alignment.center,
            decoration: pw.BoxDecoration(
              borderRadius: pw.BorderRadius.circular(13),
              gradient: const pw.LinearGradient(
                colors: [PdfColor.fromInt(0xFFFF7A12), PdfColor.fromInt(0xFFF45F04)],
                begin: pw.Alignment.topCenter,
                end: pw.Alignment.bottomCenter,
              ),
            ),
            child: pw.Text('$count', style: pw.TextStyle(font: a.bold, fontSize: 12.5, color: PdfColors.white)),
          ),
        ],
      ),
    ),
    for (var gi = 0; gi < groups.length; gi++)
      ...cards(groups[gi].$1, gi % 2 == 0 ? _leaf : _basket, [
        for (var i = 0; i < groups[gi].$2.length; i++)
          row(
            groups[gi].$2[i].imagePath,
            groups[gi].$2[i].name ?? '',
            PurchaseRules.text(groups[gi].$2[i]),
            last: i == groups[gi].$2.length - 1 || (i + 1) % _rowsPerBlock == 0,
          ),
      ]),
    if (groups.isEmpty)
      pw.Padding(
        padding: const pw.EdgeInsets.only(top: 20),
        child: pw.Center(child: pw.Text('لا توجد مكونات في هذا الطلب.', style: pw.TextStyle(font: a.regular, fontSize: 12))),
      ),
    if (withDishes && o.items.isNotEmpty)
      ...cards('الأكلات المختارة', _cloche, dishes: true, [
        for (var i = 0; i < o.items.length; i++)
          row(
            o.items[i].imagePath,
            o.items[i].name,
            '${o.items[i].repeatCount} مرة${o.items[i].peopleCount != null ? ' · ${o.items[i].peopleCount} أفراد' : ''}',
            last: i == o.items.length - 1 || (i + 1) % _rowsPerBlock == 0,
            dish: true,
          ),
      ]),
    if ((o.notes ?? '').trim().isNotEmpty)
      pw.Padding(
        padding: const pw.EdgeInsets.fromLTRB(10, 10, 10, 0),
        child: pw.Text('ملاحظات: ${o.notes!.trim()}', style: pw.TextStyle(font: a.regular, fontSize: 11, color: _ink)),
      ),
    pw.SizedBox(height: 12),
    pw.Center(child: pw.Image(a.logo, height: 26)),
  ];
}

const _paper = PdfColor.fromInt(0xFFFFFDFB);

Future<_Assets> _prepare(OrderDetail o) async {
  final a = await _Assets.load();
  await a.fetch([...o.lines.where((l) => l.quantity > 0).map((l) => l.imagePath), ...o.items.map((i) => i.imagePath)]);
  return a;
}

/// A4 pages for printing, with the same cards and colours as the screen.
Future<Uint8List> orderPdf(OrderDetail o, {bool withDishes = false}) async {
  final a = await _prepare(o);
  final doc = pw.Document(theme: pw.ThemeData.withFont(base: a.regular, bold: a.bold));
  doc.addPage(
    pw.MultiPage(
      pageTheme: pw.PageTheme(
        pageFormat: PdfPageFormat.a4,
        textDirection: pw.TextDirection.rtl,
        margin: const pw.EdgeInsets.symmetric(horizontal: 70, vertical: 30),
        buildBackground: (_) => pw.FullPage(ignoreMargins: true, child: pw.Container(color: _paper)),
      ),
      build: (_) => _content(o, a, withDishes: withDishes),
    ),
  );
  return doc.save();
}

/// One tall phone-width page of the order, the source of the shared picture.
Future<Uint8List> orderSharePdf(OrderDetail o) async {
  final a = await _prepare(o);
  final doc = pw.Document(theme: pw.ThemeData.withFont(base: a.regular, bold: a.bold));
  doc.addPage(
    pw.Page(
      pageFormat: const PdfPageFormat(400, double.infinity, marginAll: 16),
      textDirection: pw.TextDirection.rtl,
      build: (_) => pw.Container(
        color: _paper,
        child: pw.Column(crossAxisAlignment: pw.CrossAxisAlignment.stretch, children: _content(o, a, withDishes: true)),
      ),
    ),
  );
  return doc.save();
}

/// One tall phone-width picture of the order (for WhatsApp etc.).
Future<Uint8List> orderPng(OrderDetail o) async {
  final bytes = await orderSharePdf(o);
  await for (final page in Printing.raster(bytes, dpi: 220)) {
    return page.toPng();
  }
  throw StateError('raster');
}

/// Prints the purchase order (and optionally the dishes) through the system print dialog.
Future<void> printOrder(WidgetRef ref, OrderDetail order, {bool withDishes = false}) async {
  final repo = ref.read(ordersRepositoryProvider);
  await Printing.layoutPdf(name: order.name ?? 'طلب الشراء', onLayout: (_) => orderPdf(order, withDishes: withDishes));
  try {
    await repo.markPrinted(order.id);
  } catch (_) {}
}

/// Shares the order as a picture that looks like the screen, with the text list as caption.
Future<void> shareOrder(WidgetRef ref, OrderDetail order) async {
  final repo = ref.read(ordersRepositoryProvider);
  final title = order.name ?? 'طلب شراء';
  try {
    final png = await orderPng(order);
    await SharePlus.instance.share(
      ShareParams(
        files: [XFile.fromData(png, mimeType: 'image/png', name: 'طلب-الشراء.png')],
        fileNameOverrides: ['order.png'],
        text: orderShareText(order),
        subject: title,
      ),
    );
  } catch (_) {
    // No picture (e.g. raster not available): fall back to the plain text list.
    await SharePlus.instance.share(ShareParams(text: orderShareText(order), subject: title));
  }
  try {
    await repo.markShared(order.id);
  } catch (_) {}
}
