"use client";

import type { ReactElement, ReactNode } from "react";
import { createContext, useContext, useMemo, useState } from "react";

export type BreadcrumbItem = {
  label: string;
  href?: string;
};

export type BreadcrumbOverride = {
  pathname: string;
  items: BreadcrumbItem[];
};

type BreadcrumbContextValue = {
  override: BreadcrumbOverride | null;
  setOverride: (override: BreadcrumbOverride | null) => void;
};

const BreadcrumbContext = createContext<BreadcrumbContextValue | undefined>(
  undefined,
);

export const BreadcrumbProvider = ({
  children,
}: {
  children: ReactNode;
}): ReactElement => {
  const [override, setOverride] = useState<BreadcrumbOverride | null>(null);

  const value = useMemo<BreadcrumbContextValue>(() => {
    return {
      override,
      setOverride,
    };
  }, [override]);

  return (
    <BreadcrumbContext.Provider value={value}>
      {children}
    </BreadcrumbContext.Provider>
  );
};

export function useBreadcrumbs(): BreadcrumbContextValue {
  const context = useContext(BreadcrumbContext);
  if (!context) {
    throw new Error("useBreadcrumbs must be used within BreadcrumbProvider.");
  }

  return context;
}
