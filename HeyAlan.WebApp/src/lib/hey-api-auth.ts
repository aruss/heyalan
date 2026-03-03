import { client } from '@/lib/api/client.gen';
import { cookies } from 'next/headers';
import 'server-only';
import { AUTH_COOKIE_NAME } from './contants';

function createHeaders(value: unknown): Headers {
  if (value instanceof Headers) {
    return new Headers(value);
  }

  const headers = new Headers();
  if (!value) {
    return headers;
  }

  if (Array.isArray(value)) {
    for (const entry of value) {
      if (!Array.isArray(entry) || entry.length !== 2) {
        continue;
      }

      const [key, headerValue] = entry;
      headers.append(String(key), String(headerValue));
    }
    return headers;
  }

  if (typeof value === 'object') {
    for (const [key, headerValue] of Object.entries(value as Record<string, unknown>)) {
      if (headerValue === undefined || headerValue === null) {
        continue;
      }

      if (Array.isArray(headerValue)) {
        for (const item of headerValue) {
          headers.append(key, String(item));
        }
        continue;
      }

      headers.set(key, String(headerValue));
    }
  }

  return headers;
}

let isIdentityCookieInterceptorRegistered = false;

if (!isIdentityCookieInterceptorRegistered) {
  client.interceptors.request.use(async (request) => {
    if (typeof window !== 'undefined') {
      return;
    }

    const cookieStore = await cookies();
    const identityCookie = cookieStore.get(AUTH_COOKIE_NAME);
    if (identityCookie === undefined) {
      return;
    }

    const headers = createHeaders(request.headers);
    request.headers = headers;

    const existingCookieHeader = headers.get('Cookie');
    const identityCookieHeader = `${identityCookie.name}=${identityCookie.value}`;

    if (existingCookieHeader === null || existingCookieHeader.length === 0) {
      headers.set('Cookie', identityCookieHeader);
      return;
    }

    const hasIdentityCookie = existingCookieHeader
      .split(';')
      .map((item: string) => item.trim())
      .some((item: string) => item.startsWith(`${AUTH_COOKIE_NAME}=`));

    if (!hasIdentityCookie) {
      headers.set('Cookie', `${existingCookieHeader}; ${identityCookieHeader}`);
    }
  });

  isIdentityCookieInterceptorRegistered = true;
}
