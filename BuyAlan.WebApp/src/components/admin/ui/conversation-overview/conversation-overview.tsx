"use client"

import { Button } from "@/components/admin/button"
import {
  getAgentsByAgentIdConversationsByConversationIdMessagesOptions,
  getAgentsByAgentIdConversationsOptions,
  getAgentsOptions,
} from "@/lib/api/@tanstack/react-query.gen"
import { useSession } from "@/lib/session-context"
import { useMobileState } from "@/lib/use-mobile"
import { cx } from "@/lib/utils"
import { useQuery } from "@tanstack/react-query"
import { ChevronLeft } from "lucide-react"
import { useMemo, useState } from "react"
import { chatInfo, ChatInfo } from "@/data/data"
import { ChatInfoPanel } from "./chat-info-panel"
import { ChatPanel } from "./chat-panel"
import { ConversationListPanel } from "./conversation-list-panel"

type MobileView = "list" | "chat" | "info"

const DEFAULT_INBOX_ERROR = "Unable to load inbox."
const DEFAULT_MESSAGES_ERROR = "Unable to load conversation messages."
const MISSING_SUBSCRIPTION_ERROR = "No active subscription available for this account."
const NO_AGENT_ERROR = "No agent found for this subscription."
const QUERY_SKIP = 0
const QUERY_TAKE = 1000

const resolveApiErrorMessage = (error: unknown, fallback: string) => {
  if (error && typeof error === "object") {
    const errorRecord = error as Record<string, unknown>
    const message = errorRecord.message
    if (typeof message === "string" && message.trim().length > 0) {
      return message
    }
  }

  return fallback
}

interface ConversationPlaceholderPanelProps {
  title: string
  description: string
}

function ConversationPlaceholderPanel({
  title,
  description,
}: ConversationPlaceholderPanelProps) {
  return (
    <section className="flex h-full min-h-0 flex-col border-r border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-950">
      <div className="flex min-h-0 flex-1 items-center justify-center p-6 text-center">
        <div className="space-y-2">
          <h2 className="text-sm font-semibold text-gray-900 dark:text-gray-50">
            {title}
          </h2>
          <p className="text-sm text-gray-500 dark:text-gray-400">{description}</p>
        </div>
      </div>
    </section>
  )
}

