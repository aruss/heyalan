import { ReactElement } from "react";
import { LandingCompliance } from "@/components/landing/landing-compliance";
import { LandingDashboard } from "@/components/landing/landing-dashboard";
import { LandingFeatures } from "@/components/landing/landing-features";
import { LandingHero } from "@/components/landing/landing-hero";
import { LandingPricing } from "@/components/landing/landing-pricing";

export default function Home(): ReactElement {
    return (
        <>
            <LandingHero />
            <LandingFeatures />
            <LandingDashboard />
            <LandingPricing />
            <LandingCompliance />
        </>
    );
}
