import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../app/providers.dart';
import '../../auth/presentation/auth_controller.dart';
import '../domain/selected_item.dart';
import '../domain/shopping_line.dart';
import '../../prices/prices.dart';

/// The working order's name on the server ("حفظ مؤقت").
const draftOrderName = 'طلباتي';

class CartState {
  const CartState({
    this.orderId,
    this.items = const [],
    this.lines = const [],
    this.linesFor = '',
    this.ready = false,
    this.syncing = false,
  });

  final String? orderId;
  final List<SelectedItem> items;
  final List<ShoppingLine> lines;

  /// Fingerprint of the dishes the current [lines] were made for.
  final String linesFor;
  final bool ready;
  final bool syncing;

  int get selectedCount => items.where((i) => i.repeatCount > 0).length;
  int count(String itemId) => items.where((i) => i.itemId == itemId).fold(0, (a, i) => a + i.repeatCount);
  bool get linesMatchItems => lines.isNotEmpty && linesFor == fingerprint(items);

  CartState copyWith({
    String? Function()? orderId,
    List<SelectedItem>? items,
    List<ShoppingLine>? lines,
    String? linesFor,
    bool? ready,
    bool? syncing,
  }) => CartState(
    orderId: orderId != null ? orderId() : this.orderId,
    items: items ?? this.items,
    lines: lines ?? this.lines,
    linesFor: linesFor ?? this.linesFor,
    ready: ready ?? this.ready,
    syncing: syncing ?? this.syncing,
  );

  static String fingerprint(List<SelectedItem> items) =>
      (items.map((i) => '${i.itemId.toLowerCase()}:${i.repeatCount}:${i.peopleCount ?? ''}:${i.countryId ?? ''}').toList()..sort()).join('|');
}

final cartControllerProvider = NotifierProvider<CartController, CartState>(CartController.new);

/// The dishes the customer picked, saved on the server as a temporary order
/// so the web site and the phone share the same selection.
class CartController extends Notifier<CartState> {
  Timer? _timer;
  bool _saving = false;
  bool _again = false;
  bool _linesDirty = false;
  bool _booting = false;
  int _rev = 0;

  static final _day = DateFormat('yyyy-MM-dd');

  @override
  CartState build() {
    ref.onDispose(() => _timer?.cancel());
    // Start fresh when the customer logs out or another one logs in.
    ref.listen(authControllerProvider.select((a) => a.loggedIn), (prev, loggedIn) {
      _timer?.cancel();
      state = const CartState();
      if (loggedIn) boot();
    });
    // Prices turned on/off or an ingredient price changed: take the new prices, keep the quantities.
    ref.listen(pricesVersionProvider, (_, _) => refreshPrices());
    return const CartState();
  }

  Future<void> refreshPrices() async {
    final id = state.orderId;
    if (id == null) return;
    try {
      final detail = await ref.read(ordersRepositoryProvider).order(id);
      final byKey = {for (final l in detail.lines) l.key: l.unitPrice};
      final byItem = {for (final i in detail.items) i.itemId: i.price};
      final lines = state.lines.map((l) => ShoppingLine.fromJson({...l.toJson(), 'unitPrice': byKey[l.key]})).toList();
      final items = state.items.map((i) => i.copy()..price = byItem[i.itemId]).toList();
      state = state.copyWith(lines: lines, items: items);
    } catch (_) {}
  }

  String get _today => _day.format(DateTime.now());

  /// Loads the working order from the server once.
  Future<void> boot({bool force = false}) async {
    if (_booting || (state.ready && !force)) return;
    if (!ref.read(authControllerProvider).loggedIn) return;
    _booting = true;
    final rev = _rev;
    try {
      final repo = ref.read(ordersRepositoryProvider);
      final session = ref.read(sessionStorageProvider);
      String? id = session.draftOrderId;
      if (id == null) {
        final orders = await repo.orders();
        for (final o in orders) {
          if ((o.name ?? '').trim() == draftOrderName) {
            id = o.id;
            break;
          }
        }
      }
      if (id != null) {
        try {
          final detail = await repo.order(id);
          if (rev == _rev) {
            state = state.copyWith(
              orderId: () => detail.id,
              items: detail.items.where((i) => i.repeatCount > 0).toList(),
              lines: detail.lines,
              linesFor: detail.lines.isEmpty ? '' : CartState.fingerprint(detail.items),
            );
            await session.setDraftOrderId(detail.id);
          }
        } catch (_) {
          await session.setDraftOrderId(null);
        }
      }
    } catch (_) {
      // Offline: start with an empty selection, saving will retry later.
    } finally {
      _booting = false;
      state = state.copyWith(ready: true);
    }
  }

  void setQty(SelectedItem item, int qty) {
    final next = state.items.map((i) => i.copy()).toList();
    final index = next.indexWhere((i) => i.itemId == item.itemId);
    if (qty <= 0) {
      if (index < 0) return;
      next.removeAt(index);
    } else if (index >= 0) {
      if (next[index].repeatCount == qty && next[index].countryId == item.countryId) return;
      next[index].repeatCount = qty;
      next[index].countryId = item.countryId;
    } else {
      next.add(item.copy()..repeatCount = qty);
    }
    _apply(next);
  }

