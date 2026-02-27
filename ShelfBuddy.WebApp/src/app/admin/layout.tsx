import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/admin/Sidebar"
import { AppSidebar } from "@/components/admin/ui/navigation/app-sidebar"
import { Breadcrumbs } from "@/components/admin/ui/navigation/breadcrumbs"
import { ThemeProvider } from "next-themes"
import localFont from "next/font/local"
import { cookies } from "next/headers"
import "../globals.css"
import { BreadcrumbProvider } from "@/lib/breadcrumb-context"

const geistSans = localFont({
  src: "../fonts/GeistVF.woff",
  variable: "--font-geist-sans",
  weight: "100 900",
})
const geistMono = localFont({
  src: "../fonts/GeistMonoVF.woff",
  variable: "--font-geist-mono",
  weight: "100 900",
})

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const cookieStore = await cookies()
  const defaultOpen = cookieStore.get("sidebar:state")?.value === "true"

  return (
    <html
      lang="en"
      className="h-full text-[18px]"
      suppressHydrationWarning
    >
      <body
        className={`${geistSans.variable} ${geistMono.variable} h-full bg-gray-50 antialiased dark:bg-gray-950`}
      >
        <ThemeProvider
          defaultTheme="light"
          disableTransitionOnChange
          attribute="class"
        >
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
        </ThemeProvider>
      </body>
    </html>
  )
}
