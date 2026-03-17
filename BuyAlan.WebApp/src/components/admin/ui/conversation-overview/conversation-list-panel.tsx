// import { Input } from "@/components/admin/input"
import type { ConversationListItem } from "@/lib/api"
import { cx } from "@/lib/utils"
import { formatInboxTimestamp } from "./conversation-overview-formatters"

export interface ConversationListPanelProps {
  conversations: ConversationListItem[]
  activeConversationId: string
  // searchQuery?: string
  // onSearchQueryChange?: (value: string) => void
  onSelectConversation: (conversationId: string) => void
}

export function ConversationListPanel({
  conversations,
  activeConversationId,  
  onSelectConversation,
}: ConversationListPanelProps) {
  return (
    <section className="flex h-full min-h-0 flex-col border-r border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-950">
      {/*<div className="border-b border-gray-200 p-4 dark:border-gray-800">
        <h2 className="mb-2 text-base font-semibold text-gray-900 dark:text-gray-50">
          Conversations
        </h2>
        <Input
          type="search"
          value={searchQuery}
          onChange={(event) => {
            onSearchQueryChange(event.target.value)
          }}
          placeholder="Search conversations..."
        />
      </div>*/}

      <div className="min-h-0 flex-1 overflow-y-auto">
        {conversations.length === 0 ? (
          <div className="p-4 text-sm text-gray-500 dark:text-gray-400">
            No conversations found.
          </div>
        ) : (
          conversations.map((conversation) => {
            const isActive = conversation.conversationId === activeConversationId
            return (
              <button
                key={conversation.conversationId}
                type="button"
                onClick={() => {
                  onSelectConversation(conversation.conversationId)
                }}
                className={cx(
                  "w-full border-b border-gray-100 px-4 py-3 text-left transition hover:bg-gray-50 dark:border-gray-900 dark:hover:bg-gray-900",
                  isActive && "bg-gray-50 dark:bg-gray-900",
                )}
              >
                <div className="flex items-center justify-between gap-2">
                  <span className="truncate text-sm font-medium text-gray-900 dark:text-gray-50">
                    {conversation.participantExternalId}
                  </span>
                  <span className="text-xs text-gray-500 dark:text-gray-400">
                    {formatInboxTimestamp(conversation.lastMessageAt)}
                  </span>
                </div>
                <div className="mt-1 flex items-center justify-between gap-2">
                  <span className="truncate text-xs text-gray-500 dark:text-gray-400">
                    {conversation.lastMessagePreview ?? ""}
                  </span>
                  <div className="flex items-center gap-2">
                    {conversation.hasUnread ? (
                      <span className="size-1.5 rounded-full bg-gray-900 dark:bg-gray-100" />
                    ) : null}
                    <span className="rounded-md bg-gray-100 px-1.5 py-0.5 text-[10px] font-medium text-gray-700 dark:bg-gray-800 dark:text-gray-300">
                      {conversation.channel}
                    </span>
                  </div>
                </div>
              </button>
            )
          })
        )}
      </div>
    </section>
  )
}
