import type { ReactElement } from "react";
import { getAuthProviders, type ExternalLoginProviderItem } from "@/lib/api";
import { resolveAuthErrorCode, resolveAuthErrorMessage } from "./auth-error";

type LoginPageProps = {
    searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
};

const DEFAULT_RETURN_URL = "/admin";
const PROVIDER_BUTTON_CLASS_NAME =
    "flex w-full items-center justify-center gap-3 rounded-xl bg-zinc-900 px-8 py-4 text-lg font-medium text-white transition-colors hover:bg-zinc-800";
type ProviderIconProps = {
    className?: string;
};

type ProviderIconComponent = (props: ProviderIconProps) => ReactElement;

const GOOGLE_PROVIDER_NAME = "google";
const MICROSOFT_PROVIDER_NAME = "microsoft";
const SQUARE_PROVIDER_NAME = "square";
const UNKNOWN_PROVIDER_ICON_KEY = "unknown";

function GoogleProviderIcon(props: ProviderIconProps): ReactElement {
    return (
        <svg viewBox="0 0 24 24" aria-hidden="true" className={props.className} fill="none">
            <path
                d="M22 12.2C22 11.4 21.9 10.7 21.8 10H12V14H17.6C17.3 15.4 16.5 16.6 15.3 17.4V20.5H19.3C21 19 22 16 22 12.2Z"
                fill="currentColor"
            />
            <path
                d="M12 22C14.7 22 17 21.1 19.3 20.5L15.3 17.4C14.3 18.1 13.2 18.5 12 18.5C9.4 18.5 7.1 16.8 6.4 14.4H2.2V17.6C3.5 20.2 7.4 22 12 22Z"
                fill="currentColor"
            />
            <path
                d="M6.4 14.4C6 13.2 6 10.9 6.4 9.6V6.4H2.2C1.2 8.5 1.2 15.6 2.2 17.6L6.4 14.4Z"
                fill="currentColor"
            />
            <path
                d="M12 5.5C13.3 5.5 14.5 5.9 15.5 6.8L19.4 2.9C17.3 1 14.8 0 12 0C7.4 0 3.5 1.8 2.2 6.4L6.4 9.6C7.1 7.2 9.4 5.5 12 5.5Z"
                fill="currentColor"
            />
        </svg>
    );
}

function MicrosoftProviderIcon(props: ProviderIconProps): ReactElement {
    return (
        <svg viewBox="0 0 24 24" aria-hidden="true" className={props.className} fill="none">
            <path d="M3 3H11V11H3V3Z" fill="currentColor" />
            <path d="M13 3H21V11H13V3Z" fill="currentColor" />
            <path d="M3 13H11V21H3V13Z" fill="currentColor" />
            <path d="M13 13H21V21H13V13Z" fill="currentColor" />
        </svg>
    );
}

function SquareProviderIcon(props: ProviderIconProps): ReactElement {
    return (
        <svg viewBox="0 0 24 24" aria-hidden="true" className={props.className} fill="none">
            <rect x="2.5" y="2.5" width="19" height="19" rx="4.5" stroke="currentColor" strokeWidth="2.5" />
            <rect x="8" y="8" width="8" height="8" rx="1.5" fill="currentColor" />
        </svg>
    );
}

function UnknownProviderIcon(props: ProviderIconProps): ReactElement {
    return (
        <svg viewBox="0 0 24 24" aria-hidden="true" className={props.className} fill="none">
            <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="2" />
            <path
                d="M9.8 9.3C9.8 8.1 10.8 7.1 12 7.1C13.2 7.1 14.2 8.1 14.2 9.3C14.2 10.3 13.6 10.9 12.7 11.5C11.9 12 11.5 12.5 11.5 13.4V14"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
            />
            <circle cx="12" cy="16.8" r="1" fill="currentColor" />
        </svg>
    );
}

const PROVIDER_ICON_BY_NAME: Record<string, ProviderIconComponent> = {
    [GOOGLE_PROVIDER_NAME]: GoogleProviderIcon,
    [MICROSOFT_PROVIDER_NAME]: MicrosoftProviderIcon,
    [SQUARE_PROVIDER_NAME]: SquareProviderIcon,
};

export function resolveProviderIconKey(providerName: string): string {
    const normalizedProviderName = providerName.toLowerCase();
    const iconByName = PROVIDER_ICON_BY_NAME[normalizedProviderName];
    if (!iconByName) {
        return UNKNOWN_PROVIDER_ICON_KEY;
    }

    return normalizedProviderName;
}

