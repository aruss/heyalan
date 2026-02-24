import type { Metadata } from "next";
import { Inter } from "next/font/google";
import type { ReactElement, ReactNode } from "react";
import "./globals.css";

const inter = Inter({
    variable: "--font-inter",
    subsets: ["latin"],
    weight: ["300", "400", "500", "600", "700"],
});

export const metadata: Metadata = {
    title: "SquareBuddy",
    description: "AI Sales Agent for Squarespace",
};

export default function RootLayout({
    children,
}: Readonly<{
    children: ReactNode;
}>): ReactElement {
    return (
        <html lang="en" className={`${inter.variable} scroll-smooth`}>
            <body className="bg-white text-zinc-900 antialiased smooth-scroll selection:bg-zinc-900 selection:text-white">
                {children}
            </body>
        </html>
    );
}
