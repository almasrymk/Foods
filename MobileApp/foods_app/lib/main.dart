import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:material_ui/material_ui.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'app/app.dart';
import 'app/providers.dart';
import 'core/storage/session_storage.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initializeDateFormatting('ar');
  final prefs = await SharedPreferences.getInstance();
  final session = SessionStorage(prefs);
  await session.load();
  runApp(ProviderScope(overrides: [sessionStorageProvider.overrideWithValue(session)], child: const FoodsApp()));
}
