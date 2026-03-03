"use client";

import { getAdminBreadcrumbs } from "@/lib/admin-breadcrumbs";
import { useBreadcrumbs } from "@/lib/breadcrumb-context";
import { ChevronRight } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";

export function Breadcrumbs() {
  const pathname = usePathname();
  const { override } = useBreadcrumbs();
  const routeItems = getAdminBreadcrumbs(pathname);
  const hasOverride =
    override?.pathname === pathname && override.items.length > 0;
  const items = hasOverride ? override.items : routeItems;
  const title = items[items.length - 1]?.label ?? "";

  return (
    <div className="ml-2 flex flex-col gap-0.5">
      <div className="text-base font-semibold text-gray-900 dark:text-gray-50">
        {title}
      </div>
      {items.length > 1 &&
        <nav aria-label="Breadcrumb">
          <ol role="list" className="flex items-center gap-2 text-xs">
            {items.map((item, index) => {
              const isLast = index === items.length - 1;
              const showLink = item.href && !isLast;

              return (
                <li key={`${item.label}-${index}`} className="flex items-center gap-2">
                  {showLink ? (
                    <Link
                      href={item.href as string}
                      className="text-gray-500 transition hover:text-gray-700 dark:text-gray-400 hover:dark:text-gray-300"
                    >
                      {item.label}
                    </Link>
                  ) : (
                    <span className="text-gray-500 dark:text-gray-400">
                      {item.label}
                    </span>
                  )}
                  {!isLast && (
                    <ChevronRight
                      className="size-4 shrink-0 text-gray-600 dark:text-gray-400"
                      aria-hidden="true"
                    />
                  )}
                </li>
              );
            })}
          </ol>
        </nav>}

    </div>
  );
}
