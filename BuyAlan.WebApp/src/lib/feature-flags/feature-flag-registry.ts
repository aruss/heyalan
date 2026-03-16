export type FeatureFlagDefinition<Key extends string = string> = {
  key: Key;
  defaultValue: boolean;
  description: string;
  owner?: string;
  category?: string;
};

const defineFeatureFlags = <
  Flags extends {
    [Key in keyof Flags]: FeatureFlagDefinition<Extract<Key, string>>;
  },
>(
  flags: Flags,
): Flags => {
  return flags;
};

const featureFlagRegistryDefinition = defineFeatureFlags({
  landingPricing: {
    key: "landingPricing",
    defaultValue: false,
    description:
      "Controls pricing visibility on the landing page, including pricing-related navigation.",
    category: "landing",
  },
  teamMembers: {
    key: "teamMembers",
    defaultValue: true,
    description:
      "Controls whether onboarding and admin expose team-member invitation and membership management UI.",
    category: "admin",
  },
});

export const featureFlagRegistry = featureFlagRegistryDefinition;

export type FeatureFlagKey = keyof typeof featureFlagRegistry;

export type FeatureFlagSnapshot = {
  [Key in FeatureFlagKey]: boolean;
};

export const featureFlagKeys = Object.keys(featureFlagRegistry) as FeatureFlagKey[];
