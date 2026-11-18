/// The currency of the country the customer lives in (settings), shown after every amount.
/// The default "ج.م" is written as people say it: "جنيه".
String _currency = 'جنيه';

String get faCurrency => _currency;

void setFaCurrency(String? code) {
  final c = (code ?? '').trim();
  _currency = c.isEmpty || c == 'ج.م' ? 'جنيه' : c;
}
