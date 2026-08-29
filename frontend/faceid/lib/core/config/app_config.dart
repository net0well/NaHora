/// Build-time configuration.
///
/// Values arrive through --dart-define, never as literals in source: a base URL
/// or key committed to the repository is a finding under OWASP Mobile M1.
///
/// ```
/// flutter run -d windows --dart-define=API_BASE_URL=https://api.faceid.local
/// ```
abstract final class AppConfig {
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5080',
  );

  static const bool isProduction = bool.fromEnvironment('PRODUCTION');

  static const Duration requestTimeout = Duration(seconds: 30);

  /// HTTPS is mandatory outside development (OWASP Mobile M5). Enforced at
  /// startup so a misconfigured build fails immediately instead of shipping
  /// credentials over plaintext.
  static bool get isTransportSecure =>
      !isProduction || apiBaseUrl.startsWith('https://');
}
