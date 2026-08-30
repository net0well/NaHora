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
/// Anything technical stays out: a stack trace, a table name or a raw
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
    'ACCOUNT_LOCKED':
        'Muitas tentativas. Aguarde alguns minutos e tente novamente.',
    'WEAK_PASSWORD': 'A senha não atende aos requisitos de segurança.',
    'CURRENT_PASSWORD_INVALID': 'A senha atual está incorreta.',
    'ONBOARDING_INCOMPLETE': 'Conclua seu primeiro acesso para continuar.',
    'COMPANY_SUSPENDED': 'O acesso da sua empresa está suspenso. Procure o RH.',

    // Localização
    'LOCATION_REQUIRED':
        'Não conseguimos sua localização. Verifique se o GPS está ligado.',
    'LOCATION_PERMISSION_DENIED':
        'Permita o acesso à localização para marcar o ponto.',

    // Ponto
    'PUNCH_NOT_FOUND': 'Marcação não encontrada.',
    'PUNCH_ALREADY_REGISTERED': 'Esta marcação já foi registrada.',

    // Ajuste de ponto
    'JUSTIFICATION_REQUIRED': 'Escolha uma justificativa para o ajuste.',
    'JUSTIFICATION_NOTES_REQUIRED': 'Descreva o motivo do ajuste.',
    'ATTACHMENT_TOO_LARGE': 'O anexo é grande demais. O limite é 5 MB.',
    'ATTACHMENT_TYPE_NOT_ALLOWED': 'Envie uma imagem ou um PDF.',

    // Cadastro
    'CNPJ_INVALID': 'CNPJ inválido.',
    'CNPJ_ALREADY_REGISTERED': 'Este CNPJ já está cadastrado.',
    'CPF_INVALID': 'CPF inválido.',
    'CPF_ALREADY_REGISTERED': 'Este CPF já está cadastrado na empresa.',
  };

  static String resolve(String code) => _messages[code] ?? _fallback;
}