export function resolveProviderIcon(providerName: string): ProviderIconComponent {
    const iconKey = resolveProviderIconKey(providerName);
    if (iconKey === UNKNOWN_PROVIDER_ICON_KEY) {
        return UnknownProviderIcon;
    }

    return PROVIDER_ICON_BY_NAME[iconKey];
}

function getFirstSearchParamValue(value: string | string[] | undefined): string | undefined {
    if (Array.isArray(value)) {
        return value[0];
    }

    return value;
}

function isSafeRelativeReturnUrl(value: string): boolean {
    if (!value.startsWith("/")) {
        return false;
    }

    if (value.startsWith("//")) {
        return false;
    }

    const lowerCaseValue = value.toLowerCase();
    if (lowerCaseValue.startsWith("/http:") || lowerCaseValue.startsWith("/https:")) {
        return false;
    }

    return true;
}

function resolveSafeReturnUrl(rawReturnUrl: string | string[] | undefined): string {
    const firstValue = getFirstSearchParamValue(rawReturnUrl);
    if (!firstValue) {
        return DEFAULT_RETURN_URL;
    }

    if (!isSafeRelativeReturnUrl(firstValue)) {
        return DEFAULT_RETURN_URL;
    }

    return firstValue;
}

function createProviderLoginHref(providerName: string, returnUrl: string): string {
    const encodedProvider = encodeURIComponent(providerName);
    const encodedReturnUrl = encodeURIComponent(returnUrl);
    return `/api/auth/providers/${encodedProvider}/authorize?returnUrl=${encodedReturnUrl}`;
}

export default async function LoginPage({ searchParams }: LoginPageProps): Promise<ReactElement> {
    const resolvedSearchParams = await searchParams;
    const returnUrl = resolveSafeReturnUrl(resolvedSearchParams.returnUrl);
    const authErrorCode = resolveAuthErrorCode(resolvedSearchParams.authError);
    const webApiEndpoint = process.env.WEBAPI_ENDPOINT;

    if (!webApiEndpoint) {
        const authErrorMessage = resolveAuthErrorMessage(authErrorCode, 0);
        return (
            <div className="w-full max-w-sm mx-auto">
                <div className="mb-10 text-center lg:text-left">
                    <h2 className="text-3xl font-bold tracking-tight text-neutral-900 mb-2">Welcome back</h2>
                    <p className="text-neutral-500">Log in to manage your workspace and integrations.</p>
                </div>
                {authErrorMessage ? (
                    <div role="alert" className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3">
                        <p className="text-sm text-red-700">{authErrorMessage}</p>
                    </div>
                ) : null}
                <p className="text-sm text-red-700">Unable to load login providers. Please try again later.</p>
            </div>
        );
    }

    let providers: ExternalLoginProviderItem[] = [];
    let hasProviderLoadError = false;

    try {
        const providerResult = await getAuthProviders({
            baseUrl: webApiEndpoint,
            cache: "no-store",
            throwOnError: true,
        });
        providers = providerResult.data.providers;
    } catch {
        hasProviderLoadError = true;
    }
    const authErrorMessage = resolveAuthErrorMessage(authErrorCode, providers.length);

    return (
        <div className="w-full max-w-sm mx-auto">
            <div className="mb-10 text-center lg:text-left">
                <h2 className="text-3xl font-bold tracking-tight text-neutral-900 mb-2">Welcome back</h2>
                <p className="text-neutral-500">Log in to manage your workspace and integrations.</p>
            </div>
            {authErrorMessage ? (
                <div role="alert" className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3">
                    <p className="text-sm text-red-700">{authErrorMessage}</p>
                </div>
            ) : null}

            {hasProviderLoadError ? (
                <p className="text-sm text-red-700">Unable to load login providers. Please try again later.</p>
            ) : providers.length === 0 ? (
                <p className="text-sm text-neutral-600">No login providers are available.</p>
            ) : (
                <ul className="space-y-3">
                    {providers.map((provider) => {
                        const providerHref = createProviderLoginHref(provider.name, returnUrl);
                        const ProviderIcon = resolveProviderIcon(provider.name);
                        return (
                            <li key={provider.name}>
                                <a href={providerHref} className={PROVIDER_BUTTON_CLASS_NAME}>
                                    <ProviderIcon className="size-5 shrink-0 text-white" />
                                    Continue with {provider.displayName}
                                </a>
                            </li>
                        );
                    })}
                </ul>
            )}
        </div>
    );
}
