import { ReactElement } from "react";
import { PrivacySmsSignup } from "@/components/landing/privacy-sms-signup";


export default function PrivacyPage(): ReactElement {
    return (
        <section
            id="privacy"
            className="border-y border-zinc-100 bg-zinc-50 py-24 text-zinc-900 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-100"
        >
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-16 md:w-2/3">
                    <h2 className="mb-6 text-3xl font-bold tracking-tight md:text-5xl">
                        Privacy Policy
                    </h2>

                    <p className="mb-10 text-lg font-light text-zinc-600 dark:text-zinc-400">
                        <strong>Atlas Delivery Software, Inc.</strong> (a Delaware C Corporation) / <strong>BuyAlan</strong><br />
                        <strong>Website:</strong> buyalan.com<br />
                        <strong>Last Updated:</strong> March 15, 2026
                    </p>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-100">
                        1. Introduction
                    </h3>
                    <p className="mb-6 text-lg font-light leading-relaxed text-zinc-600 dark:text-zinc-400">
                        Atlas Delivery Software, Inc., a Delaware C corporation, doing business as BuyAlan ("Company," "we," "us," or "our"), operates the website buyalan.com (the "Website") and provides related software and services (the "Services"). This Privacy Policy explains how we collect, use, disclose, and safeguard personal information.
                    </p>
                    <p className="mb-6 text-lg font-light leading-relaxed text-zinc-600 dark:text-zinc-400">
                        <strong>Corporate Address:</strong> 584 Castro St. #2045 San Francisco, CA 94114<br />
                        <strong>Contact Email:</strong> info@buyalan.com
                    </p>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-100">
                        2. Information We Collect
                    </h3>

                    <h4 className="mt-8 mb-3 text-xl font-medium text-zinc-900 dark:text-zinc-100">2.1 Information You Provide</h4>
                    <ul className="mb-6 ml-6 list-disc space-y-2 text-lg font-light text-zinc-600 dark:text-zinc-400">
                        <li>Phone number</li>
                        <li>Name (if provided)</li>
                        <li>Billing address and associated account details</li>
                        <li>Order details, subscription information, and order history</li>
                        <li>Customer service inquiries and communications</li>
                        <li>SMS conversation content (to process requests and provide support)</li>
                        <li>Business name and related details (for commercial customers)</li>
                    </ul>

                    <h4 className="mt-8 mb-3 text-xl font-medium text-zinc-900 dark:text-zinc-100">2.2 Information Collected Automatically</h4>
                    <ul className="mb-6 ml-6 list-disc space-y-2 text-lg font-light text-zinc-600 dark:text-zinc-400">
                        <li>IP address, device identifiers, browser type, and operating system</li>
                        <li>Usage data (pages viewed, actions taken, referral URLs)</li>
                        <li>Cookies and similar technologies (where used)</li>
                        <li>Log files and security/audit logs</li>
                    </ul>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-100">
                        3. SMS Communications, TCPA Consent, and Do Not Call
                    </h3>

                    <h4 className="mt-8 mb-3 text-xl font-medium text-zinc-900 dark:text-zinc-100">3.1 Transactional/Service SMS Consent</h4>
                    <p className="mb-6 text-lg font-light leading-relaxed text-zinc-600 dark:text-zinc-400">
                        By checking an SMS consent box on our Website, submitting your phone number through the Website, or by initiating a conversation via SMS with our service, you consent to receive SMS text messages from Atlas Delivery Software, Inc. (BuyAlan) related to your inquiry, account, or customer support. Message frequency varies. Message and data rates may apply. You may opt out at any time by replying <strong>STOP</strong>. Reply <strong>HELP</strong> for assistance. Consent is not a condition of purchase.
                    </p>

                    <h4 className="mt-8 mb-3 text-xl font-medium text-zinc-900 dark:text-zinc-100">3.2 Marketing SMS Consent (Separate Opt-In)</h4>
                    <p className="mb-6 text-lg font-light leading-relaxed text-zinc-600 dark:text-zinc-400">
                        If you separately opt in to receive promotional or marketing text messages (for example, discounts, promotions, or special offers), you provide your prior express written consent to receive marketing SMS messages at the phone number provided. Marketing consent is not required to purchase and is separate from transactional messaging consent. You may opt out at any time by replying <strong>STOP</strong>.
                    </p>

                    <h4 className="mt-8 mb-3 text-xl font-medium text-zinc-900 dark:text-zinc-100">3.3 Do Not Call / Do Not Contact</h4>
                    <p className="mb-6 text-lg font-light leading-relaxed text-zinc-600 dark:text-zinc-400">
                        We honor applicable federal and state "Do Not Call" and similar rules. If you do not want to receive marketing communications, do not opt in to marketing messages (if offered) and/or opt out by replying <strong>STOP</strong>. You may also contact us at info@buyalan.com to be placed on our internal do-not-contact list for marketing communications.
                    </p>
                    <p className="mb-6 text-lg font-light italic leading-relaxed text-zinc-600 dark:text-zinc-400">
                        Note: Do-not-contact preferences may not apply to transactional messages necessary to fulfill orders, provide customer support, or deliver required notices, unless you revoke SMS consent as described above.
                    </p>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-100">
                        4. How We Use Personal Information
                    </h3>
                    <ul className="mb-6 ml-6 list-disc space-y-2 text-lg font-light text-zinc-600 dark:text-zinc-400">
                        <li>To process your requests, manage your account, and provide our Services</li>
                        <li>To communicate with you (including via SMS) about your account and service requests</li>
                        <li>To provide customer support</li>
                        <li>To detect, prevent, and investigate fraud, abuse, or security incidents</li>
                        <li>To operate, maintain, and improve the Website and Services</li>
                        <li>To comply with legal obligations and enforce agreements</li>
                    </ul>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-100">
                        5. Payments
                    </h3>
                    <p className="mb-6 text-lg font-light leading-relaxed text-zinc-600 dark:text-zinc-400">
                        Payment information, if collected, is processed through third-party payment processors. We do not store full payment card numbers on our servers. Your payment processor's privacy practices will govern their handling of your payment data.
                    </p>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-100">
                        6. Sharing of Information
                    </h3>
                    <p className="mb-4 text-lg font-light leading-relaxed text-zinc-600 dark:text-zinc-400">
                        We may share personal information with:
                    </p>
                    <ul className="mb-6 ml-6 list-disc space-y-2 text-lg font-light text-zinc-600 dark:text-zinc-400">
                        <li>Payment processors and transaction partners to process payments</li>
                        <li>Technology vendors and service providers (e.g., hosting, communications, analytics) performing services on our behalf</li>
                        <li>Professional advisors (e.g., legal, accounting, insurance) as needed</li>
                        <li>Law enforcement, regulators, or other parties when required by law or to protect rights and safety</li>
                    </ul>
                    <p className="mb-6 text-lg font-medium text-zinc-900 dark:text-zinc-100">
                        We do not sell personal information.
                    </p>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-100">
                        7. Data Security
                    </h3>
                    <p className="mb-6 text-lg font-light leading-relaxed text-zinc-600 dark:text-zinc-400">
                        We implement commercially reasonable safeguards designed to protect personal information. However, no security measures are perfect, and we cannot guarantee absolute security. You use the Website and Services at your own risk.
                    </p>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-100">
                        8. Data Retention
                    </h3>
                    <p className="mb-6 text-lg font-light leading-relaxed text-zinc-600 dark:text-zinc-400">
                        We retain personal information as long as reasonably necessary to provide the Services, maintain business records, comply with legal obligations, resolve disputes, and enforce agreements.
                    </p>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-100">
                        9. California Privacy Rights (CCPA/CPRA)
                    </h3>
                    <p className="mb-6 text-lg font-light leading-relaxed text-zinc-600 dark:text-zinc-400">
                        If you are a California resident, you may have rights to request access to, deletion of, and correction of certain personal information, and to know categories of information collected and disclosed. To exercise these rights, contact us at: <strong>info@buyalan.com</strong>. We will verify your request before responding.
                    </p>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-100">
                        10. Children's Privacy
                    </h3>
                    <p className="mb-6 text-lg font-light leading-relaxed text-zinc-600 dark:text-zinc-400">
                        The Services are not intended for individuals under 18 years of age. We do not knowingly collect personal information from minors.
                    </p>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-100">
                        11. Changes to This Policy
                    </h3>
                    <p className="mb-6 text-lg font-light leading-relaxed text-zinc-600 dark:text-zinc-400">
                        We may update this Privacy Policy from time to time. The "Last Updated" date above indicates when it was last revised. Continued use of the Services after an update constitutes acceptance of the revised policy.
                    </p>

                    <h3 className="mt-12 mb-4 text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-100">
                        12. Contact Us
                    </h3>
                    <p className="text-lg font-light leading-relaxed text-zinc-600 dark:text-zinc-400">
                        <strong>Atlas Delivery Software, Inc. (BuyAlan)</strong><br />
                        584 Castro St. #2045 San Francisco, CA 94114<br />
                        Website: buyalan.com<br />
                        Email: info@buyalan.com
                    </p>
                    <span id="sms-opt-in">&nbsp;</span>
                    <p className="mt-24 mb-6">
                        <PrivacySmsSignup />
                    </p>                    
                </div>
            </div>
        </section>
    );
}
