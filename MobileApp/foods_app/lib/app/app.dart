import 'package:flutter_localizations/flutter_localizations.dart' show GlobalWidgetsLocalizations;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../core/ui/fa_kit.dart';
import 'package:material_ui/material_ui.dart';

import '../core/theme/app_theme.dart';
import 'providers.dart';
import '../features/auth/presentation/auth_controller.dart';
import '../features/prices/prices.dart';
import 'router.dart';

class FoodsApp extends ConsumerWidget {
  const FoodsApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // After login, load the currency of the country the customer lives in (shown after every amount).
    if (ref.watch(authControllerProvider.select((a) => a.loggedIn))) ref.watch(priceCountryProvider);
    return MaterialApp.router(
      title: 'Foods',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      routerConfig: ref.watch(routerProvider),
      // The web sizes everything in px of the screen width and ignores the phone's font-size
      // setting, so the app does the same to keep the layout identical.
      builder: (context, child) {
        Fa.textScale = TextScaler.linear(FontLevel.scaleOf(ref.watch(fontLevelProvider)));
        return MediaQuery(
          data: MediaQuery.of(context).copyWith(textScaler: Fa.textScale),
          child: child!,
        );
      },
      locale: const Locale('ar'),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: const [GlobalMaterialLocalizations.delegate, GlobalWidgetsLocalizations.delegate],
    );
  }
}
