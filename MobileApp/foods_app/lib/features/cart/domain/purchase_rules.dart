import 'dart:math' as math;

import 'shopping_line.dart';
import '../../../core/utils/money.dart';

/// "أقل وحدة شراء": how an ingredient is really bought.
/// Port of the web client's core/purchase.ts so both apps step the same way.
///
/// 0 normal (grams / count), 1 package (bottle...), 2 kilo fractions,
/// 3 by money (EGP), 4 by money while under 1/8 kg, then kilo fractions.
class PurchaseRules {
  PurchaseRules._();

  static const normal = 0;
  static const package = 1;
  static const kilo = 2;
  static const money = 3;
  static const moneyKilo = 4;

  static const _fractions = [125.0, 250.0, 1000 / 3, 500.0, 750.0];
  static const _eps = 0.01;

  static double _round3(double v) => (v * 1000).round() / 1000;
  static double _round2(double v) => (v * 100).round() / 100;

  static String trim(double v) {
    if (v == v.roundToDouble()) return v.toInt().toString();
    return _round2(v).toString();
  }

  static int mode(ShoppingLine l) => l.purchaseMode;

  static double _moneyKiloUnit(ShoppingLine l) => l.purchaseStep > 0 ? l.purchaseStep : 125;

  static ({double unit, double price, double inc, double min}) _mk(ShoppingLine l) {
    final unit = _moneyKiloUnit(l);
    final price = l.purchaseUnitPrice;
    final inc = l.purchaseIncrement > 0 ? l.purchaseIncrement : 5.0;
    final min = l.purchaseMinAmount > 0 ? l.purchaseMinAmount : inc;
    return (unit: unit, price: price, inc: inc, min: min);
  }

  static double _moneyOf(ShoppingLine l, double qty) {
    final m = _mk(l);
    final raw = qty / m.unit * m.price;
    return math.max(m.min, (raw / m.inc - 1e-3).ceil() * m.inc);
  }

  static double _gramsFor(ShoppingLine l, double amount) {
    final m = _mk(l);
    if (amount >= m.price) return m.unit;
    return (amount / m.price * m.unit * 1000).round() / 1000;
  }

  static bool _isMoneyState(ShoppingLine l) {
    final qty = l.quantity;
    if (!(qty > 0 && qty < _moneyKiloUnit(l))) return false;
    return l.purchaseUnitPrice <= 0 || _moneyOf(l, qty) < l.purchaseUnitPrice;
  }

  static double _moneyAmount(ShoppingLine l) {
    if (l.purchaseUnitPrice > 0) return _moneyOf(l, l.quantity);
    return l.purchaseIncrement > 0 ? l.purchaseIncrement : 30;
  }

  static double step(ShoppingLine l) {
    final s = l.purchaseStep;
    switch (mode(l)) {
      case package:
        return s > 0 ? s : 1;
      case kilo:
        return s > 0 ? s : 125;
      case moneyKilo:
        return _moneyKiloUnit(l);
      case money:
        return l.purchaseIncrement > 0 ? l.purchaseIncrement : (s > 0 ? s : 10);
      default:
        final q = l.quantity;
        return q.abs() >= 1 || q == 0 ? 1 : 0.1;
    }
  }

  static double minimum(ShoppingLine l) {
    final m = mode(l);
    if (m == normal) return 0;
    if (m == moneyKilo) return _moneyKiloUnit(l);
    final s = l.purchaseStep;
    if (m == package) return s > 0 ? s : 1;
    if (m == kilo) return s > 0 ? s : 125;
    return s > 0 ? s : 10;
  }

  static List<double> _allowed(double min) => _fractions.where((f) => f >= min - _eps).toList();

  static List<double> _candidates(double min, int k) => [
    if (k > 0) k * 1000.0,
    for (final f in _allowed(min)) k * 1000 + f,
    (k + 1) * 1000.0,
  ];

  static double _kiloNext(double min, double qty) {
    final k = math.max(0, ((qty + _eps) / 1000).floor());
    return _round3(_candidates(min, k).where((v) => v > qty + _eps).reduce(math.min));
  }

  static double _kiloPrev(double min, double qty) {
    final k = math.max(0, ((qty - _eps) / 1000).floor());
    final list = [..._candidates(min, k), if (k > 0) ..._candidates(min, k - 1)].where((v) => v < qty - _eps).toList();
    return list.isEmpty ? 0 : _round3(list.reduce(math.max));
  }

