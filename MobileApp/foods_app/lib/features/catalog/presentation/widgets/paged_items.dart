import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../../../app/providers.dart';
import '../../../../core/widgets/async_views.dart';
import '../../domain/catalog_models.dart';
import '../../../../app/widgets/fa_chrome.dart';
import '../../../../app/widgets/fa_widgets.dart';
import '../../../../core/ui/fa_kit.dart';
import '../../../prices/prices.dart';

/// Pages through dishes from [source] with infinite scroll.
/// [header] slivers scroll together with the list.
class PagedItemsView extends ConsumerStatefulWidget {
  const PagedItemsView({
    super.key,
    required this.source,
    required this.query,
    this.header = const [],
    this.emptyText = 'مفيش أكلات مطابقة',
    this.pageSize = 20,
    this.redHeart = false,
    this.topGap = 28,
  });

  final bool redHeart;
  final double topGap;

  final ItemSource source;
  final ItemQuery query;
  final List<Widget> header;
  final String emptyText;
  final int pageSize;

  @override
  ConsumerState<PagedItemsView> createState() => _PagedItemsViewState();
}

class _PagedItemsViewState extends ConsumerState<PagedItemsView> {
  final _items = <ItemCard>[];
  bool _loading = false;
  bool _hasMore = true;
  Object? _error;
  int _token = 0;

  @override
  void initState() {
    super.initState();
    _reload();
  }

  @override
  void didUpdateWidget(covariant PagedItemsView old) {
    super.didUpdateWidget(old);
    if (old.query != widget.query || old.source != widget.source) _reload();
  }

  Future<void> _reload() async {
    _token++;
    setState(() {
      _items.clear();
      _hasMore = true;
      _error = null;
      _loading = false;
    });
    await _more();
  }

  Future<void> _more() async {
    if (_loading || !_hasMore) return;
    final token = _token;
    setState(() => _loading = true);
    try {
      final page = await ref.read(catalogRepositoryProvider).items(widget.source, widget.query, skip: _items.length, take: widget.pageSize);
      if (!mounted || token != _token) return;
      setState(() {
        final known = _items.map((e) => e.id).toSet();
        _items.addAll(page.where((e) => !known.contains(e.id)));
        _hasMore = page.length >= widget.pageSize;
      });
    } catch (e) {
      if (!mounted || token != _token) return;
      setState(() => _error = e);
    } finally {
      if (mounted && token == _token) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    ref.listen(pricesVersionProvider, (_, _) => _reload());
    return RefreshIndicator(
      color: Fa.orange,
      onRefresh: _reload,
      child: NotificationListener<ScrollNotification>(
        onNotification: (n) {
          if (n.metrics.pixels > n.metrics.maxScrollExtent - 400) _more();
          return false;
        },
        child: CustomScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            ...widget.header,
            SliverPadding(
              padding: EdgeInsets.fromLTRB(context.s(28), context.s(widget.topGap), context.s(24), 0),
              sliver: SliverList.separated(
                itemCount: _items.length,
                separatorBuilder: (_, _) => SizedBox(height: context.s(14)),
                itemBuilder: (_, i) => FaFoodRow(item: _items[i], redHeart: widget.redHeart),
              ),
            ),
            SliverToBoxAdapter(
              child: _error != null && _items.isEmpty
                  ? MessageView.error(_error!, onRetry: _reload)
                  : _loading
                  ? const FaNote('جاري التحميل...')
                  : _items.isEmpty
                  ? FaNote(widget.emptyText)
                  : const SizedBox.shrink(),
            ),
            SliverToBoxAdapter(child: SizedBox(height: faBottomSpace(context, ref))),
          ],
        ),
      ),
    );
  }
}
