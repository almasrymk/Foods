import '../../../core/utils/json.dart';

class AppNotification {
  AppNotification({
    required this.id,
    required this.type,
    this.title,
    this.body,
    this.link,
    required this.createdAt,
    this.readAt,
    this.image,
  });
  final String id;
  final String type;
  final String? title;
  final String? body;
  final String? link;
  final String createdAt;
  String? readAt;
  final String? image;

  bool get isRead => readAt != null;

  factory AppNotification.fromJson(Map<String, dynamic> j) => AppNotification(
    id: strOr(j['id']),
    type: strOr(j['type']),
    title: str(j['title']),
    body: str(j['body']),
    link: str(j['link']),
    createdAt: strOr(j['createdAt']),
    readAt: str(j['readAt']),
    image: str(j['image']),
  );
}

class NotificationPage {
  NotificationPage({required this.items, required this.total, required this.unread});
  final List<AppNotification> items;
  final int total;
  final int unread;
  factory NotificationPage.fromJson(Map<String, dynamic> j) =>
      NotificationPage(items: listOf(j['items'], AppNotification.fromJson), total: intOf(j['total']), unread: intOf(j['unread']));
}
