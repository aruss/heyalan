import Link from "next/link";
import { ReactElement } from "react";

export const LandingPricing = (): ReactElement => {
    return (
        <section id="pricing" className="border-t border-zinc-100 bg-white py-24">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-16 text-center">
                    <h2 className="mb-4 text-3xl font-bold tracking-tight md:text-5xl">Scalable parameters.</h2>
                    <p className="text-xl font-light text-zinc-600">Bandwidth aligned with revenue growth.</p>
                </div>

                <div className="mx-auto grid max-w-5xl grid-cols-1 gap-8 md:grid-cols-3">
                    <div className="flex flex-col rounded-3xl border border-zinc-200 bg-white p-8">
                        <h3 className="mb-2 text-xl font-bold">Starter</h3>
                        <div className="mb-6 text-4xl font-bold">
                            $49<span className="text-lg font-normal text-zinc-500">/mo</span>
                        </div>
                        <ul className="mb-8 flex-1 space-y-4 text-sm text-zinc-600">
                            <li className="flex items-center">
                                <svg className="mr-3 h-4 w-4 shrink-0 text-zinc-900" fill="currentColor" viewBox="0 0 20 20">
                                    <path
                                        fillRule="evenodd"
                                        d="M16.707 5.293a1 1 0 0 1 0 1.414l-8 8a1 1 0 0 1-1.414 0l-4-4a1 1 0 0 1 1.414-1.414L8 12.586l7.293-7.293a1 1 0 0 1 1.414 0z"
                                        clipRule="evenodd"
                                    />
                                </svg>
                                500 Active Sessions
                            </li>
                            <li className="flex items-center">
                                <svg className="mr-3 h-4 w-4 shrink-0 text-zinc-900" fill="currentColor" viewBox="0 0 20 20">
                                    <path
                                        fillRule="evenodd"
                                        d="M16.707 5.293a1 1 0 0 1 0 1.414l-8 8a1 1 0 0 1-1.414 0l-4-4a1 1 0 0 1 1.414-1.414L8 12.586l7.293-7.293a1 1 0 0 1 1.414 0z"
                                        clipRule="evenodd"
                                    />
                                </svg>
                                Web Chat & SMS Vectors
                            </li>
                            <li className="flex items-center">
                                <svg className="mr-3 h-4 w-4 shrink-0 text-zinc-900" fill="currentColor" viewBox="0 0 20 20">
                                    <path
                                        fillRule="evenodd"
                                        d="M16.707 5.293a1 1 0 0 1 0 1.414l-8 8a1 1 0 0 1-1.414 0l-4-4a1 1 0 0 1 1.414-1.414L8 12.586l7.293-7.293a1 1 0 0 1 1.414 0z"
                                        clipRule="evenodd"
                                    />
                                </svg>
                                Standard Reporting
                            </li>
                        </ul>
                        <Link
                            href="#"
                            className="mt-auto block w-full rounded-xl bg-zinc-100 px-4 py-3 text-center font-semibold text-zinc-900 transition-colors hover:bg-zinc-200"
                        >
                            Deploy Free
                        </Link>
                    </div>

                    <div className="flex transform flex-col rounded-3xl border-2 border-zinc-900 bg-zinc-900 p-8 text-white shadow-xl md:-translate-y-4">
                        <div className="mb-2 text-xs font-bold tracking-widest text-zinc-400 uppercase">Optimal</div>
                        <h3 className="mb-2 text-xl font-bold">Growth</h3>
                        <div className="mb-6 text-4xl font-bold">
                            $149<span className="text-lg font-normal text-zinc-400">/mo</span>
                        </div>
                        <ul className="mb-8 flex-1 space-y-4 text-sm text-zinc-300">
                            <li className="flex items-center">
                                <svg className="mr-3 h-4 w-4 shrink-0 text-white" fill="currentColor" viewBox="0 0 20 20">
                                    <path
                                        fillRule="evenodd"
                                        d="M16.707 5.293a1 1 0 0 1 0 1.414l-8 8a1 1 0 0 1-1.414 0l-4-4a1 1 0 0 1 1.414-1.414L8 12.586l7.293-7.293a1 1 0 0 1 1.414 0z"
                                        clipRule="evenodd"
                                    />
                                </svg>
                                2,000 Active Sessions
                            </li>
                            <li className="flex items-center">
                                <svg className="mr-3 h-4 w-4 shrink-0 text-white" fill="currentColor" viewBox="0 0 20 20">
                                    <path
                                        fillRule="evenodd"
                                        d="M16.707 5.293a1 1 0 0 1 0 1.414l-8 8a1 1 0 0 1-1.414 0l-4-4a1 1 0 0 1 1.414-1.414L8 12.586l7.293-7.293a1 1 0 0 1 1.414 0z"
                                        clipRule="evenodd"
                                    />
                                </svg>
                                All Protocol Channels (WA, TG)
                            </li>
                            <li className="flex items-center">
                                <svg className="mr-3 h-4 w-4 shrink-0 text-white" fill="currentColor" viewBox="0 0 20 20">
                                    <path
                                        fillRule="evenodd"
                                        d="M16.707 5.293a1 1 0 0 1 0 1.414l-8 8a1 1 0 0 1-1.414 0l-4-4a1 1 0 0 1 1.414-1.414L8 12.586l7.293-7.293a1 1 0 0 1 1.414 0z"
                                        clipRule="evenodd"
                                    />
                                </svg>
                                Human Takeover Enabled
                            </li>
                        </ul>
                        <Link
                            href="#"
                            className="mt-auto block w-full rounded-xl bg-white px-4 py-3 text-center font-semibold text-zinc-900 transition-colors hover:bg-zinc-100"
                        >
                            Initialize Trial
                        </Link>
                    </div>

                    <div className="flex flex-col rounded-3xl border border-zinc-200 bg-white p-8">
                        <h3 className="mb-2 text-xl font-bold">Scale</h3>
                        <div className="mb-6 text-4xl font-bold">Custom</div>
                        <ul className="mb-8 flex-1 space-y-4 text-sm text-zinc-600">
                            <li className="flex items-center">
                                <svg className="mr-3 h-4 w-4 shrink-0 text-zinc-900" fill="currentColor" viewBox="0 0 20 20">
                                    <path
                                        fillRule="evenodd"
                                        d="M16.707 5.293a1 1 0 0 1 0 1.414l-8 8a1 1 0 0 1-1.414 0l-4-4a1 1 0 0 1 1.414-1.414L8 12.586l7.293-7.293a1 1 0 0 1 1.414 0z"
                                        clipRule="evenodd"
                                    />
                                </svg>
                                Uncapped Sessions
                            </li>
                            <li className="flex items-center">
                                <svg className="mr-3 h-4 w-4 shrink-0 text-zinc-900" fill="currentColor" viewBox="0 0 20 20">
                                    <path
                                        fillRule="evenodd"
                                        d="M16.707 5.293a1 1 0 0 1 0 1.414l-8 8a1 1 0 0 1-1.414 0l-4-4a1 1 0 0 1 1.414-1.414L8 12.586l7.293-7.293a1 1 0 0 1 1.414 0z"
                                        clipRule="evenodd"
                                    />
                                </svg>
                                Custom LLM Parameter Tuning
                            </li>
                            <li className="flex items-center">
                                <svg className="mr-3 h-4 w-4 shrink-0 text-zinc-900" fill="currentColor" viewBox="0 0 20 20">
                                    <path
                                        fillRule="evenodd"
                                        d="M16.707 5.293a1 1 0 0 1 0 1.414l-8 8a1 1 0 0 1-1.414 0l-4-4a1 1 0 0 1 1.414-1.414L8 12.586l7.293-7.293a1 1 0 0 1 1.414 0z"
                                        clipRule="evenodd"
                                    />
                                </svg>
                                Priority Architecture Review
                            </li>
                        </ul>
                        <Link
                            href="#"
                            className="mt-auto block w-full rounded-xl bg-zinc-100 px-4 py-3 text-center font-semibold text-zinc-900 transition-colors hover:bg-zinc-200"
                        >
                            Contact Integrations
                        </Link>
                    </div>
                </div>
            </div>
        </section>
    );
};
