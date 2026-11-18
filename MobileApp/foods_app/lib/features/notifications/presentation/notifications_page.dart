import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart' hide TextDirection;
import 'package:material_ui/material_ui.dart';

import '../../../app/providers.dart';
import '../../../app/widgets/fa_chrome.dart';
import '../../../app/widgets/fa_widgets.dart';
import '../../../core/ui/fa_kit.dart';
import '../../../core/widgets/net_image.dart';
import '../../../core/widgets/async_views.dart';
import '../domain/notification_models.dart';
import 'notifications_providers.dart';

class NotificationsPage extends ConsumerStatefulWidget {
  const NotificationsPage({super.key});
  @override
  ConsumerState<NotificationsPage> createState() => _NotificationsPageState();
}

class _NotificationsPageState extends ConsumerState<NotificationsPage> {
  final _items = <AppNotification>[];
  bool? _unreadOnly;
  bool _loading = false;
  bool _hasMore = true;
  int _unread = 0;
  Object? _error;
  int _token = 0;

  @override
  void initState() {
    super.initState();
    _reload();
  }

  Future<void> _reload() async {
    _token++;
    setState(() {
      _items.clear();
      _hasMore = true;
      _loading = false;
      _error = null;
    });
    await _more();
  }

  Future<void> _more() async {
    if (_loading || !_hasMore) return;
    final token = _token;
    setState(() => _loading = true);
    try {
      final page = await ref.read(notificationsRepositoryProvider).page(skip: _items.length, unread: _unreadOnly);
      if (!mounted || token != _token) return;
      setState(() {
        _items.addAll(page.items);
        _unread = page.unread;
        _hasMore = _items.length < page.total && page.items.isNotEmpty;
      });
    } catch (e) {
      if (mounted && token == _token) setState(() => _error = e);
    } finally {
      if (mounted && token == _token) setState(() => _loading = false);
    }
  }

  Future<void> _open(AppNotification n) async {
    if (!n.isRead) {
      try {
        _unread = await ref.read(notificationsRepositoryProvider).markRead(n.id);
        setState(() => n.readAt = DateTime.now().toIso8601String());
        ref.invalidate(unreadCountProvider);
      } catch (_) {}
    }
    final link = n.link ?? '';
    if (link.startsWith('/') && mounted) context.push(link);
  }

  Future<void> _readAll() async {
    try {
      await ref.read(notificationsRepositoryProvider).markAllRead();
      ref.invalidate(unreadCountProvider);
      _reload();
    } catch (e) {
      if (mounted) showMessage(context, e, error: true);
    }
  }

  static const _types = {
    'login_reminder': ('bell', Color(0xFFF37202), Color(0xFFFEEBD6)),
    'welcome_back': ('home', Color(0xFF2F7D1E), Color(0xFFE3F1DB)),
    'birthday': ('cake', Color(0xFFC2416B), Color(0xFFFCE1EA)),
    'order_reminder': ('receipt', Color(0xFF6B4AA8), Color(0xFFECE4F7)),
    'weekly_plan': ('calendar', Color(0xFF1F6FB2), Color(0xFFE1EEF9)),
  };

  /// Relative Arabic time like the web: "الآن", "منذ 5 دقائق", "أمس"...
  static String _ago(String iso) {
    final hasZone = iso.endsWith('Z') || RegExp(r'[+-]\d\d:\d\d$').hasMatch(iso);
    final t = DateTime.tryParse(hasZone ? iso : '${iso}Z');
    if (t == null) return '';
    final sec = DateTime.now().difference(t).inSeconds.clamp(0, 1 << 31);
    final min = (sec / 60).round(), hr = (sec / 3600).round(), day = (sec / 86400).round();
    String unit(int n, String one, String two, String few, String many) =>
        n == 1 ? one : (n == 2 ? two : (n <= 10 ? '$n $few' : '$n $many'));
    if (sec < 60) return 'الآن';
    if (min < 60) return 'منذ ${unit(min, 'دقيقة', 'دقيقتين', 'دقائق', 'دقيقة')}';
    if (hr < 24) return 'منذ ${unit(hr, 'ساعة', 'ساعتين', 'ساعات', 'ساعة')}';
    if (day == 1) return 'أمس';
    if (day < 7) return 'منذ ${unit(day, 'يوم', 'يومين', 'أيام', 'يوم')}';
    if (day < 30) return 'منذ ${unit((day / 7).round(), 'أسبوع', 'أسبوعين', 'أسابيع', 'أسبوع')}';
    return DateFormat('d/M/y').format(t.toLocal());
  }

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    final tab = _unreadOnly == null ? 0 : (_unreadOnly! ? 1 : 2);
    void select(bool? v) {
      if (v == _unreadOnly) return;
      _unreadOnly = v;
      _reload();
    }

