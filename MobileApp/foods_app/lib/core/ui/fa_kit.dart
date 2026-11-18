import 'package:flutter_svg/flutter_svg.dart';
import 'package:material_ui/material_ui.dart';

import 'fa_icon_paths.dart';

/// The web screens are drawn on a 941px-wide mockup (`--s`) and the home page and
/// top/bottom bars on an 841px one (`--u`). These helpers turn mockup pixels into
/// device pixels so every screen keeps the web app's proportions.
extension FaScale on BuildContext {
  double get faWidth => MediaQuery.sizeOf(this).width;
  double s(num n) => n * faWidth / 941;
  double u(num n) => n * faWidth / 841;
}

/// Shared colors from styles/screens.css.
class Fa {
  Fa._();
  static const orange = Color(0xFFF37202);
  static const orangeLight = Color(0xFFFF9A1F);
  static const green = Color(0xFF2F7D1E);
  static const greenDark = Color(0xFF3E7B22);
  static const ink = Color(0xFF2E1E12);
  static const title = Color(0xFF3A2412);
  static const muted = Color(0xFF6E625A);
  static const page = Color(0xFFFDF8F3);
  static const card = Color(0xFFFFFCF9);
  static const body = Color(0xFF3F3129);

  static const ctaGradient = LinearGradient(
    colors: [Color(0xFFFF8A12), Color(0xFFF35E02)],
    begin: Alignment.centerLeft,
    end: Alignment.centerRight,
  );
  static const chipGradient = LinearGradient(
    colors: [Color(0xFFFF8A12), Color(0xFFF36202)],
    begin: Alignment.centerLeft,
    end: Alignment.centerRight,
  );
  static const orangeDiag = LinearGradient(
    colors: [Color(0xFFFF9A2E), Color(0xFFF36202)],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );
  static const orangeV = LinearGradient(colors: [orangeLight, orange], begin: Alignment.topCenter, end: Alignment.bottomCenter);
  static const greenRadial = RadialGradient(colors: [Color(0xFF5FA22E), Color(0xFF2F7514)], center: Alignment(0, -0.4), radius: 0.9);
  static const selGreen = LinearGradient(
    colors: [Color(0xFFFBFCF4), Color(0xFFF6FAEA)],
    begin: Alignment.centerLeft,
    end: Alignment.centerRight,
  );

  static BoxShadow shadow(BuildContext c, {double y = 5, double blur = 16, Color color = const Color(0x14784820)}) =>
      BoxShadow(color: color, offset: Offset(0, c.s(y)), blurRadius: c.s(blur));

  /// App-wide text size from the font-size setting (four levels); the app sets it.
  /// Ignores the phone's own font setting.
  static TextScaler textScale = TextScaler.linear(FontLevel.scales[FontLevel.normal]);

  static TextStyle text(double size, {FontWeight weight = FontWeight.w700, Color color = ink, double? height}) => TextStyle(
    fontFamily: 'Tajawal',
    fontSize: size,
    fontWeight: weight,
    color: color,
    height: height,
    // Like CSS line-height: extra space split evenly above and below, so text sits centred.
    leadingDistribution: TextLeadingDistribution.even,
  );
}

/// The four font sizes in Settings → حجم الخط (the default is the third, the size the screens were drawn at).
/// The biggest stays at 1.34 so no screen overflows.
class FontLevel {
  FontLevel._();
  static const normal = 2;
  static const count = 4;
  static const labels = ['أصغر', 'صغير', 'عادي', 'كبير'];
  // Wider steps so each level is clearly different; "عادي" stays 1.25.
  static const scales = [0.9, 1.07, 1.25, 1.4];
  static double scaleOf(int level) => scales[level.clamp(0, scales.length - 1)];
}

String _hex(Color c) => '#${(c.toARGB32() & 0xFFFFFF).toRadixString(16).padLeft(6, '0')}';

/// The web app's line icons (shared/icon.component.ts), drawn with the same SVG paths.
class FaIcon extends StatelessWidget {
  const FaIcon(this.name, {super.key, required this.size, this.color = Fa.ink, this.weight = 2, this.filled = false});
  final String name;
  final double size;
  final Color color;
  final double weight;
  final bool filled;

