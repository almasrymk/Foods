import '../../../core/network/api_client.dart';
import '../../../core/utils/json.dart';
import '../../auth/domain/auth_models.dart';
import '../domain/account_models.dart';
import '../domain/account_repository.dart';

class AccountRepositoryImpl implements AccountRepository {
  AccountRepositoryImpl(this._api);
  final ApiClient _api;

  Profile _p(dynamic j) => Profile.fromJson(Map<String, dynamic>.from(j as Map));
  UserSettings _s(dynamic j) => UserSettings.fromJson(Map<String, dynamic>.from(j as Map));
  List<FamilyMember> _f(dynamic j) => listOf(j, FamilyMember.fromJson);

  @override
  Future<Profile> profile() => _api.get('/customer/profile', parse: _p);

  @override
  Future<Profile> saveName(String name) => _api.put('/customer/profile', body: {'name': name}, parse: _p);

  @override
  Future<Profile> savePhoto(String? imagePath) => _api.put('/customer/profile/photo', body: {'imagePath': imagePath}, parse: _p);

  @override
  Future<Profile> saveCountry(String countryId) => _api.put('/customer/country', body: {'countryId': countryId}, parse: _p);

  @override
  Future<List<Country>> countries() => _api.get('/Auth/Countries', parse: (j) => listOf(j, Country.fromJson));

  @override
  Future<UserSettings> settings() => _api.get('/customer/settings', parse: _s);

  @override
  Future<UserSettings> saveSettings(UserSettings settings) => _api.put('/customer/settings', body: settings.toJson(), parse: _s);

  @override
  Future<List<FamilyMember>> family() => _api.get('/customer/family', parse: _f);

  @override
  Future<List<FamilyMember>> saveFamily(List<Map<String, dynamic>> members) => _api.put('/customer/family', body: members, parse: _f);

  @override
  Future<String> uploadImage(String filePath) => _api.uploadImage(filePath);
}
