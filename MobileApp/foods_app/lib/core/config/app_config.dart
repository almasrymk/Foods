/// App-wide configuration.
///
/// The API address can be changed at build time without touching the code:
/// `flutter build apk --dart-define=API_BASE_URL=https://example.com`
class AppConfig {
  AppConfig._();

  static const String apiBaseUrl = String.fromEnvironment('API_BASE_URL', defaultValue: 'http://almasrymk-001-site8.ktempurl.com');

  static const Duration connectTimeout = Duration(seconds: 20);
  static const Duration receiveTimeout = Duration(seconds: 30);

  /// Turns a server path such as `/uploads/x.png` into a full URL.
  static String mediaUrl(String? path) {
    if (path == null || path.trim().isEmpty) return '';
    if (path.startsWith('http')) return path;
    final base = apiBaseUrl.endsWith('/') ? apiBaseUrl.substring(0, apiBaseUrl.length - 1) : apiBaseUrl;
    return path.startsWith('/') ? '$base$path' : '$base/$path';
  }
}
