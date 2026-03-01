import localFont from "next/font/local"
import { cookies } from "next/headers"
import "../globals.css"
import { AdminShell } from "./admin-shell"

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
        <AdminShell defaultOpen={defaultOpen}>{children}</AdminShell>
      </body>
    </html>
  )
}
