import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';
import 'package:material_ui/material_ui.dart';

import '../../../app/providers.dart';
import '../../../app/widgets/fa_chrome.dart';
import '../../../app/widgets/fa_widgets.dart';
import '../../../core/ui/fa_kit.dart';
import '../../../core/widgets/async_views.dart';
import '../../../core/widgets/net_image.dart';
import '../../auth/domain/auth_models.dart';
import '../../auth/presentation/auth_controller.dart';
import '../domain/account_models.dart';
import 'family_sheet.dart';
import '../../prices/prices.dart';
import '../../catalog/presentation/catalog_providers.dart';

final profileProvider = FutureProvider.autoDispose<Profile>((ref) => ref.watch(accountRepositoryProvider).profile());
final settingsProvider = FutureProvider.autoDispose<UserSettings>((ref) => ref.watch(accountRepositoryProvider).settings());
final familyProvider = FutureProvider.autoDispose<List<FamilyMember>>((ref) => ref.watch(accountRepositoryProvider).family());
final accountCountriesProvider = FutureProvider.autoDispose<List<Country>>((ref) => ref.watch(accountRepositoryProvider).countries());

const _ink = Color(0xFF2E1E12);
const _sub = Color(0xFF5E5148);
const _grey = Color(0xFF6B625C);
const _notifyIcons = {
  NotifyKey.notifyLogin: 'bell',
  NotifyKey.notifyWelcome: 'home',
  NotifyKey.notifyBirthday: 'cake',
  NotifyKey.notifyOrders: 'receipt',
  NotifyKey.notifyPlanning: 'calendar',
};

/// Same rules as the web/API (FamilyAvatar.For): relation gender + age band.
String familyAvatar(String? relation, int age) {
  final r = (relation ?? '').trim();
  if (r == 'جد') return 'assets/family/grandpa.svg';
  if (r == 'جدة') return 'assets/family/grandma.svg';
  const male = {'أب', 'زوج', 'ابن', 'أخ', 'جد'};
  const female = {'أم', 'زوجة', 'بنت', 'أخت', 'جدة'};
  final isMale = male.contains(r);
  if (!isMale && !female.contains(r)) return 'assets/family/person.svg';
  final band = age < 4 ? 0 : (age <= 15 ? 1 : (age <= 25 ? 2 : 3));
  const names = [('baby-boy', 'baby-girl'), ('boy', 'girl'), ('young-man', 'young-woman'), ('man', 'woman')];
  return 'assets/family/${isMale ? names[band].$1 : names[band].$2}.svg';
}

/// "الإعدادات" (settings.component.html). Canvas positions are 941px mockup units.
class SettingsPage extends ConsumerStatefulWidget {
  const SettingsPage({super.key});

  @override
  ConsumerState<SettingsPage> createState() => _SettingsPageState();
}

class _SettingsPageState extends ConsumerState<SettingsPage> {
  bool _countryOpen = false;
  /// "البلد اللي أنت فيها" list open (same inline dropdown as البلد).
  bool _liveOpen = false;
  UserSettings? _settings;

  Future<void> _toggle(UserSettings s, NotifyKey key) async {
    final before = s.flags[key] ?? false;
    setState(() => s.flags[key] = !before);
    try {
      await ref.read(accountRepositoryProvider).saveSettings(s);
    } catch (e) {
      setState(() => s.flags[key] = before);
      if (mounted) showMessage(context, e, error: true);
    }
  }

  Future<void> _togglePrices(UserSettings s) async {
    final before = s.usePrices;
    setState(() => s.usePrices = !before);
    try {
      await ref.read(pricesApiProvider).setEnabled(!before);
      ref.read(pricesVersionProvider.notifier).bump();
    } catch (e) {
      setState(() => s.usePrices = before);
      if (mounted) showMessage(context, e, error: true);
    }
  }

  /// The dishes, their order and the browse lists change with the country, so load them again.
  void _reloadCatalog() {
    ref.invalidate(mealsProvider);
    ref.invalidate(cuisinesProvider);
    ref.invalidate(categoriesProvider);
    ref.invalidate(groupsProvider);
  }

  Future<void> _pickPriceCountry(String id, PriceCountry current) async {
    if (id == current.countryId) return;
    try {
      await ref.read(pricesApiProvider).setCountry(id == current.ownCountryId ? null : id);
      ref.read(pricesVersionProvider.notifier).bump();
      _reloadCatalog();
    } catch (e) {
      if (mounted) showMessage(context, e, error: true);
    }
  }

