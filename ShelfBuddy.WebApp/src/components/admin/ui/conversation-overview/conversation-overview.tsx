"use client"

import { Button } from "@/components/admin/Button"
import { useIsMobile } from "@/lib/useMobile"
import { cx } from "@/lib/utils"
import { ChevronLeft } from "lucide-react"
import { ChatInfoPanel } from "./chat-info-panel"
import { ChatPanel } from "./chat-panel"
import { ConversationListPanel } from "./conversation-list-panel"
import { useEffect, useMemo, useState } from "react"
import { chatInfo, ChatInfo, conversations, messages } from "@/data/data"



export function ConversationOverview() {
  const fallbackConversation = conversations[0]
  const fallbackChatInfo = chatInfo[0]

  const isMobile = useIsMobile()
  const [activeConversationId, setActiveConversationId] = useState(fallbackConversation.id)
  const [searchQuery, setSearchQuery] = useState("")
  const [agentActive, setAgentActive] = useState(true)
  const [mobileView, setMobileView] = useState<"list" | "chat" | "info">("list")


  const filteredConversations = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase()
    if (normalizedQuery.length === 0) {
      return conversations
    }

    return conversations.filter((conversation) => {
      const haystack = [
        conversation.name,
        conversation.channel,
        conversation.lastMsg,
      ]
        .join(" ")
        .toLowerCase()
      return haystack.includes(normalizedQuery)
    })
  }, [searchQuery])

  useEffect(() => {
    const hasActiveConversation = filteredConversations.some((conversation) => {
      return conversation.id === activeConversationId
    }); 
    
    if (!hasActiveConversation && filteredConversations.length > 0) {
      setActiveConversationId(filteredConversations[0].id)
    }
  }, [activeConversationId, filteredConversations])

  useEffect(() => {
    if (!isMobile) {
      setMobileView("list")
    }
  }, [isMobile])

  const activeConversation =
    conversations.find((conversation) => {
      return conversation.id === activeConversationId
    }) ?? fallbackConversation

  const activeMessages = messages.filter((message) => {
    return message.convoId === activeConversation.id
  })

  const activeChatInfo: ChatInfo =
    chatInfo.find((chatInfo) => {
      return chatInfo.conversationId === activeConversation.id
    }) ?? fallbackChatInfo

  const onSelectConversation = (conversationId: string) => {
    setActiveConversationId(conversationId)
    if (isMobile) {
      setMobileView("chat")
    }
  }

  
  if (!fallbackConversation || !fallbackChatInfo) {
    return (
      <section className="p-4 text-sm text-gray-500 dark:text-gray-400">
        Conversation data is not available.
      </section>
    )
  }
  
  return (
    <section
      aria-label="Conversation overview"
      className="h-[calc(100svh-4rem)] overflow-hidden md:overflow-x-auto"
    >
      <div className="hidden h-full min-h-0 md:grid md:grid-cols-[20rem_minmax(24rem,1fr)_22rem]">
        <ConversationListPanel
          conversations={filteredConversations}
          activeConversationId={activeConversation.id}
          searchQuery={searchQuery}
          onSearchQueryChange={setSearchQuery}
          onSelectConversation={onSelectConversation}
        />
        <ChatPanel
          conversation={activeConversation}
          messages={activeMessages}
          agentActive={agentActive}
          isMobile={false}
          onAgentToggle={() => {
            setAgentActive((currentValue) => !currentValue)
          }}
        />
        <ChatInfoPanel chatInfo={activeChatInfo} />
      </div>

      <div className="relative h-full min-h-0  md:hidden">
        <div
          className={cx(
            "absolute inset-0 transition-transform duration-300 ease-out",
            mobileView === "list" ? "translate-x-0" : "-translate-x-full",
          )}
        >
          <ConversationListPanel
            conversations={filteredConversations}
            activeConversationId={activeConversation.id}
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
              <ChatInfoPanel chatInfo={activeChatInfo} />
            </div>
          </section>
        </div>
      </div>
    </section>
  )
}
