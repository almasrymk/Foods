import '../../auth/domain/auth_models.dart';
import 'account_models.dart';

abstract interface class AccountRepository {
  Future<Profile> profile();
  Future<Profile> saveName(String name);
  Future<Profile> savePhoto(String? imagePath);
  Future<Profile> saveCountry(String countryId);
  Future<List<Country>> countries();
  Future<UserSettings> settings();
  Future<UserSettings> saveSettings(UserSettings settings);
  Future<List<FamilyMember>> family();
  Future<List<FamilyMember>> saveFamily(List<Map<String, dynamic>> members);
  Future<String> uploadImage(String filePath);
}