  @override
  Widget build(BuildContext context) {
    final String svg;
    if (filled && faFilledIcons.containsKey(name)) {
      svg = '<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path fill="${_hex(color)}" d="${faFilledIcons[name]}"/></svg>';
    } else {
      final paths = (faStrokeIcons[name] ?? const <String>[]).map((d) => '<path d="$d"/>').join();
      svg =
          '<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" fill="none" stroke="${_hex(color)}" '
          'stroke-width="$weight" stroke-linecap="round" stroke-linejoin="round">$paths</svg>';
    }
    return SvgPicture.string(svg, width: size, height: size);
  }
}

/// A filled 24x24 Material-style SVG path (used by the bars, as on the web).
class FaPath extends StatelessWidget {
  const FaPath(this.d, {super.key, required this.size, this.color = Fa.ink, this.evenOdd = false});
  final String d;
  final double size;
  final Color color;
  final bool evenOdd;

  @override
  Widget build(BuildContext context) => SvgPicture.string(
    '<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path fill="${_hex(color)}"${evenOdd ? ' fill-rule="evenodd"' : ''} d="$d"/></svg>',
    width: size,
    height: size,
  );
}

/// Material icon paths used by the web templates.
class FaPaths {
  FaPaths._();
  static const person = 'M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z';
  static const bell =
      'M12 22c1.1 0 2-.9 2-2h-4c0 1.1.89 2 2 2zm6-6v-5c0-3.07-1.64-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.63 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2zm-2 1H8v-6c0-2.48 1.51-4.5 4-4.5s4 2.02 4 4.5v6z';
  static const search =
      'M15.5 14h-.79l-.28-.27A6.47 6.47 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z';
  static const mic =
      'M12 14c1.66 0 3-1.34 3-3V5c0-1.66-1.34-3-3-3S9 3.34 9 5v6c0 1.66 1.34 3 3 3zm-1.2-9.1c0-.66.54-1.2 1.2-1.2s1.2.54 1.2 1.2l-.01 6.2c0 .66-.53 1.2-1.19 1.2s-1.2-.54-1.2-1.2V4.9zm6.5 6.1c0 3-2.54 5.1-5.3 5.1S6.7 14 6.7 11H5c0 3.41 2.72 6.23 6 6.72V21h2v-3.28c3.28-.48 6-3.3 6-6.72h-1.7z';
  static const plus =
      'M18 13h-5v5c0 .55-.45 1-1 1s-1-.45-1-1v-5H6c-.55 0-1-.45-1-1s.45-1 1-1h5V6c0-.55.45-1 1-1s1 .45 1 1v5h5c.55 0 1 .45 1 1s-.45 1-1 1z';
  static const minus = 'M18 13H6c-.55 0-1-.45-1-1s.45-1 1-1h12c.55 0 1 .45 1 1s-.45 1-1 1z';
  static const check = 'M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z';
  static const heartOn =
      'm12 21.35-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z';
  static const heartOff =
      'M16.5 3c-1.74 0-3.41.81-4.5 2.09C10.91 3.81 9.24 3 7.5 3 4.42 3 2 5.42 2 8.5c0 3.78 3.4 6.86 8.55 11.54L12 21.35l1.45-1.32C18.6 15.36 22 12.28 22 8.5 22 5.42 19.58 3 16.5 3zm-4.4 15.55-.1.1-.1-.1C7.14 14.24 4 11.39 4 8.5 4 6.5 5.5 5 7.5 5c1.54 0 3.04.99 3.57 2.36h1.87C13.46 5.99 14.96 5 16.5 5c2 0 3.5 1.5 3.5 3.5 0 2.89-3.14 5.74-7.9 10.05z';
  static const clock =
      'M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67z';
  static const personSmall =
      'M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v1c0 .55.45 1 1 1h14c.55 0 1-.45 1-1v-1c0-2.66-5.33-4-8-4z';
  /// Shapes (category) icon before "تصفح حسب" on home, same style as the verified badge.
  static const browse = 'M12 2l-5.5 9h11L12 2zm5.5 11c-2.49 0-4.5 2.01-4.5 4.5s2.01 4.5 4.5 4.5 4.5-2.01 4.5-4.5-2.01-4.5-4.5-4.5zM3 21.5h8v-8H3v8z';
  static const verified =
      'm23 12-2.44-2.79.34-3.69-3.61-.82-1.89-3.2L12 2.96 8.6 1.5 6.71 4.69 3.1 5.5l.34 3.7L1 12l2.44 2.79-.34 3.7 3.61.82L8.6 22.5l3.4-1.47 3.4 1.46 1.89-3.19 3.61-.82-.34-3.69L23 12zm-12.91 4.72-3.8-3.81 1.48-1.48 2.32 2.33 5.85-5.87 1.48 1.48-7.33 7.35z';
  static const calendar =
      'M19 4h-1V2h-2v2H8V2H6v2H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 16H5V10h14v10zm0-12H5V6h14v2zM9 14H7v-2h2v2zm4 0h-2v-2h2v2zm4 0h-2v-2h2v2zm-8 4H7v-2h2v2zm4 0h-2v-2h2v2zm4 0h-2v-2h2v2z';
  static const chevStart = 'M6.23 20.23 8 22l10-10L8 2 6.23 3.77 14.46 12z';
  static const chevBack = 'M17.77 3.77 16 2 6 12l10 10 1.77-1.77L9.54 12z';
}

