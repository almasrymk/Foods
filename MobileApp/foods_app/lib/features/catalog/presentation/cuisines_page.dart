import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../app/widgets/fa_chrome.dart';
import '../../../app/widgets/fa_top_bar.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/ui/fa_kit.dart';
import '../../../core/widgets/async_views.dart';
import '../../../core/widgets/net_image.dart';
import '../domain/catalog_models.dart';
import 'catalog_providers.dart';

/// "اختر المطبخ" (cuisines.component): 3-column tiles, the first one is "الكل".
/// The same screen lists categories (/categories) and groups (/groups).
class CuisinesPage extends ConsumerWidget {
  const CuisinesPage({super.key, this.kind = BrowseKind.cuisine});
  final BrowseKind kind;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final s = context.s;
    final cuisines = ref.watch(kind.provider);
    final tiles = (cuisines.value ?? const <FilterChipData>[]).where((c) => (c.id ?? '').isNotEmpty).toList();
    final total = ref.watch(browseTotalProvider).value ?? tiles.fold<int>(0, (a, c) => a + c.count);
    return Directionality(
      textDirection: TextDirection.rtl,
      child: SafeArea(
        bottom: false,
        child: ListView(
          padding: EdgeInsets.only(bottom: faBottomSpace(context, ref)),
          children: [
            const FaTopBar(),
            SizedBox(
              height: s(270),
              child: Stack(
                clipBehavior: Clip.none,
                children: [
                  Positioned(
                    left: 0,
                    top: s(-30),
                    width: s(356),
                    height: s(292),
                    child: SvgPicture.asset('assets/screens/cuisines/hero.svg', fit: BoxFit.contain),
                  ),
                  Positioned(
                    left: s(832),
                    top: s(44),
                    child: Image.asset('assets/screens/plan/fork.png', width: s(72), height: s(76)),
                  ),
                  Positioned(
                    right: s(131),
                    top: s(80),
                    child: FractionalTranslation(
                      translation: const Offset(0, -.5),
                      child: Text(kind.title, style: Fa.text(s(58), weight: FontWeight.w800)),
                    ),
                  ),
                  Positioned(
                    left: s(560),
                    top: s(108),
                    child: Image.asset('assets/screens/plan/underline.png', width: s(68), height: s(22), fit: BoxFit.fill),
                  ),
                  Positioned(
                    right: s(51),
                    top: s(162),
                    child: FractionalTranslation(
                      translation: const Offset(0, -.5),
                      child: Text(
                        kind.subtitle,
                        style: Fa.text(s(24), weight: FontWeight.w500, color: Fa.body),
                      ),
                    ),
                  ),
                ],
              ),
            ),
            if (cuisines.isLoading && tiles.isEmpty)
              const LoadingView()
            else if (cuisines.hasError && tiles.isEmpty)
              MessageView.error(cuisines.error!, onRetry: () => ref.invalidate(kind.provider))
            else if (tiles.isEmpty)
              Padding(
                padding: EdgeInsets.only(top: s(60)),
                child: Text(
                  kind.empty,
                  textAlign: TextAlign.center,
                  style: Fa.text(s(28), color: const Color(0xFF8A6A4A)),
                ),
              )
            else
              Padding(
                padding: EdgeInsets.only(right: s(36), left: s(35), top: s(10)),
                child: GridView.count(
                  crossAxisCount: 3,
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  padding: EdgeInsets.zero,
                  crossAxisSpacing: s(19),
                  mainAxisSpacing: s(22),
                  childAspectRatio: ((context.faWidth - s(71) - s(38)) / 3) / s(305),
                  children: [
                    _Tile(
                      label: 'الكل',
                      count: total,
                      all: true,
                      onTap: () => context.push('/meals/all?name=${Uri.encodeComponent('الكل')}'),
                    ),
                    for (final c in tiles)
                      _Tile(
                        label: c.name,
                        count: c.count,
                        chip: c,
                        onTap: () => context.push(Uri(path: '/meals/all', queryParameters: {'name': c.name, kind.param: c.id ?? ''}).toString()),
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

String _iconName(String name) {
  if (RegExp('نباتي|نبات|فيجان|vegan').hasMatch(name)) return 'vegan';
  if (RegExp('صحي|سلطة|دايت').hasMatch(name)) return 'leaf';
  if (RegExp('مشوي|كباب|شيش|ستيك|مشويات').hasMatch(name)) return 'grill';
  if (RegExp('غربي|باستا|بيتزا|برجر').hasMatch(name)) return 'chef';
  return 'cloche';
}

class _Tile extends StatelessWidget {
  const _Tile({required this.label, required this.count, required this.onTap, this.chip, this.all = false});
  final String label;
  final int count;
  final VoidCallback onTap;
  final FilterChipData? chip;
  final bool all;

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    final path = (chip?.iconPath ?? '').trim();
    final isImage = path.contains('/') || path.startsWith('http');
    final iconKey = all ? 'cloche' : (!isImage && path.isNotEmpty ? path : _iconName(label));
    final image = chip?.imagePath;
    final hasPhoto = (image ?? '').isNotEmpty;
    final color = all ? Colors.white : (AppColors.parse(chip?.textColor) ?? Fa.ink);
    final iconColor = all ? Fa.orange : color;
    final bg = AppColors.parse(chip?.color) ?? const Color(0xFFFFF3E6);

    Widget smallIcon() => isImage
        ? NetImage(path, width: s(36), height: s(36), fit: BoxFit.contain)
        : FaIcon(all ? 'grid' : iconKey, size: s(28), color: iconColor, weight: all ? 2 : 1.8);

    return Tap(
      onTap: onTap,
      child: Container(
        clipBehavior: Clip.antiAlias,
        // Room under the dish count, so it never touches the bottom edge.
        padding: EdgeInsets.only(top: s(12), bottom: s(18)),
        decoration: BoxDecoration(
          color: all ? null : bg,
          gradient: all ? Fa.orangeV : null,
          borderRadius: BorderRadius.circular(s(28)),
          boxShadow: [Fa.shadow(context, y: 5, blur: 14, color: const Color(0x1A784820))],
        ),
        child: Column(
          children: [
            Container(
              width: s(150),
              height: s(150),
              clipBehavior: Clip.antiAlias,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: hasPhoto || all ? Colors.white : Colors.white.withValues(alpha: .7),
                border: Border.all(color: Colors.white.withValues(alpha: .9), width: s(4)),
                boxShadow: [BoxShadow(color: const Color(0x1A000000), offset: Offset(0, s(5)), blurRadius: s(14))],
              ),
              child: hasPhoto
                  ? NetImage(image, width: s(150), height: s(150))
                  : isImage
                  ? FractionallySizedBox(widthFactor: .72, heightFactor: .72, child: NetImage(path, fit: BoxFit.contain))
                  : FaIcon(iconKey, size: s(84), color: iconColor, weight: all ? 1.6 : 1.5),
            ),
            if (hasPhoto || all)
              Transform.translate(
                offset: Offset(0, s(-22)),
                child: Container(
                  width: s(46),
                  height: s(46),
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: Colors.white,
                    boxShadow: [BoxShadow(color: const Color(0x1F000000), offset: Offset(0, s(2)), blurRadius: s(6))],
                  ),
                  child: smallIcon(),
                ),
              ),
            Transform.translate(
              offset: Offset(0, hasPhoto || all ? s(-22) : 0),
              child: Column(
                children: [
                  SizedBox(height: s(6)),
                  Padding(
                    padding: EdgeInsets.symmetric(horizontal: s(10)),
                    child: Text(
                      label,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Fa.text(s(32), weight: FontWeight.w800, color: color, height: 1.15),
                    ),
                  ),
                  SizedBox(height: s(6)),
                  // The count sits in a small circle, then the word.
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Container(
                        constraints: BoxConstraints(minWidth: s(44)),
                        height: s(44),
                        padding: EdgeInsets.symmetric(horizontal: s(6)),
                        alignment: Alignment.center,
                        decoration: BoxDecoration(color: all ? const Color(0xFFE2640A) : color, borderRadius: BorderRadius.circular(s(22))),
                        child: FittedBox(
                          fit: BoxFit.scaleDown,
                          child: Text('$count', style: Fa.text(s(21), weight: FontWeight.w800, color: Colors.white, height: 1)),
                        ),
                      ),
                      SizedBox(width: s(8)),
                      Text('أكلة', style: Fa.text(s(20), weight: FontWeight.w800, color: all ? Colors.white : color, height: 1)),
                    ],
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

void openCuisine(BuildContext context, FilterChipData c) =>
    context.push('/meals/all?name=${Uri.encodeComponent('الكل')}&cuisineId=${c.id ?? ''}');
