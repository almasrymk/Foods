import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/network/api_client.dart';
import '../core/storage/session_storage.dart';
import '../core/ui/fa_kit.dart';
import '../features/account/data/account_repository_impl.dart';
import '../features/account/domain/account_repository.dart';
import '../features/auth/data/auth_repository_impl.dart';
import '../features/auth/domain/auth_repository.dart';
import '../features/catalog/data/catalog_repository_impl.dart';
import '../features/catalog/domain/catalog_repository.dart';
import '../features/notifications/data/notifications_repository_impl.dart';
import '../features/notifications/domain/notifications_repository.dart';
import '../features/orders/data/orders_repository_impl.dart';
import '../features/orders/domain/orders_repository.dart';

/// Overridden in main() once SharedPreferences and the token are loaded.
final sessionStorageProvider = Provider<SessionStorage>((ref) => throw UnimplementedError());

final apiClientProvider = Provider<ApiClient>((ref) => ApiClient(ref.watch(sessionStorageProvider)));

final authRepositoryProvider = Provider<AuthRepository>((ref) => AuthRepositoryImpl(ref.watch(apiClientProvider)));
final catalogRepositoryProvider = Provider<CatalogRepository>((ref) => CatalogRepositoryImpl(ref.watch(apiClientProvider)));
final ordersRepositoryProvider = Provider<OrdersRepository>((ref) => OrdersRepositoryImpl(ref.watch(apiClientProvider)));
final accountRepositoryProvider = Provider<AccountRepository>((ref) => AccountRepositoryImpl(ref.watch(apiClientProvider)));
final notificationsRepositoryProvider = Provider<NotificationsRepository>(
  (ref) => NotificationsRepositoryImpl(ref.watch(apiClientProvider)),
);

/// Settings → حجم الخط (four levels, 0..3); the app's text scale follows it.
final fontLevelProvider = NotifierProvider<FontLevelController, int>(FontLevelController.new);

class FontLevelController extends Notifier<int> {
  @override
  int build() => ref.watch(sessionStorageProvider).fontLevel.clamp(0, FontLevel.count - 1);

  Future<void> set(int level) async {
    state = level.clamp(0, FontLevel.count - 1);
    await ref.read(sessionStorageProvider).setFontLevel(state);
  }
}
