"use client"

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Button } from "@/components/admin/button";
import { Card } from "@/components/admin/card";
import { Input } from "@/components/admin/input";
import { SiGooglemessages, SiTelegram, SiWhatsapp } from "react-icons/si";
import { useAgentSettings } from "../agent-settings-context";

type ChannelCard = "telegram" | "whatsapp" | "sms";

type ChannelsFormValues = {
  telegramBotToken: string;
  whatsappNumber: string;
  twilioPhoneNumber: string;
};

const E164_LIKE_REGEX = /^\+[1-9]\d{7,14}$/;
const PHONE_VALIDATION_ERROR = "Use E.164-like format (e.g. +15551234567).";
const FALLBACK_SAVE_ERROR = "Unable to save channel settings.";

const channelsSchema = z.object({
  telegramBotToken: z.string().trim(),
  whatsappNumber: z.string().trim().refine((value) => {
    return value.length === 0 || E164_LIKE_REGEX.test(value);
  }, PHONE_VALIDATION_ERROR),
  twilioPhoneNumber: z.string().trim().refine((value) => {
    return value.length === 0 || E164_LIKE_REGEX.test(value);
  }, PHONE_VALIDATION_ERROR),
});

const normalizeChannelValue = (value: string): string | null => {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
};

export default function SettingsAgentPage() {
  const { agent, isLoading, errorMessage, isOperationalReady, updateChannels } = useAgentSettings();
  const [savingCard, setSavingCard] = useState<ChannelCard | null>(null);
  const [successCard, setSuccessCard] = useState<ChannelCard | null>(null);
  const [errorCard, setErrorCard] = useState<ChannelCard | null>(null);

  const form = useForm<ChannelsFormValues>({
    resolver: zodResolver(channelsSchema),
    defaultValues: {
      telegramBotToken: "",
      whatsappNumber: "",
      twilioPhoneNumber: "",
    },
  });

  useEffect(() => {
    if (!agent) {
      return;
    }

    form.reset({
      telegramBotToken: agent.telegramBotToken ?? "",
      whatsappNumber: agent.whatsappNumber ?? "",
      twilioPhoneNumber: agent.twilioPhoneNumber ?? "",
    });
  }, [agent, form]);

  if (isLoading && !agent) {
    return <div className="p-4 text-sm text-gray-500">Loading channel settings...</div>;
  }

  if (errorMessage && !agent) {
    return <div className="p-4 text-sm text-red-500">{errorMessage}</div>;
  }

  if (!agent) {
    return <div className="p-4 text-sm text-gray-500">No channel settings available.</div>;
  }

  const getPayloadForCard = (card: ChannelCard): {
    twilioPhoneNumber: string | null;
    whatsappNumber: string | null;
    telegramBotToken: string | null;
  } => {
    if (card === "telegram") {
      return {
        twilioPhoneNumber: normalizeChannelValue(agent.twilioPhoneNumber ?? ""),
        whatsappNumber: normalizeChannelValue(agent.whatsappNumber ?? ""),
        telegramBotToken: normalizeChannelValue(form.getValues("telegramBotToken")),
      };
    }

    if (card === "whatsapp") {
      return {
        twilioPhoneNumber: normalizeChannelValue(agent.twilioPhoneNumber ?? ""),
        whatsappNumber: normalizeChannelValue(form.getValues("whatsappNumber")),
        telegramBotToken: normalizeChannelValue(agent.telegramBotToken ?? ""),
      };
    }

    return {
      twilioPhoneNumber: normalizeChannelValue(form.getValues("twilioPhoneNumber")),
      whatsappNumber: normalizeChannelValue(agent.whatsappNumber ?? ""),
      telegramBotToken: normalizeChannelValue(agent.telegramBotToken ?? ""),
    };
  };

  const saveCardAsync = async (card: ChannelCard): Promise<void> => {
    setSuccessCard(null);
    setErrorCard(null);

    const validationTargets: Array<keyof ChannelsFormValues> =
      card === "telegram"
        ? ["telegramBotToken"]
        : card === "whatsapp"
          ? ["whatsappNumber"]
          : ["twilioPhoneNumber"];

    const isValid = await form.trigger(validationTargets);
    if (!isValid) {
      return;
    }

    setSavingCard(card);

    const updatedAgent = await updateChannels(getPayloadForCard(card));
    setSavingCard(null);

    if (!updatedAgent) {
      setErrorCard(card);
      return;
    }

    form.reset({
      telegramBotToken: updatedAgent.telegramBotToken ?? "",
      whatsappNumber: updatedAgent.whatsappNumber ?? "",
      twilioPhoneNumber: updatedAgent.twilioPhoneNumber ?? "",
    });
    setSuccessCard(card);
  };

  const telegramError = form.formState.errors.telegramBotToken?.message;
  const whatsappError = form.formState.errors.whatsappNumber?.message;
  const smsError = form.formState.errors.twilioPhoneNumber?.message;
  const saveErrorMessage = errorMessage ?? FALLBACK_SAVE_ERROR;

  return (
    <section className="mx-4">
      <form>
        <div className="grid grid-cols-1 gap-x-14 gap-y-8 md:grid-cols-3">
          <div>
            <h2 className="scroll-mt-10 text-sm font-semibold text-gray-900 dark:text-gray-50">
              Communication Channels
            </h2>
            <p className="mt-1 text-xs leading-6 text-gray-500">
              Select and configure the platforms where your AI agent will interact with users. Connect your agent to popular messaging services like WhatsApp, Telegram, and SMS.
            </p>
          </div>
          <div className="md:col-span-2">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
              <div className="col-span-full sm:col-span-6">
                {!isOperationalReady ? (
                  <div className="rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800 dark:border-amber-900/60 dark:bg-amber-950/30 dark:text-amber-200">
                    Agent is not operational yet. Configure at least one channel to receive messages.
                  </div>
                ) : null}

                <Card className="mb-4 overflow-hidden border-gray-300 p-0 dark:border-gray-800 ">
                  <div className="overflow-hidden border-l-4 p-4 ">
                    <div className="flex items-center gap-4 pr-4">
                      <SiTelegram
                        className="size-8 shrink-0 "
                        aria-hidden="true"
                      />
                      <div className="truncate">
                        <h4 className="text-sm font-medium capitalize text-gray-900 dark:text-gray-50">
                          Telegram
                        </h4>
                        <p className="text-xs text-gray-600 dark:text-gray-400 truncate">
                          Link your bot token to enable chat via Telegram.
                        </p>
                      </div>
                    </div>
                    <div className="mt-6">
                      <label
                        className="text-sm leading-none text-gray-900 dark:text-gray-50 font-medium"
                      >
                        Telegram token
                      </label>
                      <Input
                        type="text"
                        id="telegram-bot-token"
                        autoComplete="given-name"
                        {...form.register("telegramBotToken")}
                        placeholder="7326267594:AA3dUee0CjDYCoFVEtemWmpwv3O5WWGpsXE"
                        hasError={Boolean(telegramError)}
                        className="mt-2"
                      />
                      {telegramError ? (
                        <p className="mt-2 text-xs text-red-500">{telegramError}</p>
                      ) : null}
                    </div>

                    <div className="col-span-full mt-6 flex justify-end gap-4">
                      {successCard === "telegram" ? (
                        <p className="mr-auto text-sm text-emerald-600">Telegram settings saved.</p>
                      ) : null}
                      {errorCard === "telegram" ? (
                        <p className="mr-auto text-sm text-red-500">{saveErrorMessage}</p>
                      ) : null}
                      <Button
                        className="gap-2"
                        variant="primary"
                        type="button"
                        isLoading={savingCard === "telegram"}
                        onClick={() => void saveCardAsync("telegram")}
                      >
                        Save
                      </Button>
                    </div>

                  </div>
                </Card>

                <Card className="mb-4 overflow-hidden border-gray-300 p-0 dark:border-gray-800 ">
                  <div className="overflow-hidden border-l-4 p-4">
                    <div className="flex items-center gap-4 pr-4">
                      <SiWhatsapp
                        className="size-8 shrink-0 "
                        aria-hidden="true"
                      />
                      <div className="truncate">
                        <h4 className="text-sm font-medium capitalize text-gray-900 dark:text-gray-50">
                          WhatsApp
                        </h4>
                        <p className="text-xs text-gray-600 dark:text-gray-400 truncate">
                          Connect your business number for WhatsApp messaging.
                        </p>
                      </div>
                    </div>
                    <div className="mt-6">
                      <label

                        className="text-sm leading-none text-gray-900 dark:text-gray-50 font-medium"
                      >
                        WhatsApp number
                      </label>
                      <Input
                        type="text"
                        id="whatsapp-number"
                        autoComplete="given-name"
                        {...form.register("whatsappNumber")}
                        placeholder="+13237023679"
                        hasError={Boolean(whatsappError)}
                        className="mt-2"
                      />
                      {whatsappError ? (
                        <p className="mt-2 text-xs text-red-500">{whatsappError}</p>
                      ) : null}
                    </div>

                    <div className="col-span-full mt-6 flex justify-end gap-4">
                      {successCard === "whatsapp" ? (
                        <p className="mr-auto text-sm text-emerald-600">WhatsApp settings saved.</p>
                      ) : null}
                      {errorCard === "whatsapp" ? (
                        <p className="mr-auto text-sm text-red-500">{saveErrorMessage}</p>
                      ) : null}
                      <Button
                        className="gap-2"
                        variant="primary"
                        type="button"
                        isLoading={savingCard === "whatsapp"}
                        onClick={() => void saveCardAsync("whatsapp")}
                      >
                        Save
                      </Button>
                    </div>

                  </div>
                </Card>

                <Card className="overflow-hidden border-gray-300 p-0 dark:border-gray-800 ">
                  <div className="overflow-hidden border-l-4 p-4">
                    <div className="flex items-center gap-4 pr-4">
                      <SiGooglemessages
                        className="size-8 shrink-0 "
                        aria-hidden="true"
                      />
                      <div className="truncate">
                        <h4 className="text-sm font-medium capitalize text-gray-900 dark:text-gray-50">
                          SMS
                        </h4>
                        <p className="text-sm text-gray-600 dark:text-gray-400 truncate">
                          Enable standard two-way text messaging.
                        </p>
                      </div>
                    </div>
                    <div className="mt-6">
                      <label

                        className="text-sm leading-none text-gray-900 dark:text-gray-50 font-medium"
                      >
                        Phone number
                      </label>
                      <Input
                        type="text"
                        id="twilio-phone-number"
                        autoComplete="given-name"
                        {...form.register("twilioPhoneNumber")}
                        placeholder="+13237023679"
                        hasError={Boolean(smsError)}
                        className="mt-2"
                      />
                      {smsError ? (
                        <p className="mt-2 text-xs text-red-500">{smsError}</p>
                      ) : null}
                    </div>

                    <div className="col-span-full mt-6 flex justify-end gap-4">
                      {successCard === "sms" ? (
                        <p className="mr-auto text-sm text-emerald-600">SMS settings saved.</p>
                      ) : null}
                      {errorCard === "sms" ? (
                        <p className="mr-auto text-sm text-red-500">{saveErrorMessage}</p>
                      ) : null}
                      <Button
                        className="gap-2"
                        variant="primary"
                        type="button"
                        isLoading={savingCard === "sms"}
                        onClick={() => void saveCardAsync("sms")}
                      >
                        Save
                      </Button>
                    </div>

                  </div>
                </Card>
              </div>
            </div>
          </div>
        </div>
      </form>
    </section>

  );
}
