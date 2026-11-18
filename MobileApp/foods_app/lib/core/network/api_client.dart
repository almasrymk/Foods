import 'package:dio/dio.dart';

import '../config/app_config.dart';
import '../storage/session_storage.dart';
import 'api_exception.dart';

/// Thin wrapper around Dio that speaks the backend's envelope:
/// `{ statusCode, response, errors }`.
class ApiClient {
  ApiClient(this._session, {Dio? dio, this.onUnauthorized})
    : _dio =
          dio ??
          Dio(
            BaseOptions(
              baseUrl: AppConfig.apiBaseUrl,
              connectTimeout: AppConfig.connectTimeout,
              receiveTimeout: AppConfig.receiveTimeout,
              headers: {'Accept': 'application/json'},
            ),
          ) {
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          final token = _session.token;
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          handler.next(options);
        },
        onError: (error, handler) {
          final hadToken = error.requestOptions.headers.containsKey('Authorization');
          if (error.response?.statusCode == 401 && hadToken) onUnauthorized?.call();
          handler.next(error);
        },
      ),
    );
  }

  final Dio _dio;
  final SessionStorage _session;

  /// Called when the server answers 401 (expired token).
  void Function()? onUnauthorized;

  Future<T> get<T>(String path, {Map<String, dynamic>? query, required T Function(dynamic json) parse}) =>
      _send(() => _dio.get(path, queryParameters: _clean(query)), parse);

  Future<T> post<T>(String path, {Object? body, required T Function(dynamic json) parse}) =>
      _send(() => _dio.post(path, data: body ?? const {}), parse);

  Future<T> put<T>(String path, {Object? body, required T Function(dynamic json) parse}) =>
      _send(() => _dio.put(path, data: body ?? const {}), parse);

  Future<T> delete<T>(String path, {required T Function(dynamic json) parse}) => _send(() => _dio.delete(path), parse);

  /// Uploads an image and returns its `/uploads/...` path.
  Future<String> uploadImage(String filePath, {String? fileName}) async {
    try {
      final form = FormData.fromMap({'file': await MultipartFile.fromFile(filePath, filename: fileName ?? filePath.split('/').last)});
      final res = await _dio.post('/files/image', data: form);
      final data = res.data;
      if (data is Map && data['path'] is String) return data['path'] as String;
      throw ApiException('تعذر رفع الصورة');
    } on DioException catch (e) {
      throw _fromDio(e);
    }
  }

  Future<T> _send<T>(Future<Response<dynamic>> Function() call, T Function(dynamic json) parse) async {
    try {
      final res = await call();
      return _unwrap<T>(res.data, parse);
    } on DioException catch (e) {
      throw _fromDio(e);
    }
  }

  T _unwrap<T>(dynamic data, T Function(dynamic json) parse) {
    if (data is Map) {
      final status = data['statusCode'];
      final response = data['response'];
      if ((status == 200 || status == 0) && response != null) return parse(response);
      throw ApiException(_errors(data) ?? 'حدث خطأ', statusCode: status is int ? status : null);
    }
    throw ApiException('رد غير متوقع من الخادم');
  }

  ApiException _fromDio(DioException e) {
    final data = e.response?.data;
    final status = e.response?.statusCode;
    if (data is Map) {
      final msg = _errors(data);
      if (msg != null) return ApiException(msg, statusCode: status);
    }
    if (status == 401) return ApiException('انتهت الجلسة، سجل الدخول تاني', statusCode: 401);
    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.sendTimeout:
        return ApiException('الخادم اتأخر في الرد، جرب تاني', statusCode: status);
      case DioExceptionType.connectionError:
        return ApiException('تعذر الاتصال بالخادم، اتأكد من النت', statusCode: status);
      default:
        return ApiException('تعذر الاتصال بالخادم', statusCode: status);
    }
  }

  String? _errors(Map data) {
    final errors = data['errors'];
    if (errors is List && errors.isNotEmpty) return errors.join('، ');
    return null;
  }

  Map<String, dynamic>? _clean(Map<String, dynamic>? query) {
    if (query == null) return null;
    final out = <String, dynamic>{};
    query.forEach((key, value) {
      if (value == null) return;
      if (value is String && value.trim().isEmpty) return;
      out[key] = value;
    });
    return out;
  }
}
