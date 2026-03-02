"use client";

import type { ReactElement, ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { createContext, useContext } from "react";
import type { CurrentUserResult } from "@/lib/api";
import { getAuthMeOptions } from "@/lib/api/@tanstack/react-query.gen";

export type SessionCurrentUser = CurrentUserResult & {
  activeSubscriptionId?: string | null;
};

type SessionContextValue = {
  currentUser: SessionCurrentUser | null;
  isLoading: boolean;
  errorMessage: string | null;
  refresh: () => Promise<void>;
};

const DEFAULT_SESSION_ERROR = "Unable to load session.";

const SessionContext = createContext<SessionContextValue | undefined>(undefined);

export const SessionProvider = ({
  children,
}: {
  children: ReactNode;
}): ReactElement => {
  const sessionQuery = useQuery({
    ...getAuthMeOptions(),
    retry: false,
  });

  const refresh = async (): Promise<void> => {
    await sessionQuery.refetch();
  };

  const currentUser = (sessionQuery.data as SessionCurrentUser | undefined) ?? null;

  const isLoading = sessionQuery.isLoading || sessionQuery.isRefetching;
  const errorMessage = sessionQuery.error == null ? null : DEFAULT_SESSION_ERROR;

  const value: SessionContextValue = {
    currentUser,
    isLoading,
    errorMessage,
    refresh,
  };

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
};

export function useSession(): SessionContextValue {
  const context = useContext(SessionContext);
  if (!context) {
    throw new Error("useSession must be used within SessionProvider.");
  }

  return context;
}
