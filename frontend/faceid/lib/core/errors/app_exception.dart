/// Errors the UI can reason about.
sealed class AppException implements Exception {
  const AppException(this.code);

  /// Stable identifier returned by the backend. Never shown to the user
  /// directly — [ErrorMessages.resolve] turns it into readable text.
  final String code;
}

class NetworkException extends AppException {
  const NetworkException() : super('NETWORK_ERROR');
}

class UnauthorizedException extends AppException {
  const UnauthorizedException() : super('UNAUTHORIZED');
}

class ValidationException extends AppException {
  const ValidationException(super.code, this.fieldErrors);

  final Map<String, List<String>> fieldErrors;
}

class BusinessException extends AppException {
  const BusinessException(super.code);
}

class UnexpectedException extends AppException {
  const UnexpectedException() : super('INTERNAL_ERROR');
}

/// Maps backend error codes to messages a person can act on.
///
/// Anything technical stays out: a stack trace, a table name or a biometric
/// score must never reach the screen. An unknown code falls back to the generic
/// message rather than leaking the raw value.
abstract final class ErrorMessages {
  static const String _fallback =
      'Não foi possível concluir a operação. Tente novamente em instantes.';

  static const Map<String, String> _messages = {
    // Rede e sessão
    'NETWORK_ERROR': 'Sem conexão com o servidor. Verifique sua internet.',
    'UNAUTHORIZED': 'Sua sessão expirou. Entre novamente.',
    'SERVICE_UNAVAILABLE': 'O serviço está indisponível no momento.',

    // Autenticação
    'INVALID_CREDENTIALS': 'CPF ou senha incorretos.',
    'ACCOUNT_LOCKED': 'Muitas tentativas. Aguarde alguns minutos e tente novamente.',
    'WEAK_PASSWORD': 'A senha não atende aos requisitos de segurança.',
    'CURRENT_PASSWORD_INVALID': 'A senha atual está incorreta.',
    'ONBOARDING_INCOMPLETE': 'Conclua seu primeiro acesso para continuar.',
    'COMPANY_SUSPENDED': 'O acesso da sua empresa está suspenso. Procure o RH.',

    // Biometria
    'FACE_NOT_DETECTED': 'Não encontramos seu rosto. Verifique a iluminação.',
    'MULTIPLE_FACES_DETECTED': 'Certifique-se de estar sozinho diante da câmera.',
    'FACE_QUALITY_TOO_LOW': 'A imagem não ficou nítida. Tente em um local mais claro.',
    'LIVENESS_FAILED': 'Não foi possível concluir a verificação. Vamos tentar de novo.',
    'LIVENESS_CHALLENGE_TIMEOUT': 'O tempo acabou. Vamos tentar novamente.',
    'FACE_ENROLLMENT_INCONSISTENT': 'As fotos não parecem ser da mesma pessoa. Vamos recomeçar.',
    'FACE_PROFILE_ALREADY_EXISTS': 'Você já possui um cadastro facial ativo.',
    'BIOMETRIC_CONSENT_REQUIRED': 'É necessário autorizar o uso da sua biometria.',
    'EMPLOYEE_PROFILE_REQUIRED': 'Cadastre seu rosto antes de bater ponto.',

    // Ponto
    'ATTENDANCE_SESSION_EXPIRED': 'A sessão expirou. Inicie novamente.',
    'MANUAL_ENTRY_REASON_REQUIRED': 'Informe a justificativa da marcação manual.',

    // Cadastro
    'CNPJ_INVALID': 'CNPJ inválido.',
    'CNPJ_ALREADY_REGISTERED': 'Este CNPJ já está cadastrado.',
    'CPF_INVALID': 'CPF inválido.',
    'CPF_ALREADY_REGISTERED': 'Este CPF já está cadastrado na empresa.',
  };

  static String resolve(String code) => _messages[code] ?? _fallback;
}
