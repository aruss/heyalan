import type { BreadcrumbItem } from "@/lib/breadcrumb-context";

const ADMIN_INBOX_PATH = "/admin/inbox";
const ADMIN_SETTINGS_PATH = "/admin/settings";
const ADMIN_SETTINGS_AGENT_PATH = "/admin/settings/agent";
const ADMIN_SETTINGS_AGENT_CHANNELS_PATH = "/admin/settings/agent/channels";
const ADMIN_SETTINGS_AGENT_SKILLS_PATH = "/admin/settings/agent/skills";
const ADMIN_SETTINGS_AGENT_INVENTORY_PATH = "/admin/settings/agent/inventory";
const ADMIN_SETTINGS_INVENTORY_PATH = "/admin/settings/inventory";
const ADMIN_SETTINGS_MEMBERS_PATH = "/admin/settings/members";

const ADMIN_BREADCRUMBS_BY_PATH: Record<string, BreadcrumbItem[]> = {
  [ADMIN_INBOX_PATH]: [{ label: "Inbox" }],
  [ADMIN_SETTINGS_PATH]: [{ label: "Settings" }],
  [ADMIN_SETTINGS_AGENT_PATH]: [{ label: "Agent Personality" }],
  [ADMIN_SETTINGS_AGENT_CHANNELS_PATH]: [{ label: "Agent Channels" }],
  [ADMIN_SETTINGS_AGENT_SKILLS_PATH]: [{ label: "Agent Skills" }],
  [ADMIN_SETTINGS_AGENT_INVENTORY_PATH]: [{ label: "Agent Inventory" }],
  [ADMIN_SETTINGS_INVENTORY_PATH]: [{ label: "Inventory" }],
  [ADMIN_SETTINGS_MEMBERS_PATH]: [{ label: "Members" }],
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
