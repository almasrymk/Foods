import 'package:material_ui/material_ui.dart';

import '../theme/app_colors.dart';

class LoadingView extends StatelessWidget {
  const LoadingView({super.key, this.padding = const EdgeInsets.all(32)});
  final EdgeInsets padding;
  @override
  Widget build(BuildContext context) => Padding(
    padding: padding,
    child: const Center(child: CircularProgressIndicator(color: AppColors.orange)),
  );
}

class MessageView extends StatelessWidget {
  const MessageView({super.key, required this.message, this.icon = Icons.info_outline, this.onRetry, this.isError = false});

  final String message;
  final IconData icon;
  final VoidCallback? onRetry;
  final bool isError;

  factory MessageView.error(Object error, {VoidCallback? onRetry}) =>
      MessageView(message: '$error'.replaceFirst('Exception: ', ''), icon: Icons.wifi_off_rounded, onRetry: onRetry, isError: true);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 28, vertical: 40),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 44, color: isError ? AppColors.error : AppColors.hint),
          const SizedBox(height: 12),
          Text(
            message,
            textAlign: TextAlign.center,
            style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: isError ? AppColors.error : AppColors.hint),
          ),
          if (onRetry != null) ...[
            const SizedBox(height: 16),
            OutlinedButton.icon(onPressed: onRetry, icon: const Icon(Icons.refresh), label: const Text('حاول تاني')),
          ],
        ],
      ),
    );
  }
}

void showMessage(BuildContext context, Object message, {bool error = false}) {
  final text = '$message'.replaceFirst('Exception: ', '');
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(
      SnackBar(
        content: Text(
          text,
          style: const TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.w700),
        ),
        backgroundColor: error ? AppColors.error : AppColors.ink,
        // Short confirmations disappear quickly; errors stay a bit longer to be read.
        duration: Duration(milliseconds: error ? 3000 : 1500),
      ),
    );
}
