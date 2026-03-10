"use client"

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { z } from "zod";
import { Button } from "@/components/admin/button";
import { Input } from "@/components/admin/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/admin/select";
import { Textarea } from "@/components/admin/textarea";
import type { AgentPersonality as ApiAgentPersonality } from "@/lib/api";
import { useAgentSettings } from "./agent-settings-context";

// https://dashboard.tremor.so/settings/general#
// https://insights.tremor.so/settings/users

type AgentPersonalityValue = "casual" | "balanced" | "business";

type PersonalityFormValues = {
  agentName: string;
  agentPersonality: AgentPersonalityValue;
  personalityPromptRaw: string;
};

const profileSchema = z.object({
  agentName: z.string().trim().min(1, "Agent name is required."),
  agentPersonality: z.enum(["casual", "balanced", "business"]),
  personalityPromptRaw: z.string(),
});

const personality: { value: AgentPersonalityValue; label: string }[] = [
  {
    value: "casual",
    label: "Casual",
  },
  {
    value: "balanced",
    label: "Balanced",
  },
  {
    value: "business",
    label: "Business",
  },
]

const mapPersonalityToApi = (personalityValue: AgentPersonalityValue): ApiAgentPersonality => {
  switch (personalityValue) {
    case "casual":
      return "Casual";
    case "balanced":
      return "Balanced";
    case "business":
      return "Business";
  }
};

const mapApiPersonalityToLocal = (
  personalityValue: ApiAgentPersonality | null | undefined,
): AgentPersonalityValue => {
  if (personalityValue === "Casual") {
    return "casual";
  }

  if (personalityValue === "Business") {
    return "business";
  }

  return "balanced";
};

export default function SettingsAgentPage() {
  const { agent, isLoading, errorMessage, updateProfile } = useAgentSettings();
  const [saveMessage, setSaveMessage] = useState<string | null>(null);
  const [saveErrorMessage, setSaveErrorMessage] = useState<string | null>(null);

  const form = useForm<PersonalityFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      agentName: "",
      agentPersonality: "balanced",
      personalityPromptRaw: "",
    },
  });

  useEffect(() => {
    if (!agent) {
      return;
    }

    form.reset({
      agentName: agent.name ?? "",
      agentPersonality: mapApiPersonalityToLocal(agent.personality),
      personalityPromptRaw: agent.personalityPromptRaw ?? "",
    });
  }, [agent, form]);

  if (isLoading) {
    return <div className="p-4 text-sm text-gray-500">Loading agent settings...</div>;
  }

  if (errorMessage) {
    return <div className="p-4 text-sm text-red-500">{errorMessage}</div>;
  }

  if (!agent) {
    return <div className="p-4 text-sm text-gray-500">No agent settings available.</div>;
  }

  const handleSaveProfile = form.handleSubmit(async (values) => {
    setSaveMessage(null);
    setSaveErrorMessage(null);

    const updatedAgent = await updateProfile({
      name: values.agentName.trim(),
      personality: mapPersonalityToApi(values.agentPersonality),
      personalityPromptRaw: values.personalityPromptRaw.trim().length > 0
        ? values.personalityPromptRaw.trim()
        : null,
    });

    if (!updatedAgent) {
      setSaveErrorMessage(errorMessage ?? "Unable to save agent settings.");
      return;
    }

    setSaveMessage("Settings saved.");
  });

  const agentNameError = form.formState.errors.agentName?.message;
  const agentPersonalityError = form.formState.errors.agentPersonality?.message;
  const personalityPromptError = form.formState.errors.personalityPromptRaw?.message;

  return (
    <>

      <section className="mx-4">
        <form onSubmit={(event) => void handleSaveProfile(event)}>
          <div className="grid grid-cols-1 gap-x-14 gap-y-8 md:grid-cols-3">
            <div>
              <h2 className="scroll-mt-10 text-sm font-semibold text-gray-900 dark:text-gray-50">
                Agent personality
              </h2>
              <p className="mt-1 text-xs leading-6 text-gray-500">
                Define how your AI agent interacts with customers. Give your agent a name, select its core tone, and provide custom instructions to guide its responses.
              </p>
            </div>
            <div className="md:col-span-2">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
                <div className="col-span-full sm:col-span-3">
                  <label
                    htmlFor="first-name"
                    className="text-sm leading-none text-gray-900 dark:text-gray-50 font-medium"
                  >
                    Agent name
                  </label>
                  <Input
                    type="text"
                    id="first-name"
                    autoComplete="given-name"
                    hasError={Boolean(agentNameError)}
                    {...form.register("agentName")}
                    placeholder="Alan"
                    className="mt-2"
                  />
                  {agentNameError ? (
                    <p className="mt-2 text-xs text-red-500">{agentNameError}</p>
                  ) : null}
                </div>
                <div className="col-span-full sm:col-span-3">
                  <label
                    htmlFor="last-name"
                    className="text-sm leading-none text-gray-900 dark:text-gray-50 font-medium"
                  >
                    Personality
                  </label>
                  <Controller
                    control={form.control}
                    name="agentPersonality"
                    render={({ field }) => {
                      return (
                        <Select value={field.value} onValueChange={field.onChange}>
                          <SelectTrigger
                            id="new-user-permission"
                            className="mt-2 w-full"
                            hasError={Boolean(agentPersonalityError)}
                          >
                            <SelectValue placeholder="Select Personality" />
                          </SelectTrigger>
                          <SelectContent>
                            {personality.map((item) => (
                              <SelectItem key={item.value} value={item.value}>
                                {item.label}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      );
                    }}
                  />
                  {agentPersonalityError ? (
                    <p className="mt-2 text-xs text-red-500">{agentPersonalityError}</p>
                  ) : null}
                </div>
                <div className="col-span-full">
                  <label
                    htmlFor="email"
                    className="text-sm leading-none text-gray-900 dark:text-gray-50 font-medium"
                  >
                    Personality instructions
                  </label>
                  <Textarea
                    id="comment"
                    rows={12}
                    hasError={Boolean(personalityPromptError)}
                    {...form.register("personalityPromptRaw")}
                    placeholder="Add your instructions..."
                    className="mt-2"
                  />
                  {personalityPromptError ? (
                    <p className="mt-2 text-xs text-red-500">{personalityPromptError}</p>
                  ) : null}

                </div>
                <div className="col-span-full mt-6 flex justify-end gap-4">
                  {saveMessage ? (
                    <p className="mr-auto text-sm text-emerald-600">{saveMessage}</p>
                  ) : null}
                  {saveErrorMessage ? (
                    <p className="mr-auto text-sm text-red-500">{saveErrorMessage}</p>
                  ) : null}
                  <Button
                    className="gap-2"
                    variant="primary"
                    type="submit"
                    isLoading={form.formState.isSubmitting}
                  >
                    Save settings
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </form>
      </section>

    </>
  );
}
