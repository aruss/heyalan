import type { ReactElement } from "react";
import { PrimaryActionButton } from "@/components/landing/ui/action-buttons";

export default function LoginPage(): ReactElement {
    return (

        <div className="w-full max-w-sm mx-auto">
            <div className="mb-10 text-center lg:text-left">
                <h2 className="text-3xl font-bold tracking-tight text-neutral-900 mb-2">Welcome back</h2>
                <p className="text-neutral-500">Log in to manage your workspace and integrations.</p>
            </div>
            <PrimaryActionButton
                href="/onboarding"
                fullWidth
                className="flex items-center justify-center gap-3"
            >
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="lucide lucide-square" aria-hidden="true">
                    <rect width="18" height="18" x="3" y="3" rx="2"></rect>
                </svg>
                Continue with Square
            </PrimaryActionButton>
        </div>

    );
}
