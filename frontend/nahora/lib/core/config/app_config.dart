/// Build-time configuration.
///
/// Values arrive through --dart-define, never as literals in source: a base URL
/// or key committed to the repository is a finding under OWASP Mobile M1.
///
/// ```
/// flutter run --dart-define=API_BASE_URL=https://api.nahora.local
/// ```
abstract final class AppConfig {
  /// Where the API lives.
  ///
  /// The default is the Android emulator's alias for the host machine.
  /// `localhost` inside the emulator is the emulated device itself, so a build
  /// left on it reaches nothing and the failure looks like the server is down.
  ///
  /// On a physical handset this has to be the machine's address on the network:
  /// `--dart-define=API_BASE_URL=http://192.168.0.10:5080`.
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5080',
  );

  /// The company to sign into, when the app was not opened through a link that
  /// names one.
  ///
  /// Signing in is per company, so without one the login screen has nothing to
  /// authenticate against. Empty by default and supplied per build — it is an
  /// identifier, not a secret, but a company baked into a released binary is
  /// still the wrong company for every other customer.
  static const String defaultCompanyId = String.fromEnvironment(
    'DEFAULT_COMPANY_ID',
  );

  static const bool isProduction = bool.fromEnvironment('PRODUCTION');

  static const Duration requestTimeout = Duration(seconds: 30);

  /// HTTPS is mandatory outside development (OWASP Mobile M5). Enforced at
  /// startup so a misconfigured build fails immediately instead of shipping
  /// credentials over plaintext.
  static bool get isTransportSecure =>
      !isProduction || apiBaseUrl.startsWith('https://');
}
