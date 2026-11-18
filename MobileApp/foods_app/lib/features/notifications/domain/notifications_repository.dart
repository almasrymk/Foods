import 'notification_models.dart';

abstract interface class NotificationsRepository {
  Future<NotificationPage> page({int skip = 0, int take = 20, bool? unread});
  Future<int> unreadCount();
  Future<int> markRead(String id);
  Future<int> markAllRead();
}
