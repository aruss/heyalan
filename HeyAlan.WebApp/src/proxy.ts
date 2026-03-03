import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';
import { AUTH_COOKIE_NAME } from './lib/contants';

const AUTH_ME_PATH = '/auth/me';
const ADMIN_HOME_PATH = '/admin';

type AuthMePayload = {
    isOnboarded?: boolean;
};

function createLoginRedirect(request: NextRequest): NextResponse {
    const loginUrl = new URL('/login', request.url);
    const returnUrl = `${request.nextUrl.pathname}${request.nextUrl.search}`;
    loginUrl.searchParams.set('returnUrl', returnUrl);
    return NextResponse.redirect(loginUrl);
}

function createAdminRedirect(request: NextRequest): NextResponse {
    return NextResponse.redirect(new URL(ADMIN_HOME_PATH, request.url));
}

async function getAuthMePayloadAsync(request: NextRequest, webApiEndpoint: string): Promise<AuthMePayload | null> {
    const authMeUrl = `${webApiEndpoint.replace(/\/$/, '')}${AUTH_ME_PATH}`;
    const cookieHeader = request.headers.get('cookie');

    const authMeResponse = await fetch(authMeUrl, {
        method: 'GET',
        headers: cookieHeader ? { Cookie: cookieHeader } : undefined,
        cache: 'no-store',
    });

    if (authMeResponse.status !== 200) {
        return null;
    }

    const authMeJson = await authMeResponse.json();
    if (authMeJson === null || typeof authMeJson !== 'object') {
        return null;
    }

    return authMeJson as AuthMePayload;
}

export async function proxy(request: NextRequest): Promise<NextResponse> {
    const pathname = request.nextUrl.pathname;
    const webApiEndpoint = process.env.WEBAPI_ENDPOINT;

    if (pathname.startsWith('/api/')) {
        if (!webApiEndpoint) {
            return new NextResponse(null, { status: 500, statusText: 'Config Error' });
        }

        const path = pathname.replace(/^\/api/, '');
        const targetUrl = `${webApiEndpoint.replace(/\/$/, '')}${path}${request.nextUrl.search}`;

        const requestHeaders = new Headers(request.headers);
        requestHeaders.set('X-Forwarded-Host', request.nextUrl.host);
        requestHeaders.set('X-Forwarded-Proto', request.nextUrl.protocol.replace(':', ''));
        requestHeaders.set('X-Forwarded-Prefix', '/api');

        return NextResponse.rewrite(new URL(targetUrl), {
            request: {
                headers: requestHeaders,
            },
        });
    }

    if (pathname.startsWith('/admin')) {
        const authCookie = request.cookies.get(AUTH_COOKIE_NAME);

        if (!authCookie) {
            return createLoginRedirect(request);
        }

        if (!webApiEndpoint) {
            return new NextResponse(null, { status: 500, statusText: 'Config Error' });
        }

        try {
            const authMePayload = await getAuthMePayloadAsync(request, webApiEndpoint);
            if (authMePayload === null) {
                return createLoginRedirect(request);
            }
        } catch {
            return createLoginRedirect(request);
        }
    }

    if (pathname.startsWith('/onboarding')) {
        const authCookie = request.cookies.get(AUTH_COOKIE_NAME);
        if (!authCookie) {
            return createLoginRedirect(request);
        }

        if (!webApiEndpoint) {
            return new NextResponse(null, { status: 500, statusText: 'Config Error' });
        }

        try {
            const authMePayload = await getAuthMePayloadAsync(request, webApiEndpoint);
            if (authMePayload === null) {
                return createLoginRedirect(request);
            }

            if (authMePayload.isOnboarded === true) {
                return createAdminRedirect(request);
            }
        } catch {
            return createLoginRedirect(request);
        }
    }

    return NextResponse.next();
}

export const config = {
    matcher: ['/api/:path*', '/admin/:path*', '/onboarding/:path*'],
};
