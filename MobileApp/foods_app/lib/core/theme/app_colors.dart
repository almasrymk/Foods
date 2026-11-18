import 'package:material_ui/material_ui.dart';

/// Colors taken from the web app's design (styles/screens.css).
class AppColors {
  AppColors._();

  static const orange = Color(0xFFF37202);
  static const orangeLight = Color(0xFFFF9A1F);
  static const green = Color(0xFF2F7D1E);
  static const greenDark = Color(0xFF3E7B22);
  static const ink = Color(0xFF2E1E12);
  static const title = Color(0xFF3A2412);
  static const muted = Color(0xFF6E625A);
  static const hint = Color(0xFF8A6A4A);
  static const background = Color(0xFFFDF8F3);
  static const backgroundWarm = Color(0xFFFCF4E7);
  static const card = Color(0xFFFFFCF9);
  static const cardBorder = Color(0xFFF3E8DC);
  static const red = Color(0xFFE8222E);
  static const error = Color(0xFFC0392B);
  static const bannerGreen = Color(0xFF3E6B2A);
  static const bannerGreenDark = Color(0xFF2F4D28);

  /// Default tones for meal categories when the server sends no color.
  static const mealTones = <(Color, Color)>[
    (Color(0xFFFEDD8C), Color(0xFF4A2A12)),
    (Color(0xFFFEDABD), Color(0xFF4A2A12)),
    (Color(0xFFE7D8F4), Color(0xFF4E1F78)),
    (Color(0xFFDBEFD9), Color(0xFF1E2A1E)),
  ];

  /// Parses `#RRGGBB` (or `#AARRGGBB`) coming from the API.
  static Color? parse(String? hex) {
    if (hex == null) return null;
    var raw = hex.trim().replaceFirst('#', '');
    if (raw.length == 6) raw = 'FF$raw';
    if (raw.length != 8) return null;
    final value = int.tryParse(raw, radix: 16);
    return value == null ? null : Color(value);
  }

  static Color darken(Color color, double amount) {
    final hsl = HSLColor.fromColor(color);
    return hsl.withLightness((hsl.lightness - amount).clamp(0.0, 1.0)).toColor();
  }
}