export function ConversationOverview() {
  const fallbackChatInfo = chatInfo[0] ?? null
  const { currentUser, errorMessage: sessionErrorMessage, isLoading: isSessionLoading } =
    useSession()
  const { isMobile, isResolved } = useMobileState()
  const [activeConversationId, setActiveConversationId] = useState("")
  const [agentActive, setAgentActive] = useState(true)
  const [mobileView, setMobileView] = useState<MobileView>("list")

  const subscriptionId = currentUser?.activeSubscriptionId ?? null

  const agentsQuery = useQuery({
    ...getAgentsOptions({
      query: {
        subscription: subscriptionId ?? "",
      },
    }),
    enabled: subscriptionId !== null,
    retry: false,
  })

  const agentId = agentsQuery.data?.items?.[0]?.agentId ?? null

  const conversationsQuery = useQuery({
    ...getAgentsByAgentIdConversationsOptions({
      path: {
        agentId: agentId ?? "",
      },
      query: {
        skip: QUERY_SKIP,
        take: QUERY_TAKE,
      },
    }),
    enabled: agentId !== null,
    retry: false,
  })

  const conversations = useMemo(() => {
    return conversationsQuery.data?.items ?? []
  }, [conversationsQuery.data?.items])

  const resolvedActiveConversationId = useMemo(() => {
    const hasSelectedConversation = conversations.some((conversation) => {
      return conversation.conversationId === activeConversationId
    })

    if (hasSelectedConversation) {
      return activeConversationId
    }

    return conversations[0]?.conversationId ?? ""
  }, [activeConversationId, conversations])

  const resolvedMobileView = useMemo(() => {
    if (!isMobile || conversations.length === 0) {
      return "list"
    }

    return mobileView
  }, [conversations.length, isMobile, mobileView])

  const messagesQuery = useQuery({
    ...getAgentsByAgentIdConversationsByConversationIdMessagesOptions({
      path: {
        agentId: agentId ?? "",
        conversationId: resolvedActiveConversationId,
      },
      query: {
        skip: QUERY_SKIP,
        take: QUERY_TAKE,
      },
    }),
    enabled: agentId !== null && resolvedActiveConversationId.length > 0,
    retry: false,
  })

  const activeConversation =
    conversations.find((conversation) => {
      return conversation.conversationId === resolvedActiveConversationId
    }) ?? null

  const activeMessages = useMemo(() => {
    const apiMessages = messagesQuery.data?.items ?? []
    return [...apiMessages].reverse()
  }, [messagesQuery.data?.items])

  const activeChatInfo: ChatInfo | null = activeConversation === null ? null : fallbackChatInfo

  const isResolvingAgent =
    isSessionLoading ||
    agentsQuery.isLoading ||
    agentsQuery.isRefetching ||
    conversationsQuery.isLoading ||
    conversationsQuery.isRefetching

  const inboxErrorMessage = useMemo(() => {
    if (sessionErrorMessage) {
      return sessionErrorMessage
    }

    if (subscriptionId === null && !isSessionLoading) {
      return MISSING_SUBSCRIPTION_ERROR
    }

    if (agentsQuery.error) {
      return resolveApiErrorMessage(agentsQuery.error, DEFAULT_INBOX_ERROR)
    }

    if (!isResolvingAgent && agentId === null) {
      return NO_AGENT_ERROR
    }

    if (conversationsQuery.error) {
      return resolveApiErrorMessage(conversationsQuery.error, DEFAULT_INBOX_ERROR)
    }

    return null
  }, [
    agentId,
    agentsQuery.error,
    conversationsQuery.error,
    isResolvingAgent,
    isSessionLoading,
    sessionErrorMessage,
    subscriptionId,
  ])

  const messagesErrorMessage = useMemo(() => {
    if (messagesQuery.error) {
      return resolveApiErrorMessage(messagesQuery.error, DEFAULT_MESSAGES_ERROR)
    }

    return null
  }, [messagesQuery.error])

  const activeConversationIdForList = activeConversation?.conversationId ?? ""
  const conversationListEmptyStateLabel = isResolvingAgent
    ? "Loading conversations..."
    : (inboxErrorMessage ?? "No conversations found.")

  const onSelectConversation = (conversationId: string) => {
    setActiveConversationId(conversationId)
    if (isMobile) {
      setMobileView("chat")
    }
  }

  if (!isResolved) {
    return null
  }

  if (isMobile) {
    return (
      <section
        aria-label="Conversation overview"
        className="h-[calc(100svh-4rem)] overflow-hidden"
      >
        <div className="relative h-full min-h-0 overflow-hidden md:hidden">
          <div
            className={cx(
              "absolute inset-0 transition-transform duration-300 ease-out",
              resolvedMobileView === "list" ? "translate-x-0" : "-translate-x-full",
            )}
          >
            <ConversationListPanel
              conversations={conversations}
              activeConversationId={activeConversationIdForList}
              emptyStateLabel={conversationListEmptyStateLabel}
              onSelectConversation={onSelectConversation}
            />
          </div>
          <div
            className={cx(
              "absolute inset-0 transition-transform duration-300 ease-out",
              resolvedMobileView === "chat"
                ? "translate-x-0"
                : resolvedMobileView === "list"
                  ? "translate-x-full"
                  : "-translate-x-full",
            )}
          >
            {activeConversation === null ? (
              <ConversationPlaceholderPanel
                title="Conversation"
                description={
                  inboxErrorMessage ?? (
                    isResolvingAgent ? "Loading conversation..." : "No conversation selected."
                  )
                }
              />
            ) : messagesQuery.isLoading || messagesQuery.isRefetching ? (
              <ConversationPlaceholderPanel
                title={activeConversation.participantExternalId}
                description="Loading messages..."
              />
            ) : messagesErrorMessage ? (
              <ConversationPlaceholderPanel
                title={activeConversation.participantExternalId}
                description={messagesErrorMessage}
              />
            ) : (
              <ChatPanel
                conversation={activeConversation}
                messages={activeMessages}
                agentActive={agentActive}
                isMobile
                onAgentToggle={() => {
                  setAgentActive((currentValue) => !currentValue)
                }}
                onBackToList={() => {
                  setMobileView("list")
                }}
                onOpenInfo={() => {
                  setMobileView("info")
                }}
              />
            )}
          </div>
          <div
            className={cx(
              "absolute inset-0 transition-transform duration-300 ease-out",
              resolvedMobileView === "info" ? "translate-x-0" : "translate-x-full",
            )}
          >
            <section className="flex h-full min-h-0 flex-col border-r border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-950">
              <header className="flex h-16 shrink-0 items-center gap-2 border-b border-gray-200 bg-gray-50 px-4 dark:border-gray-800 dark:bg-gray-925">
                <Button
                  type="button"
                  variant="ghost"
                  className="!p-2"
                  onClick={() => {
                    setMobileView("chat")
                  }}
                >
                  <ChevronLeft className="size-4" aria-hidden="true" />
                  <span className="sr-only">Back to chat</span>
                </Button>
                <h2 className="text-sm font-semibold text-gray-900 dark:text-gray-50">
                  Chat Info
                </h2>
              </header>
              <div className="min-h-0 flex-1">
                {activeChatInfo === null ? (
                  <ConversationPlaceholderPanel
                    title="Chat Info"
                    description="Select a conversation to view chat details."
                  />
                ) : (
                  <ChatInfoPanel chatInfo={activeChatInfo} />
                )}
              </div>
            </section>
          </div>
        </div>
      </section>
    )
  }

  return (
    <section
      aria-label="Conversation overview"
      className="h-[calc(100svh-4rem)] overflow-hidden md:overflow-x-auto"
    >
      <div className="h-full min-h-0 grid-cols-[20rem_minmax(24rem,1fr)_22rem] md:grid">
        <ConversationListPanel
          conversations={conversations}
          activeConversationId={activeConversationIdForList}
          emptyStateLabel={conversationListEmptyStateLabel}
          onSelectConversation={onSelectConversation}
        />
        {activeConversation === null ? (
          <ConversationPlaceholderPanel
            title="Conversation"
            description={
              inboxErrorMessage ?? (
                isResolvingAgent ? "Loading conversation..." : "No conversation selected."
              )
            }
          />
        ) : messagesQuery.isLoading || messagesQuery.isRefetching ? (
          <ConversationPlaceholderPanel
            title={activeConversation.participantExternalId}
            description="Loading messages..."
          />
        ) : messagesErrorMessage ? (
          <ConversationPlaceholderPanel
            title={activeConversation.participantExternalId}
            description={messagesErrorMessage}
          />
        ) : (
          <ChatPanel
            conversation={activeConversation}
            messages={activeMessages}
            agentActive={agentActive}
            isMobile={false}
            onAgentToggle={() => {
              setAgentActive((currentValue) => !currentValue)
            }}
          />
        )}
        {activeChatInfo === null ? (
          <ConversationPlaceholderPanel
            title="Chat Info"
            description="Select a conversation to view chat details."
          />
        ) : (
          <ChatInfoPanel chatInfo={activeChatInfo} />
        )}
      </div>
    </section>
  )
}