/// A tappable box with no Material ink, like the web's plain buttons.
class Tap extends StatelessWidget {
  const Tap({super.key, required this.child, this.onTap});
  final Widget child;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => GestureDetector(behavior: HitTestBehavior.opaque, onTap: onTap, child: child);
}

/// The orange call-to-action pill (`.fa-cta`).
class FaCta extends StatelessWidget {
  const FaCta({super.key, required this.label, this.icon, this.onTap, this.height = 74, this.fontSize = 28, this.busy = false});
  final String label;
  final String? icon;
  final VoidCallback? onTap;
  final double height;
  final double fontSize;
  final bool busy;

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    return Opacity(
      opacity: onTap == null || busy ? .55 : 1,
      child: Tap(
        onTap: busy ? null : onTap,
        child: Container(
          height: s(height),
          padding: EdgeInsets.symmetric(horizontal: s(18)),
          decoration: BoxDecoration(
            gradient: Fa.ctaGradient,
            borderRadius: BorderRadius.circular(s(height / 2)),
            boxShadow: [Fa.shadow(context, y: 6, blur: 14, color: const Color(0x4DF35E02))],
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Flexible(
                child: FittedBox(
                  fit: BoxFit.scaleDown,
                  child: Text(
                    label,
                    maxLines: 1,
                    style: Fa.text(s(fontSize), weight: FontWeight.w800, color: Colors.white, height: 1),
                  ),
                ),
              ),
              if (icon != null) ...[SizedBox(width: s(12)), FaIcon(icon!, size: s(fontSize * 1.1), color: Colors.white, weight: 2.6)],
            ],
          ),
        ),
      ),
    );
  }
}

/// Fades an image's edges into the page, like the web's CSS mask-image.
class EdgeFade extends StatelessWidget {
  const EdgeFade({super.key, required this.child, this.left = 0, this.right = 0, this.top = 0, this.bottom = 0});
  final Widget child;

  /// Fraction of the size that fades on each edge.
  final double left, right, top, bottom;

  @override
  Widget build(BuildContext context) {
    Widget out = child;
    if (left > 0 || right > 0) {
      out = ShaderMask(
        blendMode: BlendMode.dstIn,
        shaderCallback: (r) => LinearGradient(
          colors: const [Colors.transparent, Colors.black, Colors.black, Colors.transparent],
          stops: [0, left, 1 - right, 1],
        ).createShader(r),
        child: out,
      );
    }
    if (top > 0 || bottom > 0) {
      out = ShaderMask(
        blendMode: BlendMode.dstIn,
        shaderCallback: (r) => LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: const [Colors.transparent, Colors.black, Colors.black, Colors.transparent],
          stops: [0, top, 1 - bottom, 1],
        ).createShader(r),
        child: out,
      );
    }
    return out;
  }
}
