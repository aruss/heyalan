import type { ReactElement } from "react";
import { getAuthProviders, type ExternalLoginProviderItem } from "@/lib/api";

type LoginPageProps = {
    searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
};

const DEFAULT_RETURN_URL = "/admin";
const PROVIDER_BUTTON_CLASS_NAME =
    "flex w-full items-center justify-center gap-3 rounded-xl bg-zinc-900 px-8 py-4 text-lg font-medium text-white transition-colors hover:bg-zinc-800";

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
    return `/api/auth/external/${encodedProvider}/start?returnUrl=${encodedReturnUrl}`;
}

export default async function LoginPage({ searchParams }: LoginPageProps): Promise<ReactElement> {
    const resolvedSearchParams = await searchParams;
    const returnUrl = resolveSafeReturnUrl(resolvedSearchParams.returnUrl);
    const webApiEndpoint = process.env.WEBAPI_ENDPOINT;

    if (!webApiEndpoint) {
        return (
            <div className="w-full max-w-sm mx-auto">
                <div className="mb-10 text-center lg:text-left">
                    <h2 className="text-3xl font-bold tracking-tight text-neutral-900 mb-2">Welcome back</h2>
                    <p className="text-neutral-500">Log in to manage your workspace and integrations.</p>
                </div>
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

    return (
        <div className="w-full max-w-sm mx-auto">
            <div className="mb-10 text-center lg:text-left">
                <h2 className="text-3xl font-bold tracking-tight text-neutral-900 mb-2">Welcome back</h2>
                <p className="text-neutral-500">Log in to manage your workspace and integrations.</p>
            </div>

            {hasProviderLoadError ? (
                <p className="text-sm text-red-700">Unable to load login providers. Please try again later.</p>
            ) : providers.length === 0 ? (
                <p className="text-sm text-neutral-600">No login providers are available.</p>
            ) : (
                <ul className="space-y-3">
                    {providers.map((provider) => {
                        const providerHref = createProviderLoginHref(provider.name, returnUrl);
                        return (
                            <li key={provider.name}>
                                <a href={providerHref} className={PROVIDER_BUTTON_CLASS_NAME}>
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
