import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/ui/fa_kit.dart';
import '../../account/domain/account_models.dart';
import '../../account/presentation/family_sheet.dart';
import '../../account/presentation/settings_page.dart' show familyProvider, familyAvatar;
import '../../../core/widgets/net_image.dart';
import 'auth_controller.dart';
import 'auth_ui.dart';

/// Register, step 2 of 2: add family members, or skip and add them later from Settings.
class FamilyStepPage extends ConsumerWidget {
  const FamilyStepPage({super.key});

  void _finish(BuildContext context, WidgetRef ref) {
    ref.read(authControllerProvider.notifier).finishOnboarding();
    context.go('/home');
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final members = ref.watch(familyProvider).value ?? const <FamilyMember>[];
    return AuthScaffold(
      title: 'أفراد الأسرة',
      subtitle: 'ضيف أسرتك علشان نحسب الكميات صح',
      step: 2,
      footer: Center(
        child: Tap(
          onTap: () => _finish(context, ref),
          child: Padding(
            padding: const EdgeInsets.all(8),
            child: Text(
              'تخطي، هضيفهم بعدين',
              style: Fa.text(14, weight: FontWeight.w800, color: AuthScaffold.muted),
            ),
          ),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (members.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 8),
              child: Column(
                children: [
                  SvgPicture.asset('assets/family/person.svg', width: 72, height: 72),
                  const SizedBox(height: 10),
                  Text(
                    'لسه مفيش أفراد',
                    style: Fa.text(15, weight: FontWeight.w800, color: AuthScaffold.ink),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'ابدأ بنفسك وبعدين باقي الأسرة',
                    textAlign: TextAlign.center,
                    style: Fa.text(13, weight: FontWeight.w500, color: AuthScaffold.muted),
                  ),
                ],
              ),
            )
          else
            for (final m in members)
              Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: Tap(
                  onTap: () => showFamilySheet(context, ref, m),
                  child: Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: const Color(0xFFFFFAF4),
                      borderRadius: BorderRadius.circular(18),
                      border: Border.all(color: AuthScaffold.line),
                    ),
                    child: Row(
                      children: [
                        ClipOval(
                          child: SizedBox(
                            width: 48,
                            height: 48,
                            child: (m.imagePath ?? '').isNotEmpty
                                ? NetImage(m.imagePath, fit: BoxFit.cover)
                                : SvgPicture.asset(familyAvatar(m.relation, m.age)),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                m.name ?? '',
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: Fa.text(15, weight: FontWeight.w800, color: AuthScaffold.ink),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                [if ((m.relation ?? '').isNotEmpty) m.relation!, ageText(m.age)].where((e) => e.isNotEmpty).join(' · '),
                                style: Fa.text(12, weight: FontWeight.w600, color: AuthScaffold.muted),
                              ),
                            ],
                          ),
                        ),
                        const FaIcon('chevronLeft', size: 18, color: AuthScaffold.muted, weight: 2.2),
                      ],
                    ),
                  ),
                ),
              ),
          const SizedBox(height: 6),
          Tap(
            onTap: () => showFamilySheet(context, ref, null),
            child: Container(
              height: 50,
              decoration: BoxDecoration(color: const Color(0xFFEAF4D9), borderRadius: BorderRadius.circular(25)),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const FaIcon('plusCircle', size: 20, color: Color(0xFF2F7D1E), weight: 2.2),
                  const SizedBox(width: 8),
                  Text(
                    'إضافة فرد',
                    style: Fa.text(15, weight: FontWeight.w800, color: const Color(0xFF2F7D1E), height: 1),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 18),
          AuthPrimaryButton(label: 'إكمال', icon: 'check', onTap: members.isEmpty ? null : () => _finish(context, ref)),
        ],
      ),
    );
  }
}
