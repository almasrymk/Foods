import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:image_picker/image_picker.dart';
import 'package:material_ui/material_ui.dart';

import '../../../app/providers.dart';
import '../../../core/ui/fa_kit.dart';
import '../../../core/widgets/async_views.dart';
import '../../../core/widgets/net_image.dart';
import '../domain/account_models.dart';
import 'settings_page.dart';

const _ink = Color(0xFF2E1E12);

TextStyle _t(double size, {FontWeight weight = FontWeight.w700, Color color = _ink}) =>
    TextStyle(fontFamily: 'Tajawal', fontSize: size, fontWeight: weight, color: color);

/// "٣٢ سنة", "سنتين", "٨ شهور", "شهر", "حديث الولادة" (family-avatar.ts ageText).
String ageText(int? years, [int? months]) {
  if (years == null) return '';
  String ar(int n) => n.toString().split('').map((c) => '٠١٢٣٤٥٦٧٨٩'[int.parse(c)]).join();
  if (years < 1 && months != null) {
    if (months < 1) return 'حديث الولادة';
    if (months == 1) return 'شهر';
    if (months == 2) return 'شهرين';
    return '${ar(months)} ${months <= 10 ? 'شهور' : 'شهر'}';
  }
  if (years == 1) return 'سنة';
  if (years == 2) return 'سنتين';
  return '${ar(years)} ${years <= 10 ? 'سنين' : 'سنة'}';
}

/// Whole years and months from a yyyy-MM-dd birth date.
(int, int)? ageParts(String? birthDate) {
  if (birthDate == null || birthDate.length < 10) return null;
  final d = DateTime.tryParse(birthDate.substring(0, 10));
  if (d == null) return null;
  final now = DateTime.now();
  var months = (now.year - d.year) * 12 + (now.month - d.month);
  if (now.day < d.day) months--;
  if (months < 0) return null;
  return (months ~/ 12, months);
}

String _memberAge(FamilyMember m) {
  final p = ageParts(m.birthDate);
  return p != null ? ageText(p.$1, p.$2) : ageText(m.age);
}

String _prettyDate(String iso) {
  const months = ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];
  final d = DateTime.tryParse(iso.length >= 10 ? iso.substring(0, 10) : iso);
  return d == null ? '' : '${d.day} ${months[d.month - 1]} ${d.year}';
}

Widget _picture(String? imagePath, String auto, double size) => Container(
  width: size,
  height: size,
  clipBehavior: Clip.antiAlias,
  decoration: const BoxDecoration(
    shape: BoxShape.circle,
    color: Colors.white,
    boxShadow: [BoxShadow(color: Color(0x26784820), offset: Offset(0, 2), blurRadius: 6)],
  ),
  child: (imagePath ?? '').isNotEmpty ? NetImage(imagePath, width: size, height: size) : SvgPicture.asset(auto, fit: BoxFit.cover),
);

/// The server replaces the whole family list on each save.
Future<List<FamilyMember>> _saveAll(WidgetRef ref, List<Map<String, dynamic>> members) async {
  final saved = await ref.read(accountRepositoryProvider).saveFamily(members);
  ref.invalidate(familyProvider);
  return saved;
}

Future<bool> _confirm(BuildContext context, String text) async {
  final ok = await showDialog<bool>(
    context: context,
    builder: (c) => Directionality(
      textDirection: TextDirection.rtl,
      child: AlertDialog(
        backgroundColor: const Color(0xFFFFFCF8),
        title: Text(text, style: _t(18, weight: FontWeight.w800)),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(c, false),
            child: Text('إلغاء', style: _t(15)),
          ),
          TextButton(
            onPressed: () => Navigator.pop(c, true),
            child: Text('حذف', style: _t(15, color: const Color(0xFFD32F2F))),
          ),
        ],
      ),
    ),
  );
  return ok == true;
}

