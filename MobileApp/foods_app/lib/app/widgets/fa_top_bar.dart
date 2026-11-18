import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/ui/fa_kit.dart';
import '../../core/widgets/net_image.dart';
import '../../features/auth/presentation/auth_controller.dart';
import '../../features/notifications/presentation/notifications_providers.dart';

/// The bar at the top of every screen: avatar, logo, notifications (top-bar.component.ts).
/// Positions are in 841px mockup units, measured from the physical left like the CSS.
class FaTopBar extends ConsumerWidget {
  const FaTopBar({super.key, this.height = 128});
  final double height;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final u = context.u;
    return SizedBox(
      height: u(height),
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Positioned(left: u(29), top: u(29), child: const FaAvatarButton()),
          Positioned(
            left: u(100),
            top: u(34),
            child: Container(
              width: u(12),
              height: u(12),
              decoration: const BoxDecoration(shape: BoxShape.circle, color: Fa.orange),
            ),
          ),
          Positioned(
            left: u(367),
            top: u(8),
            width: u(130),
            height: u(112),
            child: Tap(
              onTap: () => context.go('/home'),
              child: Image.asset('assets/screens/shared/logo-transparent.png', fit: BoxFit.contain),
            ),
          ),
          ...faBell(context, ref),
        ],
      ),
    );
  }
}

/// Bell and its unread badge, positioned like the web top bar.
List<Widget> faBell(BuildContext context, WidgetRef ref) {
  final u = context.u;
  final count = ref.watch(unreadCountProvider).value ?? 0;
  return [
    Positioned(
      left: u(740),
      top: u(52),
      child: Tap(
        onTap: () => context.push('/notifications'),
        child: FaPath(FaPaths.bell, size: u(56)),
      ),
    ),
    if (count > 0)
      Positioned(
        left: u(777),
        top: u(45),
        child: IgnorePointer(
          child: Container(
            width: u(27),
            height: u(27),
            alignment: Alignment.center,
            decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0xFFE5262A)),
            child: Text(count > 9 ? '9+' : '$count', style: Fa.text(u(18), color: Colors.white, height: 1.1)),
          ),
        ),
      ),
  ];
}

/// The round white profile button (photo or person icon).
class FaAvatarButton extends ConsumerWidget {
  const FaAvatarButton({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final u = context.u;
    final photo = ref.watch(authControllerProvider.select((a) => a.photo)) ?? '';
    return Tap(
      onTap: () => context.go('/settings'),
      child: Container(
        width: u(92),
        height: u(92),
        alignment: Alignment.center,
        clipBehavior: Clip.antiAlias,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          color: Colors.white,
          boxShadow: [BoxShadow(color: const Color(0x14000000), offset: Offset(0, u(4)), blurRadius: u(16))],
        ),
        child: photo.isEmpty
            ? FaPath(FaPaths.person, size: u(62), color: const Color(0xFF4A2A12))
            : NetImage(photo, width: u(92), height: u(92)),
      ),
    );
  }
}
