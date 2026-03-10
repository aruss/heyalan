"use client"

import { TabNavigation, TabNavigationLink } from "@/components/admin/tab-navigation";
import { AgentSettingsProvider } from "./agent-settings-context";
import Link from "next/link";
import { usePathname } from "next/navigation";

const navigationSettings = [
    { name: "Personality", href: "/admin/settings/agent" },
    { name: "Channels", href: "/admin/settings/agent/channels" },
    { name: "Skills", href: "/admin/settings/agent/skills" },
    { name: "Inventory", href: "/admin/settings/agent/inventory" },
]

export default function Layout({
    children,
}: Readonly<{
    children: React.ReactNode
}>) {
    const pathname = usePathname()

    return (
        <AgentSettingsProvider>
            <div className=" dark:bg-gray-925 lg:dark:border-gray-900">
                <TabNavigation className="">
                    {navigationSettings.map((item) => (
                        <TabNavigationLink
                            key={item.name}
                            asChild
                            active={pathname === item.href}
                            className="mx-4 pt-4 md:pt-6"
                        >
                            <Link href={item.href}>{item.name}</Link>
                        </TabNavigationLink>
                    ))}
                </TabNavigation>

                <div className="pt-4 md:pt-6 max-w-5xl">{children}</div>
            </div>
        </AgentSettingsProvider>
    );
}
