import { expect, test } from "@playwright/test";
import { triggerLogout } from "../src/lib/logout-action";

test("triggerLogout calls logout endpoint with expected request options", async () => {
  let requestUrl = "";
  let requestInit: RequestInit | undefined;
  let redirected = false;

  const fetchFn: typeof fetch = async (input, init) => {
    requestUrl = String(input);
    requestInit = init;

    return new Response(null, { status: 200 });
  };

  triggerLogout({
    fetchFn,
    redirectToLogin: () => {
      redirected = true;
    },
  });

  await Promise.resolve();

  expect(requestUrl).toBe("/api/auth/logout");
  expect(requestInit?.method).toBe("POST");
  expect(requestInit?.credentials).toBe("same-origin");
  expect(requestInit?.cache).toBe("no-store");
  expect(requestInit?.keepalive).toBe(true);
  expect(redirected).toBe(true);
});

test("triggerLogout redirects even when request fails", async () => {
  let redirected = false;

  const fetchFn: typeof fetch = async () => {
    throw new Error("logout failed");
  };

  triggerLogout({
    fetchFn,
    redirectToLogin: () => {
      redirected = true;
    },
  });

  await Promise.resolve();

  expect(redirected).toBe(true);
});

