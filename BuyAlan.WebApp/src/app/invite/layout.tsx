import type { Metadata } from "next";
import type { ReactElement, ReactNode } from "react";
import "../globals.css";

export const metadata: Metadata = {
    title: "BuyAlan",
    description: "AI Sales Agent for Square",
};

export default async function RootLayout({
    children,
}: Readonly<{
    children: ReactNode;
}>): Promise<ReactElement> {

    return (
        <html lang="en">
            <body>
                <main>{children}</main>
            </body>
        </html>
    );
}
