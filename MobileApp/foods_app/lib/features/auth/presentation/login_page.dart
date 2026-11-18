import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import 'auth_controller.dart';
import 'auth_ui.dart';

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});
  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  final _form = GlobalKey<FormState>();
  final _user = TextEditingController();
  final _pass = TextEditingController();
  bool _busy = false;
  bool _hide = true;
  String? _error;

  @override
  void dispose() {
    _user.dispose();
    _pass.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(authControllerProvider.notifier).login(_user.text, _pass.text);
    } catch (e) {
      setState(() => _error = '$e'.replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      title: 'أهلاً بيك تاني',
      subtitle: 'سجّل دخولك وخطط أكل الشهر لأسرتك بسهولة',
      footer: AuthLink(text: 'ليس لديك حساب؟', action: 'سجّل الآن', onTap: () => context.push('/register')),
      child: AutofillGroup(
        child: Form(
          key: _form,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              AuthField(
                label: 'اسم المستخدم',
                icon: 'user',
                hint: 'اكتب اسم المستخدم',
                controller: _user,
                autofill: const [AutofillHints.username],
                validator: (v) => (v ?? '').trim().isEmpty ? 'اكتب اسم المستخدم' : null,
              ),
              const SizedBox(height: 14),
              AuthField(
                label: 'كلمة المرور',
                icon: 'lock',
                hint: 'اكتب كلمة المرور',
                controller: _pass,
                obscure: _hide,
                action: TextInputAction.done,
                autofill: const [AutofillHints.password],
                trailing: AuthEye(hidden: _hide, onTap: () => setState(() => _hide = !_hide)),
                onSubmitted: (_) => _submit(),
                validator: (v) => (v ?? '').isEmpty ? 'اكتب كلمة المرور' : null,
              ),
              if (_error != null) ...[const SizedBox(height: 14), AuthError(_error!)],
              const SizedBox(height: 22),
              AuthPrimaryButton(label: 'دخول', busy: _busy, onTap: _submit),
            ],
          ),
        ),
      ),
    );
  }
}
