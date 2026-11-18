import '../../../core/utils/json.dart';

class AuthUser {
  AuthUser({required this.id, required this.name, required this.userName, this.imagePath, required this.token, this.role});

  final String id;
  final String name;
  final String userName;
  final String? imagePath;
  final String token;
  final String? role;

  bool get isAdmin => role == 'Admin';

  factory AuthUser.fromJson(Map<String, dynamic> j) => AuthUser(
    id: strOr(j['id']),
    name: strOr(j['name']),
    userName: strOr(j['userName']),
    imagePath: str(j['imagePath']),
    token: strOr(j['token']),
    role: str(j['role']),
  );
}

class Country {
  Country({required this.id, this.name, this.dialect});
  final String id;
  final String? name;
  final String? dialect;

  String get label => (dialect ?? '').isEmpty ? (name ?? '') : '${name ?? ''} · $dialect';

  factory Country.fromJson(Map<String, dynamic> j) => Country(id: strOr(j['id']), name: str(j['name']), dialect: str(j['dialect']));
}

class FamilyMemberInput {
  FamilyMemberInput({required this.name, required this.age, this.birthDate, this.relation, this.imagePath});
  String name;
  int age;
  String? birthDate;
  String? relation;
  String? imagePath;

  Map<String, dynamic> toJson() => {'name': name, 'age': age, 'birthDate': birthDate, 'relation': relation, 'imagePath': imagePath};
}
