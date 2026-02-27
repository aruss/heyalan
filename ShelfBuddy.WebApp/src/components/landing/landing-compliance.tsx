import { ReactElement } from "react";

export const LandingCompliance = (): ReactElement => {
    return (
        <section id="compliance" className="bg-zinc-900 py-24 text-center text-white">
            <div className="mx-auto max-w-3xl px-6">
                <svg className="mx-auto mb-6 h-12 w-12 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth="2"
                        d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0 1 12 2.944a11.955 11.955 0 0 1-8.618 3.04A12.02 12.02 0 0 0 3 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
                    />
                </svg>
                <h2 className="mb-4 text-3xl font-bold md:text-4xl">Zero Spam Policy.</h2>
                <p className="mb-8 text-lg leading-relaxed text-zinc-400">
                    Customer trust is paramount. Strict opt-in requirements ensure interactions only occur when
                    requested. One-tap opt-out logic is hardcoded into every communication channel.
                </p>
            </div>
        </section>
    );
};
