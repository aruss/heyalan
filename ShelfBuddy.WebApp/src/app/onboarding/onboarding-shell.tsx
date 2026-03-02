"use client";

import type { ReactElement, ReactNode } from "react";
import { ReactQueryProvider } from "@/lib/react-query-provider";
import { SessionProvider } from "@/lib/session-context";

type OnboardingShellProps = {
    children: ReactNode;
};

export const OnboardingShell = ({ children }: OnboardingShellProps): ReactElement => {
    return (
        <ReactQueryProvider>
            <SessionProvider>{children}</SessionProvider>
        </ReactQueryProvider>
    );
};
