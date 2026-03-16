"use client";

import type { ChangeEvent, ReactElement } from "react";
import { Suspense, useCallback, useEffect, useRef, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { z } from "zod";
import {
    MessageCircle,
    Phone,
    Send,
    Link as LinkIcon,
    CheckCircle2,
    ArrowRight,
    Mail,
    Smile,
    Briefcase,
    MessagesSquare,
    UserPlus,
    Users,
    Sparkles
} from "lucide-react";
import {
    getOnboardingSubscriptionsBySubscriptionIdState,
    patchOnboardingAgentsByAgentIdChannels,
    patchOnboardingAgentsByAgentIdProfile,
    postOnboardingSubscriptionsBySubscriptionIdAgents,
    postOnboardingSubscriptionsBySubscriptionIdFinalize,
    postOnboardingSubscriptionsBySubscriptionIdMembersInvitations,
    postOnboardingSubscriptionsBySubscriptionIdMembersInvitationsComplete,
    postSubscriptionsBySubscriptionIdSquareAuthorize,
    type AgentPersonality as ApiAgentPersonality,
    type GetSubscriptionOnboardingStateResult,
    type SubscriptionInvitationItem,
    type SubscriptionMemberItem,
    type SubscriptionUserRole
} from "@/lib/api";
import { PrimaryActionButton, SecondaryActionButton } from "@/components/landing/ui/action-buttons";
import { useFeatureFlag } from "@/lib/feature-flags";
import { useSession } from "@/lib/session-context";

type OnboardingStep = 1 | 2 | 3 | 4 | 5;

type AgentPersonality = "casual" | "balanced" | "business";

type ChannelKey = "whatsapp" | "phone" | "telegram";

type ChannelState = {
    whatsapp: string;
    phone: string;
    telegram: string;
};

type FormState = {
    squareConnected: boolean;
    hasSavedTelegramToken: boolean;
    agentName: string;
    agentPersonality: AgentPersonality;
    channels: ChannelState;
    inviteEmail: string;
    inviteRole: SubscriptionUserRole;
};

type StepMessageKind = "error" | "info";

type StepMessage = {
    kind: StepMessageKind;
    text: string;
};

type OnboardingProfileDraft = {
    agentName: string;
    agentPersonality: AgentPersonality;
};

type OnboardingProfilePrefill = {
    name: string | null;
    personality: ApiAgentPersonality | null;
};

type OnboardingChannelsPrefill = {
    twilioPhoneNumber: string | null;
    whatsappNumber: string | null;
    hasTelegramBotToken: boolean;
};

type ExtendedOnboardingState = GetSubscriptionOnboardingStateResult & {
    profilePrefill?: OnboardingProfilePrefill;
    channelsPrefill?: OnboardingChannelsPrefill;
};

const E164_LIKE_REGEX = /^\+[1-9]\d{7,14}$/;
const ONBOARDING_PROFILE_DRAFT_KEY_PREFIX = "onboarding-profile-draft";
const OWNER_ROLE = 0 as SubscriptionUserRole;
const MEMBER_ROLE = 1 as SubscriptionUserRole;

const roleLabelByValue: Record<number, string> = {
    [OWNER_ROLE]: "Owner",
    [MEMBER_ROLE]: "Member"
};

const profileSchema = z.object({
    agentName: z.string().trim().min(1, "Agent name is required."),
    agentPersonality: z.enum(["casual", "balanced", "business"])
});

const channelsSchema = z.object({
    whatsapp: z.string().trim().refine((value) => {
        return value.length === 0 || E164_LIKE_REGEX.test(value);
    }, "Use E.164-like format (e.g. +15551234567)."),
    phone: z.string().trim().refine((value) => {
        return value.length === 0 || E164_LIKE_REGEX.test(value);
    }, "Use E.164-like format (e.g. +15551234567)."),
    telegram: z.string().trim()
}).superRefine((channels, ctx) => {
    const hasAnyChannel = channels.telegram.length > 0 ||
        channels.phone.length > 0 ||
        channels.whatsapp.length > 0;

    if (!hasAnyChannel) {
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: "At least one channel must be connected.",
            path: ["telegram"]
        });
    }
});

const inviteSchema = z.object({
    inviteEmail: z.string().trim().min(1, "Email is required.").email("Use a valid email format."),
    inviteRole: z.union([z.literal(OWNER_ROLE), z.literal(MEMBER_ROLE)])
});

const mapApiStepToUiStep = (apiStep: string): OnboardingStep => {
    const normalizedStep = apiStep.toLowerCase();
    switch (normalizedStep) {
        case "square_connect":
            return 1;
        case "profile":
            return 2;
        case "channels":
            return 3;
        case "invitations":
        case "finalize":
            return 4;
        default:
            return 1;
    }
};

const isSquareConnectedFromState = (state: GetSubscriptionOnboardingStateResult | null, fallback: boolean): boolean => {
    if (!state) {
        return fallback;
    }

    const squareConnectStep = state.steps.find((item) => item.step === "square_connect");
    return squareConnectStep?.status === "completed";
};

const resolveApiErrorMessage = (error: unknown, fallback: string): string => {
    if (error && typeof error === "object") {
        const errorRecord = error as Record<string, unknown>;
        const message = errorRecord.message;
        if (typeof message === "string" && message.trim().length > 0) {
            return message;
        }

        const detail = errorRecord.detail;
        if (typeof detail === "string" && detail.trim().length > 0) {
            return detail;
        }
    }

    return fallback;
};

const normalizeEmail = (value: string): string => {
    return value.trim().toLowerCase();
};

const getRoleLabel = (role: SubscriptionUserRole): string => {
    return roleLabelByValue[role] ?? "Member";
};

const formatDateTime = (value: string | null): string => {
    if (!value) {
        return "Not available";
    }

    const parsedDate = new Date(value);
    if (Number.isNaN(parsedDate.getTime())) {
        return "Not available";
    }

    return new Intl.DateTimeFormat(undefined, {
        dateStyle: "medium",
        timeStyle: "short"
    }).format(parsedDate);
};

