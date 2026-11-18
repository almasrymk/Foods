# Foods (Flutter)

The customer app of Foods, for Android, talking to the hosted API.

## Run / build

```bash
flutter pub get
flutter run                      # on a phone or emulator
flutter build apk --release      # APK in build/app/outputs/flutter-apk/app-release.apk
```

The API address defaults to `http://almasrymk-001-site8.ktempurl.com`.
Change it without editing code:

```bash
flutter build apk --release --dart-define=API_BASE_URL=https://your-domain
```

The host is plain HTTP, so `android/app/src/main/res/xml/network_security_config.xml`
allows cleartext traffic for that domain only. Remove it once the site has HTTPS.

## Structure (feature-first, clean architecture)

```
lib/
  main.dart                 loads saved session, starts the app
  app/                      app widget, router (go_router), DI providers, bottom-bar shell
  core/
    config/                 API base URL, media URLs
    network/                ApiClient (Dio + Bearer token + {statusCode,response,errors} envelope)
    storage/                SessionStorage (token in secure storage, small prefs)
    theme/                  colors and theme (Tajawal, RTL)
    utils/                  JSON helpers
    widgets/                shared widgets (images, steppers, headers, loading/error)
  features/<feature>/
    domain/                 entities + repository interfaces (no Flutter, no Dio)
    data/                   repository implementations calling the API
    presentation/           pages, widgets and Riverpod controllers
```

Features: `auth`, `home`, `catalog` (meals, dishes, cuisines, favorites, dish details),
`cart` (selected dishes + shopping list, synced with the server as the "طلباتي" draft,
purchase rules ported from the web client), `orders` (purchase order, old orders,
reorder, share), `notifications`, `account` (profile, photo, country, family, settings).

State management: Riverpod. Navigation: go_router with a StatefulShellRoute for the
five bottom tabs.
