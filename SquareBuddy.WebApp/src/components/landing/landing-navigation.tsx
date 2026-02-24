import Link from "next/link";
import { ReactElement } from "react";

export const LandingNavigation = (): ReactElement => {
    return (
        <nav className="fixed top-0 z-50 w-full border-b border-zinc-100 bg-white/80 backdrop-blur-md">
            <div className="mx-auto flex h-20 max-w-7xl items-center justify-between px-6">
                <div className="text-xl font-bold tracking-tight">
                    Square<span className="text-zinc-500">Buddy</span>
                </div>
                <div className="hidden space-x-8 text-sm font-medium md:flex">
                    <Link href="#features" className="transition-colors hover:text-zinc-500">
                        Features
                    </Link>
                    <Link href="#dashboard" className="transition-colors hover:text-zinc-500">
                        Merchant Dashboard
                    </Link>
                    <Link href="#pricing" className="transition-colors hover:text-zinc-500">
                        Pricing
                    </Link>
                    <Link href="#compliance" className="transition-colors hover:text-zinc-500">
                        Trust
                    </Link>
                </div>
                <div className="flex items-center space-x-4">
                    <Link href="/admin" className="hidden text-sm font-medium transition-colors hover:text-zinc-500 md:block">
                        Log In
                    </Link>
                    <Link
                        href="/admin"
                        className="rounded-full bg-zinc-900 px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-zinc-800"
                    >
                        Start Free Trial
                    </Link>
                </div>
            </div>
        </nav>
    );
};