const mapPersonalityToApi = (personality: AgentPersonality): ApiAgentPersonality => {
    switch (personality) {
        case "casual":
            return "Casual";
        case "balanced":
            return "Balanced";
        case "business":
            return "Business";
    }
};

const mapApiPersonalityToLocal = (
    personality: ApiAgentPersonality | null | undefined,
    fallback: AgentPersonality): AgentPersonality => {
    if (personality === "Casual") {
        return "casual";
    }

    if (personality === "Business") {
        return "business";
    }

    if (personality === "Balanced") {
        return "balanced";
    }

    return fallback;
};

const getRandomName = (names = ["Liam", "Olivia", "Noah", "Emma", "Oliver", "Ava", "Elijah", "Alan", "Charlotte", "William", "Sophia"]) => names[Math.floor(Math.random() * names.length)];

const getProfileDraftStorageKey = (subscriptionId: string): string => {
    return `${ONBOARDING_PROFILE_DRAFT_KEY_PREFIX}:${subscriptionId}`;
};

const readProfileDraft = (subscriptionId: string): OnboardingProfileDraft | null => {
    if (typeof window === "undefined") {
        return null;
    }

    const rawDraft = window.localStorage.getItem(getProfileDraftStorageKey(subscriptionId));
    if (!rawDraft) {
        return null;
    }

    try {
        const parsedDraft = JSON.parse(rawDraft) as Partial<OnboardingProfileDraft>;
        if (typeof parsedDraft.agentName !== "string") {
            return null;
        }

        if (parsedDraft.agentPersonality !== "casual" &&
            parsedDraft.agentPersonality !== "balanced" &&
            parsedDraft.agentPersonality !== "business") {
            return null;
        }

        return {
            agentName: parsedDraft.agentName,
            agentPersonality: parsedDraft.agentPersonality
        };
    } catch {
        return null;
    }
};

const writeProfileDraft = (subscriptionId: string, draft: OnboardingProfileDraft): void => {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.setItem(getProfileDraftStorageKey(subscriptionId), JSON.stringify(draft));
};

