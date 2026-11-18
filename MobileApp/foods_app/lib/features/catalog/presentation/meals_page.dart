import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../app/widgets/fa_chrome.dart';
import '../../../app/widgets/fa_top_bar.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/ui/fa_kit.dart';
import '../../../core/widgets/async_views.dart';
import '../../../core/widgets/net_image.dart';
import 'catalog_providers.dart';

const _fallbacks = [
  ('assets/screens/plan/breakfast.png', Color(0xFF3A2210)),
  ('assets/screens/plan/lunch.png', Color(0xFF3A2210)),
  ('assets/screens/plan/dinner.png', Color(0xFF5B2A86)),
  ('assets/screens/plan/snacks.png', Color(0xFF1E2A1E)),
  ('assets/screens/plan/drinks.png', Color(0xFF1E2A1E)),
  ('assets/screens/plan/healthy.png', Color(0xFF1E2A1E)),
];

/// "اختر نوع الوجبة" (plan step 1): positions are the web mockup's (941px wide).
class MealsPage extends ConsumerWidget {
  const MealsPage({super.key});

  static const _cols = [627.0, 332.0, 35.0];

  double _top(int row) => row == 0 ? 417 : (row == 1 ? 724 : 1008 + (row - 2) * 284.0);
  double _height(int row) => row == 0 ? 283 : 272;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final s = context.s;
    final meals = ref.watch(mealsProvider);
    final rows = meals.value ?? const [];
    final lastRow = rows.isEmpty ? 0 : (rows.length - 1) ~/ 3;
    final canvas = rows.isEmpty ? 1100.0 : _top(lastRow) + _height(lastRow) + 40;
    final top = context.u(128);
    return Directionality(
      textDirection: TextDirection.rtl,
      child: SafeArea(
        bottom: false,
        child: ListView(
          padding: EdgeInsets.only(bottom: faBottomSpace(context, ref)),
          children: [
            SizedBox(
              height: top - s(143) + s(canvas),
              child: Stack(
                clipBehavior: Clip.none,
                children: [
                  Positioned(
                    left: 0,
                    top: top - s(143) + s(110),
                    width: s(356),
                    height: s(292),
                    child: EdgeFade(
                      right: .22,
                      top: .08,
                      bottom: .15,
                      child: Image.asset('assets/screens/plan/calendar.png', fit: BoxFit.fill),
                    ),
                  ),
                  const Positioned(left: 0, right: 0, top: 0, child: FaTopBar()),
                  Positioned(
                    left: s(832),
                    top: top - s(143) + s(184),
                    child: Image.asset('assets/screens/plan/fork.png', width: s(72), height: s(76)),
                  ),
                  Positioned(
                    right: s(131),
                    top: top - s(143) + s(220),
                    child: FractionalTranslation(
                      translation: const Offset(0, -.5),
                      child: Text('اختار الوجبة', style: Fa.text(s(58), weight: FontWeight.w800)),
                    ),
                  ),
                  Positioned(
                    left: s(532),
                    top: top - s(143) + s(248),
                    child: Image.asset('assets/screens/plan/underline.png', width: s(68), height: s(22), fit: BoxFit.fill),
                  ),
                  Positioned(
                    right: s(51),
                    left: s(320),
                    top: top - s(143) + s(302),
                    child: FractionalTranslation(
                      translation: const Offset(0, -.5),
                      child: Align(
                        alignment: AlignmentDirectional.centerStart,
                        child: FittedBox(
                          fit: BoxFit.scaleDown,
                          child: Text(
                            'ابدأ بتحديد الوجبات التي تريد إضافتها إلى خطة الشهر',
                            maxLines: 1,
                            style: Fa.text(s(24), weight: FontWeight.w500, color: Fa.body),
                          ),
                        ),
                      ),
                    ),
                  ),
                  if (meals.isLoading && rows.isEmpty)
                    Positioned(
                      left: 0,
                      right: 0,
                      top: top + s(300),
                      child: const LoadingView(padding: EdgeInsets.zero),
                    ),
                  if (meals.hasError && rows.isEmpty)
                    Positioned(
                      left: 0,
                      right: 0,
                      top: top + s(300),
                      child: MessageView.error(meals.error!, onRetry: () => ref.invalidate(mealsProvider)),
                    ),
                  for (var i = 0; i < rows.length; i++)
                    Positioned(
                      left: s(_cols[i % 3]),
                      top: top - s(143) + s(_top(i ~/ 3)),
                      width: s(278),
                      height: s(_height(i ~/ 3)),
                      child: _Tile(
                        index: i,
                        id: rows[i].id,
                        name: rows[i].name ?? '',
                        image: rows[i].imagePath,
                        icon: rows[i].iconPath,
                        color: rows[i].color,
                        textColor: rows[i].textColor,
                        count: rows[i].count,
                      ),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Tile extends StatelessWidget {
  const _Tile({required this.index, required this.id, required this.name, this.image, this.icon, this.color, this.textColor, this.count = 0});
  final int index, count;
  final String id, name;
  final String? image, icon, color, textColor;

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    final fb = _fallbacks[index % _fallbacks.length];
    final bg = (color ?? '').trim().isEmpty ? Fa.card : (AppColors.parse(color) ?? Fa.card);
    return Tap(
      onTap: () => context.push('/meals/$id?name=${Uri.encodeComponent(name)}'),
      child: Container(
        clipBehavior: Clip.antiAlias,
        padding: EdgeInsets.only(top: s(10)),
        decoration: BoxDecoration(
          color: bg,
          borderRadius: BorderRadius.circular(s(28)),
          boxShadow: [Fa.shadow(context, y: 5, blur: 14, color: const Color(0x1A784820))],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if ((icon ?? '').isNotEmpty)
              Center(
                child: NetImage(icon, width: s(48), height: s(48), fit: BoxFit.contain),
              ),
            Text(
              name,
              textAlign: TextAlign.center,
              maxLines: 1,
              style: Fa.text(s(34), weight: FontWeight.w800, color: AppColors.parse(textColor) ?? fb.$2, height: 1.1),
            ),
            Text(
              '$count',
              textAlign: TextAlign.center,
              style: Fa.text(s(24), weight: FontWeight.w800, color: (AppColors.parse(textColor) ?? fb.$2).withValues(alpha: .75), height: 1.2),
            ),
            Expanded(child: NetImage(image, fallbackAsset: fb.$1)),
          ],
        ),
      ),
    );
  }
}
