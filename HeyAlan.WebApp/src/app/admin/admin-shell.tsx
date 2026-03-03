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
import { SessionProvider, useSession } from "@/lib/session-context"
import { AlertCircle } from "lucide-react"
import { ThemeProvider } from "next-themes"
import Link from "next/link"
import type { ReactElement, ReactNode } from "react"

type AdminShellProps = {
  children: ReactNode
  defaultOpen: boolean
}

export function AdminShell({
  children,
  defaultOpen,
}: AdminShellProps): ReactElement {

  function OnboardingButton() {
    const { currentUser } = useSession();
    return (<>
      {currentUser && currentUser.isOnboarded !== true && (
        <div className="inline-flex shrink-0 ml-auto">

          <Link href="/onboarding"
            type="button"
            className="bg-zinc-900 text-white transition-colors inline-flex items-center justify-between rounded-md px-4 py-2 text-sm transition dark:bg-gray-100 dark:text-gray-900 "
          >
            <span className="inline-flex items-center">
              <AlertCircle className="mr-2 size-4 text-white dark:text-gray-900" />
              Proceed onboarding
            </span>
          </Link>
        </div>
      )}</>)
  }

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

                  <OnboardingButton />
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

