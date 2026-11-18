import 'package:material_ui/material_ui.dart';

import '../../../core/ui/fa_kit.dart';

/// Shared look of the guest screens (login, register, family step):
/// curved orange header with the logo, a floating white card, then a link.
class AuthScaffold extends StatelessWidget {
  const AuthScaffold({super.key, required this.title, required this.subtitle, required this.child, this.footer, this.step});
  final String title;
  final String subtitle;
  final Widget child;
  final Widget? footer;

  /// 1 or 2 shows the register steps above the card content.
  final int? step;

  static const ink = Color(0xFF3D2B22);
  static const muted = Color(0xFF8D7366);
  static const line = Color(0xFFF1DFCB);

  @override
  Widget build(BuildContext context) {
    final top = MediaQuery.paddingOf(context).top;
    return Scaffold(
      backgroundColor: const Color(0xFFFDF6EE),
      body: Directionality(
        textDirection: TextDirection.rtl,
        child: SingleChildScrollView(
          padding: EdgeInsets.only(bottom: 32 + MediaQuery.paddingOf(context).bottom),
          child: Column(
            children: [
              ClipPath(
                clipper: _CurveClipper(),
                child: Container(
                  height: 250 + top,
                  width: double.infinity,
                  decoration: const BoxDecoration(
                    gradient: LinearGradient(begin: Alignment.topRight, end: Alignment.bottomLeft, colors: [Color(0xFFFFA23A), Color(0xFFF36202)]),
                  ),
                  child: Stack(
                    children: [
                      Positioned(left: -40, top: -30 + top, child: _bubble(140)),
                      Positioned(right: -30, top: 120 + top, child: _bubble(90)),
                      Positioned(left: 70, bottom: 40, child: _bubble(36)),
                      Positioned.fill(
                        top: top + 18,
                        child: Column(
                          children: [
                            Container(
                              width: 96,
                              height: 96,
                              padding: const EdgeInsets.all(10),
                              decoration: BoxDecoration(
                                color: Colors.white,
                                shape: BoxShape.circle,
                                boxShadow: [BoxShadow(color: const Color(0x33000000), blurRadius: 18, offset: const Offset(0, 8))],
                              ),
                              child: Image.asset('assets/screens/shared/logo-transparent.png', fit: BoxFit.contain),
                            ),
                            const SizedBox(height: 12),
                            Text(
                              title,
                              style: Fa.text(22, weight: FontWeight.w800, color: Colors.white),
                            ),
                            const SizedBox(height: 4),
                            Padding(
                              padding: const EdgeInsets.symmetric(horizontal: 24),
                              child: Text(
                                subtitle,
                                textAlign: TextAlign.center,
                                style: Fa.text(13, weight: FontWeight.w500, color: const Color(0xEEFFFFFF)),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              Transform.translate(
                offset: const Offset(0, -34),
                child: Center(
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 460),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 18),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Container(
                            padding: const EdgeInsets.fromLTRB(18, 20, 18, 20),
                            decoration: BoxDecoration(
                              color: Colors.white,
                              borderRadius: BorderRadius.circular(26),
                              boxShadow: const [BoxShadow(color: Color(0x1FB8641E), offset: Offset(0, 14), blurRadius: 32)],
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.stretch,
                              children: [
                                if (step != null) ...[AuthSteps(step: step!), const SizedBox(height: 18)],
                                child,
                              ],
                            ),
                          ),
                          if (footer != null) ...[const SizedBox(height: 18), footer!],
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  static Widget _bubble(double size) => Container(
    width: size,
    height: size,
    decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0x1FFFFFFF)),
  );
}

class _CurveClipper extends CustomClipper<Path> {
  @override
  Path getClip(Size size) => Path()
    ..lineTo(0, size.height - 46)
    ..quadraticBezierTo(size.width / 2, size.height + 22, size.width, size.height - 46)
    ..lineTo(size.width, 0)
    ..close();

  @override
  bool shouldReclip(covariant CustomClipper<Path> oldClipper) => false;
}

/// "1 البيانات الأساسية —— 2 أفراد الأسرة".
class AuthSteps extends StatelessWidget {
  const AuthSteps({super.key, required this.step});
  final int step;

  @override
  Widget build(BuildContext context) {
    Widget dot(int n, String label) {
      final done = step > n;
      final on = step >= n;
      return Column(
        children: [
          Container(
            width: 32,
            height: 32,
            alignment: Alignment.center,
            decoration: BoxDecoration(shape: BoxShape.circle, gradient: on ? Fa.ctaGradient : null, color: on ? null : const Color(0xFFF4ECE3)),
            child: done
                ? const FaIcon('check', size: 16, color: Colors.white, weight: 3)
                : Text(
                    '$n',
                    style: Fa.text(13, weight: FontWeight.w800, color: on ? Colors.white : AuthScaffold.muted, height: 1),
                  ),
          ),
          const SizedBox(height: 6),
          Text(
            label,
            style: Fa.text(11, weight: FontWeight.w700, color: on ? AuthScaffold.ink : AuthScaffold.muted),
          ),
        ],
      );
    }

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(child: dot(1, 'البيانات الأساسية')),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.only(top: 15),
            child: Container(
              height: 3,
              decoration: BoxDecoration(color: step > 1 ? Fa.orange : const Color(0xFFF1E4D6), borderRadius: BorderRadius.circular(2)),
            ),
          ),
        ),
        Expanded(child: dot(2, 'أفراد الأسرة')),
      ],
    );
  }
}

/// Label + rounded field with a leading icon.
class AuthField extends StatelessWidget {
  const AuthField({
    super.key,
    required this.label,
    required this.icon,
    this.controller,
    this.hint,
    this.obscure = false,
    this.trailing,
    this.validator,
    this.onSubmitted,
    this.action = TextInputAction.next,
    this.autofill,
  });
  final String label;
  final String icon;
  final TextEditingController? controller;
  final String? hint;
  final bool obscure;
  final Widget? trailing;
  final FormFieldValidator<String>? validator;
  final ValueChanged<String>? onSubmitted;
  final TextInputAction action;
  final Iterable<String>? autofill;

  static InputDecoration decoration({String? hint, required String icon, Widget? trailing}) {
    final b = OutlineInputBorder(
      borderRadius: BorderRadius.circular(16),
      borderSide: const BorderSide(color: AuthScaffold.line),
    );
    return InputDecoration(
      hintText: hint,
      hintStyle: Fa.text(14, weight: FontWeight.w500, color: const Color(0xFFB3A194)),
      filled: true,
      fillColor: const Color(0xFFFFFAF4),
      isDense: true,
      contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 15),
      prefixIcon: Padding(
        padding: const EdgeInsetsDirectional.only(start: 12, end: 8),
        child: FaIcon(icon, size: 20, color: Fa.orange, weight: 2),
      ),
      prefixIconConstraints: const BoxConstraints(minWidth: 40, minHeight: 20),
      suffixIcon: trailing,
      border: b,
      enabledBorder: b,
      focusedBorder: b.copyWith(borderSide: const BorderSide(color: Fa.orange, width: 1.6)),
      errorBorder: b.copyWith(borderSide: const BorderSide(color: Color(0xFFC0392B))),
      focusedErrorBorder: b.copyWith(borderSide: const BorderSide(color: Color(0xFFC0392B), width: 1.6)),
      errorStyle: Fa.text(11, weight: FontWeight.w600, color: const Color(0xFFC0392B)),
    );
  }

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(
        label,
        style: Fa.text(13, weight: FontWeight.w800, color: AuthScaffold.ink),
      ),
      const SizedBox(height: 6),
      TextFormField(
        controller: controller,
        obscureText: obscure,
        textInputAction: action,
        autofillHints: autofill,
        validator: validator,
        onFieldSubmitted: onSubmitted,
        style: Fa.text(15, weight: FontWeight.w600, color: AuthScaffold.ink),
        decoration: decoration(hint: hint, icon: icon, trailing: trailing),
      ),
    ],
  );
}

/// Full-width orange gradient button.
class AuthPrimaryButton extends StatelessWidget {
  const AuthPrimaryButton({super.key, required this.label, required this.onTap, this.busy = false, this.icon = 'arrowLeft'});
  final String label;
  final VoidCallback? onTap;
  final bool busy;
  final String? icon;

  @override
  Widget build(BuildContext context) => Opacity(
    opacity: busy || onTap == null ? .6 : 1,
    child: Tap(
      onTap: busy ? null : onTap,
      child: Container(
        height: 54,
        alignment: Alignment.center,
        decoration: BoxDecoration(
          gradient: Fa.ctaGradient,
          borderRadius: BorderRadius.circular(27),
          boxShadow: const [BoxShadow(color: Color(0x55F36202), offset: Offset(0, 8), blurRadius: 18)],
        ),
        child: busy
            ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.6, color: Colors.white))
            : Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    label,
                    style: Fa.text(16, weight: FontWeight.w800, color: Colors.white, height: 1),
                  ),
                  if (icon != null) ...[const SizedBox(width: 10), FaIcon(icon!, size: 20, color: Colors.white, weight: 2.6)],
                ],
              ),
      ),
    ),
  );
}

/// "ليس لديك حساب؟ سجّل الآن".
class AuthLink extends StatelessWidget {
  const AuthLink({super.key, required this.text, required this.action, required this.onTap});
  final String text;
  final String action;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Row(
    mainAxisAlignment: MainAxisAlignment.center,
    children: [
      Text(
        text,
        style: Fa.text(14, weight: FontWeight.w600, color: AuthScaffold.muted),
      ),
      const SizedBox(width: 6),
      Tap(
        onTap: onTap,
        child: Text(
          action,
          style: Fa.text(14, weight: FontWeight.w800, color: Fa.orange),
        ),
      ),
    ],
  );
}

class AuthError extends StatelessWidget {
  const AuthError(this.text, {super.key});
  final String text;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
    decoration: BoxDecoration(color: const Color(0xFFFDECEA), borderRadius: BorderRadius.circular(12)),
    child: Text(
      text,
      style: Fa.text(13, weight: FontWeight.w700, color: const Color(0xFFB42318)),
    ),
  );
}

/// Eye button that shows / hides a password.
class AuthEye extends StatelessWidget {
  const AuthEye({super.key, required this.hidden, required this.onTap});
  final bool hidden;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Tap(
    onTap: onTap,
    child: Padding(
      padding: const EdgeInsets.symmetric(horizontal: 12),
      child: FaIcon(hidden ? 'eye' : 'eyeOff', size: 20, color: AuthScaffold.muted, weight: 2),
    ),
  );
}
