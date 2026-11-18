import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../../app/widgets/fa_widgets.dart';
import '../../../core/ui/fa_kit.dart';
import '../domain/catalog_models.dart';
import 'catalog_providers.dart';
import 'widgets/paged_items.dart';

/// Dishes of one meal type, or every dish when [mealId] is `all` (meal-type.component.ts).
class MealItemsPage extends ConsumerStatefulWidget {
  const MealItemsPage({super.key, required this.mealId, this.title, this.initialQuery = const ItemQuery()});

  final String mealId;
  final String? title;
  final ItemQuery initialQuery;

  @override
  ConsumerState<MealItemsPage> createState() => _MealItemsPageState();
}

class _MealItemsPageState extends ConsumerState<MealItemsPage> {
  late ItemQuery _query = widget.initialQuery;
  /// A meal opens on the customer's own country's cuisine (once; they can pick another).
  bool _autoPicked = false;

  @override
  void didUpdateWidget(covariant MealItemsPage old) {
    super.didUpdateWidget(old);
    // Same page reused for another cuisine / search (e.g. tapped from home again).
    if (old.initialQuery != widget.initialQuery || old.mealId != widget.mealId) {
      _query = widget.initialQuery;
      _autoPicked = false;
    }
  }

  bool get _isAll => widget.mealId == 'all' || widget.mealId == 'الكل';

  @override
  Widget build(BuildContext context) {
    final filters = (_isAll ? ref.watch(cuisinesProvider) : ref.watch(mealFiltersProvider(widget.mealId))).value ?? const [];
    // A meal, a category or a group opens on the cuisine of the customer's country (once; they can pick another).
    final browsing = widget.initialQuery.categoryId != null || widget.initialQuery.groupId != null;
    if ((!_isAll || browsing) && !_autoPicked && filters.isNotEmpty) {
      _autoPicked = true;
      final mine = filters.where((c) => c.isMine && c.id != null && c.count > 0).firstOrNull;
      if (mine != null && widget.initialQuery.cuisineId == null && _query.cuisineId == null) {
        WidgetsBinding.instance.addPostFrameCallback((_) {
          if (mounted && _query.cuisineId == null) setState(() => _query = _query.copyWith(cuisineId: () => mine.id));
        });
      }
    }
    var title = _isAll ? 'الكل' : (widget.title ?? '');
    if (title.isEmpty) {
      title = ref.watch(mealsProvider).value?.where((m) => m.id == widget.mealId).firstOrNull?.name ?? 'الوجبة';
    }
    final s = context.s;
    return Directionality(
      textDirection: TextDirection.rtl,
      child: SafeArea(
        bottom: false,
        child: PagedItemsView(
          source: _isAll ? const AllItemsSource() : MealItemsSource(widget.mealId),
          query: _query,
          emptyText: 'لا توجد أكلات لهذا المطبخ حالياً',
          header: [
            SliverToBoxAdapter(
              child: FaScreenHeader(
                title: title,
                art: 'assets/screens/art/meal-type.svg',
                titleIcon: 'cloche',
                subtitle: 'اختر الأكلات التي تريد إضافتها إلى خطتك',
              ),
            ),
            SliverToBoxAdapter(
              child: FaSearchBar(
                hint: 'ابحث عن أكلة أو مكوّن أو وصفة...',
                query: _query,
                onChanged: (q) => setState(() => _query = q.copyWith(cuisineId: () => _query.cuisineId)),
              ),
            ),
            if (filters.isNotEmpty)
              SliverToBoxAdapter(
                child: Padding(
                  padding: EdgeInsets.only(top: s(28) - s(4)),
                  child: FaChipsRow(
                    selected: filters.indexWhere((c) => (c.id ?? '') == (_query.cuisineId ?? '')),
                    children: [
                      for (final c in filters)
                        FaChip(
                          label: c.name,
                          badge: c.count,
                          countCircle: true,
                          on: (c.id ?? '') == (_query.cuisineId ?? ''),
                          icon: (c.iconPath ?? '').trim().isNotEmpty && !(c.iconPath!.contains('/'))
                              ? c.iconPath!.trim()
                              : faChipIcon(c.name),
                          image: (c.iconPath ?? '').contains('/') || (c.iconPath ?? '').startsWith('http') ? c.iconPath : null,
                          onTap: () => setState(() => _query = _query.copyWith(cuisineId: () => c.id)),
                        ),
                    ],
                  ),
                ),
              ),
          ],
          topGap: filters.isEmpty ? 28 : 12,
        ),
      ),
    );
  }
}
