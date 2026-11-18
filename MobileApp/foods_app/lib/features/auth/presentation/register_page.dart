import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../app/providers.dart';
import '../../../core/ui/fa_kit.dart';
import '../domain/auth_models.dart';
import 'auth_controller.dart';
import 'auth_ui.dart';

final countriesProvider = FutureProvider.autoDispose<List<Country>>((ref) => ref.watch(authRepositoryProvider).countries());

/// Register, step 1 of 2: basic data. Step 2 (family members) opens right after.
class RegisterPage extends ConsumerStatefulWidget {
  const RegisterPage({super.key});
  @override
  ConsumerState<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends ConsumerState<RegisterPage> {
  final _form = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _user = TextEditingController();
  final _pass = TextEditingController();
  String? _countryId;
  bool _hide = true;
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    for (final c in [_name, _user, _pass]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref
          .read(authControllerProvider.notifier)
          .register(name: _name.text, userName: _user.text, password: _pass.text, countryId: _countryId!, family: const []);
    } catch (e) {
      setState(() => _error = '$e'.replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final countries = ref.watch(countriesProvider).value ?? const <Country>[];
    String? required(String? v) => (v ?? '').trim().isEmpty ? 'مطلوب' : null;
    return AuthScaffold(
      title: 'حساب جديد',
      subtitle: 'خطوتين بس وتبدأ تخطط أكلك',
      step: 1,
      footer: AuthLink(text: 'عندك حساب؟', action: 'سجّل دخول', onTap: () => context.canPop() ? context.pop() : context.go('/login')),
      child: Form(
        key: _form,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            AuthField(label: 'الاسم', icon: 'idCard', hint: 'اسمك اللي هيظهر في التطبيق', controller: _name, validator: required),
            const SizedBox(height: 14),
            AuthField(label: 'اسم المستخدم', icon: 'user', hint: 'هتدخل بيه بعد كده', controller: _user, validator: required),
            const SizedBox(height: 14),
            Text(
              'البلد',
              style: Fa.text(13, weight: FontWeight.w800, color: AuthScaffold.ink),
            ),
            const SizedBox(height: 6),
            DropdownButtonFormField<String>(
              initialValue: _countryId,
              isExpanded: true,
              icon: const FaIcon('chevronDown', size: 18, color: AuthScaffold.muted, weight: 2.4),
              style: Fa.text(15, weight: FontWeight.w600, color: AuthScaffold.ink),
              decoration: AuthField.decoration(hint: 'اختار بلدك', icon: 'globe'),
              borderRadius: BorderRadius.circular(16),
              dropdownColor: Colors.white,
              items: [for (final c in countries) DropdownMenuItem(value: c.id, child: Text(c.label))],
              onChanged: (v) => setState(() => _countryId = v),
              validator: (v) => v == null ? 'اختار بلدك' : null,
            ),
            const SizedBox(height: 14),
            AuthField(
              label: 'كلمة المرور',
              icon: 'lock',
              hint: '4 حروف أو أرقام على الأقل',
              controller: _pass,
              obscure: _hide,
              action: TextInputAction.done,
              trailing: AuthEye(hidden: _hide, onTap: () => setState(() => _hide = !_hide)),
              onSubmitted: (_) => _submit(),
              validator: (v) => (v ?? '').length < 4 ? 'كلمة المرور قصيرة' : null,
            ),
            if (_error != null) ...[const SizedBox(height: 14), AuthError(_error!)],
            const SizedBox(height: 22),
            AuthPrimaryButton(label: 'التالي', busy: _busy, onTap: _submit),
          ],
        ),
      ),
    );
  }
}
