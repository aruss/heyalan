import { ReactElement } from "react";

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
                            <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth="2"
                                    d="M21 21l-6-6m2-5a7 7 0 1 1-14 0 7 7 0 0 1 14 0z"
                                />
                            </svg>
                        </div>
                        <h3 className="mb-3 text-lg font-semibold">Product Expertise</h3>
                        <p className="text-sm leading-relaxed text-zinc-600">
                            Answers complex queries regarding materials, sizing, and availability based on your live
                            inventory.
                        </p>
                    </div>

                    <div className="rounded-3xl border border-zinc-100 bg-white p-8 shadow-sm transition-shadow hover:shadow-md">
                        <div className="mb-6 flex h-12 w-12 items-center justify-center rounded-full bg-zinc-100 text-zinc-900">
                            <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth="2"
                                    d="M13 7h8m0 0v8m0-8-8 8-4-4-6 6"
                                />
                            </svg>
                        </div>
                        <h3 className="mb-3 text-lg font-semibold">Dynamic Upselling</h3>
                        <p className="text-sm leading-relaxed text-zinc-600">
                            Intelligently recommends complementary products and alternatives, increasing average order
                            value organically.
                        </p>
                    </div>

                    <div className="rounded-3xl border border-zinc-100 bg-white p-8 shadow-sm transition-shadow hover:shadow-md">
                        <div className="mb-6 flex h-12 w-12 items-center justify-center rounded-full bg-zinc-100 text-zinc-900">
                            <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth="2"
                                    d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 0 0 3-3V8a3 3 0 0 0-3-3H6a3 3 0 0 0-3 3v8a3 3 0 0 0 3 3z"
                                />
                            </svg>
                        </div>
                        <h3 className="mb-3 text-lg font-semibold">In-Chat Checkout</h3>
                        <p className="text-sm leading-relaxed text-zinc-600">
                            Handles payment processing securely within the chat thread. Automated status updates for
                            billing.
                        </p>
                    </div>

                    <div className="rounded-3xl border border-zinc-100 bg-white p-8 shadow-sm transition-shadow hover:shadow-md">
                        <div className="mb-6 flex h-12 w-12 items-center justify-center rounded-full bg-zinc-100 text-zinc-900">
                            <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth="2"
                                    d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2z"
                                />
                            </svg>
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
