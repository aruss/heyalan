import { ReactElement } from "react";
import { PrimaryActionButton, SecondaryActionButton } from "@/components/landing/ui/action-buttons";
import { SiGooglemessages, SiTelegram, SiWhatsapp } from "react-icons/si";

export const LandingHero = (): ReactElement => {
    return (
        <section className="flex min-h-screen items-center px-6 pb-12 pt-24 md:pt-20">
            <div className="mx-auto max-w-5xl text-center">
                <h1 className="mb-8 text-5xl leading-tight font-bold tracking-tighter md:text-7xl">
                    Conversational Sales <br className="hidden md:block" /> for your <span className="bg-gradient-to-r from-zinc-900 to-zinc-500 bg-clip-text text-transparent">
                        Square.
                    </span>
                </h1>
                <p className="mx-auto mb-12 max-w-3xl text-xl leading-relaxed font-light text-zinc-600 md:text-2xl">
                    An autonomous AI agent that knows your inventory. Engage customers via Text, WhatsApp, and Telegram.
                    Drive upsells, process payments, and schedule shipping - all through natural language.
                </p>
                <div className="flex flex-col items-center justify-center space-y-4 sm:flex-row sm:space-x-6 sm:space-y-0">
                    <PrimaryActionButton
                        href="/admin"
                        className="w-full sm:w-auto"
                    >
                        Connect Square
                    </PrimaryActionButton>
                    <SecondaryActionButton
                        href="#demo"
                        className="w-full sm:w-auto"
                    >
                        View Live Demo
                    </SecondaryActionButton>
                </div>

                <div className="mt-24 flex flex-col items-center border-t border-zinc-100 pt-10">
                    <p className="mb-6 text-sm font-medium tracking-widest text-zinc-400 uppercase">Supported Channels</p>
                    <div className="flex space-x-8 grayscale opacity-70">
                        <SiWhatsapp className="h-8 w-8" aria-hidden="true" />
                        <SiTelegram className="h-8 w-8" aria-hidden="true" />
                        <SiGooglemessages className="h-8 w-8" aria-hidden="true" />
                    </div>
                </div>
            </div>
        </section>
    );
};
