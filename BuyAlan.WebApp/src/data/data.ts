import type {
  ConversationListItem,
  ConversationMessageItem,
  MessageRole,
} from "@/lib/api"


export interface CartItem {
  id: string
  name: string
  variant: string
  price: number
  quantity: number
}

export interface PastOrder {
  id: string
  dateLabel: string
  total: number
  status: string
}

export interface ManualAction {
  id: string
  label: string
  action: "upsell" | "payment-link" | "shipping"
}

export interface CustomerInfo {
  name: string
  contact: string
  lifetimeValue: number
  tags: string[]
}

export interface ChatInfo {
  conversationId: string
  customer: CustomerInfo
  cartItems: CartItem[]
  manualActions: ManualAction[]
  pastOrders: PastOrder[]
}
const MINUTES_PER_HOUR = 60
const HOURS_PER_DAY = 24

const createRelativeTimestamp = ({
  daysAgo = 0,
  hoursAgo = 0,
  minutesAgo = 0,
}: {
  daysAgo?: number
  hoursAgo?: number
  minutesAgo?: number
}) => {
  const timestamp = new Date()
  timestamp.setMinutes(
    timestamp.getMinutes()
      - minutesAgo
      - (hoursAgo * MINUTES_PER_HOUR)
      - (daysAgo * HOURS_PER_DAY * MINUTES_PER_HOUR),
  )
  return timestamp.toISOString()
}

const createConversationMock = ({
  conversationId,
  participantExternalId,
  channel,
  lastMessagePreview,
  daysAgo = 0,
  hoursAgo = 0,
  minutesAgo = 0,
  lastMessageRole,
  unreadCount,
}: {
  conversationId: string
  participantExternalId: string
  channel: ConversationListItem["channel"]
  lastMessagePreview: ConversationListItem["lastMessagePreview"]
  daysAgo?: number
  hoursAgo?: number
  minutesAgo?: number
  lastMessageRole: ConversationListItem["lastMessageRole"]
  unreadCount: number
}): ConversationListItem => {
  return {
    conversationId,
    participantExternalId,
    channel,
    lastMessagePreview,
    lastMessageAt: createRelativeTimestamp({ daysAgo, hoursAgo, minutesAgo }),
    lastMessageRole,
    unreadCount,
    hasUnread: unreadCount > 0,
  }
}

const createMessageMock = ({
  messageId,
  role,
  content,
  from,
  to,
  daysAgo = 0,
  hoursAgo = 0,
  minutesAgo = 0,
  isRead = true,
}: {
  messageId: string
  role: MessageRole
  content: string
  from: string
  to: string
  daysAgo?: number
  hoursAgo?: number
  minutesAgo?: number
  isRead?: boolean
}): ConversationMessageItem => {
  const occurredAt = createRelativeTimestamp({ daysAgo, hoursAgo, minutesAgo })

  return {
    messageId,
    role,
    content,
    from,
    to,
    occurredAt,
    isRead,
    readAt: isRead ? occurredAt : null,
  }
}