const OnboardingPageContent = (): ReactElement => {
    const router = useRouter();
    const searchParams = useSearchParams();
    const {
        currentUser,
        isLoading: isSessionLoading,
        errorMessage: sessionErrorMessage
    } = useSession();
    const isTeamMembersEnabled = useFeatureFlag("teamMembers");

    const [step, setStep] = useState<OnboardingStep>(1);
    const [subscriptionId, setSubscriptionId] = useState<string | null>(null);
    const [onboardingState, setOnboardingState] = useState<GetSubscriptionOnboardingStateResult | null>(null);
    const [stepMessages, setStepMessages] = useState<Partial<Record<OnboardingStep, StepMessage>>>({});
    const [isBusy, setIsBusy] = useState<boolean>(false);
    const [hasResolvedSubscriptionContext, setHasResolvedSubscriptionContext] = useState<boolean>(false);

    const [formData, setFormData] = useState<FormState>({
        squareConnected: false,
        hasSavedTelegramToken: false,
        agentName: getRandomName(),
        agentPersonality: "balanced",
        channels: {
            whatsapp: "",
            phone: "",
            telegram: ""
        },
        inviteEmail: "",
        inviteRole: MEMBER_ROLE
    });
    const formDataRef = useRef<FormState>(formData);

    const profileForm = useForm({
        resolver: zodResolver(profileSchema),
        defaultValues: {
            agentName: formData.agentName,
            agentPersonality: formData.agentPersonality
        }
    });

    const channelsForm = useForm({
        resolver: zodResolver(channelsSchema),
        defaultValues: {
            whatsapp: formData.channels.whatsapp,
            phone: formData.channels.phone,
            telegram: formData.channels.telegram
        }
    });

    const inviteForm = useForm({
        resolver: zodResolver(inviteSchema),
        defaultValues: {
            inviteEmail: formData.inviteEmail,
            inviteRole: formData.inviteRole
        }
    });

    const setMessage = (targetStep: OnboardingStep, kind: StepMessageKind, text: string): void => {
        setStepMessages((prev) => ({
            ...prev,
            [targetStep]: { kind, text }
        }));
    };

    const clearMessage = (targetStep: OnboardingStep): void => {
        setStepMessages((prev) => {
            const next = { ...prev };
            delete next[targetStep];
            return next;
        });
    };

    const applyProfileDraft = useCallback((draft: OnboardingProfileDraft): void => {
        setFormData((prev) => ({
            ...prev,
            agentName: draft.agentName,
            agentPersonality: draft.agentPersonality
        }));
        profileForm.setValue("agentName", draft.agentName, { shouldDirty: false });
        profileForm.setValue("agentPersonality", draft.agentPersonality, { shouldDirty: false });
    }, [profileForm]);

    const loadOnboardingStateAsync = useCallback(async (
        id: string,
        options?: {
            resumeMode?: boolean;
            applyServerPrefill?: boolean;
        }): Promise<GetSubscriptionOnboardingStateResult> => {
        const requestOptions: unknown = {
            path: { subscriptionId: id },
            cache: "no-store",
            throwOnError: true,
            query: { resumeMode: options?.resumeMode === true }
        };

        const onboardingResponse = await getOnboardingSubscriptionsBySubscriptionIdState(
            requestOptions as Parameters<typeof getOnboardingSubscriptionsBySubscriptionIdState>[0]);

        const state = onboardingResponse.data as ExtendedOnboardingState;
        setOnboardingState(state);

        const squareConnectStep = state.steps.find((item) => item.step === "square_connect");
        const squareConnected = squareConnectStep?.status === "completed";
        const formSnapshot = formDataRef.current;
        const applyServerPrefill = options?.applyServerPrefill === true;

        const prefillName = applyServerPrefill
            ? state.profilePrefill?.name?.trim() ?? ""
            : "";
        const nextAgentName = prefillName.length > 0
            ? prefillName
            : formSnapshot.agentName;
        const nextAgentPersonality = applyServerPrefill
            ? mapApiPersonalityToLocal(state.profilePrefill?.personality, formSnapshot.agentPersonality)
            : formSnapshot.agentPersonality;
        const nextWhatsapp = applyServerPrefill
            ? (state.channelsPrefill?.whatsappNumber ?? "")
            : formSnapshot.channels.whatsapp;
        const nextPhone = applyServerPrefill
            ? (state.channelsPrefill?.twilioPhoneNumber ?? "")
            : formSnapshot.channels.phone;
        const hasSavedTelegramToken = state.channelsPrefill?.hasTelegramBotToken === true;
        const availableRoles = state.invitations.availableRoles;
        const nextInviteRole = availableRoles.includes(formSnapshot.inviteRole)
            ? formSnapshot.inviteRole
            : (availableRoles[0] ?? MEMBER_ROLE);

        const nextFormData: FormState = {
            squareConnected,
            hasSavedTelegramToken,
            agentName: nextAgentName,
            agentPersonality: nextAgentPersonality,
            channels: {
                whatsapp: nextWhatsapp,
                phone: nextPhone,
                telegram: applyServerPrefill ? "" : formSnapshot.channels.telegram
            },
            inviteEmail: formSnapshot.inviteEmail,
            inviteRole: nextInviteRole
        };

        setFormData(nextFormData);
        profileForm.setValue("agentName", nextFormData.agentName, { shouldDirty: false });
        profileForm.setValue("agentPersonality", nextFormData.agentPersonality, { shouldDirty: false });
        channelsForm.setValue("whatsapp", nextFormData.channels.whatsapp, { shouldDirty: false });
        channelsForm.setValue("phone", nextFormData.channels.phone, { shouldDirty: false });
        channelsForm.setValue("telegram", nextFormData.channels.telegram, { shouldDirty: false });
        inviteForm.setValue("inviteEmail", nextFormData.inviteEmail, { shouldDirty: false });
        inviteForm.setValue("inviteRole", nextFormData.inviteRole, { shouldDirty: false });
        return state;
    }, [channelsForm, inviteForm, profileForm]);

    const refreshOnboardingStateAsync = async (id: string): Promise<GetSubscriptionOnboardingStateResult> => {
        return loadOnboardingStateAsync(id, { resumeMode: false, applyServerPrefill: false });
    };

    const ensurePrimaryAgentIdAsync = async (id: string): Promise<string> => {
        if (onboardingState?.primaryAgentId) {
            return onboardingState.primaryAgentId;
        }

        const createResponse = await postOnboardingSubscriptionsBySubscriptionIdAgents({
            path: { subscriptionId: id },
            throwOnError: true
        });

        setOnboardingState(createResponse.data.state);
        return createResponse.data.agentId;
    };

    useEffect(() => {
        formDataRef.current = formData;
    }, [formData]);

    const isSquareConnected = isSquareConnectedFromState(onboardingState, formData.squareConnected);
    const isSubscriptionContextLoading = isSessionLoading || (!hasResolvedSubscriptionContext && !subscriptionId);

    useEffect(() => {
        let isCancelled = false;

        const initializeAsync = async (): Promise<void> => {
            try {
                if (isSessionLoading) {
                    return;
                }

                if (currentUser?.isOnboarded === true) {
                    setHasResolvedSubscriptionContext(true);
                    router.replace("/admin");
                    return;
                }

                setHasResolvedSubscriptionContext(false);
                setIsBusy(true);
                const activeSubscriptionId = currentUser?.activeSubscriptionId ?? null;

                if (isCancelled) {
                    return;
                }

                if (!activeSubscriptionId) {
                    setSubscriptionId(null);
                    const message = sessionErrorMessage ?? "No active subscription membership was found for your account.";
                    setMessage(1, "error", message);
                    setHasResolvedSubscriptionContext(true);
                    return;
                }

                setSubscriptionId(activeSubscriptionId);
                const state = await loadOnboardingStateAsync(activeSubscriptionId, {
                    resumeMode: true,
                    applyServerPrefill: true
                });
                const profileDraft = readProfileDraft(activeSubscriptionId);
                if (profileDraft && !(state as ExtendedOnboardingState).profilePrefill?.name) {
                    applyProfileDraft(profileDraft);
                }
                setStep(mapApiStepToUiStep(state.currentStep));

                const squareConnectStatus = searchParams.get("squareConnect");
                const squareConnectError = searchParams.get("squareConnectError");

                if (squareConnectStatus === "success") {
                    setMessage(1, "info", "Square account connected.");
                }

                if (squareConnectError) {
                    setMessage(1, "error", `Square connection failed: ${squareConnectError}`);
                }

                if (squareConnectStatus || squareConnectError) {
                    router.replace("/onboarding");
                }
                setHasResolvedSubscriptionContext(true);
            } catch (error: unknown) {
                if (!isCancelled) {
                    setMessage(1, "error", resolveApiErrorMessage(error, "Unable to load onboarding state."));
                    setHasResolvedSubscriptionContext(true);
                }
            } finally {
                if (!isCancelled) {
                    setIsBusy(false);
                }
            }
        };

        void initializeAsync();
        return () => {
            isCancelled = true;
        };
    }, [applyProfileDraft, currentUser?.activeSubscriptionId, isSessionLoading, loadOnboardingStateAsync, router, searchParams, sessionErrorMessage]);

    useEffect(() => {
        if (step !== 4 || isSquareConnected) {
            return;
        }

        router.replace("/admin");
    }, [isSquareConnected, router, step]);

    const nextStep = (): void => {
        setStep((prev) => {
            return (prev + 1) as OnboardingStep;
        });
    };

    const handleSkipWithWarning = (currentStep: OnboardingStep): void => {
        nextStep();
        setMessage(currentStep, "info", "Continue setup later. You can always return from the dashboard.");
    };

    const handleConnectSquare = async (): Promise<void> => {
        if (!subscriptionId) {
            if (isSubscriptionContextLoading) {
                setMessage(1, "info", "Loading subscription context. Please try again in a moment.");
                return;
            }

            setMessage(1, "error", "Missing subscription context.");
            return;
        }

        try {
            setIsBusy(true);
            clearMessage(1);

            const response = await postSubscriptionsBySubscriptionIdSquareAuthorize({
                path: { subscriptionId },
                query: { returnUrl: "/onboarding" },
                throwOnError: true
            });

            window.location.assign(response.data.authorizeUrl);
        } catch (error: unknown) {
            setMessage(1, "error", resolveApiErrorMessage(error, "Unable to start Square connect."));
        } finally {
            setIsBusy(false);
        }
    };

    const handleAgentNameChange = (e: ChangeEvent<HTMLInputElement>): void => {
        const nextValue = e.target.value;
        setFormData({ ...formData, agentName: nextValue });
        profileForm.setValue("agentName", nextValue, { shouldDirty: true, shouldTouch: true, shouldValidate: true });
        if (subscriptionId) {
            writeProfileDraft(subscriptionId, {
                agentName: nextValue,
                agentPersonality: formData.agentPersonality
            });
        }
    };

    const handlePersonalityChange = (type: AgentPersonality): void => {
        setFormData({ ...formData, agentPersonality: type });
        profileForm.setValue("agentPersonality", type, { shouldDirty: true, shouldTouch: true, shouldValidate: true });
        if (subscriptionId) {
            writeProfileDraft(subscriptionId, {
                agentName: formData.agentName,
                agentPersonality: type
            });
        }
    };

    const handleInviteEmailChange = (e: ChangeEvent<HTMLInputElement>): void => {
        const nextValue = e.target.value;
        setFormData({ ...formData, inviteEmail: nextValue });
        inviteForm.setValue("inviteEmail", nextValue, { shouldDirty: true, shouldTouch: true, shouldValidate: true });
    };

    const handleInviteRoleChange = (e: ChangeEvent<HTMLSelectElement>): void => {
        const nextRole = Number(e.target.value) as SubscriptionUserRole;
        setFormData({ ...formData, inviteRole: nextRole });
        inviteForm.setValue("inviteRole", nextRole, { shouldDirty: true, shouldTouch: true, shouldValidate: true });
    };

    const handleSuggestionSelect = (email: string): void => {
        setFormData({ ...formData, inviteEmail: email });
        inviteForm.setValue("inviteEmail", email, { shouldDirty: true, shouldTouch: true, shouldValidate: true });
        clearMessage(4);
    };

    const handleChannelChange = (channel: ChannelKey, value: string): void => {
        const nextState = {
            ...formData,
            channels: { ...formData.channels, [channel]: value }
        };
        setFormData(nextState);
        channelsForm.setValue(channel, value, { shouldDirty: true, shouldTouch: true, shouldValidate: true });
    };

    const handleProfileContinue = async (): Promise<void> => {
        if (!subscriptionId) {
            setMessage(2, "error", "Missing subscription context.");
            return;
        }

        const isValid = await profileForm.trigger();
        if (!isValid) {
            setMessage(2, "error", "Please fill all required profile fields.");
            return;
        }

        try {
            setIsBusy(true);
            clearMessage(2);

            const agentId = await ensurePrimaryAgentIdAsync(subscriptionId);
            await patchOnboardingAgentsByAgentIdProfile({
                path: { agentId },
                body: {
                    name: formData.agentName,
                    personality: mapPersonalityToApi(formData.agentPersonality)
                },
                throwOnError: true
            });

            const state = await refreshOnboardingStateAsync(subscriptionId);
            writeProfileDraft(subscriptionId, {
                agentName: formData.agentName,
                agentPersonality: formData.agentPersonality
            });

            if (!isSquareConnectedFromState(state, formData.squareConnected)) {
                setStep(3);
                setMessage(2, "info", "Profile saved. Continue with channels or skip for now.");
                return;
            }

            setStep(mapApiStepToUiStep(state.currentStep));
        } catch (error: unknown) {
            setMessage(2, "error", resolveApiErrorMessage(error, "Unable to save agent profile."));
        } finally {
            setIsBusy(false);
        }
    };

    const handleChannelsContinue = async (): Promise<void> => {
        if (!subscriptionId) {
            setMessage(3, "error", "Missing subscription context.");
            return;
        }

        const isValid = await channelsForm.trigger();
        if (!isValid) {
            setMessage(3, "error", "Please provide valid channel values.");
            return;
        }

        try {
            setIsBusy(true);
            clearMessage(3);

            const agentId = await ensurePrimaryAgentIdAsync(subscriptionId);
            await patchOnboardingAgentsByAgentIdChannels({
                path: { agentId },
                body: {
                    twilioPhoneNumber: formData.channels.phone,
                    telegramBotToken: formData.channels.telegram,
                    whatsappNumber: formData.channels.whatsapp
                },
                throwOnError: true
            });

            const state = await refreshOnboardingStateAsync(subscriptionId);
            setStep(mapApiStepToUiStep(state.currentStep));
        } catch (error: unknown) {
            setMessage(3, "error", resolveApiErrorMessage(error, "Unable to save channel settings."));
        } finally {
            setIsBusy(false);
        }
    };

    const handleInvitationCreate = async (): Promise<void> => {
        if (!subscriptionId) {
            setMessage(4, "error", "Missing subscription context.");
            return;
        }

        if (!isSquareConnected) {
            router.replace("/admin");
            return;
        }

        const isValid = await inviteForm.trigger();
        if (!isValid) {
            setMessage(4, "error", "Enter a valid email address and role.");
            return;
        }

        try {
            setIsBusy(true);
            clearMessage(4);

            const response = await postOnboardingSubscriptionsBySubscriptionIdMembersInvitations({
                path: { subscriptionId },
                body: {
                    email: formData.inviteEmail,
                    role: formData.inviteRole
                },
                throwOnError: true
            });

            setOnboardingState(response.data);
            setFormData((current) => ({
                ...current,
                inviteEmail: ""
            }));
            inviteForm.setValue("inviteEmail", "", { shouldDirty: false, shouldTouch: false, shouldValidate: false });
            setStep(mapApiStepToUiStep(response.data.currentStep));
            setMessage(4, "info", "Invitation sent. You can send more or finish setup.");
        } catch (error: unknown) {
            setMessage(4, "error", resolveApiErrorMessage(error, "Unable to send that invitation."));
        } finally {
            setIsBusy(false);
        }
    };

    const runCompleteOnboardingAsync = async (): Promise<void> => {
        if (!subscriptionId) {
            setMessage(4, "error", "Missing subscription context.");
            return;
        }

        if (!isSquareConnected) {
            router.replace("/admin");
            return;
        }

        try {
            setIsBusy(true);
            clearMessage(4);

            const completionResponse = await postOnboardingSubscriptionsBySubscriptionIdMembersInvitationsComplete({
                path: { subscriptionId },
                throwOnError: true
            });
            setOnboardingState(completionResponse.data);

            const finalizeResponse = await postOnboardingSubscriptionsBySubscriptionIdFinalize({
                path: { subscriptionId },
                throwOnError: true
            });
            setOnboardingState(finalizeResponse.data);
            setStep(5);
        } catch (error: unknown) {
            setMessage(4, "error", resolveApiErrorMessage(error, "Unable to complete onboarding yet."));
        } finally {
            setIsBusy(false);
        }
    };

    const completeOnboarding = (): void => {
        void runCompleteOnboardingAsync();
    };

    const profileNameError = profileForm.formState.errors.agentName?.message;
    const profilePersonalityError = profileForm.formState.errors.agentPersonality?.message;
    const whatsappError = channelsForm.formState.errors.whatsapp?.message;
    const phoneError = channelsForm.formState.errors.phone?.message;
    const telegramError = channelsForm.formState.errors.telegram?.message;
    const inviteEmailError = inviteForm.formState.errors.inviteEmail?.message;
    const inviteRoleError = inviteForm.formState.errors.inviteRole?.message;
    const invitationStep = onboardingState?.invitations;
    const availableRoles = invitationStep?.availableRoles ?? [OWNER_ROLE, MEMBER_ROLE];
    const pendingInvitations = invitationStep?.invitations.filter((item) => item.status === "pending") ?? [];
    const currentMembers = invitationStep?.members ?? [];
    const suggestionItems = invitationStep?.suggestions ?? [];
    const memberEmails = new Set(currentMembers.map((item) => normalizeEmail(item.email)));
    const pendingInvitationEmails = new Set(
        pendingInvitations
            .filter((item) => item.status === "pending")
            .map((item) => normalizeEmail(item.email))
    );

    return (

        <div>
            {/* Progress Indicator */}
            {step < 5 && (
                <div className="flex justify-center gap-2 mb-12">
                    {[1, 2, 3, 4].map((i) => (
                        <div
                            key={i}
                            className={`h-1.5 rounded-full transition-all duration-300 ${step >= i ? "w-8 bg-slate-900" : "w-2 bg-slate-200"}`}
                        />
                    ))}
                </div>
            )}

            {/* Form Content */}
            <div className="animate-in fade-in slide-in-from-bottom-4 duration-500">
                {step === 1 && (
                    <div className="space-y-8 text-center">
                        <div className="space-y-4">
                            <h2 className="text-4xl font-extrabold tracking-tight">Connect Square.</h2>
                            <p className="text-lg text-slate-500 leading-relaxed">
                                Sync your catalog, customers, and orders automatically to provide seamless support.
                            </p>
                        </div>

                        <div className="pt-4 flex flex-col gap-3">
                            <PrimaryActionButton
                                onClick={() => void handleConnectSquare()}
                                disabled={isBusy || isSubscriptionContextLoading}
                                fullWidth
                                className="flex items-center justify-center gap-2"
                            >
                                <LinkIcon size={18} />
                                Connect Square Account
                            </PrimaryActionButton>

                        </div>
                        <div>
                            <button
                                onClick={() => handleSkipWithWarning(1)}
                                className="text-sm font-medium text-slate-400 hover:text-slate-600 transition-colors underline underline-offset-4"
                            >
                                Skip for now
                            </button>
                            <div className="text-xs pt-4 text-slate-400">Square must be linked for everything to work properly.</div>
                            {stepMessages[1] ? (
                                <div className={`text-xs pt-2 ${stepMessages[1]?.kind === "error" ? "text-red-500" : "text-slate-500"}`}>
                                    {stepMessages[1]?.text}
                                </div>
                            ) : null}
                        </div>
                    </div>
                )}

                {step === 2 && (
                    <div className="space-y-8 text-center">
                        <div className="space-y-4">
                            <h2 className="text-3xl font-extrabold tracking-tight">Agent Profile.</h2>
                            <p className="text-slate-500">Name your AI and set its communication style.</p>
                        </div>


                        <div className="space-y-6 text-left">
                            <div className="space-y-2">
                                <div className="flex items-center gap-2 font-semibold text-slate-900">
                                    Agent Name
                                </div>
                                <input
                                    type="text"
                                    placeholder="e.g. Alan, SupportBot"
                                    value={formData.agentName}
                                    onChange={handleAgentNameChange}
                                    className="w-full px-5 py-3.5 border border-slate-200 rounded-xl focus:ring-1 focus:ring-slate-900 focus:border-slate-900 outline-none transition-all shadow-sm"
                                    autoFocus
                                />
                                {profileNameError ? (
                                    <div className="text-xs text-red-500">{profileNameError}</div>
                                ) : null}
                            </div>

                            <div className="space-y-2">
                                <div className="flex items-center gap-2 font-semibold text-slate-900">
                                    Agent Personality
                                </div>
                                <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                                    <button
                                        onClick={() => handlePersonalityChange("casual")}
                                        className={`p-4 border rounded-xl flex flex-col items-center gap-2 transition-all ${formData.agentPersonality === "casual" ? "border-slate-900 bg-slate-50 ring-1 ring-slate-900" : "border-slate-200 hover:border-slate-300"}`}
                                    >
                                        <Smile size={24} className={formData.agentPersonality === "casual" ? "text-slate-900" : "text-slate-500"} />
                                        <span className="text-sm font-medium text-slate-900">Casual</span>
                                    </button>
                                    <button
                                        onClick={() => handlePersonalityChange("balanced")}
                                        className={`p-4 border rounded-xl flex flex-col items-center gap-2 transition-all ${formData.agentPersonality === "balanced" ? "border-slate-900 bg-slate-50 ring-1 ring-slate-900" : "border-slate-200 hover:border-slate-300"}`}
                                    >
                                        <MessagesSquare size={24} className={formData.agentPersonality === "balanced" ? "text-slate-900" : "text-slate-500"} />
                                        <span className="text-sm font-medium text-slate-900">Balanced</span>
                                    </button>
                                    <button
                                        onClick={() => handlePersonalityChange("business")}
                                        className={`p-4 border rounded-xl flex flex-col items-center gap-2 transition-all ${formData.agentPersonality === "business" ? "border-slate-900 bg-slate-50 ring-1 ring-slate-900" : "border-slate-200 hover:border-slate-300"}`}
                                    >
                                        <Briefcase size={24} className={formData.agentPersonality === "business" ? "text-slate-900" : "text-slate-500"} />
                                        <span className="text-sm font-medium text-slate-900">Business</span>
                                    </button>
                                </div>
                                {profilePersonalityError ? (
                                    <div className="text-xs text-red-500">{profilePersonalityError}</div>
                                ) : null}
                            </div>
                        </div>

                        <div className="pt-4 flex gap-3">
                            <SecondaryActionButton
                                onClick={() => setStep(1)}
                            >
                                Back
                            </SecondaryActionButton>
                            <PrimaryActionButton
                                onClick={() => void handleProfileContinue()}
                                disabled={isBusy || !formData.agentName.trim()}
                                className="flex flex-1 items-center justify-center gap-2"
                            >
                                Continue
                                <ArrowRight size={18} />
                            </PrimaryActionButton>
                        </div>
                        {stepMessages[2] ? (
                            <div className={`text-xs ${stepMessages[2]?.kind === "error" ? "text-red-500" : "text-slate-500"}`}>
                                {stepMessages[2]?.text}
                            </div>
                        ) : null}
                    </div>
                )}

                {step === 3 && (
                    <div className="space-y-8 text-center">
                        <div className="space-y-4">
                            <h2 className="text-3xl font-extrabold tracking-tight">Connect Channels.</h2>
                            <p className="text-slate-500">Add the communication channels where your customers reach out.</p>
                        </div>

                        <div className="space-y-4 text-left">
                            {/* Telegram */}
                            <div className="space-y-2">
                                <div className="flex items-center gap-2 font-semibold text-slate-900">
                                    <Send size={18} />
                                    Telegram
                                </div>
                                <input
                                    type="text"
                                    placeholder="Enter Telegram Bot Token"
                                    value={formData.channels.telegram}
                                    onChange={(e) => handleChannelChange("telegram", e.target.value)}
                                    className="w-full px-5 py-3.5 border border-slate-200 rounded-2xl focus:ring-2 focus:ring-slate-900 focus:border-slate-900 outline-none transition-all shadow-sm"
                                />
                                {telegramError ? (
                                    <div className="text-xs text-red-500">{telegramError}</div>
                                ) : null}
                                {!telegramError && formData.hasSavedTelegramToken && !formData.channels.telegram.trim() ? (
                                    <div className="text-xs text-slate-500">
                                        A Telegram token is already saved. Enter a new token only if you want to replace it.
                                    </div>
                                ) : null}
                            </div>

                            {/* Phone */}
                            <div className="space-y-2 opacity-50">
                                <div className="flex items-center gap-2 font-semibold text-slate-900">
                                    <Phone size={18} />
                                    Phone Number (SMS/Voice)
                                </div>
                                <input
                                    disabled   
                                    type="text"
                                    placeholder="Enter Support Phone Number"
                                    value={formData.channels.phone}
                                    onChange={(e) => handleChannelChange("phone", e.target.value)}
                                    className="w-full px-5 py-3.5 border border-slate-200 rounded-2xl focus:ring-2 focus:ring-slate-900 focus:border-slate-900 outline-none transition-all shadow-sm"
                                />
                                {phoneError ? (
                                    <div className="text-xs text-red-500">{phoneError}</div>
                                ) : null}
                            </div>

                            {/* WhatsApp */}
                            <div className="space-y-2 opacity-50">
                                <div className="flex items-center gap-2 font-semibold text-slate-900">
                                    <MessageCircle size={18} />
                                    WhatsApp
                                </div>
                                <input
                                    disabled
                                    type="text"
                                    placeholder="Enter WhatsApp Business Number"
                                    value={formData.channels.whatsapp}
                                    onChange={(e) => handleChannelChange("whatsapp", e.target.value)}
                                    className="w-full px-5 py-3.5 border border-slate-200 rounded-2xl focus:ring-2 focus:ring-slate-900 focus:border-slate-900 outline-none transition-all shadow-sm"
                                />
                                {whatsappError ? (
                                    <div className="text-xs text-red-500">{whatsappError}</div>
                                ) : null}
                            </div>
                        </div>

                        <div className="pt-4 flex gap-3">
                            <SecondaryActionButton
                                onClick={() => setStep(2)}
                            >
                                Back
                            </SecondaryActionButton>
                            <PrimaryActionButton
                                onClick={() => void handleChannelsContinue()}
                                disabled={isBusy}
                                className="flex-1"
                            >
                                Continue
                            </PrimaryActionButton>
                        </div>
                        <div>
                            <button
                                onClick={() => handleSkipWithWarning(3)}
                                className="text-sm font-medium text-slate-400 hover:text-slate-600 transition-colors underline underline-offset-4"
                            >
                                Skip for now
                            </button>
                            <div className="text-xs pt-4 text-slate-400">At least one channel must be configured for everything to work properly.</div>
                            {stepMessages[3] ? (
                                <div className={`text-xs pt-2 ${stepMessages[3]?.kind === "error" ? "text-red-500" : "text-slate-500"}`}>
                                    {stepMessages[3]?.text}
                                </div>
                            ) : null}
                        </div>
                    </div>
                )}

                {step === 4 && isSquareConnected && isTeamMembersEnabled && (
                    <div className="space-y-8 text-center">
                        <div className="space-y-4">
                            <h2 className="text-3xl font-extrabold tracking-tight">Invite Team.</h2>
                            <p className="text-slate-500">
                                Invite teammates now or finish onboarding and manage access later from settings.
                            </p>
                        </div>

                        <div className="space-y-6 text-left">
                            <div className="rounded-3xl border border-slate-200 bg-white/90 p-6 shadow-sm">
                                <div className="flex items-center gap-3">
                                    <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-amber-100 text-amber-700">
                                        <Sparkles size={20} />
                                    </div>
                                    <div>
                                        <h3 className="text-lg font-semibold text-slate-900">Square suggestions</h3>
                                        <p className="text-sm text-slate-500">
                                            Pull likely teammates from your connected Square account to avoid retyping addresses.
                                        </p>
                                    </div>
                                </div>

                                {suggestionItems.length > 0 ? (
                                    <div className="mt-5 flex flex-wrap gap-3">
                                        {suggestionItems.map((suggestion) => {
                                            const normalizedSuggestionEmail = normalizeEmail(suggestion.email);
                                            const isAlreadyMember = memberEmails.has(normalizedSuggestionEmail);
                                            const hasPendingInvite = pendingInvitationEmails.has(normalizedSuggestionEmail);
                                            const suggestionStateLabel = isAlreadyMember
                                                ? "Already a member"
                                                : (hasPendingInvite ? "Invite pending" : "Use suggestion");

                                            return (
                                                <button
                                                    key={suggestion.email}
                                                    type="button"
                                                    onClick={() => handleSuggestionSelect(suggestion.email)}
                                                    disabled={isAlreadyMember || hasPendingInvite || isBusy}
                                                    className={`min-w-[220px] rounded-2xl border px-4 py-3 text-left transition ${isAlreadyMember || hasPendingInvite ? "cursor-not-allowed border-slate-200 bg-slate-50 text-slate-400" : "border-amber-200 bg-amber-50 text-slate-900 hover:border-amber-300 hover:bg-amber-100"}`}
                                                >
                                                    <div className="font-semibold">{suggestion.displayName}</div>
                                                    <div className="mt-1 text-sm">{suggestion.email}</div>
                                                    <div className="mt-2 text-xs uppercase tracking-[0.2em] text-slate-500">{suggestionStateLabel}</div>
                                                </button>
                                            );
                                        })}
                                    </div>
                                ) : (
                                    <p className="mt-5 rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-500">
                                        No Square team members were available. You can still invite by email below.
                                    </p>
                                )}
                            </div>

                            <div className="rounded-3xl border border-slate-200 bg-white/90 p-6 shadow-sm">
                                <div className="flex items-center gap-3">
                                    <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-slate-900 text-white">
                                        <UserPlus size={20} />
                                    </div>
                                    <div>
                                        <h3 className="text-lg font-semibold text-slate-900">Send invitation</h3>
                                        <p className="text-sm text-slate-500">
                                            Invite an owner or member now. Pending invitations stay visible until accepted or revoked.
                                        </p>
                                    </div>
                                </div>

                                <div className="mt-5 grid gap-4 md:grid-cols-[minmax(0,1fr)_180px]">
                                    <div className="space-y-2">
                                        <label className="text-sm font-semibold text-slate-900" htmlFor="invite-email">
                                            Email
                                        </label>
                                        <div className="relative">
                                            <Mail className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                                            <input
                                                id="invite-email"
                                                type="email"
                                                placeholder="colleague@company.com"
                                                value={formData.inviteEmail}
                                                onChange={handleInviteEmailChange}
                                                className="w-full rounded-2xl border border-slate-200 py-3.5 pl-11 pr-4 outline-none transition-all shadow-sm focus:border-slate-900 focus:ring-2 focus:ring-slate-900"
                                            />
                                        </div>
                                        {inviteEmailError ? (
                                            <div className="text-xs text-red-500">{inviteEmailError}</div>
                                        ) : null}
                                    </div>

                                    <div className="space-y-2">
                                        <label className="text-sm font-semibold text-slate-900" htmlFor="invite-role">
                                            Role
                                        </label>
                                        <select
                                            id="invite-role"
                                            value={formData.inviteRole}
                                            onChange={handleInviteRoleChange}
                                            className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3.5 text-slate-900 outline-none transition-all shadow-sm focus:border-slate-900 focus:ring-2 focus:ring-slate-900"
                                        >
                                            {availableRoles.map((role) => (
                                                <option key={role} value={role}>
                                                    {getRoleLabel(role)}
                                                </option>
                                            ))}
                                        </select>
                                        {inviteRoleError ? (
                                            <div className="text-xs text-red-500">{inviteRoleError}</div>
                                        ) : null}
                                    </div>
                                </div>

                                <div className="mt-5 flex justify-end">
                                    <PrimaryActionButton
                                        onClick={() => void handleInvitationCreate()}
                                        disabled={isBusy}
                                        className="flex items-center justify-center gap-2"
                                    >
                                        <UserPlus size={18} />
                                        Send invite
                                    </PrimaryActionButton>
                                </div>
                            </div>

                            <div className="grid gap-6 xl:grid-cols-2">
                                <div className="rounded-3xl border border-slate-200 bg-white/90 p-6 shadow-sm">
                                    <div className="flex items-center gap-3">
                                        <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-sky-100 text-sky-700">
                                            <Mail size={20} />
                                        </div>
                                        <div>
                                            <h3 className="text-lg font-semibold text-slate-900">Pending invitations</h3>
                                            <p className="text-sm text-slate-500">Review who still needs to accept access.</p>
                                        </div>
                                    </div>

                                    {pendingInvitations.length > 0 ? (
                                        <div className="mt-5 space-y-3">
                                            {pendingInvitations.map((invitation: SubscriptionInvitationItem) => (
                                                <div key={invitation.invitationId} className="rounded-2xl border border-slate-200 px-4 py-3">
                                                    <div className="flex items-start justify-between gap-3">
                                                        <div>
                                                            <div className="font-semibold text-slate-900">{invitation.email}</div>
                                                            <div className="mt-1 text-sm text-slate-500">
                                                                {getRoleLabel(invitation.role)} invited {formatDateTime(invitation.sentAtUtc)}
                                                            </div>
                                                        </div>
                                                        <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em] text-amber-700">
                                                            {invitation.status}
                                                        </span>
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    ) : (
                                        <p className="mt-5 rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-500">
                                            No invitations have been sent yet.
                                        </p>
                                    )}
                                </div>

                                <div className="rounded-3xl border border-slate-200 bg-white/90 p-6 shadow-sm">
                                    <div className="flex items-center gap-3">
                                        <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-emerald-100 text-emerald-700">
                                            <Users size={20} />
                                        </div>
                                        <div>
                                            <h3 className="text-lg font-semibold text-slate-900">Current members</h3>
                                            <p className="text-sm text-slate-500">These people already have access to this subscription.</p>
                                        </div>
                                    </div>

                                    {currentMembers.length > 0 ? (
                                        <div className="mt-5 space-y-3">
                                            {currentMembers.map((member: SubscriptionMemberItem) => (
                                                <div key={member.userId} className="rounded-2xl border border-slate-200 px-4 py-3">
                                                    <div className="flex items-start justify-between gap-3">
                                                        <div>
                                                            <div className="font-semibold text-slate-900">
                                                                {member.displayName || member.email}
                                                            </div>
                                                            <div className="mt-1 text-sm text-slate-500">{member.email}</div>
                                                            <div className="mt-1 text-sm text-slate-500">
                                                                {getRoleLabel(member.role)} joined {formatDateTime(member.joinedAtUtc)}
                                                            </div>
                                                        </div>
                                                        {member.isCurrentUser ? (
                                                            <span className="rounded-full bg-slate-900 px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em] text-white">
                                                                You
                                                            </span>
                                                        ) : null}
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    ) : (
                                        <p className="mt-5 rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-500">
                                            You are the only member right now.
                                        </p>
                                    )}
                                </div>
                            </div>
                        </div>

                        <div className="rounded-3xl border border-slate-200 bg-stone-50 px-5 py-4 text-left text-sm text-slate-600">
                            Invitations are optional. Sending them now helps teammates join faster, and finishing setup will mark this step complete before onboarding finalizes.
                        </div>

                        <div className="pt-4 flex gap-3">
                            <SecondaryActionButton
                                onClick={() => setStep(3)}
                            >
                                Back
                            </SecondaryActionButton>
                            <PrimaryActionButton
                                onClick={completeOnboarding}
                                disabled={isBusy}
                                className="flex-1"
                            >
                                Finish Setup
                            </PrimaryActionButton>
                        </div>
                        {stepMessages[4] ? (
                            <div className={`text-xs ${stepMessages[4]?.kind === "error" ? "text-red-500" : "text-slate-500"}`}>
                                {stepMessages[4]?.text}
                            </div>
                        ) : null}
                    </div>
                )}

                {step === 4 && isSquareConnected && !isTeamMembersEnabled && (
                    <div className="space-y-8 text-center">
                        <div className="space-y-4">
                            <h2 className="text-3xl font-extrabold tracking-tight">Finish Setup.</h2>
                            <p className="text-slate-500">
                                Team member invitations are disabled for this workspace right now. You can finish onboarding without configuring access here.
                            </p>
                        </div>

                        <div className="rounded-3xl border border-slate-200 bg-stone-50 px-6 py-5 text-left text-sm text-slate-600">
                            The onboarding flow will skip invite suggestions, invitations, and current member management while the team-members feature flag is turned off.
                        </div>

                        <div className="pt-4 flex gap-3">
                            <SecondaryActionButton
                                onClick={() => setStep(3)}
                            >
                                Back
                            </SecondaryActionButton>
                            <PrimaryActionButton
                                onClick={completeOnboarding}
                                disabled={isBusy}
                                className="flex-1"
                            >
                                Finish Setup
                            </PrimaryActionButton>
                        </div>
                        {stepMessages[4] ? (
                            <div className={`text-xs ${stepMessages[4]?.kind === "error" ? "text-red-500" : "text-slate-500"}`}>
                                {stepMessages[4]?.text}
                            </div>
                        ) : null}
                    </div>
                )}

                {step === 5 && (
                    <div className="text-center space-y-6 py-4">
                        <div className="mx-auto w-20 h-20 bg-slate-900 text-white rounded-full flex items-center justify-center mb-6 shadow-lg">
                            <CheckCircle2 size={40} />
                        </div>
                        <h2 className="text-4xl font-extrabold tracking-tight">Setup Complete.</h2>
                        <p className="text-lg text-slate-500">
                            Welcome aboard. {formData.agentName || "Your AI agent"} is ready.
                        </p>
                        <PrimaryActionButton
                            href="/admin"
                            fullWidth
                            className="mt-8 block text-center"
                        >
                            Go to Dashboard
                        </PrimaryActionButton>
                    </div>
                )}
            </div>
        </div>
    );
};

const OnboardingFallback = (): ReactElement => {
    return <div className="min-h-[480px]" aria-hidden="true" />;
};

const OnboardingPage = (): ReactElement => {
    return (
        <Suspense fallback={<OnboardingFallback />}>
            <OnboardingPageContent />
        </Suspense>
    );
};

export default OnboardingPage;
