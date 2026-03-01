"use client"

import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/admin/Sidebar"
import { AppSidebar } from "@/components/admin/ui/navigation/app-sidebar"
import { Breadcrumbs } from "@/components/admin/ui/navigation/breadcrumbs"
import { BreadcrumbProvider } from "@/lib/breadcrumb-context"
import { ReactQueryProvider } from "@/lib/react-query-provider"
import { SessionProvider } from "@/lib/session-context"
import { ThemeProvider } from "next-themes"
import type { ReactElement, ReactNode } from "react"

type AdminShellProps = {
  children: ReactNode
  defaultOpen: boolean
}

export function AdminShell({
  children,
  defaultOpen,
}: AdminShellProps): ReactElement {
  return (
    <ThemeProvider
      defaultTheme="light"
      disableTransitionOnChange
      attribute="class"
    >
      <ReactQueryProvider>
        <SessionProvider>
          <BreadcrumbProvider>
            <SidebarProvider defaultOpen={defaultOpen}>
              <AppSidebar />
              <SidebarInset>
                <header className="sticky top-0 z-10 flex h-16 shrink-0 items-center gap-2 border-b border-gray-200 bg-white px-4 dark:border-gray-800 dark:bg-gray-950">
                  <SidebarTrigger className="-ml-1" />
                  <div className="mr-2 h-4 w-px bg-gray-200 dark:bg-gray-800" />
                  <Breadcrumbs />
                </header>
                <main>{children}</main>
              </SidebarInset>
            </SidebarProvider>
          </BreadcrumbProvider>
        </SessionProvider>
      </ReactQueryProvider>
    </ThemeProvider>
  )
}

