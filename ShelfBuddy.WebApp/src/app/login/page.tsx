import type { ReactElement } from "react";
import { getAuthProviders, type ExternalLoginProviderItem } from "@/lib/api";
import { FaMicrosoft } from "react-icons/fa6";
import type { IconType } from "react-icons";
import { SiGoogle, SiSimpleicons, SiSquare } from "react-icons/si";
import { resolveAuthErrorCode, resolveAuthErrorMessage } from "./auth-error";

type LoginPageProps = {
    searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
};

const DEFAULT_RETURN_URL = "/admin";
const PROVIDER_BUTTON_CLASS_NAME =
    "flex w-full items-center justify-center gap-3 rounded-xl bg-zinc-900 px-8 py-4 text-lg font-medium text-white transition-colors hover:bg-zinc-800";

const GOOGLE_PROVIDER_NAME = "google";
const MICROSOFT_PROVIDER_NAME = "microsoft";
const SQUARE_PROVIDER_NAME = "square";
const UNKNOWN_PROVIDER_ICON_KEY = "unknown";
const PROVIDER_ICON_BY_NAME: Record<string, IconType> = {
    [GOOGLE_PROVIDER_NAME]: SiGoogle,
    [MICROSOFT_PROVIDER_NAME]: FaMicrosoft,
    [SQUARE_PROVIDER_NAME]: SiSquare,
};

export function resolveProviderIconKey(providerName: string): string {
    const normalizedProviderName = providerName.toLowerCase();
    const iconByName = PROVIDER_ICON_BY_NAME[normalizedProviderName];
    if (!iconByName) {
        return UNKNOWN_PROVIDER_ICON_KEY;
    }

    return normalizedProviderName;
}

export function resolveProviderIcon(providerName: string): IconType {
    const iconKey = resolveProviderIconKey(providerName);
    if (iconKey === UNKNOWN_PROVIDER_ICON_KEY) {
        return SiSimpleicons;
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
                                    <ProviderIcon className="size-5 shrink-0 text-white" aria-hidden="true" />
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