  void step(SelectedItem item, int delta) => setQty(item, state.count(item.itemId) + delta);

  void remove(String itemId) => _apply(state.items.where((i) => i.itemId != itemId).map((i) => i.copy()).toList());

  void clear() => _apply([]);

  /// Saves the edited shopping list for the dishes chosen now.
  void setLines(List<ShoppingLine> lines, {bool sync = true}) {
    state = state.copyWith(lines: lines.map((l) => l.copyWith()).toList(), linesFor: CartState.fingerprint(state.items));
    if (!sync) return;
    _linesDirty = true;
    _queue(immediate: true);
  }

  /// "حفظ الطلب": stores a confirmed order as its own record and clears the selection.
  Future<String> confirm() async {
    if (state.items.isEmpty) throw Exception('اختر أكلة واحدة على الأقل');
    _timer?.cancel();
    final repo = ref.read(ordersRepositoryProvider);
    final day = _today;
    final date = '${day}T00:00:00';
    final draftId = state.orderId;
    final lines = state.lines.where((l) => l.quantity > 0).toList();
    state = state.copyWith(syncing: true);
    try {
      final id = await repo.saveOrder(name: 'طلب شراء $day', date: date, items: state.items, lines: lines, confirm: true);
      if (draftId != null && draftId != id) {
        try {
          await repo.saveOrder(id: draftId, name: draftOrderName, date: date, items: const [], lines: const []);
        } catch (_) {}
      }
      _rev++;
      _linesDirty = false;
      await ref.read(sessionStorageProvider).setDraftOrderId(null);
      state = const CartState(ready: true);
      return id;
    } finally {
      if (state.syncing) state = state.copyWith(syncing: false);
    }
  }

  /// "إعادة الطلب": adds an old order's dishes and ingredients to the selection.
  Future<void> mergeOrder(String orderId) async {
    final repo = ref.read(ordersRepositoryProvider);
    final current = state.items.map((i) => i.copy()).toList();
    final currentLines = state.linesMatchItems ? state.lines.map((l) => l.copyWith()).toList() : <ShoppingLine>[];
    final old = await repo.order(orderId);
    final calc = current.isEmpty ? <ShoppingLine>[] : await repo.calculate(current);

    final merged = <String, ShoppingLine>{};
    for (final l in (currentLines.isNotEmpty ? currentLines : calc)) {
      if (l.quantity > 0) merged[l.key] = l.copyWith();
    }
    final full = {for (final l in calc) l.key: l};
    for (final l in old.lines) {
      if (l.quantity <= 0) continue;
      final mine = merged[l.key];
      if (mine != null) {
        mine.quantity = ((mine.quantity + l.quantity) * 100).round() / 100;
      } else {
        merged[l.key] = l.copyWith(quantity: (((full[l.key]?.quantity ?? 0) + l.quantity) * 100).round() / 100);
      }
    }
    final items = [...current];
    for (final o in old.items) {
      if (items.any((i) => i.itemId == o.itemId)) continue;
      items.add(o.copy()..repeatCount = o.repeatCount > 0 ? o.repeatCount : 1);
    }
    _rev++;
    state = state.copyWith(items: items, lines: merged.values.toList(), linesFor: CartState.fingerprint(items));
    _linesDirty = true;
    _queue(immediate: true);
    try {
      await repo.markReordered(orderId);
    } catch (_) {}
  }

  /// A dish's ingredients changed (optional pick or part): the server recalculates طلباتي.
  void recipeChanged(String itemId) {
    if (state.count(itemId) <= 0) return;
    _linesDirty = false;
    state = state.copyWith(lines: const [], linesFor: '');
    _queue(immediate: true);
  }

  void _apply(List<SelectedItem> items) {
    _rev++;
    state = state.copyWith(items: items);
    _queue();
  }

  void _queue({bool immediate = false}) {
    _timer?.cancel();
    if (immediate) {
      _push();
    } else {
      _timer = Timer(const Duration(milliseconds: 400), _push);
    }
  }

  Future<void> _push() async {
    if (_saving) {
      _again = true;
      return;
    }
    _saving = true;
    state = state.copyWith(syncing: true);
    final repo = ref.read(ordersRepositoryProvider);
    final session = ref.read(sessionStorageProvider);
    try {
      do {
        _again = false;
        final items = state.items.map((i) => i.copy()).toList();
        final id = state.orderId;
        if (items.isEmpty && id == null) break;
        final keepLines = _linesDirty && state.linesMatchItems;
        final lines = keepLines ? state.lines.where((l) => l.quantity > 0).toList() : <ShoppingLine>[];
        final savedId = await repo.saveOrder(id: id, name: draftOrderName, date: '${_today}T00:00:00', items: items, lines: lines);
        _linesDirty = false;
        if (items.isEmpty) {
          // An empty working order is removed on the server.
          state = state.copyWith(orderId: () => null);
          await session.setDraftOrderId(null);
        } else if (savedId.isNotEmpty) {
          state = state.copyWith(orderId: () => savedId);
          await session.setDraftOrderId(savedId);
        }
      } while (_again);
    } catch (_) {
      // Keep the local selection; the next change retries.
    } finally {
      _saving = false;
      state = state.copyWith(syncing: false);
    }
  }
}
