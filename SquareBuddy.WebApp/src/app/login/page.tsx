import type { ReactElement } from "react";
import Link from "next/link";
import { Button } from "@/components/ui/button";

export default function LoginPage(): ReactElement {
    return (
        <main className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
            <section className="w-full max-w-md rounded-xl border border-slate-200 bg-white p-8 shadow-sm">
                <h1 className="text-2xl font-semibold text-slate-900">Login</h1>
                <p className="mt-2 text-sm text-slate-600">
                    Authentication UI is not implemented yet. This placeholder route supports admin redirect flow.
                </p>
                <div className="mt-6">
                    <Link href="/" className="inline-flex">
                        <Button variant="ghost">Back to landing page</Button>
                    </Link>
                </div>
            </section>
        </main>
    );
}
