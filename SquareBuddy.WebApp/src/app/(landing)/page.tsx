import { ReactElement } from "react";
import { LandingCompliance } from "@/components/landing/landing-compliance";
import { LandingDashboard } from "@/components/landing/landing-dashboard";
import { LandingFeatures } from "@/components/landing/landing-features";
import { LandingFooter } from "@/components/landing/landing-footer";
import { LandingHero } from "@/components/landing/landing-hero";
import { LandingNavigation } from "@/components/landing/landing-navigation";
import { LandingPricing } from "@/components/landing/landing-pricing";

export default function Home(): ReactElement {
    return (
        <div className="bg-white text-zinc-900 antialiased smooth-scroll selection:bg-zinc-900 selection:text-white">
            <LandingNavigation />
            <LandingHero />
            <LandingFeatures />
            <LandingDashboard />
            <LandingPricing />
            <LandingCompliance />
            <LandingFooter />
        </div>
    );
}
