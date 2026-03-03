import { ReactElement } from "react";
import { LuCalendarDays, LuCreditCard, LuSearch, LuTrendingUp } from "react-icons/lu";

export const LandingFeatures = (): ReactElement => {
    return (
        <section id="features" className="border-y border-zinc-100 bg-zinc-50 py-24">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-16 md:w-2/3">
                    <h2 className="mb-6 text-3xl font-bold tracking-tight md:text-5xl">
                        Seamless shopping,
                        <br />
                        powered by natural language.
                    </h2>
                    <p className="text-xl font-light text-zinc-600">
                        The agent integrates directly with your Square product catalog to deliver a complete
                        conversational commerce experience.
                    </p>
                </div>

                <div className="grid grid-cols-1 gap-8 md:grid-cols-2 lg:grid-cols-4">
                    <div className="rounded-3xl border border-zinc-100 bg-white p-8 shadow-sm transition-shadow hover:shadow-md">
                        <div className="mb-6 flex h-12 w-12 items-center justify-center rounded-full bg-zinc-100 text-zinc-900">
                            <LuSearch className="h-6 w-6" aria-hidden="true" />
                        </div>
                        <h3 className="mb-3 text-lg font-semibold">Product Expertise</h3>
                        <p className="text-sm leading-relaxed text-zinc-600">
                            Answers complex queries regarding materials, sizing, and availability based on your live
                            inventory.
                        </p>
                    </div>

                    <div className="rounded-3xl border border-zinc-100 bg-white p-8 shadow-sm transition-shadow hover:shadow-md">
                        <div className="mb-6 flex h-12 w-12 items-center justify-center rounded-full bg-zinc-100 text-zinc-900">
                            <LuTrendingUp className="h-6 w-6" aria-hidden="true" />
                        </div>
                        <h3 className="mb-3 text-lg font-semibold">Dynamic Upselling</h3>
                        <p className="text-sm leading-relaxed text-zinc-600">
                            Intelligently recommends complementary products and alternatives, increasing average order
                            value organically.
                        </p>
                    </div>

                    <div className="rounded-3xl border border-zinc-100 bg-white p-8 shadow-sm transition-shadow hover:shadow-md">
                        <div className="mb-6 flex h-12 w-12 items-center justify-center rounded-full bg-zinc-100 text-zinc-900">
                            <LuCreditCard className="h-6 w-6" aria-hidden="true" />
                        </div>
                        <h3 className="mb-3 text-lg font-semibold">In-Chat Checkout</h3>
                        <p className="text-sm leading-relaxed text-zinc-600">
                            Handles payment processing securely within the chat thread. Automated status updates for
                            billing.
                        </p>
                    </div>

                    <div className="rounded-3xl border border-zinc-100 bg-white p-8 shadow-sm transition-shadow hover:shadow-md">
                        <div className="mb-6 flex h-12 w-12 items-center justify-center rounded-full bg-zinc-100 text-zinc-900">
                            <LuCalendarDays className="h-6 w-6" aria-hidden="true" />
                        </div>
                        <h3 className="mb-3 text-lg font-semibold">Logistics & Shipping</h3>
                        <p className="text-sm leading-relaxed text-zinc-600">
                            Arranges shipping appointments, modifies delivery details, and pushes real-time tracking
                            notifications.
                        </p>
                    </div>
                </div>
            </div>
        </section>
    );
};
