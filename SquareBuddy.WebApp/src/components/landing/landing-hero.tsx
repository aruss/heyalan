import Link from "next/link";
import { ReactElement } from "react";

export const LandingHero = (): ReactElement => {
    return (
        <section className="flex min-h-screen items-center px-6 pb-12 pt-20">
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
                    <Link
                        href="#"
                        className="w-full rounded-full bg-zinc-900 px-8 py-4 text-lg font-medium text-white transition-all hover:scale-105 hover:bg-zinc-800 sm:w-auto"
                    >
                        Connect Square
                    </Link>
                    <Link
                        href="#demo"
                        className="w-full rounded-full border border-zinc-200 px-8 py-4 text-lg font-medium text-zinc-900 transition-colors hover:bg-zinc-50 sm:w-auto"
                    >
                        View Live Demo
                    </Link>
                </div>

                <div className="mt-24 flex flex-col items-center border-t border-zinc-100 pt-10">
                    <p className="mb-6 text-sm font-medium tracking-widest text-zinc-400 uppercase">Supported Channels</p>
                    <div className="flex space-x-8 grayscale opacity-70">
                        <svg className="h-8 w-8" viewBox="0 0 24 24" fill="currentColor">
                            <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 0 1-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 0 1-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 0 1 2.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0 0 12.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 0 0 5.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 0 0-3.48-8.413z" />
                        </svg>
                        <svg className="h-8 w-8" viewBox="0 0 24 24" fill="currentColor">
                            <path d="M12 0C5.373 0 0 5.373 0 12s5.373 12 12 12 12-5.373 12-12S18.627 0 12 0zm5.894 8.221-1.97 9.28c-.145.658-.537.818-1.084.508l-3-2.21-1.446 1.394c-.14.18-.357.295-.6.295h-.005l.213-3.054 5.56-5.022c.24-.213-.054-.334-.373-.121l-6.869 4.326-2.96-.924c-.64-.203-.658-.64.135-.954l11.566-4.458c.538-.196 1.006.128.832.94z" />
                        </svg>
                        <svg className="h-8 w-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth="2"
                                d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2h-5l-5 5v-5z"
                            />
                        </svg>
                    </div>
                </div>
            </div>
        </section>
    );
};