  Future<void> _pickCountry(Country c, String? current) async {
    setState(() => _countryOpen = false);
    if (c.id == current) return;
    try {
      await ref.read(accountRepositoryProvider).saveCountry(c.id);
      ref.invalidate(profileProvider);
      ref.read(pricesVersionProvider.notifier).bump();
      _reloadCatalog();
    } catch (e) {
      if (mounted) showMessage(context, e, error: true);
    }
  }

  // Root navigator so the floating tab bar never covers the sheet.
  void _openAccount(Profile? profile) => showModalBottomSheet<void>(
    context: context,
    useRootNavigator: true,
    isScrollControlled: true,
    barrierColor: const Color(0x6B2E1E12),
    constraints: const BoxConstraints(maxWidth: 560),
    backgroundColor: const Color(0xFFFFFCF8),
    shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(22))),
    builder: (_) => _AccountSheet(profile: profile),
  );

  Future<void> _logout() async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (c) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          backgroundColor: const Color(0xFFFFFCF8),
          title: Text('تسجيل الخروج؟', style: Fa.text(20, weight: FontWeight.w800)),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(c, false),
              child: Text('لا', style: Fa.text(16, color: _ink)),
            ),
            TextButton(
              onPressed: () => Navigator.pop(c, true),
              child: Text('خروج', style: Fa.text(16, color: const Color(0xFFB71C1C))),
            ),
          ],
        ),
      ),
    );
    if (ok == true) ref.read(authControllerProvider.notifier).logout();
  }

  @override
  Widget build(BuildContext context) {
    final s = context.s;
    final u = context.u;
    final profile = ref.watch(profileProvider).value;
    final auth = ref.watch(authControllerProvider);
    final settings = ref.watch(settingsProvider).value;
    if (settings != null && !identical(settings, _settings)) _settings = settings;
    final family = ref.watch(familyProvider).value ?? const <FamilyMember>[];
    final countries = ref.watch(accountCountriesProvider).value ?? const <Country>[];
    final priceCountry = ref.watch(priceCountryProvider).value;
    final photo = auth.photo ?? '';
    final name = profile?.name ?? auth.name;
    final countryName = profile?.countryName ?? '';
    final canvasTop = u(128) + s(65);

    BoxDecoration card() => BoxDecoration(
      color: const Color(0xFFFFFCF8),
      borderRadius: BorderRadius.circular(s(26)),
      border: Border.all(color: const Color(0xFFF5E8D8), width: s(1.5)),
      boxShadow: [BoxShadow(color: const Color(0x17784820), offset: Offset(0, s(5)), blurRadius: s(18))],
    );
    Widget chev() => Padding(
      padding: EdgeInsets.only(left: s(1)),
      child: FaIcon('chevronLeft', size: s(34), color: _grey),
    );

    // language switch
    final lang = Container(
      width: s(320),
      height: s(48),
      decoration: BoxDecoration(
        color: const Color(0xFFFBF1E4),
        borderRadius: BorderRadius.circular(s(24)),
        border: Border.all(color: const Color(0xFFF1E2CF), width: s(1.5)),
      ),
      child: Row(
        children: [
          Expanded(
            child: Container(
              alignment: Alignment.center,
              decoration: BoxDecoration(
                gradient: const LinearGradient(colors: [Color(0xFFF36202), Color(0xFFFF8A12)]),
                borderRadius: BorderRadius.circular(s(24)),
                boxShadow: [BoxShadow(color: const Color(0x4DF36202), offset: Offset(0, s(4)), blurRadius: s(10))],
              ),
              child: FittedBox(fit: BoxFit.scaleDown, child: Text('العربية', style: Fa.text(s(25), color: Colors.white))),
            ),
          ),
          Expanded(
            child: Opacity(
              opacity: .55,
              child: FittedBox(
                fit: BoxFit.scaleDown,
                child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                textDirection: TextDirection.ltr,
                children: [
                  Text('English', style: Fa.text(s(25), color: _ink)),
                  SizedBox(width: s(6)),
                  Text(
                    'قريباً',
                    style: Fa.text(s(17), weight: FontWeight.w600, color: const Color(0xFF8A6E58)),
                  ),
                ],
              ),
              ),
            ),
          ),
        ],
      ),
    );

    // The country picker box (flag + name + arrow); البلد and البلد اللي أنت فيها look the same.
    Widget countryBox(String name, VoidCallback onTap) => Tap(
      onTap: onTap,
      child: Container(
        width: s(322),
        height: s(53),
        padding: EdgeInsets.only(right: s(14), left: s(22)),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(s(22)),
          border: Border.all(color: const Color(0xFFE8D8C5), width: s(1.5)),
        ),
        child: Row(
          children: [
            if (name == 'مصر')
              ClipOval(
                child: Image.asset('assets/screens/settings/flag-eg.png', width: s(38), height: s(38), fit: BoxFit.cover),
              )
            else
              FaIcon('pin', size: s(30), color: Fa.orange),
            SizedBox(width: s(14)),
            Expanded(
              child: Text(
                name.isEmpty ? 'اختار البلد' : name,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: Fa.text(s(27), color: _ink),
              ),
            ),
            FaIcon('chevronDown', size: s(32), color: const Color(0xFF4A3A2E), weight: 2.2),
          ],
        ),
      ),
    );
    final country = countryBox(countryName, () => setState(() {
      _countryOpen = !_countryOpen;
      _liveOpen = false;
    }));
    final liveCountry = countryBox(priceCountry?.countryName ?? countryName, () {
      if (priceCountry == null) return;
      setState(() {
        _liveOpen = !_liveOpen;
        _countryOpen = false;
      });
    });

    Widget sw(bool on, VoidCallback onTap) => Tap(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        width: s(78),
        height: s(42),
        decoration: BoxDecoration(
          color: on ? null : const Color(0xFFE4D9CC),
          gradient: on ? const LinearGradient(colors: [Color(0xFFFF8A12), Color(0xFFF36202)]) : null,
          borderRadius: BorderRadius.circular(s(21)),
        ),
        child: Stack(
          children: [
            AnimatedPositioned(
              duration: const Duration(milliseconds: 200),
              top: s(4),
              left: on ? s(40) : s(4),
              child: Container(
                width: s(34),
                height: s(34),
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: Colors.white,
                  boxShadow: [BoxShadow(color: const Color(0x2E000000), offset: Offset(0, s(2)), blurRadius: s(5))],
                ),
              ),
            ),
          ],
        ),
      ),
    );

    // The countries the customer can be in (own country first, then its neighbors), same look as the البلد list.
    final liveMenu = priceCountry == null
        ? const SizedBox.shrink()
        : Container(
            constraints: BoxConstraints(maxHeight: s(420)),
            margin: EdgeInsets.fromLTRB(s(25), 0, s(23), s(14)),
            padding: EdgeInsets.all(s(8)),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(s(18)),
              border: Border.all(color: const Color(0xFFE8D8C5), width: s(1.5)),
              boxShadow: [BoxShadow(color: const Color(0x2E784820), offset: Offset(0, s(10)), blurRadius: s(28))],
            ),
            child: ListView(
              shrinkWrap: true,
              padding: EdgeInsets.zero,
              children: [
                for (final c in priceCountry.countries)
                  Tap(
                    onTap: () {
                      setState(() => _liveOpen = false);
                      _pickPriceCountry(c.id, priceCountry);
                    },
                    child: Container(
                      padding: EdgeInsets.symmetric(horizontal: s(14), vertical: s(10)),
                      decoration: BoxDecoration(
                        color: c.id == priceCountry.countryId ? const Color(0xFFFEF1E2) : null,
                        borderRadius: BorderRadius.circular(s(12)),
                      ),
                      child: Row(
                        children: [
                          Expanded(
                            child: Text(
                              c.id == priceCountry.ownCountryId ? '${c.name ?? ''} (بلدي)' : (c.name ?? ''),
                              style: Fa.text(s(25), color: c.id == priceCountry.countryId ? const Color(0xFFC45A00) : _ink),
                            ),
                          ),
                          Text(c.currency == null || c.currency == 'ج.م' ? 'جنيه' : c.currency!, style: Fa.text(s(19), weight: FontWeight.w500, color: const Color(0xFF8A7A6E))),
                        ],
                      ),
                    ),
                  ),
              ],
            ),
          );

    final rows = NotifyKey.values;
    final about = [('layers', 'الإصدار', '1.0.0', false), ('shield', 'سياسة الخصوصية', null, true), ('doc', 'الشروط والأحكام', null, true)];
    final members = [for (final m in family) (m, (m.imagePath ?? '').isNotEmpty ? null : familyAvatar(m.relation, m.age))];

    // ---- flow layout: every row grows with its text, so the three font sizes all fit ----
    Widget secF(String icon, String text, {bool filled = true, String? sub}) => Padding(
      padding: EdgeInsets.only(right: s(54), left: s(50), top: s(30), bottom: s(14)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: s(54),
                height: s(54),
                alignment: Alignment.center,
                decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0xFFFCE6CC)),
                child: FaIcon(icon, size: s(34), color: Fa.orange, filled: filled, weight: 2),
              ),
              SizedBox(width: s(14)),
              Expanded(child: Text(text, style: Fa.text(s(31), weight: FontWeight.w800, color: _ink))),
            ],
          ),
          if (sub != null)
            Padding(
              padding: EdgeInsets.only(right: s(68), top: s(4)),
              child: Text(sub, style: Fa.text(s(21), weight: FontWeight.w400, color: _sub, height: 1.3)),
            ),
        ],
      ),
    );
    Widget cardF(Widget child, {double left = 50, double right = 53, VoidCallback? onTap}) => Padding(
      padding: EdgeInsets.only(left: s(left), right: s(right)),
      child: Tap(
        onTap: onTap,
        child: Container(decoration: card(), child: child),
      ),
    );
    Widget hlF() => Container(height: s(1.5), margin: EdgeInsets.only(left: s(25), right: s(23)), color: const Color(0xFFEEE2D4));
    // A row inside a card: icon at the right, the label, and a trailing widget at the left.
    Widget rowF(String icon, Widget lbl, {Widget? trailing, VoidCallback? onTap, bool filledIcon = false, double opacity = 1}) => Opacity(
      opacity: opacity,
      child: Tap(
        onTap: onTap,
        child: ConstrainedBox(
          constraints: BoxConstraints(minHeight: s(72)),
          child: Padding(
            padding: EdgeInsets.fromLTRB(s(35), s(10), s(31), s(10)),
            child: Row(
              children: [
                SizedBox(width: s(44), child: Center(child: FaIcon(icon, size: s(40), color: Fa.orange, weight: 1.8, filled: filledIcon))),
                SizedBox(width: s(35)),
                Expanded(child: Align(alignment: AlignmentDirectional.centerStart, child: lbl)),
                if (trailing != null) ...[SizedBox(width: s(14)), trailing],
              ],
            ),
          ),
        ),
      ),
    );
    Widget labelF(String t, {String? small}) => Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(t, style: Fa.text(s(28), weight: FontWeight.w800, color: _ink, height: 1.25)),
        if (small != null) Text(small, style: Fa.text(s(21), weight: FontWeight.w500, color: _sub, height: 1.3)),
      ],
    );

    final countryMenu = Container(
      constraints: BoxConstraints(maxHeight: s(420)),
      margin: EdgeInsets.fromLTRB(s(25), 0, s(23), s(14)),
      padding: EdgeInsets.all(s(8)),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(s(18)),
        border: Border.all(color: const Color(0xFFE8D8C5), width: s(1.5)),
        boxShadow: [BoxShadow(color: const Color(0x2E784820), offset: Offset(0, s(10)), blurRadius: s(28))],
      ),
      child: ListView(
        shrinkWrap: true,
        padding: EdgeInsets.zero,
        children: [
          for (final c in countries)
            Tap(
              onTap: () => _pickCountry(c, profile?.countryId),
              child: Container(
                padding: EdgeInsets.symmetric(horizontal: s(14), vertical: s(10)),
                decoration: BoxDecoration(
                  color: c.id == profile?.countryId ? const Color(0xFFFEF1E2) : null,
                  borderRadius: BorderRadius.circular(s(12)),
                ),
                child: Row(
                  children: [
                    Expanded(
                      child: Text(c.name ?? '', style: Fa.text(s(25), color: c.id == profile?.countryId ? const Color(0xFFC45A00) : _ink)),
                    ),
                    Text(c.dialect ?? '', style: Fa.text(s(19), weight: FontWeight.w500, color: const Color(0xFF8A7A6E))),
                  ],
                ),
              ),
            ),
        ],
      ),
    );

    // حجم الخط: four levels, each shown as a letter at its size; the whole app follows.
    // The picker itself never grows with the setting (fixed scale) and shrinks to fit the row.
    final level = ref.watch(fontLevelProvider);
    final fontPicker = FittedBox(
      fit: BoxFit.scaleDown,
      child: MediaQuery.withNoTextScaling(
      child: Container(
      padding: EdgeInsets.all(s(4)),
      decoration: BoxDecoration(
        color: const Color(0xFFFBF1E4),
        borderRadius: BorderRadius.circular(s(26)),
        border: Border.all(color: const Color(0xFFF1E2CF), width: s(1.5)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          for (var i = 0; i < FontLevel.labels.length; i++)
            Tap(
              onTap: () => ref.read(fontLevelProvider.notifier).set(i),
              child: Container(
                constraints: BoxConstraints(minWidth: s(58), minHeight: s(48)),
                padding: EdgeInsets.symmetric(horizontal: s(12), vertical: s(4)),
                alignment: Alignment.center,
                decoration: i == level
                    ? BoxDecoration(
                        gradient: const LinearGradient(colors: [Color(0xFFF36202), Color(0xFFFF8A12)]),
                        borderRadius: BorderRadius.circular(s(22)),
                        boxShadow: [BoxShadow(color: const Color(0x4DF36202), offset: Offset(0, s(4)), blurRadius: s(10))],
                      )
                    : null,
                child: Semantics(
                  label: FontLevel.labels[i],
                  child: Text(
                    'أ',
                    style: Fa.text(s(17 + i * 5.0), weight: FontWeight.w800, color: i == level ? Colors.white : _ink, height: 1.1),
                  ),
                ),
              ),
            ),
        ],
      ),
      ),
    ),
    );

    final canvas = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // profile
        secF('person', 'الملف الشخصي'),
        cardF(
          onTap: () => _openAccount(profile),
          Padding(
            padding: EdgeInsets.fromLTRB(s(55), s(8), s(36), s(8)),
            child: Row(
              children: [
                FaIcon('chevronRight', size: s(36), color: _grey),
                SizedBox(width: s(46)),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(name.isEmpty ? '—' : name, maxLines: 2, overflow: TextOverflow.ellipsis, style: Fa.text(s(36), weight: FontWeight.w800, color: _ink, height: 1.2)),
                      if ((profile?.userName ?? '').isNotEmpty)
                        Text(profile!.userName!, maxLines: 1, overflow: TextOverflow.ellipsis, textDirection: TextDirection.ltr, style: Fa.text(s(26), weight: FontWeight.w400, color: _grey)),
                    ],
                  ),
                ),
                Container(width: s(2), height: s(92), margin: EdgeInsets.symmetric(horizontal: s(18)), color: const Color(0xFFE7DACB)),
                SizedBox(
                  width: s(116),
                  height: s(116),
                  child: Stack(
                    clipBehavior: Clip.none,
                    children: [
                      Container(
                        width: s(116),
                        height: s(116),
                        alignment: Alignment.center,
                        clipBehavior: Clip.antiAlias,
                        decoration: const BoxDecoration(
                          shape: BoxShape.circle,
                          gradient: RadialGradient(colors: [Color(0xFFFDEBD3), Color(0xFFFBD9B0)], center: Alignment(0, -.1)),
                        ),
                        child: photo.isEmpty ? FaIcon('person', size: s(66), color: Fa.orange, filled: true) : NetImage(photo, width: s(116), height: s(116)),
                      ),
                      Positioned(
                        right: s(-8),
                        bottom: s(-2),
                        child: Container(
                          width: s(40),
                          height: s(40),
                          alignment: Alignment.center,
                          decoration: BoxDecoration(
                            shape: BoxShape.circle,
                            color: const Color(0xFF3E8E1E),
                            border: Border.all(color: Colors.white, width: s(3)),
                          ),
                          child: FaIcon('pencil', size: s(20), color: Colors.white, filled: true),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),

        // app settings
        secF('gear', 'إعدادات التطبيق'),
        cardF(
          Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              rowF('globe', labelF('اللغة'), trailing: lang),
              hlF(),
              rowF('pin', labelF('البلد', small: 'اللهجة وأسماء الأكلات والمكونات'), trailing: country),
              if (_countryOpen) countryMenu,
              hlF(),
              // The country the customer is in: dishes, recipe versions, order, search and prices follow it (not the dialect).
              rowF(
                'home',
                labelF('البلد اللي أنت فيها', small: 'الأكلات والترتيب والبحث والأسعار'),
                trailing: liveCountry,
              ),
              if (_liveOpen) liveMenu,
              hlF(),
              rowF('type', labelF('حجم الخط', small: 'أربع مقاسات من الأصغر للأكبر'), trailing: fontPicker),
              hlF(),
              rowF('idCard', labelF('بيانات الحساب', small: 'الاسم واسم المستخدم'), trailing: chev(), onTap: () => _openAccount(profile)),
            ],
          ),
        ),

        // family
        secF('group', 'أفراد أسرتي', sub: 'أدر أفراد أسرتك لتجربة مخصصة وأفضل في FOODS'),
        cardF(
          left: 45,
          right: 48,
          Padding(
            padding: EdgeInsets.only(top: s(19), bottom: s(18)),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (members.isNotEmpty)
                  SizedBox(
                    height: s(150),
                    child: LayoutBuilder(
                      builder: (context, c) {
                        final inner = c.maxWidth - s(18) - s(23);
                        final fits = members.length * s(170) + (members.length - 1) * s(16) <= inner;
                        final w = fits ? (inner - (members.length - 1) * s(16)) / members.length : s(170);
                        return ListView.separated(
                          scrollDirection: Axis.horizontal,
                          padding: EdgeInsets.only(right: s(18), left: s(23)),
                          itemCount: members.length,
                          separatorBuilder: (_, _) => SizedBox(width: s(16)),
                          itemBuilder: (_, i) => _member(context, members[i].$1, members[i].$2, w),
                        );
                      },
                    ),
                  ),
                if (members.isNotEmpty) SizedBox(height: s(20)),
                Padding(
                  padding: EdgeInsets.only(left: s(25), right: s(22)),
                  child: Tap(
                    onTap: () => showFamilySheet(context, ref, null),
                    child: Container(
                      constraints: BoxConstraints(minHeight: s(60)),
                      padding: EdgeInsets.symmetric(vertical: s(6)),
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(s(14)),
                        gradient: const LinearGradient(colors: [Color(0xFF7AA83F), Color(0xFF557F28)], begin: Alignment.topCenter, end: Alignment.bottomCenter),
                        boxShadow: [BoxShadow(color: const Color(0x40466E1E), offset: Offset(0, s(4)), blurRadius: s(10))],
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          FaIcon('plusCircle', size: s(38), color: Colors.white),
                          SizedBox(width: s(16)),
                          Flexible(child: Text('إضافة فرد جديد', style: Fa.text(s(28), color: Colors.white))),
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),

        // prices
        secF('tag', 'الأسعار', sub: 'سعر الأكلة والطلب بيتحسب من أسعار المكونات'),
        cardF(
          Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              rowF(
                'coins',
                labelF('التعامل بالأسعار', small: 'يظهر السعر في الأكلات والمفضلات والطلبات'),
                trailing: _settings == null ? null : sw(_settings!.usePrices, () => _togglePrices(_settings!)),
              ),
              hlF(),
              rowF('tag', labelF('أسعار المكونات', small: 'غيّر سعر أي مكون ليك انت بس'), trailing: chev(), onTap: () => context.push('/prices')),
            ],
          ),
        ),

        // notifications
        secF('bell', 'الإشعارات'),
        cardF(
          Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              for (var i = 0; i < rows.length; i++) ...[
                rowF(
                  _notifyIcons[rows[i]]!,
                  labelF(rows[i].label, small: rows[i].sub),
                  filledIcon: rows[i] == NotifyKey.notifyLogin,
                  trailing: _settings == null ? null : sw(_settings!.flags[rows[i]] ?? false, () => _toggle(_settings!, rows[i])),
                ),
                hlF(),
              ],
              rowF('list', labelF('كل الإشعارات'), trailing: chev(), onTap: () => context.push('/notifications')),
            ],
          ),
        ),

        // about
        secF('info', 'عن التطبيق', filled: false),
        cardF(
          Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              for (var i = 0; i < about.length; i++) ...[
                rowF(
                  about[i].$1,
                  labelF(about[i].$2),
                  opacity: about[i].$4 ? .6 : 1,
                  trailing: about[i].$4
                      ? Container(
                          padding: EdgeInsets.symmetric(horizontal: s(14), vertical: s(4)),
                          decoration: BoxDecoration(color: const Color(0xFFFBF1E4), borderRadius: BorderRadius.circular(s(14))),
                          child: Text('قريباً', style: Fa.text(s(19), color: const Color(0xFF8A6E58))),
                        )
                      : (about[i].$3 == null ? null : Text(about[i].$3!, textDirection: TextDirection.ltr, style: Fa.text(s(23), weight: FontWeight.w500, color: _grey))),
                ),
                if (i < about.length - 1) hlF(),
              ],
            ],
          ),
        ),

        // logout
        Padding(
          padding: EdgeInsets.fromLTRB(s(49), s(32), s(48), s(10)),
          child: Tap(
            onTap: _logout,
            child: Container(
              constraints: BoxConstraints(minHeight: s(71)),
              padding: EdgeInsets.symmetric(vertical: s(6)),
              decoration: BoxDecoration(
                color: const Color(0xFFFDE8E4),
                borderRadius: BorderRadius.circular(s(36)),
                border: Border.all(color: const Color(0xFFB71C1C), width: s(2.5)),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  FaIcon('logout', size: s(40), color: const Color(0xFFB71C1C)),
                  SizedBox(width: s(16)),
                  Text('تسجيل الخروج', style: Fa.text(s(32), color: const Color(0xFFB71C1C))),
                ],
              ),
            ),
          ),
        ),
      ],
    );

    return Directionality(
      textDirection: TextDirection.rtl,
      child: ColoredBox(
        color: const Color(0xFFFDF8F3),
        child: SafeArea(
          bottom: false,
          child: RefreshIndicator(
            onRefresh: () async {
              ref.invalidate(profileProvider);
              ref.invalidate(settingsProvider);
              ref.invalidate(familyProvider);
            },
            child: ListView(
              padding: EdgeInsets.only(bottom: faBottomSpace(context, ref)),
              children: [
                SizedBox(
                  height: canvasTop + s(167),
                  child: const Stack(
                    clipBehavior: Clip.none,
                    children: [
                      Positioned(
                        left: 0,
                        right: 0,
                        top: 0,
                        child: FaScreenHeader(
                          title: 'الإعدادات',
                          subtitle: 'خصص حسابك وتفضيلات التطبيق وأفراد أسرتك',
                          art: 'assets/screens/art/settings.svg',
                          titleIcon: 'gear',
                          titleFilled: true,
                        ),
                      ),
                    ],
                  ),
                ),
                canvas,
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _member(BuildContext context, FamilyMember m, String? fallback, double width) {
    final s = context.s;
    Widget mini(String icon, Color color, VoidCallback onTap, {bool filled = false}) => Tap(
      onTap: onTap,
      child: Container(
        width: s(38),
        height: s(38),
        alignment: Alignment.center,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          color: Colors.white,
          boxShadow: [BoxShadow(color: const Color(0x1F784820), offset: Offset(0, s(2)), blurRadius: s(6))],
        ),
        child: FaIcon(icon, size: s(22), color: color, filled: filled),
      ),
    );
    return Container(
      width: width,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(s(22)),
        gradient: const LinearGradient(colors: [Color(0xFFFFF5E8), Color(0xFFFDEFDD)], begin: Alignment.topCenter, end: Alignment.bottomCenter),
        border: Border.all(color: const Color(0xFFF6E4CD), width: s(1.5)),
        boxShadow: [BoxShadow(color: const Color(0x0F784820), offset: Offset(0, s(3)), blurRadius: s(10))],
      ),
      child: Stack(
        children: [
          Positioned(
            right: s(37),
            top: s(8),
            child: Container(
              width: s(90),
              height: s(90),
              clipBehavior: Clip.antiAlias,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: Colors.white,
                boxShadow: [BoxShadow(color: const Color(0x1F784820), offset: Offset(0, s(2)), blurRadius: s(6))],
              ),
              child: fallback != null ? SvgPicture.asset(fallback, fit: BoxFit.cover) : NetImage(m.imagePath, width: s(90), height: s(90)),
            ),
          ),
          Positioned(
            right: s(37),
            width: s(90),
            top: s(102),
            child: FittedBox(
              fit: BoxFit.scaleDown,
              child: Text(
                m.name ?? '',
                maxLines: 1,
                softWrap: false,
                textAlign: TextAlign.center,
                style: Fa.text(s(27), weight: FontWeight.w800, color: _ink),
              ),
            ),
          ),
          Positioned(left: s(14), top: s(10), child: mini('trash', const Color(0xFFD32F2F), () => removeFamilyMember(context, ref, m))),
          Positioned(left: s(69), top: s(10), child: mini('pencil', const Color(0xFF3E8E1E), () => showFamilySheet(context, ref, m), filled: true)),
        ],
      ),
    );
  }
}

class _AccountSheet extends ConsumerStatefulWidget {
  const _AccountSheet({this.profile});
  final Profile? profile;
  @override
  ConsumerState<_AccountSheet> createState() => _AccountSheetState();
}

class _AccountSheetState extends ConsumerState<_AccountSheet> {
  late final _name = TextEditingController(text: widget.profile?.name ?? ref.read(authControllerProvider).name);
  late String? _photo = ref.read(authControllerProvider).photo;
  bool _busy = false;

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  Future<void> _pickPhoto() async {
    final file = await ImagePicker().pickImage(source: ImageSource.gallery, maxWidth: 512, maxHeight: 512, imageQuality: 85);
    if (file == null) return;
    setState(() => _busy = true);
    try {
      final path = await ref.read(accountRepositoryProvider).uploadImage(file.path);
      setState(() => _photo = path);
    } catch (e) {
      if (mounted) showMessage(context, e, error: true);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _save() async {
    final name = _name.text.trim();
    if (name.isEmpty) return;
    setState(() => _busy = true);
    try {
      final repo = ref.read(accountRepositoryProvider);
      final auth = ref.read(authControllerProvider.notifier);
      if (_photo != ref.read(authControllerProvider).photo) {
        await repo.savePhoto(_photo);
        await auth.updatePhoto(_photo);
      }
      await repo.saveName(name);
      await auth.updateName(name);
      ref.invalidate(profileProvider);
      if (mounted) Navigator.pop(context);
    } catch (e) {
      if (mounted) showMessage(context, e, error: true);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final mq = MediaQuery.of(context);
    return MediaQuery(
      data: mq.copyWith(textScaler: Fa.textScale),
      child: Padding(
        padding: EdgeInsets.fromLTRB(20, 18, 20, mq.viewInsets.bottom + mq.padding.bottom + 20),
        child: Directionality(
          textDirection: TextDirection.rtl,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('بيانات الحساب', style: Fa.text(19, weight: FontWeight.w800)),
                const SizedBox(height: 16),
                Center(
                  child: Stack(
                    children: [
                      CircleAvatar(
                        radius: 46,
                        backgroundColor: const Color(0xFFFFE7CC),
                        child: ClipOval(
                          child: (_photo ?? '').isEmpty
                              ? const Icon(Icons.person_rounded, size: 54, color: Fa.orange)
                              : NetImage(_photo, width: 92, height: 92),
                        ),
                      ),
                      PositionedDirectional(
                        bottom: 0,
                        end: 0,
                        child: IconButton.filled(
                          onPressed: _busy ? null : _pickPhoto,
                          style: IconButton.styleFrom(backgroundColor: Fa.orange),
                          icon: const Icon(Icons.photo_camera_rounded, color: Colors.white, size: 20),
                        ),
                      ),
                    ],
                  ),
                ),
                if ((_photo ?? '').isNotEmpty) TextButton(onPressed: _busy ? null : () => setState(() => _photo = null), child: const Text('إزالة الصورة')),
                const SizedBox(height: 12),
                TextField(
                  controller: _name,
                  maxLength: 100,
                  decoration: const InputDecoration(labelText: 'الاسم'),
                ),
                TextField(
                  enabled: false,
                  controller: TextEditingController(text: widget.profile?.userName ?? ''),
                  decoration: const InputDecoration(labelText: 'اسم المستخدم'),
                ),
                const SizedBox(height: 18),
                Opacity(
                  opacity: _busy ? .6 : 1,
                  child: FaCta(label: _busy ? 'جاري الحفظ...' : 'حفظ', onTap: _busy ? () {} : _save, height: 110, fontSize: 40),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
