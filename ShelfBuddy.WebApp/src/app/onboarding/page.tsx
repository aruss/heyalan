"use client";

import type { ChangeEvent, ReactElement } from "react";
import { useState } from "react";
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
import { PrimaryActionButton, SecondaryActionButton } from "@/components/landing/ui/action-buttons";

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
    agentName: string;
    agentPersonality: AgentPersonality;
    channels: ChannelState;
    teamMembers: string[];
};

const OnboardingPage = (): ReactElement => {
    const [step, setStep] = useState<OnboardingStep>(1);
    const [formData, setFormData] = useState<FormState>({
        squareConnected: false,
        agentName: "",
        agentPersonality: "balanced",
        channels: {
            whatsapp: "",
            phone: "",
            telegram: ""
        },
        teamMembers: [""]
    });

    const nextStep = (): void => {
        setStep((prev) => {
            return (prev + 1) as OnboardingStep;
        });
    };
    const skipStep = (): void => {
        setStep((prev) => {
            return (prev + 1) as OnboardingStep;
        });
    };

    const handleConnectSquare = (): void => {
        setFormData({ ...formData, squareConnected: true });
        nextStep();
    };

    const handleAgentNameChange = (e: ChangeEvent<HTMLInputElement>): void => {
        setFormData({ ...formData, agentName: e.target.value });
    };

    const handlePersonalityChange = (type: AgentPersonality): void => {
        setFormData({ ...formData, agentPersonality: type });
    };

    const handleMemberChange = (index: number, value: string): void => {
        const newMembers = [...formData.teamMembers];
        newMembers[index] = value;
        setFormData({ ...formData, teamMembers: newMembers });
    };

    const addMember = (): void => {
        setFormData({ ...formData, teamMembers: [...formData.teamMembers, ""] });
    };

    const removeMember = (index: number): void => {
        const newMembers = formData.teamMembers.filter((_, i) => i !== index);
        setFormData({ ...formData, teamMembers: newMembers });
    };

    const handleChannelChange = (channel: ChannelKey, value: string): void => {
        setFormData({
            ...formData,
            channels: { ...formData.channels, [channel]: value }
        });
    };

    const completeOnboarding = (): void => {
        setStep(5); // Success state
    };

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
                                onClick={handleConnectSquare}
                                fullWidth
                                className="flex items-center justify-center gap-2"
                            >
                                <LinkIcon size={18} />
                                Connect SquareUp Account
                            </PrimaryActionButton>
                            <SecondaryActionButton
                                onClick={skipStep}
                                fullWidth
                            >
                                Skip for now
                            </SecondaryActionButton>
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
                                    placeholder="e.g. Buddy, SupportBot"
                                    value={formData.agentName}
                                    onChange={handleAgentNameChange}
                                    className="w-full px-5 py-3.5 border border-slate-200 rounded-xl focus:ring-1 focus:ring-slate-900 focus:border-slate-900 outline-none transition-all shadow-sm"
                                    autoFocus
                                />
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
                            </div>
                        </div>

                        <div className="pt-4 flex gap-3">
                            <SecondaryActionButton
                                onClick={() => setStep(1)}
                            >
                                Back
                            </SecondaryActionButton>
                            <PrimaryActionButton
                                onClick={nextStep}
                                disabled={!formData.agentName.trim()}
                                className="flex flex-1 items-center justify-center gap-2"
                            >
                                Continue
                                <ArrowRight size={18} />
                            </PrimaryActionButton>
                        </div>
                    </div>
                )}

                {step === 3 && (
                    <div className="space-y-8 text-center">
                        <div className="space-y-4">
                            <h2 className="text-3xl font-extrabold tracking-tight">Connect Channels.</h2>
                            <p className="text-slate-500">Add the communication channels where your customers reach out.</p>
                        </div>

                        <div className="space-y-4 text-left">
                            {/* WhatsApp */}
                            <div className="space-y-2">
                                <div className="flex items-center gap-2 font-semibold text-slate-900">
                                    <MessageCircle size={18} />
                                    WhatsApp
                                </div>
                                <input
                                    type="text"
                                    placeholder="Enter WhatsApp Business Number"
                                    value={formData.channels.whatsapp}
                                    onChange={(e) => handleChannelChange("whatsapp", e.target.value)}
                                    className="w-full px-5 py-3.5 border border-slate-200 rounded-2xl focus:ring-2 focus:ring-slate-900 focus:border-slate-900 outline-none transition-all shadow-sm"
                                />
                            </div>

                            {/* Phone */}
                            <div className="space-y-2">
                                <div className="flex items-center gap-2 font-semibold text-slate-900">
                                    <Phone size={18} />
                                    Phone Number (SMS/Voice)
                                </div>
                                <input
                                    type="text"
                                    placeholder="Enter Support Phone Number"
                                    value={formData.channels.phone}
                                    onChange={(e) => handleChannelChange("phone", e.target.value)}
                                    className="w-full px-5 py-3.5 border border-slate-200 rounded-2xl focus:ring-2 focus:ring-slate-900 focus:border-slate-900 outline-none transition-all shadow-sm"
                                />
                            </div>

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
                            </div>
                        </div>

                        <div className="pt-4 flex gap-3">
                            <SecondaryActionButton
                                onClick={() => setStep(2)}
                            >
                                Back
                            </SecondaryActionButton>
                            <PrimaryActionButton
                                onClick={nextStep}
                                className="flex-1"
                            >
                                Continue
                            </PrimaryActionButton>
                        </div>
                        <div>
                            <button
                                onClick={nextStep}
                                className="text-sm font-medium text-slate-400 hover:text-slate-600 transition-colors underline underline-offset-4"
                            >
                                Skip channel setup
                            </button>
                        </div>
                    </div>
                )}

                {step === 4 && (
                    <div className="space-y-8 text-center">
                        <div className="space-y-4">
                            <h2 className="text-3xl font-extrabold tracking-tight">Invite Team.</h2>
                            <p className="text-slate-500">Add team members to handle escalations and monitor chats.</p>
                        </div>

                        <div className="space-y-4 text-left">
                            {formData.teamMembers.map((email, index) => (
                                <div key={index} className="flex items-center gap-2">
                                    <div className="relative flex-1">
                                        <Mail className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                                        <input
                                            type="email"
                                            placeholder="colleague@company.com"
                                            value={email}
                                            onChange={(e) => handleMemberChange(index, e.target.value)}
                                            className="w-full pl-11 pr-4 py-3.5 border border-slate-200 rounded-2xl focus:ring-2 focus:ring-slate-900 focus:border-slate-900 outline-none transition-all shadow-sm"
                                        />
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
                                onClick={addMember}
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
                                className="flex-1"
                            >
                                Complete Setup
                            </PrimaryActionButton>
                        </div>
                        <div>
                            <button
                                onClick={completeOnboarding}
                                className="text-sm font-medium text-slate-400 hover:text-slate-600 transition-colors underline underline-offset-4"
                            >
                                Skip invites
                            </button>
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
                        <div className="pt-6 text-left bg-slate-50 p-5 rounded-2xl border border-slate-200 text-sm font-mono text-slate-600 overflow-hidden shadow-inner">
                            <p className="text-slate-400 mb-2">{"// Configuration Output"}</p>
                            <pre>{JSON.stringify(formData, null, 2)}</pre>
                        </div>
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

export default OnboardingPage;
