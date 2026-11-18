import '../../../core/network/api_client.dart';
import '../../../core/utils/json.dart';
import '../domain/notification_models.dart';
import '../domain/notifications_repository.dart';

class NotificationsRepositoryImpl implements NotificationsRepository {
  NotificationsRepositoryImpl(this._api);
  final ApiClient _api;

  @override
  Future<NotificationPage> page({int skip = 0, int take = 20, bool? unread}) => _api.get(
    '/customer/notifications',
    query: {'skip': skip, 'take': take, 'unread': unread?.toString()},
    parse: (j) => NotificationPage.fromJson(Map<String, dynamic>.from(j as Map)),
  );

  @override
  Future<int> unreadCount() => _api.get('/customer/notifications/unread-count', parse: intOf);

  @override
  Future<int> markRead(String id) => _api.post('/customer/notifications/$id/read', parse: intOf);

  @override
  Future<int> markAllRead() => _api.post('/customer/notifications/read-all', parse: intOf);
}
