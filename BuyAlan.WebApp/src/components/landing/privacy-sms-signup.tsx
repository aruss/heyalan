"use client";

import type { ChangeEvent, FormEvent, ReactElement } from "react";
import Link from "next/link";
import { useState } from "react";
import { postSmsSubscribe } from "@/lib/api";
import {
  buildPrivacySmsSignupPayload,
  validatePrivacySmsPhoneNumber,
} from "../../lib/privacy-sms-signup-utils";

type SubmitState = "idle" | "submitting" | "success";
const SUBMIT_ERROR_MESSAGE = "Unable to process your request right now. Please try again.";

export const PrivacySmsSignup = (): ReactElement => {
  const [phoneNumber, setPhoneNumber] = useState("");
  const [transactionalConsent, setTransactionalConsent] = useState(false);
  const [marketingConsent, setMarketingConsent] = useState(false);
  const [phoneNumberError, setPhoneNumberError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitState, setSubmitState] = useState<SubmitState>("idle");

  const onPhoneNumberChange = (event: ChangeEvent<HTMLInputElement>): void => {
    const nextPhoneNumber = event.target.value;

    setPhoneNumber(nextPhoneNumber);

    if (phoneNumberError !== null) {
      setPhoneNumberError(validatePrivacySmsPhoneNumber(nextPhoneNumber));
    }
  };

  const onSubmit = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault();

    const validationError = validatePrivacySmsPhoneNumber(phoneNumber);
    if (validationError !== null) {
      setPhoneNumberError(validationError);
      setSubmitError(null);
      return;
    }

    setPhoneNumberError(null);
    setSubmitError(null);
    setSubmitState("submitting");

    try {
      const response = await postSmsSubscribe({
        body: buildPrivacySmsSignupPayload({
          phoneNumber,
          transactionalConsent,
          marketingConsent,
        }),
        throwOnError: true,
      });

      if (response.data?.accepted !== true) {
        throw new Error("sms_subscription_not_accepted");
      }

      setSubmitState("success");
    } catch {
      setSubmitState("idle");
      setSubmitError(SUBMIT_ERROR_MESSAGE);
    }
  };

  if (submitState === "success") {
    return (
      <div className="mt-16 rounded-xl border border-zinc-800 bg-zinc-950 p-6 text-zinc-50 dark:border-zinc-200 dark:bg-white dark:text-zinc-900 ">
        <h3 className="mt-3 text-2xl font-semibold tracking-tight">
          You&apos;re signed up for BuyAlan SMS updates.
        </h3>
        <p className="mt-4 max-w-2xl text-base font-light leading-7 text-zinc-300 dark:text-zinc-600">
          We stored your consent preferences and will use them for future operational communications and compliance review.
        </p>
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-xl border border-zinc-800 bg-zinc-950 text-zinc-50 dark:border-zinc-200 dark:bg-white dark:text-zinc-900 ">
      <div className="grid gap-0 lg:grid-cols-[1.02fr_0.98fr]">
        <div className="border-b border-zinc-800 p-6 lg:border-b-0 lg:border-r lg:p-8 dark:border-zinc-200">
          <h3 className="text-3xl font-semibold tracking-tight text-white dark:text-zinc-900">
            Stay in the loop on orders, delivery notices, and account updates.
          </h3>
          <p className="mt-5 max-w-xl text-base font-light leading-7 text-zinc-300 dark:text-zinc-600">
            Enter your mobile number to receive SMS updates from BuyAlan by Atlas Delivery Software, Inc.
          </p>
          <div className="mt-8 rounded-xl border border-zinc-800 bg-white/5 p-5 dark:border-zinc-200 dark:bg-zinc-50">
            <p className="text-sm font-medium text-white dark:text-zinc-900">What this signup stores</p>
            <p className="mt-2 text-sm font-light leading-6 text-zinc-300 dark:text-zinc-600">
              Your phone number is saved exactly as submitted together with the consent selections you choose below.
            </p>
          </div>
        </div>

        <form id="" className="p-6 lg:p-8" onSubmit={(event) => void onSubmit(event)} noValidate>
          <div className="mb-6">
            <label
              htmlFor="privacy-sms-phone-number"
              className="text-sm leading-none dark:text-zinc-400 text-zinc-500 font-medium "
            >
              Mobile Phone Number *
            </label>
            <input
              id="privacy-sms-phone-number"
              type="tel"
              value={phoneNumber}
              onChange={onPhoneNumberChange}
              placeholder="(555) 123-4567"
              aria-invalid={phoneNumberError !== null}
              aria-describedby={phoneNumberError !== null ? "privacy-sms-phone-number-error" : undefined}
              className="mt-3 w-full rounded-xl border border-zinc-800 bg-white/5 px-4 py-3.5 text-base text-white placeholder:text-zinc-500 focus:border-zinc-400 focus:outline-none focus:ring-2 focus:ring-zinc-400/20 dark:border-zinc-300 dark:bg-zinc-50 dark:text-zinc-900 dark:placeholder:text-zinc-400 dark:focus:border-zinc-500 dark:focus:ring-zinc-300/50"
            />
            {phoneNumberError !== null ? (
              <p
                id="privacy-sms-phone-number-error"
                className="mt-3 text-sm text-rose-300 dark:text-rose-600"
              >
                {phoneNumberError}
              </p>
            ) : null}
          </div>

          <div className="space-y-5">
            <label className="flex items-start gap-4 rounded-xl border border-zinc-800 bg-white/5 p-4 dark:border-zinc-200 dark:bg-zinc-50">
              <input
                type="checkbox"
                checked={transactionalConsent}
                onChange={(event) => setTransactionalConsent(event.target.checked)}
                className="mt-1 h-5 w-5 rounded-xl border border-zinc-500 bg-white accent-zinc-900 dark:border-zinc-400 dark:accent-zinc-950"
              />
              <span className="text-sm font-light leading-6 text-zinc-300 dark:text-zinc-600">
                By checking, I agree to receive <strong className="font-semibold text-white dark:text-zinc-900">transactional/informational SMS</strong> from <strong className="font-semibold text-white dark:text-zinc-900">BuyAlan</strong> (Atlas Delivery Software, Inc.) regarding orders, delivery updates, account notifications, and customer care. Message frequency may vary. Message and data rates may apply. Reply <strong className="font-semibold text-white dark:text-zinc-900">HELP</strong> for help or <strong className="font-semibold text-white dark:text-zinc-900">STOP</strong> to opt out.
              </span>
            </label>

            <label className="flex items-start gap-4 rounded-xl border border-zinc-800 bg-white/5 p-4 dark:border-zinc-200 dark:bg-zinc-50">
              <input
                type="checkbox"
                checked={marketingConsent}
                onChange={(event) => setMarketingConsent(event.target.checked)}
                className="mt-1 h-5 w-5 rounded-xl border border-zinc-500 bg-white accent-zinc-900 dark:border-zinc-400 dark:accent-zinc-950"
              />
              <span className="text-sm font-light leading-6 text-zinc-300 dark:text-zinc-600">
                By checking, I agree to receive <strong className="font-semibold text-white dark:text-zinc-900">promotional/marketing SMS</strong> from <strong className="font-semibold text-white dark:text-zinc-900">BuyAlan</strong>, including special offers and product updates. Message frequency may vary. Message and data rates may apply. Reply <strong className="font-semibold text-white dark:text-zinc-900">HELP</strong> for help or <strong className="font-semibold text-white dark:text-zinc-900">STOP</strong> to opt out.
              </span>
            </label>
          </div>

          <p className="mt-6 text-sm font-light leading-6 text-zinc-400 dark:text-zinc-500">
            By adding your number above, you accept our{" "}
            <Link href="/terms" className="font-medium text-white transition-colors hover:text-zinc-300 dark:text-zinc-900 dark:hover:text-zinc-700">
              Terms &amp; Conditions
            </Link>{" "}
            and{" "}
            <Link href="/privacy" className="font-medium text-white transition-colors hover:text-zinc-300 dark:text-zinc-900 dark:hover:text-zinc-700">
              Privacy Policy
            </Link>
            . Consent is not a condition of purchase. Message and data rates may apply. Message frequency varies.
          </p>

          {submitError !== null ? (
            <p className="mt-4 rounded-xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-200 dark:bg-rose-50 dark:text-rose-700">
              {submitError}
            </p>
          ) : null}

          <button
            type="submit"
     
            className="
            w-full text-zinc-950 bg-white hover:bg-zinc-200  disabled:bg-zinc-400
            rounded-xl   font-medium text-white transition-colors disabled:cursor-not-allowed disabled:text-zinc-400
            mt-8 px-6 py-4     "
          >
            {submitState === "submitting" ? "Submitting..." : "Sign Up for Updates"}
          </button>
        </form>
      </div>
    </div>
  );
};
