"use client";

import { client } from "@/lib/api/client.gen";

let isUnauthorizedResponseInterceptorRegistered = false;

if (!isUnauthorizedResponseInterceptorRegistered) {
  client.interceptors.response.use(async (response) => {
    if (response.status !== 401) {
      return response;
    }

    const currentPath = window.location.pathname;
    if (currentPath.startsWith("/login")) {
      return response;
    }

    const returnUrl = `${window.location.pathname}${window.location.search}`;
    const loginUrl = `/login?returnUrl=${encodeURIComponent(returnUrl)}`;
    window.location.assign(loginUrl);

    return response;
  });

  isUnauthorizedResponseInterceptorRegistered = true;
}
