export type PrivacySmsSignupValues = {
  phoneNumber: string;
  transactionalConsent: boolean;
  marketingConsent: boolean;
};

export type PrivacySmsSignupPayload = {
  phoneNumber: string;
  transactionalConsent: boolean;
  marketingConsent: boolean;
};

export const INVALID_PHONE_NUMBER_MESSAGE =
  "Enter a valid mobile number using digits and common phone formatting.";

const MINIMUM_DIGIT_COUNT = 7;
const MAXIMUM_DIGIT_COUNT = 20;

export const validatePrivacySmsPhoneNumber = (
  phoneNumber: string,
): string | null => {
  const trimmedPhoneNumber = phoneNumber.trim();

  if (trimmedPhoneNumber.length === 0) {
    return "Mobile phone number is required.";
  }

  let digitCount = 0;

  for (const character of trimmedPhoneNumber) {
    const isDigit = character >= "0" && character <= "9";

    if (isDigit) {
      digitCount += 1;
      continue;
    }

    if (
      character === "+" ||
      character === "(" ||
      character === ")" ||
      character === "-" ||
      character === "." ||
      /\s/.test(character)
    ) {
      continue;
    }

    return INVALID_PHONE_NUMBER_MESSAGE;
  }

  if (digitCount < MINIMUM_DIGIT_COUNT || digitCount > MAXIMUM_DIGIT_COUNT) {
    return INVALID_PHONE_NUMBER_MESSAGE;
  }

  return null;
};

export const buildPrivacySmsSignupPayload = (
  values: PrivacySmsSignupValues,
): PrivacySmsSignupPayload => {
  return {
    phoneNumber: values.phoneNumber.trim(),
    transactionalConsent: values.transactionalConsent,
    marketingConsent: values.marketingConsent,
  };
};
