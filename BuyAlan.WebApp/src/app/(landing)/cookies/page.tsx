import { ReactElement } from "react";

export default function CookiesPage(): ReactElement {
    return (
        <section id="cookies" className="border-y border-zinc-100 bg-zinc-50 py-24">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-16 md:w-2/3">
                    <h2 className="mb-6 text-3xl font-bold tracking-tight md:text-5xl">
                        Cookie Policy
                    </h2>

                    <p className="mb-10 text-lg font-light text-zinc-600">
                        <strong>Atlas Delivery Software, Inc.</strong> (a Delaware C Corporation) / <strong>BuyAlan</strong><br />
                        <strong>Website:</strong> buyalan.com<br />
                        <strong>Last Updated:</strong> March 15, 2026
                    </p>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900">
                        1. What are Cookies?
                    </h3>
                    <p className="mb-6 text-lg font-light text-zinc-600 leading-relaxed">
                        Cookies are small text files that are placed on your computer or mobile device when you visit a website. They are widely used to make websites and software work more efficiently, as well as to provide a seamless and personalized user experience.
                    </p>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900">
                        2. How We Use Cookies
                    </h3>
                    <p className="mb-6 text-lg font-light text-zinc-600 leading-relaxed">
                        At BuyAlan, we respect your privacy and believe in total transparency. We strictly use cookies only for essential and functional purposes to ensure our application operates smoothly. <strong>We do not use any third-party advertising, marketing, or invasive tracking cookies.</strong>
                    </p>
                    <p className="mb-4 text-lg font-light text-zinc-600 leading-relaxed">
                        Specifically, our application utilizes cookies solely for the following reasons:
                    </p>
                    <ul className="mb-6 ml-6 list-disc text-lg font-light text-zinc-600 space-y-2">
                        <li><strong>Authentication & Security (Essential):</strong> To securely maintain your active login session so you don't have to repeatedly enter your credentials while navigating the software.</li>
                        <li><strong>User Interface Preferences (Functional):</strong> To remember your custom layout choices across sessions, such as whether your navigation sidebar is toggled open or closed, and your selected visual theme (e.g., Light or Dark mode).</li>
                    </ul>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900">
                        3. Managing Your Cookies
                    </h3>
                    <p className="mb-6 text-lg font-light text-zinc-600 leading-relaxed">
                        Most modern web browsers allow you to control and manage cookies through their settings. You can choose to clear your cookies at any time. However, please note that if you disable or clear the cookies associated with buyalan.com, your active session will be logged out, and your interface preferences (like your theme or sidebar layout) will revert to their default states.
                    </p>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900">
                        4. Contact
                    </h3>
                    <p className="mb-6 text-lg font-light text-zinc-600 leading-relaxed">
                        If you have any questions or concerns regarding our straightforward use of cookies, please reach out to us at:
                    </p>
                    <p className="mb-6 text-lg font-light text-zinc-600 leading-relaxed">
                        <strong>Atlas Delivery Software, Inc. (BuyAlan)</strong><br />
                        584 Castro St. #2045 San Francisco, CA 94114<br />
                        Website: buyalan.com<br />
                        Email: info@buyalan.com
                    </p>
                </div>
            </div>
        </section>
    );
}
