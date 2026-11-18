import 'auth_models.dart';

abstract interface class AuthRepository {
  Future<AuthUser> login(String userName, String password);
  Future<AuthUser> register({
    required String name,
    required String userName,
    required String password,
    required String countryId,
    required List<FamilyMemberInput> family,
  });
  Future<List<Country>> countries();
}
