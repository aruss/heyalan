const EXTERNAL_PROVIDER_ERROR_CODE = "external_provider_error";
const EXTERNAL_LOGIN_INFO_MISSING_ERROR_CODE = "external_login_info_missing";
const USER_NOT_ALLOWED_ERROR_CODE = "user_not_allowed";
const USER_LOCKED_OUT_ERROR_CODE = "user_locked_out";
const EMAIL_CLAIM_MISSING_ERROR_CODE = "email_claim_missing";
const EXTERNAL_EMAIL_NOT_VERIFIED_ERROR_CODE = "external_email_not_verified";
const USER_CREATE_FAILED_ERROR_CODE = "user_create_failed";
const LOCAL_EMAIL_NOT_CONFIRMED_ERROR_CODE = "local_email_not_confirmed";
const EXTERNAL_LOGIN_LINK_FAILED_ERROR_CODE = "external_login_link_failed";
const SUBSCRIPTION_PROVISION_FAILED_ERROR_CODE = "subscription_provision_failed";

export type KnownAuthErrorCode =
    | typeof EXTERNAL_PROVIDER_ERROR_CODE
    | typeof EXTERNAL_LOGIN_INFO_MISSING_ERROR_CODE
    | typeof USER_NOT_ALLOWED_ERROR_CODE
    | typeof USER_LOCKED_OUT_ERROR_CODE
    | typeof EMAIL_CLAIM_MISSING_ERROR_CODE
    | typeof EXTERNAL_EMAIL_NOT_VERIFIED_ERROR_CODE
    | typeof USER_CREATE_FAILED_ERROR_CODE
    | typeof LOCAL_EMAIL_NOT_CONFIRMED_ERROR_CODE
    | typeof EXTERNAL_LOGIN_LINK_FAILED_ERROR_CODE
    | typeof SUBSCRIPTION_PROVISION_FAILED_ERROR_CODE;

const GENERIC_AUTH_ERROR_MESSAGE = "We could not sign you in. Please try again.";
const LOCAL_EMAIL_NOT_CONFIRMED_BASE_MESSAGE =
    "Please confirm your email for your existing account before signing in with this provider.";
const LOCAL_EMAIL_NOT_CONFIRMED_MULTI_PROVIDER_SUFFIX = " Or use another provider.";

const AUTH_ERROR_MESSAGE_BY_CODE: Record<KnownAuthErrorCode, string> = {
    [EXTERNAL_PROVIDER_ERROR_CODE]: "The external provider returned an error. Please try signing in again.",
    [EXTERNAL_LOGIN_INFO_MISSING_ERROR_CODE]: "We could not complete external sign-in. Please try again.",
    [USER_NOT_ALLOWED_ERROR_CODE]: "Your account is not allowed to sign in.",
    [USER_LOCKED_OUT_ERROR_CODE]: "Your account is locked. Please try again later.",
    [EMAIL_CLAIM_MISSING_ERROR_CODE]:
        "Your external account did not provide an email address required for sign-in.",
    [EXTERNAL_EMAIL_NOT_VERIFIED_ERROR_CODE]:
        "Please verify your email with your external provider before signing in.",
    [USER_CREATE_FAILED_ERROR_CODE]: "We could not create your account from external sign-in. Please try again.",
    [LOCAL_EMAIL_NOT_CONFIRMED_ERROR_CODE]: LOCAL_EMAIL_NOT_CONFIRMED_BASE_MESSAGE,
    [EXTERNAL_LOGIN_LINK_FAILED_ERROR_CODE]:
        "We could not link your external login to your account. Please try again.",
    [SUBSCRIPTION_PROVISION_FAILED_ERROR_CODE]:
        "We created your account but could not initialize your workspace. Please try signing in again.",
};

function getFirstSearchParamValue(value: string | string[] | undefined): string | undefined {
    if (Array.isArray(value)) {
        return value[0];
    }

    return value;
}

export function resolveAuthErrorCode(rawAuthError: string | string[] | undefined): string | undefined {
    const firstValue = getFirstSearchParamValue(rawAuthError);
    if (firstValue === undefined) {
        return undefined;
    }

    const trimmedValue = firstValue.trim();
    if (trimmedValue.length === 0) {
        return undefined;
    }

    return trimmedValue;
}

function isKnownAuthErrorCode(errorCode: string): errorCode is KnownAuthErrorCode {
    return errorCode in AUTH_ERROR_MESSAGE_BY_CODE;
}

export function resolveAuthErrorMessage(authErrorCode: string | undefined, providerCount: number): string | undefined {
    if (authErrorCode === undefined) {
        return undefined;
    }

    if (!isKnownAuthErrorCode(authErrorCode)) {
        return GENERIC_AUTH_ERROR_MESSAGE;
    }

    if (authErrorCode === LOCAL_EMAIL_NOT_CONFIRMED_ERROR_CODE) {
        if (providerCount > 1) {
            return `${LOCAL_EMAIL_NOT_CONFIRMED_BASE_MESSAGE}${LOCAL_EMAIL_NOT_CONFIRMED_MULTI_PROVIDER_SUFFIX}`;
        }

        return LOCAL_EMAIL_NOT_CONFIRMED_BASE_MESSAGE;
    }

    return AUTH_ERROR_MESSAGE_BY_CODE[authErrorCode];
}
