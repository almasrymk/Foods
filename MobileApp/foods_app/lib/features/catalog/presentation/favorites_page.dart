import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../../app/widgets/fa_widgets.dart';
import '../../../core/ui/fa_kit.dart';
import '../domain/catalog_models.dart';
import 'catalog_providers.dart';
import 'widgets/paged_items.dart';

/// "أكلاتي المفضلة" (favorites.component.ts).
class FavoritesPage extends ConsumerStatefulWidget {
  const FavoritesPage({super.key});
  @override
  ConsumerState<FavoritesPage> createState() => _FavoritesPageState();
}

class _FavoritesPageState extends ConsumerState<FavoritesPage> {
  ItemQuery _query = const ItemQuery();

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    final filters = ref.watch(favoriteFiltersProvider).value ?? const <FilterChipData>[];
    return Directionality(
      textDirection: TextDirection.rtl,
      child: SafeArea(
        bottom: false,
        child: PagedItemsView(
          source: const FavoritesSource(),
          query: _query,
          emptyText: 'لسه مفيش أكلات في المفضلة',
          topGap: filters.isEmpty ? 33 : 17,
          header: [
            const SliverToBoxAdapter(
              child: FaScreenHeader(
                title: 'أكلاتي المفضلة',
                art: 'assets/screens/art/favorites.svg',
                subtitle: 'قائمة الأكلات المحفوظة لديك',
                heart: true,
              ),
            ),
            SliverToBoxAdapter(
              child: Padding(
                padding: EdgeInsets.only(top: s(10)),
                child: FaSearchBar(
                  hint: 'ابحث في المفضلة ...',
                  query: _query,
                  onChanged: (q) => setState(() => _query = q.copyWith(cuisineId: () => _query.cuisineId)),
                ),
              ),
            ),
            if (filters.isNotEmpty)
              SliverToBoxAdapter(
                child: Padding(
                  padding: EdgeInsets.only(top: s(24)),
                  child: FaChipsRow(
                    selected: filters.indexWhere((c) => (c.id ?? '') == (_query.cuisineId ?? '')),
                    children: [
                      for (final c in filters)
                        FaChip(
                          label: c.name,
                          badge: c.count,
                          countCircle: true,
                          on: (c.id ?? '') == (_query.cuisineId ?? ''),
                          fontSize: 22,
                          pad: (c.id ?? '') == (_query.cuisineId ?? '') ? 26 : 16,
                          icon: (c.iconPath ?? '').trim().isNotEmpty && !c.iconPath!.contains('/')
                              ? c.iconPath!.trim()
                              : faChipIcon(c.name),
                          image: (c.iconPath ?? '').contains('/') ? c.iconPath : null,
                          onTap: () => setState(() => _query = _query.copyWith(cuisineId: () => c.id)),
                        ),
                    ],
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
