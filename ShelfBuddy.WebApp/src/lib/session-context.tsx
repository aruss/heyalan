"use client";

import type { ReactElement, ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { createContext, useContext } from "react";
import { getAuthMeOptions } from "@/lib/api/@tanstack/react-query.gen";

export type SessionUser = {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
};

type SessionContextValue = {
  currentUser: SessionUser | null;
  isLoading: boolean;
  errorMessage: string | null;
  refresh: () => Promise<void>;
};

const DEFAULT_SESSION_ERROR = "Unable to load session.";

const SessionContext = createContext<SessionContextValue | undefined>(undefined);

function isSessionUser(value: unknown): value is SessionUser {
  if (typeof value !== "object" || value === null) {
    return false;
  }

  const record = value as Record<string, unknown>;
  const roles = record.roles;

  return (
    typeof record.id === "string" &&
    typeof record.email === "string" &&
    typeof record.displayName === "string" &&
    Array.isArray(roles) &&
    roles.every((role) => typeof role === "string")
  );
}

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

  const currentUser = isSessionUser(sessionQuery.data) ? sessionQuery.data : null;

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
