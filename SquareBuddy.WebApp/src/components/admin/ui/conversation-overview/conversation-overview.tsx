"use client"

import {
  Drawer,
  DrawerBody,
  DrawerContent,
  DrawerHeader,
  DrawerTitle,
} from "@/components/admin/Drawer"
import { useIsMobile } from "@/lib/useMobile"
import { cx } from "@/lib/utils"
import { ChatInfoPanel } from "./chat-info-panel"
import { ChatPanel } from "./chat-panel"
import { ConversationListPanel } from "./conversation-list-panel"
import type { ChatInfo, ConversationOverviewData } from "./types"
import { useEffect, useMemo, useState } from "react"

export interface ConversationOverviewProps {
  data: ConversationOverviewData
}

export function ConversationOverview({ data }: ConversationOverviewProps) {
  const fallbackConversation = data.conversations[0]
  const fallbackChatInfo = data.chatInfo[0]

  if (!fallbackConversation || !fallbackChatInfo) {
    return (
      <section className="p-4 text-sm text-gray-500 dark:text-gray-400">
        Conversation data is not available.
      </section>
    )
  }

  const isMobile = useIsMobile()
  const [activeConversationId, setActiveConversationId] = useState(fallbackConversation.id)
  const [searchQuery, setSearchQuery] = useState("")
  const [agentActive, setAgentActive] = useState(true)
  const [mobileView, setMobileView] = useState<"list" | "chat">("list")
  const [isInfoDrawerOpen, setIsInfoDrawerOpen] = useState(false)

  const filteredConversations = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase()
    if (normalizedQuery.length === 0) {
      return data.conversations
    }
    return data.conversations.filter((conversation) => {
      const haystack = [
        conversation.name,
        conversation.channel,
        conversation.lastMessage,
      ]
        .join(" ")
        .toLowerCase()
      return haystack.includes(normalizedQuery)
    })
  }, [data.conversations, searchQuery])

  useEffect(() => {
    const hasActiveConversation = filteredConversations.some((conversation) => {
      return conversation.id === activeConversationId
    })
    if (!hasActiveConversation && filteredConversations.length > 0) {
      setActiveConversationId(filteredConversations[0].id)
    }
  }, [activeConversationId, filteredConversations])

  useEffect(() => {
    if (!isMobile) {
      setMobileView("list")
      setIsInfoDrawerOpen(false)
    }
  }, [isMobile])

  const activeConversation =
    data.conversations.find((conversation) => {
      return conversation.id === activeConversationId
    }) ?? fallbackConversation

  const activeMessages = data.messages.filter((message) => {
    return message.conversationId === activeConversation.id
  })

  const activeChatInfo: ChatInfo =
    data.chatInfo.find((chatInfo) => {
      return chatInfo.conversationId === activeConversation.id
    }) ?? fallbackChatInfo

  const onSelectConversation = (conversationId: string) => {
    setActiveConversationId(conversationId)
    if (isMobile) {
      setMobileView("chat")
    }
  }

  return (
    <section
      aria-label="Conversation overview"
      className="h-[calc(100svh-4rem)] overflow-hidden"
    >
      <div className="hidden h-full min-h-0 md:grid md:grid-cols-[20rem_minmax(0,1fr)_22rem]">
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

      <div className="relative h-full min-h-0 md:hidden">
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
            mobileView === "chat" ? "translate-x-0" : "translate-x-full",
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
              setIsInfoDrawerOpen(true)
            }}
          />
        </div>

        <Drawer open={isInfoDrawerOpen} onOpenChange={setIsInfoDrawerOpen}>
          <DrawerContent className="!inset-y-2 !left-2 !right-auto !mx-0 !w-[88vw] !max-w-sm !p-4 sm:!max-w-sm">
            <DrawerHeader>
              <DrawerTitle>Chat Info</DrawerTitle>
            </DrawerHeader>
            <DrawerBody className="min-h-0 px-0 pb-0">
              <ChatInfoPanel chatInfo={activeChatInfo} />
            </DrawerBody>
          </DrawerContent>
        </Drawer>
      </div>
    </section>
  )
}
