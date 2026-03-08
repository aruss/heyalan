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
    Plus,
    Trash2,
    Smile,
    Briefcase,
    MessagesSquare
} from "lucide-react";
import {
    getOnboardingSubscriptionsBySubscriptionIdState,
    patchOnboardingAgentsByAgentIdChannels,
    patchOnboardingAgentsByAgentIdProfile,
    postOnboardingSubscriptionsBySubscriptionIdAgents,
    postOnboardingSubscriptionsBySubscriptionIdFinalize,
    postOnboardingSubscriptionsBySubscriptionIdMembersInvitations,
    postSubscriptionsBySubscriptionIdSquareAuthorize,
    type AgentPersonality as ApiAgentPersonality,
    type GetSubscriptionOnboardingStateResult
} from "@/lib/api";
import { PrimaryActionButton, SecondaryActionButton } from "@/components/landing/ui/action-buttons";
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
    teamMembers: string[];
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

const teamSchema = z.object({
    teamMembers: z.array(
        z.object({
            email: z.string().trim()
        })
    ).superRefine((members, ctx) => {
        for (let index = 0; index < members.length; index++) {
            const email = members[index].email;
            if (!email) {
                continue;
            }

            const isValid = z.string().email().safeParse(email).success;
            if (!isValid) {
                ctx.addIssue({
                    code: z.ZodIssueCode.custom,
                    message: "Use a valid email format.",
                    path: [index, "email"]
                });
            }
        }
    })
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
        teamMembers: [""]
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

    const teamForm = useForm({
        resolver: zodResolver(teamSchema),
        defaultValues: {
            teamMembers: formData.teamMembers.map((email) => ({ email }))
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
            teamMembers: [...formSnapshot.teamMembers]
        };

        setFormData(nextFormData);
        profileForm.setValue("agentName", nextFormData.agentName, { shouldDirty: false });
        profileForm.setValue("agentPersonality", nextFormData.agentPersonality, { shouldDirty: false });
        channelsForm.setValue("whatsapp", nextFormData.channels.whatsapp, { shouldDirty: false });
        channelsForm.setValue("phone", nextFormData.channels.phone, { shouldDirty: false });
        channelsForm.setValue("telegram", nextFormData.channels.telegram, { shouldDirty: false });
        teamForm.setValue("teamMembers", nextFormData.teamMembers.map((email) => ({ email })), { shouldDirty: false });
        return state;
    }, [channelsForm, profileForm, teamForm]);

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

    const handleMemberChange = (index: number, value: string): void => {
        const newMembers = [...formData.teamMembers];
        newMembers[index] = value;
        setFormData({ ...formData, teamMembers: newMembers });
        teamForm.setValue(`teamMembers.${index}.email`, value, { shouldDirty: true, shouldTouch: true, shouldValidate: true });
    };

    const addMember = (): void => {
        const updatedMembers = [...formData.teamMembers, ""];
        setFormData({ ...formData, teamMembers: updatedMembers });
        teamForm.setValue("teamMembers", updatedMembers.map((email) => ({ email })), { shouldDirty: true });
    };

    const removeMember = (index: number): void => {
        if (formData.teamMembers.length <= 1) {
            return;
        }

        const newMembers = formData.teamMembers.filter((_, i) => i !== index);
        setFormData({ ...formData, teamMembers: newMembers });
        teamForm.setValue("teamMembers", newMembers.map((email) => ({ email })), { shouldDirty: true });
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

    const runCompleteOnboardingAsync = async (validateTeamInputs: boolean): Promise<void> => {
        if (!subscriptionId) {
            setMessage(4, "error", "Missing subscription context.");
            return;
        }

        if (!isSquareConnected) {
            router.replace("/admin");
            return;
        }

        if (validateTeamInputs) {
            const isValid = await teamForm.trigger();
            if (!isValid) {
                setMessage(4, "error", "Please enter valid team email formats.");
                return;
            }
        }

        try {
            setIsBusy(true);
            clearMessage(4);

            await postOnboardingSubscriptionsBySubscriptionIdMembersInvitations({
                path: { subscriptionId },
                throwOnError: true
            });

            await refreshOnboardingStateAsync(subscriptionId);
            await postOnboardingSubscriptionsBySubscriptionIdFinalize({
                path: { subscriptionId },
                throwOnError: true
            });

            await refreshOnboardingStateAsync(subscriptionId);
            setStep(5);
        } catch (error: unknown) {
            setMessage(4, "error", resolveApiErrorMessage(error, "Unable to complete onboarding yet."));
        } finally {
            setIsBusy(false);
        }
    };

    const completeOnboarding = (): void => {
        void runCompleteOnboardingAsync(true);
    };

    const skipInvites = (): void => {
        if (!isSquareConnected) {
            router.replace("/admin");
            return;
        }

        void runCompleteOnboardingAsync(false);
    };

    const profileNameError = profileForm.formState.errors.agentName?.message;
    const profilePersonalityError = profileForm.formState.errors.agentPersonality?.message;
    const whatsappError = channelsForm.formState.errors.whatsapp?.message;
    const phoneError = channelsForm.formState.errors.phone?.message;
    const telegramError = channelsForm.formState.errors.telegram?.message;
    const teamErrors = teamForm.formState.errors.teamMembers;

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

                {step === 4 && isSquareConnected && (
                    <div className="space-y-8 text-center">
                        <div className="space-y-4 opacity-50">
                            <h2 className="text-3xl font-extrabold tracking-tight">Invite Team.</h2>
                            <p className="text-slate-500">Add team members to handle escalations and monitor chats.</p>
                        </div>

                        <div className="space-y-4 text-left opacity-50">
                            {formData.teamMembers.map((email, index) => (
                                <div key={index} className="flex items-center gap-2">
                                    <div className="relative flex-1">
                                        <Mail className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                                        <input
                                            disabled
                                            type="email"
                                            placeholder="colleague@company.com"
                                            value={email}
                                            onChange={(e) => handleMemberChange(index, e.target.value)}
                                            className="w-full pl-11 pr-4 py-3.5 border border-slate-200 rounded-2xl focus:ring-2 focus:ring-slate-900 focus:border-slate-900 outline-none transition-all shadow-sm"
                                        />
                                        {teamErrors?.[index]?.email?.message ? (
                                            <div className="text-xs text-red-500 pt-2">{teamErrors[index]?.email?.message}</div>
                                        ) : null}
                                    </div>
                                    {formData.teamMembers.length > 1 && (
                                        <button
                                            onClick={() => removeMember(index)}
                                            className="p-3.5 text-slate-400 hover:text-red-500 transition-colors rounded-2xl border border-slate-200 hover:border-red-200 bg-white"
                                        >
                                            <Trash2 size={18} />
                                        </button>
                                    )}
                                </div>
                            ))}

                            <button
                                onClick={() => false /*addMember*/}
                                className="flex items-center gap-2 text-sm font-semibold text-slate-900 hover:text-slate-700 transition-colors ml-1"
                            >
                                <Plus size={16} />
                                Add another member
                            </button>
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
                                Complete Setup
                            </PrimaryActionButton>
                        </div>
                        <div>
                            <button
                                onClick={skipInvites}
                                className="text-sm font-medium text-slate-400 hover:text-slate-600 transition-colors underline underline-offset-4"
                            >
                                Skip invites
                            </button>
                            {stepMessages[4] ? (
                                <div className={`text-xs pt-2 ${stepMessages[4]?.kind === "error" ? "text-red-500" : "text-slate-500"}`}>
                                    {stepMessages[4]?.text}
                                </div>
                            ) : null}
                        </div>
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