export const conversations: ConversationListItem[] = [
  createConversationMock({
    conversationId: "1",
    participantExternalId: "+1 555-0101",
    channel: "WhatsApp",
    lastMessagePreview: "Is the Pro version in stock?",
    minutesAgo: 8,
    lastMessageRole: "Customer",
    unreadCount: 2,
  }),
  createConversationMock({
    conversationId: "2",
    participantExternalId: "@alex_dev",
    channel: "Telegram",
    lastMessagePreview: "Payment completed.",
    minutesAgo: 95,
    lastMessageRole: "Agent",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "3",
    participantExternalId: "+44 7700 900077",
    channel: "SMS",
    lastMessagePreview: "When will it ship?",
    daysAgo: 1,
    hoursAgo: 2,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "4",
    participantExternalId: "+49 1512 3456789",
    channel: "WhatsApp",
    lastMessagePreview: "Can you invoice with VAT ID?",
    hoursAgo: 3,
    minutesAgo: 5,
    lastMessageRole: "Customer",
    unreadCount: 1,
  }),
  createConversationMock({
    conversationId: "5",
    participantExternalId: "@maria_pm",
    channel: "Telegram",
    lastMessagePreview: "Do you have the white keycaps set?",
    daysAgo: 2,
    hoursAgo: 3,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "6",
    participantExternalId: "+33 6 12 34 56 78",
    channel: "SMS",
    lastMessagePreview: "Need to change delivery address.",
    daysAgo: 3,
    hoursAgo: 1,
    lastMessageRole: "Customer",
    unreadCount: 1,
  }),
  createConversationMock({
    conversationId: "7",
    participantExternalId: "+1 555-0199",
    channel: "WhatsApp",
    lastMessagePreview: "Any discount for 2 units?",
    daysAgo: 3,
    hoursAgo: 7,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "8",
    participantExternalId: "@kamil_w",
    channel: "Telegram",
    lastMessagePreview: "Order #4921: please confirm switch type.",
    daysAgo: 4,
    hoursAgo: 2,
    lastMessageRole: "Customer",
    unreadCount: 3,
  }),
  createConversationMock({
    conversationId: "9",
    participantExternalId: "+48 600 700 800",
    channel: "SMS",
    lastMessagePreview: "Can I pick up in store?",
    daysAgo: 4,
    hoursAgo: 9,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "10",
    participantExternalId: "+81 90 1234 5678",
    channel: "WhatsApp",
    lastMessagePreview: "Does it support Mac layout?",
    daysAgo: 5,
    hoursAgo: 4,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "11",
    participantExternalId: "@noah_sre",
    channel: "Telegram",
    lastMessagePreview: "Tracking link says label created only.",
    daysAgo: 5,
    hoursAgo: 12,
    lastMessageRole: "Customer",
    unreadCount: 1,
  }),
  createConversationMock({
    conversationId: "12",
    participantExternalId: "+61 412 345 678",
    channel: "SMS",
    lastMessagePreview: "Please cancel my order.",
    daysAgo: 5,
    hoursAgo: 14,
    lastMessageRole: "Customer",
    unreadCount: 1,
  }),
  createConversationMock({
    conversationId: "13",
    participantExternalId: "+1 555-0133",
    channel: "WhatsApp",
    lastMessagePreview: "Can you recommend a quieter switch?",
    daysAgo: 6,
    hoursAgo: 3,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "14",
    participantExternalId: "@lina_design",
    channel: "Telegram",
    lastMessagePreview: "Need 10 keyboards for the team. Bulk pricing?",
    daysAgo: 6,
    hoursAgo: 10,
    lastMessageRole: "Customer",
    unreadCount: 2,
  }),
  createConversationMock({
    conversationId: "15",
    participantExternalId: "+39 320 123 4567",
    channel: "SMS",
    lastMessagePreview: "Do you ship to Italy?",
    daysAgo: 7,
    hoursAgo: 4,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "16",
    participantExternalId: "+34 612 345 678",
    channel: "WhatsApp",
    lastMessagePreview: "My package arrived damaged.",
    daysAgo: 7,
    hoursAgo: 6,
    lastMessageRole: "Customer",
    unreadCount: 1,
  }),
  createConversationMock({
    conversationId: "17",
    participantExternalId: "@tom_qc",
    channel: "Telegram",
    lastMessagePreview: "Key A sometimes double registers.",
    daysAgo: 8,
    hoursAgo: 1,
    lastMessageRole: "Customer",
    unreadCount: 1,
  }),
  createConversationMock({
    conversationId: "18",
    participantExternalId: "+1 555-0177",
    channel: "SMS",
    lastMessagePreview: "Is there a wrist rest included?",
    daysAgo: 8,
    hoursAgo: 5,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "19",
    participantExternalId: "+46 70 123 45 67",
    channel: "WhatsApp",
    lastMessagePreview: "Can I get ISO-Nordic layout?",
    daysAgo: 8,
    hoursAgo: 9,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "20",
    participantExternalId: "@jen_ops",
    channel: "Telegram",
    lastMessagePreview: "Please resend confirmation email.",
    daysAgo: 9,
    hoursAgo: 2,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "21",
    participantExternalId: "+1 555-0148",
    channel: "WhatsApp",
    lastMessagePreview: "Do you have the aluminum case in black?",
    daysAgo: 10,
    hoursAgo: 4,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "22",
    participantExternalId: "@viktor_ml",
    channel: "Telegram",
    lastMessagePreview: "Can you split shipment into 2 addresses?",
    daysAgo: 11,
    hoursAgo: 6,
    lastMessageRole: "Customer",
    unreadCount: 2,
  }),
  createConversationMock({
    conversationId: "23",
    participantExternalId: "+52 55 1234 5678",
    channel: "SMS",
    lastMessagePreview: "Need help pairing via Bluetooth.",
    daysAgo: 12,
    hoursAgo: 3,
    lastMessageRole: "Customer",
    unreadCount: 1,
  }),
  createConversationMock({
    conversationId: "24",
    participantExternalId: "+1 555-0112",
    channel: "WhatsApp",
    lastMessagePreview: "What is your return policy?",
    daysAgo: 13,
    hoursAgo: 4,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "25",
    participantExternalId: "@sasha_fin",
    channel: "Telegram",
    lastMessagePreview: "Can you provide a proforma invoice?",
    daysAgo: 15,
    hoursAgo: 4,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "26",
    participantExternalId: "+7 916 123-45-67",
    channel: "SMS",
    lastMessagePreview: "Do you sell replacement stabilizers?",
    daysAgo: 16,
    hoursAgo: 1,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "27",
    participantExternalId: "+1 555-0166",
    channel: "WhatsApp",
    lastMessagePreview: "My promo code is not working.",
    daysAgo: 17,
    hoursAgo: 6,
    lastMessageRole: "Customer",
    unreadCount: 1,
  }),
  createConversationMock({
    conversationId: "28",
    participantExternalId: "@hanna_hr",
    channel: "Telegram",
    lastMessagePreview: "Need 5 units by Friday. Possible?",
    daysAgo: 18,
    hoursAgo: 2,
    lastMessageRole: "Customer",
    unreadCount: 2,
  }),
  createConversationMock({
    conversationId: "29",
    participantExternalId: "+1 555-0181",
    channel: "SMS",
    lastMessagePreview: null,
    daysAgo: 20,
    hoursAgo: 4,
    lastMessageRole: null,
    unreadCount: 0,
  }),
  createConversationMock({
    conversationId: "30",
    participantExternalId: "+86 138 0013 8000",
    channel: "WhatsApp",
    lastMessagePreview: "Does it have hot-swap sockets?",
    daysAgo: 21,
    hoursAgo: 5,
    lastMessageRole: "Customer",
    unreadCount: 0,
  }),
]

