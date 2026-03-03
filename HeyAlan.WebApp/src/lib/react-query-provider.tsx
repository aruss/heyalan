"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactElement, ReactNode } from "react";
import { useState } from "react";
import "@/lib/hey-api-client-auth";

type ReactQueryProviderProps = {
    children: ReactNode;
};

export function ReactQueryProvider({ children }: ReactQueryProviderProps): ReactElement {
    const [queryClient] = useState(() => {
        return new QueryClient();
    });

    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}
