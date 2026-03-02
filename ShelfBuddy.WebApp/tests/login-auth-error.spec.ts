import { expect, test } from "@playwright/test";
import { resolveAuthErrorCode, resolveAuthErrorMessage } from "../src/app/login/auth-error";

test("maps known auth error code to user-facing copy", () => {
  const message = resolveAuthErrorMessage("external_provider_error", 1);

  expect(message).toBe("The external provider returned an error. Please try signing in again.");
});

test("uses generic fallback copy for unknown auth error code", () => {
  const message = resolveAuthErrorMessage("unexpected_error", 1);

  expect(message).toBe("We could not sign you in. Please try again.");
});

test("multiple authError values select the first value only", () => {
  const selectedCode = resolveAuthErrorCode(["user_locked_out", "external_provider_error"]);
  const message = resolveAuthErrorMessage(selectedCode, 1);

  expect(selectedCode).toBe("user_locked_out");
  expect(message).toBe("Your account is locked. Please try again later.");
});

test("local_email_not_confirmed includes alternate provider guidance with multiple providers", () => {
  const message = resolveAuthErrorMessage("local_email_not_confirmed", 2);

  expect(message).toBe(
    "Please confirm your email for your existing account before signing in with this provider. Or use another provider.",
  );
});

test("local_email_not_confirmed omits alternate provider guidance with one provider", () => {
  const message = resolveAuthErrorMessage("local_email_not_confirmed", 1);

  expect(message).toBe("Please confirm your email for your existing account before signing in with this provider.");
});

test("subscription_provision_failed uses dedicated workspace provisioning message", () => {
  const message = resolveAuthErrorMessage("subscription_provision_failed", 1);

  expect(message).toBe("We created your account but could not initialize your workspace. Please try signing in again.");
});