export const conversationMessages: Record<string, ConversationMessageItem[]> = {
  "1": [
    createMessageMock({
      messageId: "m1",
      role: "Customer",
      content: "Hi, I want to order the mechanical keyboard.",
      from: "+1 555-0101",
      to: "BuyAlan",
      minutesAgo: 15,
      isRead: true,
    }),
    createMessageMock({
      messageId: "m2",
      role: "Agent",
      content: "Hello! I can help with that. Are you looking for tactile or linear switches?",
      from: "BuyAlan",
      to: "+1 555-0101",
      minutesAgo: 14,
      isRead: true,
    }),
    createMessageMock({
      messageId: "m3",
      role: "Customer",
      content: "Tactile, please. Is the Pro version in stock?",
      from: "+1 555-0101",
      to: "BuyAlan",
      minutesAgo: 8,
      isRead: false,
    }),
    createMessageMock({
      messageId: "m4",
      role: "Agent",
      content: "Yes. Pro is in stock in black and silver. Which case color do you prefer?",
      from: "BuyAlan",
      to: "+1 555-0101",
      minutesAgo: 7,
      isRead: true,
    }),
    createMessageMock({
      messageId: "m5",
      role: "Customer",
      content: "Black case. Also, do you have ISO-DE layout?",
      from: "+1 555-0101",
      to: "BuyAlan",
      minutesAgo: 6,
      isRead: false,
    }),
    createMessageMock({
      messageId: "m6",
      role: "Operator",
      content: "I can confirm ISO-DE is available and I can hold one while you decide on keycaps.",
      from: "Max from support",
      to: "+1 555-0101",
      minutesAgo: 4,
      isRead: true,
    }),
  ],
  "2": [
    createMessageMock({
      messageId: "m9",
      role: "Customer",
      content: "Hey, just paid the invoice for order #4920.",
      from: "@alex_dev",
      to: "BuyAlan",
      minutesAgo: 100,
      isRead: true,
    }),
    createMessageMock({
      messageId: "m10",
      role: "Agent",
      content: "Thanks. I see the payment and your order is confirmed.",
      from: "BuyAlan",
      to: "@alex_dev",
      minutesAgo: 95,
      isRead: true,
    }),
  ],
  "3": [
    createMessageMock({
      messageId: "m13",
      role: "Customer",
      content: "Hi, order placed yesterday. When will it ship?",
      from: "+44 7700 900077",
      to: "BuyAlan",
      daysAgo: 1,
      hoursAgo: 4,
      isRead: true,
    }),
    createMessageMock({
      messageId: "m14",
      role: "Agent",
      content: "Orders before 2 PM ship the same day. What is your order number?",
      from: "BuyAlan",
      to: "+44 7700 900077",
      daysAgo: 1,
      hoursAgo: 3,
      isRead: true,
    }),
    createMessageMock({
      messageId: "m15",
      role: "Customer",
      content: "Order #4917.",
      from: "+44 7700 900077",
      to: "BuyAlan",
      daysAgo: 1,
      hoursAgo: 2,
      isRead: true,
    }),
  ],
  "4": [
    createMessageMock({
      messageId: "m17",
      role: "Customer",
      content: "Can you issue an invoice with VAT ID for our company?",
      from: "+49 1512 3456789",
      to: "BuyAlan",
      hoursAgo: 3,
      minutesAgo: 8,
      isRead: false,
    }),
    createMessageMock({
      messageId: "m18",
      role: "Agent",
      content: "Yes. Please send your company name, address, and VAT ID.",
      from: "BuyAlan",
      to: "+49 1512 3456789",
      hoursAgo: 3,
      minutesAgo: 7,
      isRead: true,
    }),
  ],
  "6": [
    createMessageMock({
      messageId: "m21",
      role: "Customer",
      content: "Need to change delivery address for order #4902.",
      from: "+33 6 12 34 56 78",
      to: "BuyAlan",
      daysAgo: 3,
      hoursAgo: 2,
      isRead: false,
    }),
    createMessageMock({
      messageId: "m22",
      role: "Operator",
      content: "Please send the new address. If the label already exists, we may need a reroute.",
      from: "Emma from support",
      to: "+33 6 12 34 56 78",
      daysAgo: 3,
      hoursAgo: 1,
      isRead: true,
    }),
  ],
  "8": [
    createMessageMock({
      messageId: "m25",
      role: "Customer",
      content: "Order #4921, can you confirm I selected linear switches?",
      from: "@kamil_w",
      to: "BuyAlan",
      daysAgo: 4,
      hoursAgo: 3,
      isRead: false,
    }),
    createMessageMock({
      messageId: "m26",
      role: "Agent",
      content: "Checking now. You selected tactile. Want me to change it before fulfillment?",
      from: "BuyAlan",
      to: "@kamil_w",
      daysAgo: 4,
      hoursAgo: 2,
      isRead: true,
    }),
  ],
  "11": [
    createMessageMock({
      messageId: "m29",
      role: "Customer",
      content: "Tracking only shows label created. Any update?",
      from: "@noah_sre",
      to: "BuyAlan",
      daysAgo: 5,
      hoursAgo: 13,
      isRead: false,
    }),
    createMessageMock({
      messageId: "m30",
      role: "Operator",
      content: "That usually means the carrier has not scanned it yet. I will check with the warehouse.",
      from: "Sarah from ops",
      to: "@noah_sre",
      daysAgo: 5,
      hoursAgo: 12,
      isRead: true,
    }),
  ],
  "12": [
    createMessageMock({
      messageId: "m33",
      role: "Customer",
      content: "Please cancel my order #4899.",
      from: "+61 412 345 678",
      to: "BuyAlan",
      daysAgo: 5,
      hoursAgo: 15,
      isRead: false,
    }),
    createMessageMock({
      messageId: "m34",
      role: "Agent",
      content: "If it has not shipped yet, we can cancel it immediately.",
      from: "BuyAlan",
      to: "+61 412 345 678",
      daysAgo: 5,
      hoursAgo: 14,
      isRead: true,
    }),
  ],
  "16": [
    createMessageMock({
      messageId: "m37",
      role: "Customer",
      content: "My package arrived damaged. The box is torn.",
      from: "+34 612 345 678",
      to: "BuyAlan",
      daysAgo: 7,
      hoursAgo: 7,
      isRead: false,
    }),
    createMessageMock({
      messageId: "m38",
      role: "Agent",
      content: "Please send photos of the box and the keyboard so we can file a claim.",
      from: "BuyAlan",
      to: "+34 612 345 678",
      daysAgo: 7,
      hoursAgo: 6,
      isRead: true,
    }),
  ],
  "17": [
    createMessageMock({
      messageId: "m41",
      role: "Customer",
      content: "Key A sometimes double registers. Any fix?",
      from: "@tom_qc",
      to: "BuyAlan",
      daysAgo: 8,
      hoursAgo: 2,
      isRead: false,
    }),
    createMessageMock({
      messageId: "m42",
      role: "Agent",
      content: "If it is hot-swap, try reseating the switch and testing another cable.",
      from: "BuyAlan",
      to: "@tom_qc",
      daysAgo: 8,
      hoursAgo: 1,
      isRead: true,
    }),
  ],
  "23": [
    createMessageMock({
      messageId: "m45",
      role: "Customer",
      content: "Need help pairing via Bluetooth.",
      from: "+52 55 1234 5678",
      to: "BuyAlan",
      daysAgo: 12,
      hoursAgo: 4,
      isRead: false,
    }),
    createMessageMock({
      messageId: "m46",
      role: "Agent",
      content: "Hold Fn + 1 for 3 seconds until the LED blinks, then pair KB Pro in Bluetooth settings.",
      from: "BuyAlan",
      to: "+52 55 1234 5678",
      daysAgo: 12,
      hoursAgo: 3,
      isRead: true,
    }),
  ],
}

 export const chatInfo : ChatInfo[] = [
    {
      conversationId: "1",
      customer: {
        name: "Unknown User",
        contact: "+1 555-0101",
        lifetimeValue: 165.5,
        tags: ["Returning", "Tech"],
      },
      cartItems: [
        {
          id: "c1",
          name: "Mech Keyboard Pro",
          variant: "Tactile / Black",
          price: 149.99,
          quantity: 1,
        },
      ],
      manualActions: [
        {
          id: "a1",
          label: "Trigger Upsell",
          action: "upsell",
        },
        {
          id: "a2",
          label: "Send Payment Link",
          action: "payment-link",
        },
        {
          id: "a3",
          label: "Schedule Shipping",
          action: "shipping",
        },
      ],
      pastOrders: [
        {
          id: "ORD-882",
          dateLabel: "Oct 12, 2025",
          total: 45,
          status: "Delivered",
        },
        {
          id: "ORD-751",
          dateLabel: "Aug 04, 2025",
          total: 120.5,
          status: "Delivered",
        },
      ],
    },
    {
      conversationId: "2",
      customer: {
        name: "Alex Dev",
        contact: "@alex_dev",
        lifetimeValue: 320.75,
        tags: ["VIP", "Telegram"],
      },
      cartItems: [
        {
          id: "c2",
          name: "Wireless Mouse",
          variant: "Graphite",
          price: 79,
          quantity: 1,
        },
      ],
      manualActions: [
        {
          id: "a4",
          label: "Trigger Upsell",
          action: "upsell",
        },
        {
          id: "a5",
          label: "Send Payment Link",
          action: "payment-link",
        },
        {
          id: "a6",
          label: "Schedule Shipping",
          action: "shipping",
        },
      ],
      pastOrders: [
        {
          id: "ORD-912",
          dateLabel: "Jan 03, 2026",
          total: 99.99,
          status: "Delivered",
        },
      ],
    },
    {
      conversationId: "3",
      customer: {
        name: "UK Customer",
        contact: "+44 7700 900077",
        lifetimeValue: 58.2,
        tags: ["SMS", "First-Time"],
      },
      cartItems: [],
      manualActions: [
        {
          id: "a7",
          label: "Trigger Upsell",
          action: "upsell",
        },
        {
          id: "a8",
          label: "Send Payment Link",
          action: "payment-link",
        },
        {
          id: "a9",
          label: "Schedule Shipping",
          action: "shipping",
        },
      ],
      pastOrders: [
        {
          id: "ORD-644",
          dateLabel: "Jul 30, 2025",
          total: 58.2,
          status: "Delivered",
        },
      ],
    },
  ];
