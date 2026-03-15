"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQuery } from "@tanstack/react-query";
import type { ReactElement } from "react";
import { useMemo, useState } from "react";
import {
    getAuthMe,
    type GetSubscriptionInvitationByTokenResult,
    type PostSubscriptionInvitationAcceptResult,
    type SubscriptionMemberManagementErrorResult,
    type SubscriptionUserRole,
} from "@/lib/api";
import {
    getSubscriptionInvitationsByTokenOptions,
    postSubscriptionInvitationsByTokenAcceptMutation,
} from "@/lib/api/@tanstack/react-query.gen";
import { ReactQueryProvider } from "@/lib/react-query-provider";
import { SessionProvider, useSession } from "@/lib/session-context";

type InviteRedemptionPageProps = {
    token: string;
};

type FeedbackTone = "error" | "success";

type FeedbackState = {
    tone: FeedbackTone;
    text: string;
};

const DEFAULT_LOOKUP_ERROR = "Unable to load this invitation right now.";
const DEFAULT_ACCEPT_ERROR = "Unable to accept this invitation right now.";

const roleLabelByValue: Record<SubscriptionUserRole, string> = {
    0: "Owner",
    1: "Member",
};

const statusLabelByValue: Record<string, string> = {
    accepted: "Accepted",
    expired: "Expired",
    pending: "Pending",
    revoked: "Revoked",
};

function resolveApiError(error: unknown): SubscriptionMemberManagementErrorResult | null {
    if (error === null || typeof error !== "object") {
        return null;
    }

    const errorRecord = error as Record<string, unknown>;
    const code = errorRecord.code;
    const message = errorRecord.message;
    if (typeof code !== "string" || typeof message !== "string") {
        return null;
    }

    return {
        code,
        message,
    };
}

function resolveLookupErrorMessage(error: unknown): string {
    const apiError = resolveApiError(error);
    if (apiError?.message) {
        return apiError.message;
    }

    return DEFAULT_LOOKUP_ERROR;
}

function resolveAcceptErrorMessage(
    error: unknown,
    currentUserEmail: string | null,
    maskedEmail: string): string {
    const apiError = resolveApiError(error);
    if (apiError === null) {
        return DEFAULT_ACCEPT_ERROR;
    }

    if (apiError.code === "invitation_email_mismatch") {
        const signedInAsText = currentUserEmail ? `You are signed in as ${currentUserEmail}. ` : "";
        return `${signedInAsText}This invitation was sent to ${maskedEmail}. Sign in with the matching email to accept it.`;
    }

    return apiError.message || DEFAULT_ACCEPT_ERROR;
}

function resolveDestination(isOnboarded: boolean): string {
    return isOnboarded ? "/admin" : "/onboarding";
}