    Widget item(AppNotification n) {
      final k = _types[n.type] ?? _types['login_reminder']!;
      final unread = !n.isRead;
      return Tap(
        onTap: () => _open(n),
        child: Container(
          padding: EdgeInsets.symmetric(vertical: s(22), horizontal: s(24)),
          decoration: BoxDecoration(
            color: unread ? const Color(0xFFFFF4E6) : const Color(0xFFFFFCF8),
            borderRadius: BorderRadius.circular(s(24)),
            border: Border.all(color: unread ? const Color(0xFFF9CF9F) : const Color(0xFFF2E6D8), width: s(1.5)),
            boxShadow: [
              unread
                  ? BoxShadow(color: const Color(0x1FF37202), offset: Offset(0, s(5)), blurRadius: s(16))
                  : BoxShadow(color: const Color(0x12784820), offset: Offset(0, s(4)), blurRadius: s(14)),
            ],
          ),
          child: Stack(
            clipBehavior: Clip.none,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if ((n.image ?? '').isNotEmpty)
                    Container(
                      width: s(78),
                      height: s(78),
                      clipBehavior: Clip.antiAlias,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        color: Colors.white,
                        border: Border.all(color: Colors.white, width: s(3)),
                        boxShadow: [BoxShadow(color: const Color(0x26784820), offset: Offset(0, s(2)), blurRadius: s(6))],
                      ),
                      child: NetImage(n.image, width: s(78), height: s(78)),
                    )
                  else
                    Container(
                      width: s(78),
                      height: s(78),
                      alignment: Alignment.center,
                      decoration: BoxDecoration(shape: BoxShape.circle, color: k.$3),
                      child: FaIcon(k.$1, size: s(40), color: k.$2, filled: k.$1 == 'bell'),
                    ),
                  SizedBox(width: s(22)),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          n.title ?? '',
                          style: Fa.text(
                            s(29),
                            weight: unread ? FontWeight.w800 : FontWeight.w700,
                            color: unread ? const Color(0xFF2E1E12) : const Color(0xFF4A3A2E),
                            height: 1.3,
                          ),
                        ),
                        if ((n.body ?? '').isNotEmpty) ...[
                          SizedBox(height: s(6)),
                          Text(
                            n.body!,
                            style: Fa.text(s(24), weight: FontWeight.w400, color: const Color(0xFF5E5148), height: 1.45),
                          ),
                        ],
                        SizedBox(height: s(6)),
                        Text(
                          _ago(n.createdAt),
                          style: Fa.text(s(20), weight: FontWeight.w400, color: const Color(0xFF9A8B7E)),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              if (unread)
                Positioned(
                  left: 0,
                  top: s(4),
                  child: Container(
                    width: s(16),
                    height: s(16),
                    decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0xFFF37202)),
                  ),
                ),
            ],
          ),
        ),
      );
    }

    return Directionality(
      textDirection: TextDirection.rtl,
      child: ColoredBox(
        color: Fa.page,
        child: SafeArea(
          bottom: false,
          child: RefreshIndicator(
            onRefresh: _reload,
            child: NotificationListener<ScrollNotification>(
              onNotification: (n) {
                if (n.metrics.pixels > n.metrics.maxScrollExtent - 300) _more();
                return false;
              },
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: EdgeInsets.only(bottom: faBottomSpace(context, ref)),
                children: [
                  const FaScreenHeader(
                    title: 'الإشعارات',
                    titleIcon: 'bell',
                    subtitle: 'تذكيرات وتهاني وأخبار طلباتك في مكان واحد',
                    height: 210,
                    titleY: 157,
                    backY: 120,
                  ),
                  Padding(
                    padding: EdgeInsets.fromLTRB(s(36), s(48), s(36), 0),
                    child: Row(
                      children: [
                        FaChip(label: 'الكل', on: tab == 0, onTap: () => select(null), height: 60),
                        SizedBox(width: s(14)),
                        FaChip(
                          label: 'غير مقروءة',
                          on: tab == 1,
                          onTap: () => select(true),
                          height: 60,
                          badge: _unread > 0 ? _unread : null,
                        ),
                        SizedBox(width: s(14)),
                        FaChip(label: 'مقروءة', on: tab == 2, onTap: () => select(false), height: 60),
                        const Spacer(),
                        Opacity(
                          opacity: _unread == 0 ? .4 : 1,
                          child: Tap(
                            onTap: _unread == 0 ? null : _readAll,
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                FaIcon('check', size: s(28), color: const Color(0xFFC45A00), weight: 2.4),
                                SizedBox(width: s(8)),
                                Text(
                                  'تحديد الكل كمقروء',
                                  style: Fa.text(s(23), weight: FontWeight.w800, color: const Color(0xFFC45A00)),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  Padding(
                    padding: EdgeInsets.fromLTRB(s(36), s(26), s(36), 0),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        for (final n in _items) ...[item(n), SizedBox(height: s(18))],
                        if (!_loading && _items.isEmpty && _error == null)
                          Padding(
                            padding: EdgeInsets.symmetric(vertical: s(80)),
                            child: Column(
                              children: [
                                FaIcon('bell', size: s(90), color: const Color(0xFFB3A496), filled: true),
                                SizedBox(height: s(14)),
                                Text(
                                  tab == 1 ? 'مفيش إشعارات جديدة' : (tab == 2 ? 'مفيش إشعارات مقروءة' : 'مفيش إشعارات لسه'),
                                  style: Fa.text(s(27), color: const Color(0xFF8A7A6E)),
                                ),
                              ],
                            ),
                          ),
                        if (_loading)
                          Padding(
                            padding: EdgeInsets.all(s(30)),
                            child: Text(
                              'جاري التحميل...',
                              textAlign: TextAlign.center,
                              style: Fa.text(s(24), weight: FontWeight.w400, color: const Color(0xFF8A7A6E)),
                            ),
                          ),
                        if (_error != null)
                          Text(
                            '$_error'.replaceFirst('Exception: ', ''),
                            textAlign: TextAlign.center,
                            style: Fa.text(s(24), weight: FontWeight.w400, color: const Color(0xFFB71C1C)),
                          ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
