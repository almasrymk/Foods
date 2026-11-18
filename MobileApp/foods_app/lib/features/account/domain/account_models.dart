import '../../../core/utils/json.dart';

class Profile {
  Profile({this.countryId, this.countryName, this.dialect, this.name, this.userName, this.imagePath});
  final String? countryId;
  final String? countryName;
  final String? dialect;
  final String? name;
  final String? userName;
  final String? imagePath;

  factory Profile.fromJson(Map<String, dynamic> j) => Profile(
    countryId: str(j['countryId']),
    countryName: str(j['countryName']),
    dialect: str(j['dialect']),
    name: str(j['name']),
    userName: str(j['userName']),
    imagePath: str(j['imagePath']),
  );
}

class FamilyMember {
  FamilyMember({required this.id, this.name, this.age = 0, this.birthDate, this.relation, this.imagePath});
  final String id;
  final String? name;
  final int age;
  final String? birthDate;
  final String? relation;
  final String? imagePath;

  factory FamilyMember.fromJson(Map<String, dynamic> j) => FamilyMember(
    id: strOr(j['id']),
    name: str(j['name']),
    age: intOf(j['age']),
    birthDate: str(j['birthDate']),
    relation: str(j['relation']),
    imagePath: str(j['imagePath']),
  );

  Map<String, dynamic> toInput() => {'name': name ?? '', 'age': age, 'birthDate': birthDate, 'relation': relation, 'imagePath': imagePath};
}

const familyRelations = ['أب', 'أم', 'زوج', 'زوجة', 'ابن', 'بنت', 'أخ', 'أخت', 'جد', 'جدة', 'أخرى'];

/// Notification switches in the settings screen.
enum NotifyKey {
  notifyLogin('تذكير بالدخول', 'لو مدخلتش التطبيق 3 أيام'),
  notifyWelcome('ترحيب بالعودة', 'لما ترجع بعد غياب أسبوعين'),
  notifyBirthday('أعياد ميلاد الأسرة', 'قبلها بيوم ويوم عيد الميلاد'),
  notifyOrders('تذكير بالطلبات', 'طلب لسه متطبعش ولا اتبعت'),
  notifyPlanning('تخطيط الأسبوع', 'تذكير أسبوعي بتخطيط الوجبات');

  const NotifyKey(this.label, this.sub);
  final String label;
  final String sub;
}

class UserSettings {
  UserSettings({this.language = 'ar', Map<NotifyKey, bool>? flags, this.usePrices = false}) : flags = flags ?? {for (final k in NotifyKey.values) k: true};
  String language;
  /// The customer works with prices (saved by its own call, not with the other settings).
  bool usePrices;
  final Map<NotifyKey, bool> flags;

  factory UserSettings.fromJson(Map<String, dynamic> j) =>
      UserSettings(language: strOr(j['language'], 'ar'), flags: {for (final k in NotifyKey.values) k: boolOf(j[k.name])}, usePrices: boolOf(j['usePrices']));

  Map<String, dynamic> toJson() => {'language': language, for (final e in flags.entries) e.key.name: e.value};
}
