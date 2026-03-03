import { ReactElement } from "react";
import { LuShieldCheck } from "react-icons/lu";

export const LandingCompliance = (): ReactElement => {
    return (
        <section id="compliance" className="bg-zinc-900 py-24 text-center text-white">
            <div className="mx-auto max-w-3xl px-6">
                <LuShieldCheck className="mx-auto mb-6 h-12 w-12 text-zinc-400" aria-hidden="true" />
                <h2 className="mb-4 text-3xl font-bold md:text-4xl">Zero Spam Policy.</h2>
                <p className="mb-8 text-lg leading-relaxed text-zinc-400">
                    Customer trust is paramount. Strict opt-in requirements ensure interactions only occur when
                    requested. One-tap opt-out logic is hardcoded into every communication channel.
                </p>
            </div>
        </section>
    );
};
