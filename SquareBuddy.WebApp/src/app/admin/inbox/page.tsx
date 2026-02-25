"use client"

import { ConversationOverview } from "@/components/admin/ui/conversation-overview/conversation-overview";
import { conversationOverviewData } from "@/data/data";

export default function Overview() {
  return <ConversationOverview data={conversationOverviewData} />;
}