Future<void> removeFamilyMember(BuildContext context, WidgetRef ref, FamilyMember m) async {
  final all = await ref.read(familyProvider.future);
  if (!context.mounted) return;
  if (all.length <= 1) {
    showMessage(context, 'لازم يفضل فرد واحد على الأقل في الأسرة', error: true);
    return;
  }
  if (!await _confirm(context, 'حذف ${m.name ?? ''} من أفراد الأسرة؟')) return;
  try {
    await _saveAll(ref, [
      for (final x in all)
        if (x.id != m.id) x.toInput(),
    ]);
  } catch (e) {
    if (context.mounted) showMessage(context, e, error: true);
  }
}

/// "أفراد الأسرة" popup (family-sheet.component.ts): the list, or straight into the add form
/// when [member] is null. Opened on the root navigator so the tab bar never covers it.
Future<void> showFamilySheet(BuildContext context, WidgetRef ref, FamilyMember? member) => showModalBottomSheet<void>(
  context: context,
  useRootNavigator: true,
  isScrollControlled: true,
  backgroundColor: const Color(0xFFFFFCF8),
  barrierColor: const Color(0x6B2E1E12),
  constraints: const BoxConstraints(maxWidth: 560),
  shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(26))),
  builder: (_) => _FamilySheet(startAdding: member == null),
);

class _Draft {
  String name = '';
  String relation = '';
  String customRelation = '';
  String birthDate = '';
  String imagePath = '';
  int age = 30;
}

class _FamilySheet extends ConsumerStatefulWidget {
  const _FamilySheet({required this.startAdding});
  final bool startAdding;
  @override
  ConsumerState<_FamilySheet> createState() => _FamilySheetState();
}

