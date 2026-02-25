"use client"

import { Divider } from "@/components/admin/Divider"
import { Input } from "@/components/admin/Input"
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarLink,
  SidebarMenu,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarSubLink,
} from "@/components/admin/Sidebar"
import { cx, focusRing } from "@/lib/utils"
import { RiArrowDownSFill } from "@remixicon/react"
import { BookText, House, PackageSearch, Settings } from "lucide-react"
import { usePathname } from "next/navigation"
import { useCallback, useState, type ComponentProps } from "react"
import { Logo } from "../../../../../public/Logo"
import { UserProfile } from "./UserProfile"

const navigation = [
   {
    name: "Inbox",
    href: "/admin/inbox",
    icon: PackageSearch,
    notifications: 2,
  },
  {
    name: "Settings",
    href: "#",
    icon: Settings,
    children: [
      {
        name: "Agent",
        href: "/admin/settings/agent",
        active: true,
      },
      {
        name: "Channels",
        href: "/admin/settings/channels",
        active: false,
      },
      {
        name: "Inventory",
        href: "/admin/settings/inventory",
        active: false,
      },
    ],
  },

] as const

export function AppSidebar({ ...props }: ComponentProps<typeof Sidebar>) {
  const pathname = usePathname()
  const [openMenus, setOpenMenus] = useState<string[]>([
    navigation[0].name
  ])

  const isActivePath = useCallback(
    (href: string) => {
      if (!pathname) {
        return false
      }

      if (pathname === href) {
        return true
      }

      return pathname.startsWith(`${href}/`)
    },
    [pathname],
  )

  const toggleMenu = (name: string) => {
    setOpenMenus((prev: string[]) =>
      prev.includes(name)
        ? prev.filter((item: string) => item !== name)
        : [...prev, name],
    )
  }

  return (
    <Sidebar {...props} className="bg-gray-50 dark:bg-gray-925">
      <SidebarHeader className="px-4 py-3 border-b border-gray-200 dark:border-gray-800 h-16">
        <div className="flex items-center gap-3 ">
          <span className="flex size-9 items-center justify-center rounded-md bg-white p-1.5 shadow-sm ring-1 ring-gray-200 dark:bg-gray-900 dark:ring-gray-800">
            <Logo className="size-6 text-gray-500 dark:text-gray-500" />
          </span>
          <div>
            <span className="block text-sm font-semibold text-gray-900 dark:text-gray-50">
              <div className="text-xl font-bold tracking-tight">Square<span className="text-zinc-500">Buddy</span></div>
            </span>

          </div>
        </div>
      </SidebarHeader>
      <SidebarContent>
        {/*<SidebarGroup>
          <SidebarGroupContent>
            <Input
              type="search"
              placeholder="Search items..."
              className="[&>input]:sm:py-1.5"
            />
          </SidebarGroupContent>
        </SidebarGroup> */}
        <SidebarGroup className="pt-4">
          <SidebarGroupContent>
      


            <SidebarMenu className="space-y-1">
              {navigation.map((item) => (
                <SidebarMenuItem key={item.name}>

                  {item.children ? <>
                    <button
                      onClick={() => toggleMenu(item.name)}
                      className={cx(
                        "flex w-full items-center justify-between gap-x-2.5 rounded-md p-2 text-base text-gray-900 transition hover:bg-gray-200/50 sm:text-sm dark:text-gray-400 hover:dark:bg-gray-900 hover:dark:text-gray-50",
                        focusRing,
                      )}
                    >
                      <div className="flex items-center gap-2.5">
                        <item.icon
                          className="size-[18px] shrink-0"
                          aria-hidden="true"
                        />
                        {item.name}
                      </div>
                      <RiArrowDownSFill
                        className={cx(
                          openMenus.includes(item.name)
                            ? "rotate-0"
                            : "-rotate-90",
                          "size-5 shrink-0 transform text-gray-400 transition-transform duration-150 ease-in-out dark:text-gray-600",
                        )}
                        aria-hidden="true"
                      />
                    </button>
                    {item.children && openMenus.includes(item.name) && (
                      <SidebarMenuSub>
                        <div className="absolute inset-y-0 left-4 w-px bg-gray-300 dark:bg-gray-800" />
                        {item.children.map((child) => (
                          <SidebarMenuItem key={child.name}>
                            <SidebarSubLink
                              href={child.href}
                              isActive={child.active}
                            >
                              {child.name}
                            </SidebarSubLink>
                          </SidebarMenuItem>
                        ))}
                      </SidebarMenuSub>
                    )}
                  </> : <SidebarLink
                    href={item.href}
                    isActive={isActivePath(item.href)}
                    icon={item.icon}
                    notifications={item.notifications}
                  >
                    {item.name}
                  </SidebarLink>}
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

       
      </SidebarContent>
      <SidebarFooter>
        <div className="border-t border-gray-200 dark:border-gray-800" />
        <UserProfile />
      </SidebarFooter>
    </Sidebar>
  )
}