function InviteRedemptionPageContent({ token }: InviteRedemptionPageProps): ReactElement {
    const router = useRouter();
    const { currentUser, isLoading: isSessionLoading, refresh } = useSession();
    const [feedback, setFeedback] = useState<FeedbackState | null>(null);

    const invitationQuery = useQuery({
        ...getSubscriptionInvitationsByTokenOptions({
            path: {
                token,
            },
        }),
        retry: false,
    });

    const acceptMutation = useMutation(postSubscriptionInvitationsByTokenAcceptMutation());

    const invitation = invitationQuery.data as GetSubscriptionInvitationByTokenResult | undefined;
    const maskedEmail = invitation?.invitation.maskedEmail ?? "the invited account";

    const lookupErrorMessage = useMemo(() => {
        if (!invitationQuery.error) {
            return null;
        }

        return resolveLookupErrorMessage(invitationQuery.error);
    }, [invitationQuery.error]);

    const invitationStatusLabel = invitation ? (statusLabelByValue[invitation.status] ?? invitation.status) : "";
    const invitationRoleLabel = invitation ? roleLabelByValue[invitation.invitation.role] ?? "Member" : "";

    const handleAccept = async (): Promise<void> => {
        setFeedback(null);

        try {
            const result = await acceptMutation.mutateAsync({
                path: {
                    token,
                },
            }) as PostSubscriptionInvitationAcceptResult;

            await refresh();

            const authMeResult = await getAuthMe({
                throwOnError: true,
            });

            const destination = resolveDestination(authMeResult.data.isOnboarded);
            const successMessage = result.status === "already_accepted"
                ? "This invitation was already accepted for your account. Redirecting now."
                : "Invitation accepted. Redirecting now.";

            setFeedback({
                tone: "success",
                text: successMessage,
            });

            router.replace(destination);
            router.refresh();
        } catch (error: unknown) {
            setFeedback({
                tone: "error",
                text: resolveAcceptErrorMessage(error, currentUser?.email ?? null, maskedEmail),
            });
        }
    };

    const isLoading = invitationQuery.isLoading || isSessionLoading;
    const canAccept = invitation?.status === "pending" && !acceptMutation.isPending;
    const canContinue = invitation?.status === "accepted" && !acceptMutation.isPending;

    return (
        <main className="min-h-screen bg-[radial-gradient(circle_at_top,_rgba(161,98,7,0.16),_transparent_32%),linear-gradient(180deg,_#fffdf6_0%,_#f5f1e8_100%)] px-6 py-10 text-stone-900">
            <div className="mx-auto flex min-h-[calc(100vh-5rem)] max-w-5xl items-center">
                <div className="grid w-full gap-8 lg:grid-cols-[1.05fr_0.95fr]">
                    <section className="rounded-[2rem] border border-stone-300/70 bg-white/85 p-8 shadow-[0_30px_80px_rgba(28,25,23,0.08)] backdrop-blur">
                        <p className="text-sm font-semibold uppercase tracking-[0.22em] text-amber-700">Team invitation</p>
                        <h1 className="mt-4 text-4xl font-semibold tracking-tight text-stone-950">
                            Join a BuyAlan workspace
                        </h1>
                        <p className="mt-4 max-w-xl text-base leading-7 text-stone-600">
                            Accept the invitation sent to <strong>{maskedEmail}</strong> and switch directly into that subscription.
                        </p>

                        <div className="mt-8 grid gap-4 rounded-[1.5rem] border border-stone-200 bg-stone-50 p-5 sm:grid-cols-2">
                            <div>
                                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-stone-500">Subscription</p>
                                <p className="mt-2 text-lg font-medium text-stone-900">
                                    {invitation?.invitation.subscriptionDisplayText ?? "Loading"}
                                </p>
                            </div>
                            <div>
                                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-stone-500">Role</p>
                                <p className="mt-2 text-lg font-medium text-stone-900">{invitationRoleLabel || "Loading"}</p>
                            </div>
                            <div>
                                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-stone-500">Status</p>
                                <p className="mt-2 text-lg font-medium text-stone-900">{invitationStatusLabel || "Loading"}</p>
                            </div>
                            <div>
                                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-stone-500">Signed in as</p>
                                <p className="mt-2 text-lg font-medium text-stone-900">
                                    {currentUser?.email ?? "Checking session"}
                                </p>
                            </div>
                        </div>

                        {lookupErrorMessage ? (
                            <div className="mt-6 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                                {lookupErrorMessage}
                            </div>
                        ) : null}

                        {feedback ? (
                            <div
                                className={`mt-6 rounded-2xl px-4 py-3 text-sm ${
                                    feedback.tone === "success"
                                        ? "border border-emerald-200 bg-emerald-50 text-emerald-800"
                                        : "border border-red-200 bg-red-50 text-red-700"
                                }`}
                            >
                                {feedback.text}
                            </div>
                        ) : null}

                        <div className="mt-8 flex flex-col gap-3 sm:flex-row">
                            <button
                                type="button"
                                onClick={canContinue ? () => {
                                    router.replace(currentUser?.isOnboarded === true ? "/admin" : "/onboarding");
                                    router.refresh();
                                } : handleAccept}
                                disabled={isLoading || (!canAccept && !canContinue)}
                                className="inline-flex min-h-12 items-center justify-center rounded-full bg-stone-950 px-6 text-sm font-semibold text-white transition hover:bg-stone-800 disabled:cursor-not-allowed disabled:bg-stone-400"
                            >
                                {isLoading
                                    ? "Loading invitation..."
                                    : acceptMutation.isPending
                                        ? "Accepting..."
                                        : canContinue
                                            ? "Continue"
                                            : "Accept invitation"}
                            </button>
                            <Link
                                href={currentUser?.isOnboarded === true ? "/admin" : "/onboarding"}
                                className="inline-flex min-h-12 items-center justify-center rounded-full border border-stone-300 px-6 text-sm font-semibold text-stone-700 transition hover:border-stone-400 hover:text-stone-950"
                            >
                                Go to workspace
                            </Link>
                        </div>
                    </section>

                    <aside className="rounded-[2rem] border border-stone-300/60 bg-stone-950 p-8 text-stone-100 shadow-[0_30px_80px_rgba(28,25,23,0.18)]">
                        <p className="text-sm font-semibold uppercase tracking-[0.22em] text-amber-300">How it works</p>
                        <ol className="mt-6 space-y-5 text-sm leading-7 text-stone-300">
                            <li>
                                Sign in with the same email address that received the invitation.
                            </li>
                            <li>
                                Accept the invitation to create the subscription membership if it does not already exist.
                            </li>
                            <li>
                                Your active subscription switches immediately, so the admin area and onboarding use the invited workspace.
                            </li>
                        </ol>

                        <div className="mt-8 rounded-[1.5rem] border border-white/10 bg-white/5 p-5">
                            <p className="text-sm font-semibold text-white">Wrong account?</p>
                            <p className="mt-2 text-sm leading-6 text-stone-300">
                                This flow enforces an email match. If the signed-in email does not match the invitation, sign out and log back in with the invited address.
                            </p>
                        </div>
                    </aside>
                </div>
            </div>
        </main>
    );
}

export function InviteRedemptionPage({ token }: InviteRedemptionPageProps): ReactElement {
    return (
        <ReactQueryProvider>
            <SessionProvider>
                <InviteRedemptionPageContent token={token} />
            </SessionProvider>
        </ReactQueryProvider>
    );
}
