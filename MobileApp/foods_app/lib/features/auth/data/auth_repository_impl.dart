import '../../../core/network/api_client.dart';
import '../../../core/utils/json.dart';
import '../domain/auth_models.dart';
import '../domain/auth_repository.dart';

class AuthRepositoryImpl implements AuthRepository {
  AuthRepositoryImpl(this._api);
  final ApiClient _api;

  AuthUser _user(dynamic j) => AuthUser.fromJson(Map<String, dynamic>.from(j as Map));

  @override
  Future<AuthUser> login(String userName, String password) =>
      _api.post('/Auth/Login', body: {'userName': userName, 'password': password}, parse: _user);

  @override
  Future<AuthUser> register({
    required String name,
    required String userName,
    required String password,
    required String countryId,
    required List<FamilyMemberInput> family,
  }) => _api.post(
    '/Auth/Register',
    body: {
      'name': name,
      'userName': userName,
      'password': password,
      'imagePath': null,
      'countryId': countryId,
      'family': family.map((f) => f.toJson()).toList(),
    },
    parse: _user,
  );

  @override
  Future<List<Country>> countries() => _api.get('/Auth/Countries', parse: (j) => listOf(j, Country.fromJson));
}
