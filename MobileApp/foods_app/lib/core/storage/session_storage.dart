import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Keeps the login token in secure storage and small preferences in
/// SharedPreferences. Values are cached in memory after [load].
class SessionStorage {
  SessionStorage(this._prefs, {FlutterSecureStorage? secure}) : _secure = secure ?? const FlutterSecureStorage();

  final SharedPreferences _prefs;
  final FlutterSecureStorage _secure;

  static const _tokenKey = 'foods.token';
  static const _nameKey = 'foods.name';
  static const _roleKey = 'foods.role';
  static const _photoKey = 'foods.photo';
  static const _draftKey = 'foods.draft.orderId';
  static const _fontKey = 'foods.fontLevel4';
  static const _oldFontKey = 'foods.fontLevel';

  String? _token;

  String? get token => _token;
  String get name => _prefs.getString(_nameKey) ?? '';
  String? get role => _prefs.getString(_roleKey);
  String? get photo => _prefs.getString(_photoKey);
  bool get isLoggedIn => (_token ?? '').isNotEmpty;

  String? get draftOrderId => _prefs.getString(_draftKey);

  /// Font size setting: 0..3, 2 is the default. Kept after logout.
  /// The old three-level setting (0 صغير، 1 متوسط، 2 كبير) moves up one step, so nobody's size jumps.
  int get fontLevel {
    final level = _prefs.getInt(_fontKey);
    if (level != null) return level;
    final old = _prefs.getInt(_oldFontKey);
    return old == null ? 2 : (old + 1).clamp(0, 3);
  }
  Future<void> setFontLevel(int level) => _prefs.setInt(_fontKey, level);

  Future<void> load() async {
    try {
      _token = await _secure.read(key: _tokenKey);
    } catch (_) {
      _token = null;
    }
  }

  Future<void> saveUser({required String token, required String name, String? role, String? photo}) async {
    _token = token;
    try {
      await _secure.write(key: _tokenKey, value: token);
    } catch (_) {
      /* keep it in memory */
    }
    await _prefs.setString(_nameKey, name);
    if (role != null) {
      await _prefs.setString(_roleKey, role);
    } else {
      await _prefs.remove(_roleKey);
    }
    await setPhoto(photo);
  }

  Future<void> setName(String name) => _prefs.setString(_nameKey, name);

  Future<void> setPhoto(String? photo) async {
    if (photo == null || photo.isEmpty) {
      await _prefs.remove(_photoKey);
    } else {
      await _prefs.setString(_photoKey, photo);
    }
  }

  Future<void> setDraftOrderId(String? id) async {
    if (id == null) {
      await _prefs.remove(_draftKey);
    } else {
      await _prefs.setString(_draftKey, id);
    }
  }

  Future<void> clear() async {
    _token = null;
    try {
      await _secure.delete(key: _tokenKey);
    } catch (_) {}
    await _prefs.remove(_nameKey);
    await _prefs.remove(_roleKey);
    await _prefs.remove(_photoKey);
    await _prefs.remove(_draftKey);
  }
}
