import { AppError } from '../contracts/errors/appError';

export type UiDirective =
  | { readonly kind: 'OPEN_STEP_UP' }
  | { readonly kind: 'SHOW_ACCESS_DENIED'; readonly refreshCapabilities: boolean }
  | { readonly kind: 'APPROVAL_PENDING'; readonly approvalId?: string }
  | { readonly kind: 'SESSION_EXPIRED' }
  | { readonly kind: 'RETRY_WITH_NEW_KEY' }
  | { readonly kind: 'OUTCOME_UNKNOWN'; readonly checkJobStatus: boolean }
  | { readonly kind: 'SHOW_ERROR_BANNER'; readonly i18nKey: string };

export function mapAppErrorToUi(error: AppError): UiDirective {
  switch (error.code) {
    case 'STEPUP_REQUIRED':
      return { kind: 'OPEN_STEP_UP' };

    case 'APPROVAL_REQUIRED':
      return {
        kind: 'APPROVAL_PENDING',
        ...(error.approvalId !== undefined ? { approvalId: error.approvalId } : {}),
      };

    case 'PERMISSION_MISSING':
    case 'RESOURCE_OUT_OF_SCOPE':
    case 'SOD_VIOLATION':
    case 'CREDENTIAL_DENIED':
      return { kind: 'SHOW_ACCESS_DENIED', refreshCapabilities: true };

    case 'AUTHN_MISSING':
    case 'SESSION_INVALID':
      return { kind: 'SESSION_EXPIRED' };

    case 'IDEMPOTENCY_CONFLICT':
      return { kind: 'RETRY_WITH_NEW_KEY' };

    case 'TIMEOUT':
      return { kind: 'OUTCOME_UNKNOWN', checkJobStatus: true };

    case 'POLICY_INVALID':
      return { kind: 'SHOW_ERROR_BANNER', i18nKey: 'errors.POLICY_INVALID' };

    case 'OPERATION_UNMAPPED':
      return { kind: 'SHOW_ERROR_BANNER', i18nKey: 'errors.OPERATION_UNMAPPED' };

    case 'RATE_LIMITED':
      return { kind: 'SHOW_ERROR_BANNER', i18nKey: 'errors.RATE_LIMITED' };

    case 'INTERNAL_DENY':
      return { kind: 'SHOW_ERROR_BANNER', i18nKey: 'errors.INTERNAL_DENY' };

    case 'VALIDATION_FAILED':
      return { kind: 'SHOW_ERROR_BANNER', i18nKey: 'errors.VALIDATION_FAILED' };

    case 'OPERATION_IN_PROGRESS':
      return { kind: 'SHOW_ERROR_BANNER', i18nKey: 'errors.OPERATION_IN_PROGRESS' };

    case 'EGRESS_BLOCKED':
      return { kind: 'SHOW_ERROR_BANNER', i18nKey: 'errors.EGRESS_BLOCKED' };

    case 'NOT_FOUND':
      return { kind: 'SHOW_ERROR_BANNER', i18nKey: 'errors.NOT_FOUND' };

    case 'INVALID_REQUEST':
      return { kind: 'SHOW_ERROR_BANNER', i18nKey: 'errors.INVALID_REQUEST' };

    default:
      return { kind: 'SHOW_ERROR_BANNER', i18nKey: 'errors.GENERIC_ERROR' };
  }
}
