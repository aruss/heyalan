import type { BreadcrumbItem } from "@/lib/breadcrumb-context";

const ADMIN_INBOX_PATH = "/admin/inbox";
const ADMIN_SETTINGS_PATH = "/admin/settings";

const ADMIN_BREADCRUMBS_BY_PATH: Record<string, BreadcrumbItem[]> = {
  [ADMIN_INBOX_PATH]: [{ label: "Inbox" }],
  [ADMIN_SETTINGS_PATH]: [{ label: "Settings" }],
};

const normalizeAdminPath = (pathname: string): string => {
  if (pathname.length > 1 && pathname.endsWith("/")) {
    return pathname.slice(0, -1);
  }

  return pathname;
};

export const getAdminBreadcrumbs = (pathname: string): BreadcrumbItem[] => {
  const normalizedPath = normalizeAdminPath(pathname);
  const mappedItems = ADMIN_BREADCRUMBS_BY_PATH[normalizedPath];
  if (mappedItems) {
    return mappedItems;
  }

  if (normalizedPath.startsWith("/admin/")) {
    return [{ label: "Admin" }];
  }

  return [];
};
