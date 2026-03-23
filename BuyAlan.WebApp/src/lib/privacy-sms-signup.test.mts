import assert from "node:assert/strict";
import test from "node:test";
import {
  INVALID_PHONE_NUMBER_MESSAGE,
  buildPrivacySmsSignupPayload,
  validatePrivacySmsPhoneNumber,
} from "./privacy-sms-signup-utils.ts";

test("validatePrivacySmsPhoneNumber rejects empty and obviously invalid values", () => {
  assert.equal(
    validatePrivacySmsPhoneNumber(""),
    "Mobile phone number is required.",
  );
  assert.equal(
    validatePrivacySmsPhoneNumber("abc123"),
    INVALID_PHONE_NUMBER_MESSAGE,
  );
  assert.equal(
    validatePrivacySmsPhoneNumber("123"),
    INVALID_PHONE_NUMBER_MESSAGE,
  );
  assert.equal(validatePrivacySmsPhoneNumber("(555) 123-4567"), null);
});

test("buildPrivacySmsSignupPayload preserves consent flags and trims the phone number", () => {
  assert.deepEqual(
    buildPrivacySmsSignupPayload({
      phoneNumber: " (555) 123-4567 ",
      transactionalConsent: true,
      marketingConsent: false,
    }),
    {
      phoneNumber: "(555) 123-4567",
      transactionalConsent: true,
      marketingConsent: false,
    },
  );
});
