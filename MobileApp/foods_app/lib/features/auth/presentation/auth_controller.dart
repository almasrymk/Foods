import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../app/providers.dart';
import '../domain/auth_models.dart';

class AuthState {
  const AuthState({required this.loggedIn, this.name = '', this.photo, this.isAdmin = false, this.onboarding = false});
  final bool loggedIn;

  /// Just registered: show the family step before the app.
  final bool onboarding;
  final String name;
  final String? photo;
  final bool isAdmin;

  AuthState copyWith({String? name, String? Function()? photo}) =>
      AuthState(loggedIn: loggedIn, name: name ?? this.name, photo: photo != null ? photo() : this.photo, isAdmin: isAdmin, onboarding: onboarding);
}

final authControllerProvider = NotifierProvider<AuthController, AuthState>(AuthController.new);

class AuthController extends Notifier<AuthState> {
  @override
  AuthState build() {
    final s = ref.read(sessionStorageProvider);
    // An expired token logs the customer out from anywhere in the app.
    ref.read(apiClientProvider).onUnauthorized = () {
      if (state.loggedIn) logout();
    };
    return AuthState(loggedIn: s.isLoggedIn, name: s.name, photo: s.photo, isAdmin: s.role == 'Admin');
  }

  Future<void> login(String userName, String password) async {
    final user = await ref.read(authRepositoryProvider).login(userName.trim(), password);
    await _store(user);
  }

  Future<void> register({
    required String name,
    required String userName,
    required String password,
    required String countryId,
    required List<FamilyMemberInput> family,
  }) async {
    final user = await ref
        .read(authRepositoryProvider)
        .register(name: name.trim(), userName: userName.trim(), password: password, countryId: countryId, family: family);
    await _store(user, onboarding: true);
  }

  void finishOnboarding() {
    state = AuthState(loggedIn: state.loggedIn, name: state.name, photo: state.photo, isAdmin: state.isAdmin);
  }

  Future<void> logout() async {
    await ref.read(sessionStorageProvider).clear();
    state = const AuthState(loggedIn: false);
  }

  Future<void> updateName(String name) async {
    await ref.read(sessionStorageProvider).setName(name);
    state = state.copyWith(name: name);
  }

  Future<void> updatePhoto(String? photo) async {
    await ref.read(sessionStorageProvider).setPhoto(photo);
    state = state.copyWith(photo: () => photo);
  }

  Future<void> _store(AuthUser user, {bool onboarding = false}) async {
    if (user.token.isEmpty) throw Exception('تعذر تسجيل الدخول');
    await ref.read(sessionStorageProvider).saveUser(token: user.token, name: user.name, role: user.role, photo: user.imagePath);
    state = AuthState(loggedIn: true, name: user.name, photo: user.imagePath, isAdmin: user.isAdmin, onboarding: onboarding);
  }
}
