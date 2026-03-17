"use client"

import { Button } from "@/components/admin/button"
import { useMobileState } from "@/lib/use-mobile"
import { cx } from "@/lib/utils"
import { ChevronLeft } from "lucide-react"
import { ChatInfoPanel } from "./chat-info-panel"
import { ChatPanel } from "./chat-panel"
import { ConversationListPanel } from "./conversation-list-panel"
import { useEffect, useMemo, useState } from "react"
import { chatInfo, ChatInfo, conversationMessages, conversations } from "@/data/data"

type MobileView = "list" | "chat" | "info"

export function ConversationOverview() {
  const fallbackConversation = conversations[0] ?? null
  const fallbackChatInfo = chatInfo[0] ?? null
  const hasRequiredData = fallbackConversation !== null && fallbackChatInfo !== null

  const { isMobile, isResolved } = useMobileState()
  const [activeConversationId, setActiveConversationId] = useState(() => {
    return fallbackConversation?.conversationId ?? ""
  })
  const [searchQuery, setSearchQuery] = useState("")
  const [agentActive, setAgentActive] = useState(true)
  const [mobileView, setMobileView] = useState<MobileView>("list")

  const filteredConversations = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase()
    if (normalizedQuery.length === 0) {
      return conversations
    }

    return conversations.filter((conversation) => {
      const haystack = [
        conversation.participantExternalId,
        conversation.channel,
        conversation.lastMessagePreview ?? "",
      ]
        .join(" ")
        .toLowerCase()
      return haystack.includes(normalizedQuery)
    })
  }, [searchQuery])

  useEffect(() => {
    if (!hasRequiredData) {
      return
    }

    const hasActiveConversation = filteredConversations.some((conversation) => {
      return conversation.conversationId === activeConversationId
    })

    if (!hasActiveConversation && filteredConversations.length > 0) {
      setActiveConversationId(filteredConversations[0].conversationId)
    }
  }, [activeConversationId, filteredConversations, hasRequiredData])

  useEffect(() => {
    if (!isResolved) {
      return
    }

    if (!isMobile) {
      setMobileView("list")
    }
  }, [isMobile, isResolved])

  const activeConversation =
    hasRequiredData
      ? conversations.find((conversation) => {
          return conversation.conversationId === activeConversationId
        }) ?? fallbackConversation
      : null

  const activeMessages = activeConversation
    ? conversationMessages[activeConversation.conversationId] ?? []
    : []

  const activeChatInfo: ChatInfo | null =
    activeConversation !== null
      ? chatInfo.find((chatInfoItem) => {
          return chatInfoItem.conversationId === activeConversation.conversationId
        }) ?? fallbackChatInfo
      : null

  if (!hasRequiredData || activeConversation === null || activeChatInfo === null) {
    return (
      <section className="p-4 text-sm text-gray-500 dark:text-gray-400">
        Conversation data is not available.
      </section>
    )
  }

  if (!isResolved) {
    return null
  }

  const resolvedActiveConversation = activeConversation
  const resolvedActiveChatInfo = activeChatInfo

  const onSelectConversation = (conversationId: string) => {
    setActiveConversationId(conversationId)
    if (isMobile) {
      setMobileView("chat")
    }
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
              mobileView === "list" ? "translate-x-0" : "-translate-x-full",
            )}
          >
            <ConversationListPanel
              conversations={filteredConversations}
              activeConversationId={resolvedActiveConversation.conversationId}
              searchQuery={searchQuery}
              onSearchQueryChange={setSearchQuery}
              onSelectConversation={onSelectConversation}
            />
          </div>
          <div
            className={cx(
              "absolute inset-0 transition-transform duration-300 ease-out",
              mobileView === "chat"
                ? "translate-x-0"
                : mobileView === "list"
                  ? "translate-x-full"
                  : "-translate-x-full",
            )}
          >
            <ChatPanel
              conversation={resolvedActiveConversation}
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
          </div>
          <div
            className={cx(
              "absolute inset-0 transition-transform duration-300 ease-out",
              mobileView === "info" ? "translate-x-0" : "translate-x-full",
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
                <ChatInfoPanel chatInfo={resolvedActiveChatInfo} />
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
          conversations={filteredConversations}
          activeConversationId={resolvedActiveConversation.conversationId}
          searchQuery={searchQuery}
          onSearchQueryChange={setSearchQuery}
          onSelectConversation={onSelectConversation}
        />
        <ChatPanel
          conversation={resolvedActiveConversation}
          messages={activeMessages}
          agentActive={agentActive}
          isMobile={false}
          onAgentToggle={() => {
            setAgentActive((currentValue) => !currentValue)
          }}
        />
        <ChatInfoPanel chatInfo={resolvedActiveChatInfo} />
      </div>
    </section>
  )
}
