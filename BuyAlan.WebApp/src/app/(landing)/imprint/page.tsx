import { ReactElement } from "react";

export default function Imprint(): ReactElement {
    return (
       <section id="imprint" className="border-y border-zinc-100 bg-zinc-50 py-24">
    <div className="mx-auto max-w-7xl px-6">
        <div className="mb-16 md:w-2/3">
            <h2 className="mb-6 text-3xl font-bold tracking-tight md:text-5xl">
                Imprint
            </h2>

            <p className="mb-10 text-lg font-light text-zinc-600">
                <strong>Atlas Delivery Software, Inc.</strong> (a Delaware C Corporation) / <strong>BuyAlan</strong><br />
                <strong>Website:</strong> buyalan.com<br />
                <strong>Last Updated:</strong> March 15, 2026
            </p>

            <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900">
                Information pursuant to legal requirements
            </h3>
            <p className="mb-6 text-lg font-light text-zinc-600 leading-relaxed">
                <strong>Atlas Delivery Software, Inc.</strong><br />
                584 Castro St. #2045<br />
                San Francisco, CA 94114<br />
                United States
            </p>

            <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900">
                Represented by
            </h3>
            <p className="mb-6 text-lg font-light text-zinc-600 leading-relaxed">
                [Insert Name], CEO / Managing Director
            </p>

            <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900">
                Contact Information
            </h3>
            <p className="mb-6 text-lg font-light text-zinc-600 leading-relaxed">
                <strong>Email:</strong> info@buyalan.com<br />
                <strong>Phone:</strong> [Insert Phone Number, if applicable]
            </p>

            <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900">
                Corporate Registration
            </h3>
            <p className="mb-6 text-lg font-light text-zinc-600 leading-relaxed">
                Registered in the State of Delaware, USA.<br />
                <strong>Registration Number:</strong> [Insert Delaware File Number or equivalent]
            </p>

            <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900">
                Dispute Resolution
            </h3>
            <p className="mb-6 text-lg font-light text-zinc-600 leading-relaxed">
                We are not willing or obliged to participate in dispute resolution proceedings before a consumer arbitration board. The contents of our pages have been created with the utmost care; however, we cannot guarantee the contents' accuracy, completeness, or topicality.
            </p>
        </div>
    </div>
</section>
    );
}
