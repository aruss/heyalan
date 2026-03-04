"use client"


// https://dashboard.tremor.so/settings/general#
// https://insights.tremor.so/settings/users

export default function Layout({
    children,
}: Readonly<{
    children: React.ReactNode
}>) {
    return (<>
        {children}
    </>);
}