  /// Next quantity after pressing + (delta 1) or − (delta -1).
  static double stepQuantity(ShoppingLine l, int delta) {
    var qty = l.quantity;
    final s = step(l);
    final m = mode(l);
    if (m == normal) return math.max(0, _round2(qty + delta * s));
    if (m == kilo) {
      final min = minimum(l);
      return delta > 0 ? _kiloNext(min, math.max(0, qty)) : (qty <= 0 ? 0 : _kiloPrev(min, qty));
    }
    if (m == moneyKilo && l.purchaseUnitPrice > 0) {
      final mk = _mk(l);
      if (qty > 0 && qty < mk.unit && !_isMoneyState(l)) qty = mk.unit;
      if (delta > 0) {
        if (qty <= 0) return _gramsFor(l, mk.min);
        if (qty < mk.unit) return _gramsFor(l, _moneyOf(l, qty) + mk.inc);
        return _kiloNext(mk.unit, qty);
      }
      if (qty <= 0) return 0;
      if (qty < mk.unit) {
        final amount = _moneyOf(l, qty) - mk.inc;
        return amount < mk.min ? 0 : _gramsFor(l, amount);
      }
      if (qty - mk.unit < _eps) {
        final top = math.max(mk.min, ((mk.price - mk.inc) / mk.inc + 1e-9).floor() * mk.inc);
        return top >= mk.price ? 0 : _gramsFor(l, top);
      }
      return _kiloPrev(mk.unit, qty);
    }
    if (m == moneyKilo) {
      final unit = _moneyKiloUnit(l);
      if (delta > 0) {
        if (qty <= 0) return 1;
        if (qty < unit) return unit;
        return _kiloNext(unit, qty);
      }
      if (qty < unit) return 0;
      if (qty - unit < _eps) return 1;
      return _kiloPrev(unit, qty);
    }
    final min = minimum(l);
    if (delta > 0) return qty <= 0 ? min : _round3(qty + s);
    final next = _round3(qty - s);
    return next < min ? 0 : next;
  }

  static const _labels = <(double, String, String)>[
    (125, '1/8', '⅛'),
    (250, '1/4', '¼'),
    (1000 / 3, '1/3', '⅓'),
    (500, '1/2', '½'),
    (750, '3/4', '¾'),
  ];
  static const _oldEighths = {3: ('3/8', '⅜'), 5: ('5/8', '⅝'), 7: ('7/8', '⅞')};

  static ({int whole, String full, String short}) _parts(double grams) {
    var whole = ((grams + _eps) / 1000).floor();
    final rest = grams - whole * 1000;
    if (rest > 999) return (whole: whole + 1, full: '', short: '');
    if (rest < 1) return (whole: whole, full: '', short: '');
    for (final e in _labels) {
      if ((rest - e.$1).abs() < 1) return (whole: whole, full: e.$2, short: e.$3);
    }
    final eighths = (rest / 125).round();
    if (eighths >= 8) return (whole: whole + 1, full: '', short: '');
    for (final e in _labels) {
      if (e.$1 == eighths * 125) return (whole: whole, full: e.$2, short: e.$3);
    }
    final old = _oldEighths[eighths];
    return old == null ? (whole: whole, full: '', short: '') : (whole: whole, full: old.$1, short: old.$2);
  }

  static String kiloText(double grams) {
    final p = _parts(grams);
    if (p.full.isEmpty) return '${p.whole} كيلو';
    return p.whole > 0 ? '${p.whole} و ${p.full} كيلو' : '${p.full} كيلو';
  }

  static String kiloShort(double grams) {
    final p = _parts(grams);
    if (p.short.isEmpty) return '${p.whole}';
    return p.whole > 0 ? '${p.whole}${p.short}' : p.short;
  }

  /// Number shown on the counter.
  static String qtyText(ShoppingLine l) {
    final qty = l.quantity;
    switch (mode(l)) {
      case package:
        return trim(qty / minimum(l));
      case kilo:
        return qty > 0 ? kiloShort(qty) : '0';
      case moneyKilo:
        if (_isMoneyState(l)) return trim(_moneyAmount(l));
        return qty > 0 ? kiloShort(math.max(qty, _moneyKiloUnit(l))) : '0';
      default:
        return trim(qty);
    }
  }

  /// Unit shown next to the counter.
  static String unitText(ShoppingLine l) {
    switch (mode(l)) {
      case package:
        final p = (l.packageName ?? '').trim();
        return p.isEmpty ? 'عبوة' : p;
      case kilo:
        return 'كيلو';
      case money:
        return faCurrency;
      case moneyKilo:
        return _isMoneyState(l) ? faCurrency : 'كيلو';
      default:
        return (l.unitName ?? '').trim();
    }
  }

  /// Full text: "2 زجاجة", "1/8 كيلو", "20 جنيه", "300 جرام".
  static String text(ShoppingLine l) {
    if (mode(l) == kilo) return l.quantity > 0 ? kiloText(l.quantity) : '0 كيلو';
    if (mode(l) == moneyKilo) {
      if (_isMoneyState(l)) return '${trim(_moneyAmount(l))} $faCurrency';
      return l.quantity > 0 ? kiloText(l.quantity) : '0 كيلو';
    }
    final unit = unitText(l);
    final q = qtyText(l);
    return unit.isEmpty ? q : '$q $unit';
  }
}
