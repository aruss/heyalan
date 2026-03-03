"use client";

export type LogoutActionDependencies = {
  fetchFn: typeof fetch;
  redirectToLogin: () => void;
};

const LOGOUT_ENDPOINT = "/api/auth/logout";

function createLogoutRequest(fetchFn: typeof fetch): Promise<Response> {
  return fetchFn(LOGOUT_ENDPOINT, {
    method: "POST",
    credentials: "same-origin",
    cache: "no-store",
    keepalive: true,
  });
}

export function triggerLogout(dependencies: LogoutActionDependencies): void {
  const logoutRequestPromise = createLogoutRequest(dependencies.fetchFn);
  void logoutRequestPromise.catch(() => {
    return undefined;
  });

  dependencies.redirectToLogin();
}

export function triggerLogoutFromBrowser(): void {
  triggerLogout({
    fetchFn: fetch,
    redirectToLogin: () => {
      window.location.assign("/login");
    },
  });
}

