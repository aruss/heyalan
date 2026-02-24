import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';
import { AUTH_COOKIE_NAME } from './lib/contants';

const AUTH_ME_PATH = '/auth/me';

function createLoginRedirect(request: NextRequest): NextResponse {
    const loginUrl = new URL('/login', request.url);
    const returnUrl = `${request.nextUrl.pathname}${request.nextUrl.search}`;
    loginUrl.searchParams.set('returnUrl', returnUrl);
    return NextResponse.redirect(loginUrl);
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

        return NextResponse.rewrite(new URL(targetUrl));
    }

    if (pathname.startsWith('/admin')) {
        const authCookie = request.cookies.get(AUTH_COOKIE_NAME);

        if (!authCookie) {
            return createLoginRedirect(request);
        }

        if (!webApiEndpoint) {
            return new NextResponse(null, { status: 500, statusText: 'Config Error' });
        }

        const authMeUrl = `${webApiEndpoint.replace(/\/$/, '')}${AUTH_ME_PATH}`;
        const cookieHeader = request.headers.get('cookie');

        try {
            const authMeResponse = await fetch(authMeUrl, {
                method: 'GET',
                headers: cookieHeader ? { Cookie: cookieHeader } : undefined,
                cache: 'no-store',
            });

            if (authMeResponse.status !== 200) {
                return createLoginRedirect(request);
            }
        } catch {
            return createLoginRedirect(request);
        }
    }

    return NextResponse.next();
}

export const config = {
    matcher: ['/api/:path*', '/admin/:path*'],
};