class _FamilySheetState extends ConsumerState<_FamilySheet> {
  List<FamilyMember> _members = [];
  bool _loading = true;
  bool _editing = false;
  int _editIndex = -1;
  _Draft _draft = _Draft();
  final _name = TextEditingController();
  final _custom = TextEditingController();
  String _formError = '';
  String _error = '';
  bool _saving = false;
  bool _uploading = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _name.dispose();
    _custom.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    try {
      final rows = await ref.read(familyProvider.future);
      if (!mounted) return;
      setState(() {
        _members = rows;
        _loading = false;
      });
      if (widget.startAdding) _add();
    } catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = '$e'.replaceFirst('Exception: ', '');
        });
      }
    }
  }

  void _add() => setState(() {
    _editIndex = -1;
    _draft = _Draft();
    _name.text = '';
    _custom.text = '';
    _formError = '';
    _editing = true;
  });

  void _edit(int i) {
    final m = _members[i];
    final known = familyRelations.contains(m.relation ?? '');
    setState(() {
      _editIndex = i;
      _draft = _Draft()
        ..name = m.name ?? ''
        ..relation = (m.relation ?? '').isEmpty ? '' : (known ? m.relation! : 'أخرى')
        ..customRelation = (m.relation ?? '').isNotEmpty && !known ? m.relation! : ''
        ..birthDate = (m.birthDate ?? '').length >= 10 ? m.birthDate!.substring(0, 10) : ''
        ..imagePath = m.imagePath ?? ''
        ..age = m.age;
      _name.text = _draft.name;
      _custom.text = _draft.customRelation;
      _formError = '';
      _editing = true;
    });
  }

  Future<void> _remove(int i) async {
    final m = _members[i];
    if (_members.length <= 1) {
      setState(() => _error = 'لازم يفضل فرد واحد على الأقل في الأسرة');
      return;
    }
    if (!await _confirm(context, 'حذف ${m.name ?? ''} من أفراد الأسرة؟')) return;
    await _persist(
      [
        for (var k = 0; k < _members.length; k++)
          if (k != i) _members[k].toInput(),
      ],
      () {},
      (msg) => setState(() => _error = msg),
    );
  }

  Future<void> _persist(List<Map<String, dynamic>> list, VoidCallback done, void Function(String) fail) async {
    setState(() {
      _saving = true;
      _error = '';
    });
    try {
      final saved = await _saveAll(ref, list);
      if (!mounted) return;
      setState(() {
        _saving = false;
        _members = saved;
      });
      done();
    } catch (e) {
      if (!mounted) return;
      setState(() => _saving = false);
      fail('$e'.replaceFirst('Exception: ', ''));
    }
  }

  Future<void> _saveDraft() async {
    final name = _name.text.trim();
    if (name.isEmpty) return setState(() => _formError = 'اكتب الاسم');
    final custom = _custom.text.trim();
    final relation = _draft.relation == 'أخرى' ? (custom.isEmpty ? 'أخرى' : custom) : _draft.relation;
    final p = ageParts(_draft.birthDate);
    final member = {
      'name': name,
      'age': p?.$1 ?? _draft.age,
      'birthDate': _draft.birthDate.isEmpty ? null : _draft.birthDate,
      'relation': relation.isEmpty ? null : relation,
      'imagePath': _draft.imagePath.isEmpty ? null : _draft.imagePath,
    };
    final next = [for (final m in _members) m.toInput()];
    if (_editIndex >= 0) {
      next[_editIndex] = member;
    } else {
      next.add(member);
    }
    await _persist(next, () {
      if (!mounted) return;
      if (widget.startAdding) {
        Navigator.pop(context);
      } else {
        setState(() => _editing = false);
      }
    }, (msg) => setState(() => _formError = msg));
  }

  Future<void> _pickPhoto() async {
    final file = await ImagePicker().pickImage(source: ImageSource.gallery, maxWidth: 512, maxHeight: 512, imageQuality: 88);
    if (file == null) return;
    setState(() {
      _uploading = true;
      _formError = '';
    });
    try {
      final path = await ref.read(accountRepositoryProvider).uploadImage(file.path);
      if (mounted) setState(() => _draft.imagePath = path);
    } catch (e) {
      if (mounted) setState(() => _formError = '$e'.replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _uploading = false);
    }
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final current = DateTime.tryParse(_draft.birthDate) ?? DateTime(now.year - 30);
    final d = await showDatePicker(
      context: context,
      useRootNavigator: true,
      initialDate: current.isAfter(now) ? now : current,
      firstDate: DateTime(1900),
      lastDate: now,
      locale: const Locale('ar'),
    );
    if (d != null) {
      setState(() => _draft.birthDate = '${d.year}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}');
    }
  }

  String get _draftPicture {
    final p = ageParts(_draft.birthDate);
    final rel = _draft.relation == 'أخرى' ? _custom.text : _draft.relation;
    return familyAvatar(rel, p?.$1 ?? _draft.age);
  }

  Widget _iconBtn(
    String icon,
    VoidCallback onTap, {
    Color bg = const Color(0xFFF7EFE5),
    Color fg = const Color(0xFF4A3A2E),
    bool filled = false,
  }) => Tap(
    onTap: onTap,
    child: Container(
      width: 38,
      height: 38,
      alignment: Alignment.center,
      decoration: BoxDecoration(shape: BoxShape.circle, color: bg),
      child: FaIcon(icon, size: 18, color: fg, weight: 2.4, filled: filled),
    ),
  );

  Widget _primary(String label, VoidCallback? onTap, {String? icon}) => Opacity(
    opacity: onTap == null ? .6 : 1,
    child: Tap(
      onTap: onTap,
      child: Container(
        height: 50,
        alignment: Alignment.center,
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(25),
          gradient: const LinearGradient(colors: [Color(0xFFF36202), Color(0xFFFF8A12)]),
          boxShadow: const [BoxShadow(color: Color(0x47F36202), offset: Offset(0, 4), blurRadius: 12)],
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            if (icon != null) ...[FaIcon(icon, size: 20, color: Colors.white), const SizedBox(width: 8)],
            Text(
              label,
              style: _t(17, weight: FontWeight.w800, color: Colors.white),
            ),
          ],
        ),
      ),
    ),
  );

  InputDecoration _input(String hint) {
    final b = OutlineInputBorder(
      borderRadius: BorderRadius.circular(14),
      borderSide: const BorderSide(color: Color(0xFFE8D8C5), width: 1.5),
    );
    return InputDecoration(
      hintText: hint,
      hintStyle: _t(16, weight: FontWeight.w500, color: const Color(0xFFA89A8E)),
      filled: true,
      fillColor: Colors.white,
      isDense: true,
      contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 13),
      border: b,
      enabledBorder: b,
      focusedBorder: b.copyWith(borderSide: const BorderSide(color: Color(0xFFF59A3C), width: 1.5)),
    );
  }

  @override
  Widget build(BuildContext context) {
    final mq = MediaQuery.of(context);
    return MediaQuery(
      data: mq.copyWith(textScaler: Fa.textScale),
      child: Directionality(
        textDirection: TextDirection.rtl,
        child: ConstrainedBox(
          constraints: BoxConstraints(maxHeight: mq.size.height * .92),
          child: Padding(
            padding: EdgeInsets.only(bottom: mq.viewInsets.bottom),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Center(
                  child: Container(
                    width: 44,
                    height: 5,
                    margin: const EdgeInsets.only(top: 10),
                    decoration: BoxDecoration(color: const Color(0xFFE6D8C8), borderRadius: BorderRadius.circular(3)),
                  ),
                ),
                Container(
                  padding: const EdgeInsets.fromLTRB(18, 12, 18, 10),
                  decoration: const BoxDecoration(
                    border: Border(bottom: BorderSide(color: Color(0xFFF1E6D9))),
                  ),
                  child: Row(
                    children: [
                      if (_editing && !widget.startAdding)
                        _iconBtn('chevronRight', () => setState(() => _editing = false))
                      else if (!_editing)
                        Container(
                          width: 38,
                          height: 38,
                          alignment: Alignment.center,
                          decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0xFFFCE6CC)),
                          child: const FaIcon('group', size: 22, color: Fa.orange, filled: true),
                        ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Row(
                          children: [
                            Text(
                              _editing ? (_editIndex >= 0 ? 'تعديل فرد' : 'إضافة فرد جديد') : 'أفراد الأسرة',
                              style: _t(20, weight: FontWeight.w800),
                            ),
                            if (!_editing) ...[
                              const SizedBox(width: 8),
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 1),
                                decoration: BoxDecoration(color: const Color(0xFFFDEBD6), borderRadius: BorderRadius.circular(10)),
                                child: Text(
                                  '${_members.length}',
                                  style: _t(13, weight: FontWeight.w800, color: const Color(0xFFC45A00)),
                                ),
                              ),
                            ],
                          ],
                        ),
                      ),
                      _iconBtn('x', () => Navigator.pop(context)),
                    ],
                  ),
                ),
                Flexible(child: _editing ? _form() : _list()),
                if (!_editing)
                  Container(
                    padding: EdgeInsets.fromLTRB(18, 12, 18, 14 + mq.padding.bottom),
                    decoration: const BoxDecoration(
                      border: Border(top: BorderSide(color: Color(0xFFF1E6D9))),
                    ),
                    child: _primary('إضافة فرد جديد', _add, icon: 'plusCircle'),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _list() {
    if (_loading) {
      return Padding(
        padding: const EdgeInsets.all(18),
        child: Text(
          'جاري التحميل...',
          textAlign: TextAlign.center,
          style: _t(15, color: const Color(0xFF8A7A6E)),
        ),
      );
    }
    return ListView(
      shrinkWrap: true,
      padding: const EdgeInsets.fromLTRB(18, 14, 18, 14),
      children: [
        if (_error.isNotEmpty)
          Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: Text(_error, style: _t(14, color: const Color(0xFFB71C1C))),
          ),
        if (_members.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 18),
            child: Text(
              'لسه مفيش أفراد. ضيف أول فرد في أسرتك.',
              textAlign: TextAlign.center,
              style: _t(15, weight: FontWeight.w500, color: const Color(0xFF8A7A6E)),
            ),
          ),
        for (var i = 0; i < _members.length; i++) ...[
          if (i > 0) const SizedBox(height: 10),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(18),
              gradient: const LinearGradient(
                colors: [Color(0xFFFFF7EC), Color(0xFFFDF0DF)],
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
              ),
              border: Border.all(color: const Color(0xFFF4E2CB)),
            ),
            child: Row(
              children: [
                _picture(_members[i].imagePath, familyAvatar(_members[i].relation, _members[i].age), 56),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        _members[i].name ?? '',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: _t(17, weight: FontWeight.w800),
                      ),
                      const SizedBox(height: 4),
                      Wrap(
                        spacing: 6,
                        runSpacing: 4,
                        crossAxisAlignment: WrapCrossAlignment.center,
                        children: [
                          if ((_members[i].relation ?? '').isNotEmpty)
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 1),
                              decoration: BoxDecoration(color: const Color(0xFFE6F2DC), borderRadius: BorderRadius.circular(9)),
                              child: Text(
                                _members[i].relation!,
                                style: _t(13, weight: FontWeight.w800, color: const Color(0xFF3E7B22)),
                              ),
                            ),
                          if (_memberAge(_members[i]).isNotEmpty)
                            Text(_memberAge(_members[i]), style: _t(13, color: const Color(0xFF6B5B4E))),
                          if ((_members[i].birthDate ?? '').isNotEmpty)
                            Text(
                              '🎂 ${_prettyDate(_members[i].birthDate!)}',
                              style: _t(13, weight: FontWeight.w500, color: const Color(0xFF8A7A6E)),
                            ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 12),
                _iconBtn('pencil', () => _edit(i), bg: const Color(0xFFE8F3E0), fg: const Color(0xFF3E8E1E), filled: true),
                const SizedBox(width: 12),
                _iconBtn('trash', () => _remove(i), bg: const Color(0xFFFDECEA), fg: const Color(0xFFD32F2F)),
              ],
            ),
          ),
        ],
      ],
    );
  }

  Widget _form() {
    final p = ageParts(_draft.birthDate);
    final age = p == null ? '' : ageText(p.$1, p.$2);
    Widget field(String label, Widget child, {bool req = false}) => Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text.rich(
          TextSpan(
            children: [
              TextSpan(text: label),
              if (req)
                TextSpan(
                  text: ' *',
                  style: _t(14, weight: FontWeight.w800, color: const Color(0xFFD32F2F)),
                ),
            ],
          ),
          style: _t(14, weight: FontWeight.w800, color: const Color(0xFF4A3A2E)),
        ),
        const SizedBox(height: 8),
        child,
      ],
    );
    return ListView(
      shrinkWrap: true,
      padding: EdgeInsets.fromLTRB(18, 14, 18, 14 + MediaQuery.paddingOf(context).bottom),
      children: [
        Center(
          child: SizedBox(
            width: 108,
            height: 108,
            child: Stack(
              children: [
                Container(
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    border: Border.all(color: Colors.white, width: 4),
                    boxShadow: const [BoxShadow(color: Color(0x2E784820), offset: Offset(0, 4), blurRadius: 14)],
                  ),
                  child: _picture(_draft.imagePath, _draftPicture, 100),
                ),
                Positioned(
                  left: 0,
                  bottom: 2,
                  child: Tap(
                    onTap: _uploading ? null : _pickPhoto,
                    child: Opacity(
                      opacity: _uploading ? .6 : 1,
                      child: Container(
                        width: 36,
                        height: 36,
                        alignment: Alignment.center,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: const Color(0xFF3E8E1E),
                          border: Border.all(color: Colors.white, width: 3),
                        ),
                        child: const FaIcon('pencil', size: 15, color: Colors.white, filled: true),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 8),
        Wrap(
          alignment: WrapAlignment.center,
          spacing: 14,
          runSpacing: 6,
          children: [
            Tap(
              onTap: _uploading ? null : _pickPhoto,
              child: Text(
                _uploading ? 'جاري الرفع...' : (_draft.imagePath.isEmpty ? 'إضافة صورة' : 'تغيير الصورة'),
                style: _t(14, weight: FontWeight.w800, color: const Color(0xFFC45A00)),
              ),
            ),
            if (_draft.imagePath.isNotEmpty)
              Tap(
                onTap: () => setState(() => _draft.imagePath = ''),
                child: Text(
                  'إزالة الصورة',
                  style: _t(14, weight: FontWeight.w800, color: const Color(0xFFB71C1C)),
                ),
              ),
          ],
        ),
        if (_draft.imagePath.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 6),
            child: Text(
              'بدون صورة بنختار صورة تلقائية حسب القرابة والسن',
              textAlign: TextAlign.center,
              style: _t(12, weight: FontWeight.w500, color: const Color(0xFF8A7A6E)),
            ),
          ),
        const SizedBox(height: 14),
        field(
          'الاسم',
          req: true,
          TextField(
            controller: _name,
            maxLength: 60,
            style: _t(16, weight: FontWeight.w600),
            decoration: _input('مثلاً: سارة').copyWith(counterText: ''),
          ),
        ),
        const SizedBox(height: 14),
        field(
          'صلة القرابة',
          Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  for (final r in familyRelations)
                    Tap(
                      onTap: () => setState(() => _draft.relation = r),
                      child: Container(
                        height: 38,
                        padding: const EdgeInsets.symmetric(horizontal: 16),
                        alignment: Alignment.center,
                        decoration: BoxDecoration(
                          color: _draft.relation == r ? null : Colors.white,
                          gradient: _draft.relation == r ? const LinearGradient(colors: [Color(0xFFF36202), Color(0xFFFF8A12)]) : null,
                          borderRadius: BorderRadius.circular(19),
                          border: _draft.relation == r ? null : Border.all(color: const Color(0xFFEBDCCB), width: 1.5),
                          boxShadow: _draft.relation == r
                              ? const [BoxShadow(color: Color(0x40F36202), offset: Offset(0, 3), blurRadius: 8)]
                              : null,
                        ),
                        child: Text(r, style: _t(15, color: _draft.relation == r ? Colors.white : const Color(0xFF4A3A2E))),
                      ),
                    ),
                ],
              ),
              if (_draft.relation == 'أخرى') ...[
                const SizedBox(height: 8),
                TextField(
                  controller: _custom,
                  maxLength: 30,
                  onChanged: (_) => setState(() {}),
                  style: _t(16, weight: FontWeight.w600),
                  decoration: _input('اكتب صلة القرابة (مثلاً: عمة)').copyWith(counterText: ''),
                ),
              ],
            ],
          ),
        ),
        const SizedBox(height: 14),
        field(
          'تاريخ الميلاد',
          Tap(
            onTap: _pickDate,
            child: Container(
              height: 48,
              padding: const EdgeInsets.symmetric(horizontal: 14),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(color: const Color(0xFFE8D8C5), width: 1.5),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: Text(
                      _draft.birthDate.isEmpty ? 'اختار التاريخ' : _prettyDate(_draft.birthDate),
                      style: _t(16, weight: FontWeight.w600, color: _draft.birthDate.isEmpty ? const Color(0xFFA89A8E) : _ink),
                    ),
                  ),
                  const FaIcon('calendar', size: 20, color: Color(0xFF6B5B4E)),
                ],
              ),
            ),
          ),
        ),
        const SizedBox(height: 10),
        Row(
          children: [
            const FaIcon('calendar', size: 18, color: Fa.orange),
            const SizedBox(width: 8),
            Expanded(
              child: age.isEmpty
                  ? Text(
                      'العمر بيتحسب لوحده من تاريخ الميلاد',
                      style: _t(14, weight: FontWeight.w500, color: const Color(0xFF6B5B4E)),
                    )
                  : Text.rich(
                      TextSpan(
                        children: [
                          TextSpan(
                            text: 'العمر: ',
                            style: _t(14, weight: FontWeight.w500, color: const Color(0xFF6B5B4E)),
                          ),
                          TextSpan(
                            text: age,
                            style: _t(16, weight: FontWeight.w800),
                          ),
                        ],
                      ),
                    ),
            ),
          ],
        ),
        if (_formError.isNotEmpty) ...[const SizedBox(height: 10), Text(_formError, style: _t(14, color: const Color(0xFFB71C1C)))],
        const SizedBox(height: 14),
        Row(
          children: [
            Tap(
              onTap: () => widget.startAdding ? Navigator.pop(context) : setState(() => _editing = false),
              child: Container(
                height: 50,
                padding: const EdgeInsets.symmetric(horizontal: 22),
                alignment: Alignment.center,
                decoration: BoxDecoration(color: const Color(0xFFFBF1E4), borderRadius: BorderRadius.circular(25)),
                child: Text('إلغاء', style: _t(16, weight: FontWeight.w800)),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(child: _primary(_saving ? 'جاري الحفظ...' : 'حفظ', _saving || _uploading ? null : _saveDraft)),
          ],
        ),
      ],
    );
  }
}
