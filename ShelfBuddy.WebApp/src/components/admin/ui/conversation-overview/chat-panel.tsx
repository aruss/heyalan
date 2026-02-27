import { Button } from "@/components/admin/Button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/admin/DropdownMenu"
import { ConversationChannel, ConversationItem, MessageItem } from "@/data/data"
import { Bot, ChevronLeft, ChevronRight, MessageSquare, MoreHorizontal, Phone, Send } from "lucide-react"
import type { ElementType } from "react"


export interface ChatPanelProps {
  conversation: ConversationItem
  messages: MessageItem[]
  agentActive: boolean
  isMobile: boolean
  onAgentToggle: () => void
  onBackToList?: () => void
  onOpenInfo?: () => void
}

const channelIcons: Record<ConversationChannel, ElementType> = {
  WhatsApp: MessageSquare,
  Telegram: Send,
  SMS: Phone,
}

export function ChatPanel({
  conversation,
  messages,
  agentActive,
  isMobile,
  onAgentToggle,
  onBackToList,
  onOpenInfo,
}: ChatPanelProps) {
  const ChannelIcon = channelIcons[conversation.channel]

  return (
    <section className="flex h-full min-h-0 flex-col bg-white dark:bg-gray-950 border-r border-gray-200 dark:border-gray-800 ">
      <header className="flex h-16 shrink-0 items-center justify-between border-b border-gray-200 bg-gray-50 px-4 dark:border-gray-800 dark:bg-gray-925">
        <div className="flex min-w-0 items-center gap-2 sm:gap-3">
          {isMobile ? (
            <Button
              type="button"
              variant="ghost"
              className="!p-2"
              onClick={onBackToList}
            >
              <ChevronLeft className="size-4" aria-hidden="true" />
              <span className="sr-only">Back to conversations</span>
            </Button>
          ) : null}
          <ChannelIcon
            className="size-4 shrink-0 text-gray-500 dark:text-gray-400"
            aria-hidden="true"
          />
          <span className="truncate text-sm font-semibold text-gray-900 dark:text-gray-50">
            {conversation.name}
          </span>
          <span className="hidden rounded-md bg-gray-100 px-2 py-1 text-xs font-medium text-gray-700 dark:bg-gray-800 dark:text-gray-300 sm:inline-flex">
            {agentActive ? "AI Agent Active" : "Human Operator"}
          </span>
        </div>

        <div className="flex items-center gap-1">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button type="button" variant="ghost" className="!p-2">
                <MoreHorizontal className="size-4" aria-hidden="true" />
                <span className="sr-only">Open chat actions</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem
                onClick={() => {
                  onAgentToggle()
                }}
              >
                {agentActive ? "Take Over" : "Return to AI"}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

          {isMobile ? (
            <Button
              type="button"
              variant="ghost"
              className="!p-2"
              onClick={onOpenInfo}
            >
              <ChevronRight className="size-4" aria-hidden="true" />
              <span className="sr-only">Open chat info</span>
            </Button>
          ) : null}
        </div>
      </header>

      <div className="min-h-0 flex-1 space-y-4 overflow-y-auto p-4">
        <div className="my-3 flex w-full items-center">
          <div className="grow border-t border-gray-200 dark:border-gray-800" />
          <span className="px-4 text-xs font-medium uppercase tracking-wider text-gray-400 dark:text-gray-500">
            Today
          </span>
          <div className="grow border-t border-gray-200 dark:border-gray-800" />
        </div>
        {messages.map((message) => (
          <div
            key={message.id}
            className={message.sender === "user" ? "flex justify-end" : "flex justify-start"}
          >
            <div
              className={
                message.sender === "user"
                  ? "flex max-w-[80%] flex-col items-end"
                  : "flex max-w-[80%] flex-col items-start"
              }
            >
              <div className="mb-1 flex items-center gap-2 text-xs text-gray-400 dark:text-gray-500">
                <span>{message.time}</span>
                {message.sender === "agent" ? (
                  <Bot className="size-3" aria-hidden="true" />
                ) : null}
              </div>
              <div
                className={
                  message.sender === "user"
                    ? "rounded-xl rounded-tr-none bg-gray-900 p-3 text-sm text-gray-50 dark:bg-gray-100 dark:text-gray-900"
                    : "rounded-xl rounded-tl-none border border-gray-200 bg-gray-100 p-3 text-sm text-gray-900 dark:border-gray-800 dark:bg-gray-900 dark:text-gray-50"
                }
              >
                {message.text}
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="flex shrink-0 items-center gap-2 border-t border-gray-200 bg-white p-4 dark:border-gray-800 dark:bg-gray-950">
        <input
          type="text"
          placeholder={agentActive ? "Take over to type a message..." : "Type your message..."}
          disabled={agentActive}
          className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm text-gray-900 outline-none transition focus:border-gray-500 focus:ring-2 focus:ring-gray-200 disabled:opacity-50 dark:border-gray-800 dark:bg-gray-950 dark:text-gray-50 dark:focus:border-gray-700 dark:focus:ring-gray-800"
        />
        <Button type="button" disabled={agentActive} className="!px-4 !py-2">
          <Send className="mr-1.5 size-4" aria-hidden="true" />
          Send
        </Button>
      </div>
    </section>
  )
}
