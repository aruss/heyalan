import { ReactElement } from "react";
import { LandingCompliance } from "@/components/landing/landing-compliance";
import { LandingDashboard } from "@/components/landing/landing-dashboard";
import { LandingFeatures } from "@/components/landing/landing-features";
import { LandingHero } from "@/components/landing/landing-hero";
import { LandingPricing } from "@/components/landing/landing-pricing";
import { isFeatureEnabled } from "@/lib/feature-flags/server";
import { createLogger } from "@/lib/logger";

export default function Home(): ReactElement {
  const isPricingEnabled = isFeatureEnabled("landingPricing");
  const logger = createLogger({
    module: "Home",
  });

  logger.information("Home: information log");
  logger.error("Home: error log");
  logger.debug("Home: debug log");
  logger.warning("Home: warning log");

  return (
    <>
      <LandingHero />
      <LandingFeatures />
      <LandingDashboard />
      {isPricingEnabled ? <LandingPricing /> : null}
      <LandingCompliance />
    </>
  );
}
