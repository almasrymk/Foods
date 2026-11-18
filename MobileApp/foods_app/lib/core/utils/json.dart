/// Small helpers to read loosely typed JSON safely.
String? str(dynamic v) => v?.toString();
String strOr(dynamic v, [String fallback = '']) => v?.toString() ?? fallback;
int intOf(dynamic v, [int fallback = 0]) => v is num ? v.toInt() : int.tryParse('${v ?? ''}') ?? fallback;
int? intOrNull(dynamic v) => v == null ? null : (v is num ? v.toInt() : int.tryParse('$v'));
double dbl(dynamic v, [double fallback = 0]) => v is num ? v.toDouble() : double.tryParse('${v ?? ''}') ?? fallback;
double? dblOrNull(dynamic v) => v == null ? null : (v is num ? v.toDouble() : double.tryParse('$v'));
bool boolOf(dynamic v) => v == true || v == 'true' || v == 1;
List<String> strList(dynamic v) => v is List ? v.map((e) => '$e').toList() : const [];
List<T> listOf<T>(dynamic v, T Function(Map<String, dynamic>) parse) =>
    v is List ? v.whereType<Map>().map((e) => parse(Map<String, dynamic>.from(e))).toList() : <T>[];
