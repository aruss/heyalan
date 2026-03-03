interface AssignedPerson {
  name: string
  initials: string
}

interface Project {
  company: string
  size: string
  probability: string
  duration: string
  status: "Drafted" | "Sent" | "Closed"
  assigned: AssignedPerson[]
}

interface Region {
  region: string
  project: Project[]
}

export const quotes: Region[] = [
  {
    region: "Europe",
    project: [
      {
        company: "Walton Holding",
        size: "50K USD",
        probability: "40%",
        duration: "18 months",
        status: "Drafted",
        assigned: [
          {
            name: "Emily Smith",
            initials: "E",
          },
          {
            name: "Max Warmer",
            initials: "M",
          },
          {
            name: "Victoria Steep",
            initials: "V",
          },
        ],
      },
      {
        company: "Zurich Coats LLC",
        size: "100-150K USD",
        probability: "80%",
        duration: "24 months",
        status: "Sent",
        assigned: [
          {
            name: "Emma Stone",
            initials: "E",
          },
          {
            name: "Chris Bold",
            initials: "C",
          },
        ],
      },
      {
        company: "Riverflow Media Group",
        size: "280-300K USD",
        probability: "80%",
        duration: "24 months",
        status: "Sent",
        assigned: [
          {
            name: "Emma Stephcorn",
            initials: "E",
          },
          {
            name: "Chris Bold",
            initials: "C",
          },
        ],
      },
      {
        company: "Nordic Solutions AG",
        size: "175K USD",
        probability: "60%",
        duration: "12 months",
        status: "Drafted",
        assigned: [
          {
            name: "Victoria Stone",
            initials: "V",
          },
          {
            name: "Max W.",
            initials: "M",
          },
        ],
      },
      {
        company: "Swiss Tech Innovations",
        size: "450K USD",
        probability: "90%",
        duration: "36 months",
        status: "Sent",
        assigned: [
          {
            name: "Emily Satally",
            initials: "E",
          },
          {
            name: "Chris Bold",
            initials: "C",
          },
        ],
      },
      {
        company: "Berlin Digital Hub",
        size: "200K USD",
        probability: "70%",
        duration: "15 months",
        status: "Drafted",
        assigned: [
          {
            name: "Emma Stone",
            initials: "E",
          },
        ],
      },
    ],
  },
  {
    region: "Asia",
    project: [
      {
        company: "Real Estate Group",
        size: "1.2M USD",
        probability: "100%",
        duration: "6 months",
        status: "Closed",
        assigned: [
          {
            name: "Lena Mayer",
            initials: "L",
          },
          {
            name: "Sara Brick",
            initials: "S",
          },
        ],
      },
      {
        company: "Grison Appartments",
        size: "100K USD",
        probability: "20%",
        duration: "12 months",
        status: "Drafted",
        assigned: [
          {
            name: "Jordan Afolter",
            initials: "J",
          },
          {
            name: "Corinna Bridge",
            initials: "C",
          },
        ],
      },
      {
        company: "Tokyo Tech Solutions",
        size: "750K USD",
        probability: "85%",
        duration: "24 months",
        status: "Sent",
        assigned: [
          {
            name: "Lena Mayer",
            initials: "L",
          },
          {
            name: "Jordan Corner",
            initials: "J",
          },
        ],
      },
      {
        company: "Singapore Systems Ltd",
        size: "300K USD",
        probability: "75%",
        duration: "18 months",
        status: "Drafted",
        assigned: [
          {
            name: "Sara Bridge",
            initials: "S",
          },
        ],
      },
      {
        company: "Seoul Digital Corp",
        size: "880K USD",
        probability: "95%",
        duration: "30 months",
        status: "Sent",
        assigned: [
          {
            name: "Corinna Berner",
            initials: "C",
          },
          {
            name: "Lena Mayer",
            initials: "L",
          },
        ],
      },
      {
        company: "Mumbai Innovations",
        size: "450K USD",
        probability: "40%",
        duration: "12 months",
        status: "Drafted",
        assigned: [
          {
            name: "Jordan Afolter",
            initials: "J",
          },
        ],
      },
    ],
  },
  {
    region: "North America",
    project: [
      {
        company: "Liquid Holdings Group",
        size: "5.1M USD",
        probability: "100%",
        duration: "Member",
        status: "Closed",
        assigned: [
          {
            name: "Charlie Anuk",
            initials: "C",
          },
        ],
      },
      {
        company: "Craft Labs, Inc.",
        size: "80-90K USD",
        probability: "80%",
        duration: "18 months",
        status: "Sent",
        assigned: [
          {
            name: "Charlie Anuk",
            initials: "C",
          },
          {
            name: "Patrick Daller",
            initials: "P",
          },
        ],
      },
      {
        company: "Toronto Tech Hub",
        size: "250K USD",
        probability: "65%",
        duration: "12 months",
        status: "Drafted",
        assigned: [
          {
            name: "Patrick Daller",
            initials: "P",
          },
          {
            name: "Charlie Anuk",
            initials: "C",
          },
        ],
      },
      {
        company: "Silicon Valley Startups",
        size: "1.5M USD",
        probability: "90%",
        duration: "24 months",
        status: "Sent",
        assigned: [
          {
            name: "Charlie Anuk",
            initials: "C",
          },
        ],
      },
      {
        company: "NYC Digital Solutions",
        size: "750K USD",
        probability: "70%",
        duration: "15 months",
        status: "Drafted",
        assigned: [
          {
            name: "Patrick Daller",
            initials: "P",
          },
        ],
      },
    ],
  },
]

interface DataChart {
  date: string
  "Current year": number
  "Same period last year": number
}

interface DataChart2 {
  date: string
  Quotes: number
  "Total deal size": number
}

interface DataChart3 {
  date: string
  Addressed: number
  Unrealized: number
}

interface DataChart4 {
  date: string
  Density: number
}

export const dataChart: DataChart[] = [
  {
    date: "Jan 24",
    "Current year": 23,
    "Same period last year": 67,
  },
  {
    date: "Feb 24",
    "Current year": 31,
    "Same period last year": 23,
  },
  {
    date: "Mar 24",
    "Current year": 46,
    "Same period last year": 78,
  },
  {
    date: "Apr 24",
    "Current year": 46,
    "Same period last year": 23,
  },
  {
    date: "May 24",
    "Current year": 39,
    "Same period last year": 32,
  },
  {
    date: "Jun 24",
    "Current year": 65,
    "Same period last year": 32,
  },
]

export const dataChart2: DataChart2[] = [
  {
    date: "Jan 24",
    Quotes: 120,
    "Total deal size": 55000,
  },
  {
    date: "Feb 24",
    Quotes: 183,
    "Total deal size": 75400,
  },
  {
    date: "Mar 24",
    Quotes: 165,
    "Total deal size": 50450,
  },
  {
    date: "Apr 24",
    Quotes: 99,
    "Total deal size": 41540,
  },
  {
    date: "May 24",
    Quotes: 194,
    "Total deal size": 63850,
  },
  {
    date: "Jun 24",
    Quotes: 241,
    "Total deal size": 73850,
  },
]

export const dataChart3: DataChart3[] = [
  {
    date: "Jan 24",
    Addressed: 8,
    Unrealized: 12,
  },
  {
    date: "Feb 24",
    Addressed: 9,
    Unrealized: 12,
  },
  {
    date: "Mar 24",
    Addressed: 6,
    Unrealized: 12,
  },
  {
    date: "Apr 24",
    Addressed: 5,
    Unrealized: 12,
  },
  {
    date: "May 24",
    Addressed: 12,
    Unrealized: 12,
  },
  {
    date: "Jun 24",
    Addressed: 9,
    Unrealized: 12,
  },
]

export const dataChart4: DataChart4[] = [
  {
    date: "Jan 24",
    Density: 0.891,
  },
  {
    date: "Feb 24",
    Density: 0.084,
  },
  {
    date: "Mar 24",
    Density: 0.155,
  },
  {
    date: "Apr 24",
    Density: 0.75,
  },
  {
    date: "May 24",
    Density: 0.221,
  },
  {
    date: "Jun 24",
    Density: 0.561,
  },
]

interface Progress {
  current: number
  total: number
}

interface AuditDate {
  date: string
  auditor: string
}

interface Document {
  name: string
  status: "OK" | "Needs update" | "In audit"
}

interface Section {
  id: string
  title: string
  certified: string
  progress: Progress
  status: "complete" | "warning"
  auditDates: AuditDate[]
  documents: Document[]
}

export const sections: Section[] = [
  {
    id: "item-1",
    title: "CompTIA Security+",
    certified: "ISO",
    progress: { current: 46, total: 46 },
    status: "complete",
    auditDates: [
      { date: "Dec 10, 2023", auditor: "Max Duster" },
      { date: "Dec 12, 2023", auditor: "Emma Stone" },
    ],
    documents: [
      { name: "policy_overview.xlsx", status: "OK" },
      { name: "employee_guidelines.xlsx", status: "Needs update" },
      { name: "compliance_checklist.xlsx", status: "In audit" },
    ],
  },
  {
    id: "item-2",
    title: "SAFe Certifications",
    certified: "IEC 2701",
    progress: { current: 32, total: 41 },
    status: "warning",
    auditDates: [
      { date: "Jan 15, 2024", auditor: "Sarah Johnson" },
      { date: "Jan 20, 2024", auditor: "Mike Peters" },
    ],
    documents: [
      { name: "certification_records.xlsx", status: "OK" },
      { name: "training_logs.xlsx", status: "In audit" },
      { name: "assessment_results.xlsx", status: "Needs update" },
    ],
  },
  {
    id: "item-3",
    title: "PMP Certifications",
    certified: "ISO",
    progress: { current: 21, total: 21 },
    status: "complete",
    auditDates: [
      { date: "Feb 5, 2024", auditor: "Lisa Chen" },
      { date: "Feb 8, 2024", auditor: "Tom Wilson" },
    ],
    documents: [
      { name: "project_documents.xlsx", status: "OK" },
      { name: "methodology_guide.xlsx", status: "OK" },
      { name: "best_practices.xlsx", status: "In audit" },
    ],
  },
  {
    id: "item-4",
    title: "Cloud Certifications",
    certified: "SOC 2",
    progress: { current: 21, total: 21 },
    status: "complete",
    auditDates: [
      { date: "Mar 1, 2024", auditor: "Alex Kumar" },
      { date: "Mar 5, 2024", auditor: "Rachel Green" },
    ],
    documents: [
      { name: "aws_certifications.xlsx", status: "OK" },
      { name: "azure_competencies.xlsx", status: "OK" },
      { name: "gcp_credentials.xlsx", status: "In audit" },
      { name: "cloud_security.xlsx", status: "OK" },
    ],
  },
]

export type ConversationChannel = "WhatsApp" | "Telegram" | "SMS"

export type MessageSender = "user" | "agent"

export interface ConversationItem {
  id: string;
  name: string;        // phone number or @handle
  channel: ConversationChannel;
  lastMsg: string;
  time: string;        // "10:42 AM", "Yesterday", "Mon", etc.
  unread: boolean;
}

export interface MessageItem {
  id: string;
  convoId: string;
  sender: MessageSender;
  text: string;
  time: string;  
}

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



/*

export interface Conversation {
  id: string;
  name: string;        // phone number or @handle
  channel: Channel;
  lastMsg: string;
  time: string;        // "10:42 AM", "Yesterday", "Mon", etc.
  unread: boolean;
}

export interface Message {
  id: string;
  convoId: string;
  sender: Sender;
  text: string;
  time: string;        // same formatting as Conversation.time
}
  */

export const conversations : ConversationItem[] = [
  { id: '1',  name: '+1 555-0101',       channel: 'WhatsApp', lastMsg: 'Is the Pro version in stock?',                     time: '10:42 AM',  unread: true  },
  { id: '2',  name: '@alex_dev',         channel: 'Telegram', lastMsg: 'Payment completed.',                                time: '09:15 AM',  unread: false },
  { id: '3',  name: '+44 7700 900077',   channel: 'SMS',      lastMsg: 'When will it ship?',                                time: 'Yesterday', unread: false },

  { id: '4',  name: '+49 1512 3456789',  channel: 'WhatsApp', lastMsg: 'Can you invoice with VAT ID?',                      time: '08:03 AM',  unread: true  },
  { id: '5',  name: '@maria_pm',         channel: 'Telegram', lastMsg: 'Do you have the white keycaps set?',                time: 'Mon',       unread: false },
  { id: '6',  name: '+33 6 12 34 56 78', channel: 'SMS',      lastMsg: 'Need to change delivery address.',                  time: 'Sun',       unread: true  },
  { id: '7',  name: '+1 555-0199',       channel: 'WhatsApp', lastMsg: 'Any discount for 2 units?',                         time: 'Sat',       unread: false },
  { id: '8',  name: '@kamil_w',          channel: 'Telegram', lastMsg: 'Order #4921: please confirm switch type.',          time: 'Sat',       unread: true  },
  { id: '9',  name: '+48 600 700 800',   channel: 'SMS',      lastMsg: 'Can I pick up in store?',                           time: 'Fri',       unread: false },
  { id: '10', name: '+81 90 1234 5678',  channel: 'WhatsApp', lastMsg: 'Does it support Mac layout?',                       time: 'Fri',       unread: false },
  { id: '11', name: '@noah_sre',         channel: 'Telegram', lastMsg: 'Tracking link says “label created” only.',          time: 'Thu',       unread: true  },
  { id: '12', name: '+61 412 345 678',   channel: 'SMS',      lastMsg: 'Please cancel my order.',                           time: 'Thu',       unread: true  },
  { id: '13', name: '+1 555-0133',       channel: 'WhatsApp', lastMsg: 'Can you recommend a quieter switch?',               time: 'Wed',       unread: false },
  { id: '14', name: '@lina_design',      channel: 'Telegram', lastMsg: 'Need 10 keyboards for the team—bulk pricing?',      time: 'Wed',       unread: true  },
  { id: '15', name: '+39 320 123 4567',  channel: 'SMS',      lastMsg: 'Do you ship to Italy?',                             time: 'Tue',       unread: false },
  { id: '16', name: '+34 612 345 678',   channel: 'WhatsApp', lastMsg: 'My package arrived damaged.',                       time: 'Tue',       unread: true  },
  { id: '17', name: '@tom_qc',           channel: 'Telegram', lastMsg: 'Key “A” sometimes double registers.',               time: 'Tue',       unread: true  },
  { id: '18', name: '+1 555-0177',       channel: 'SMS',      lastMsg: 'Is there a wrist rest included?',                   time: 'Mon',       unread: false },
  { id: '19', name: '+46 70 123 45 67',  channel: 'WhatsApp', lastMsg: 'Can I get ISO-Nordic layout?',                      time: 'Mon',       unread: false },
  { id: '20', name: '@jen_ops',          channel: 'Telegram', lastMsg: 'Please resend confirmation email.',                 time: 'Mon',       unread: false },

  { id: '21', name: '+1 555-0148',       channel: 'WhatsApp', lastMsg: 'Do you have the aluminum case in black?',           time: 'Last week', unread: false },
  { id: '22', name: '@viktor_ml',        channel: 'Telegram', lastMsg: 'Can you split shipment into 2 addresses?',          time: 'Last week', unread: true  },
  { id: '23', name: '+52 55 1234 5678',  channel: 'SMS',      lastMsg: 'Need help pairing via Bluetooth.',                  time: 'Last week', unread: true  },
  { id: '24', name: '+1 555-0112',       channel: 'WhatsApp', lastMsg: 'What’s your return policy?',                        time: 'Last week', unread: false },
  { id: '25', name: '@sasha_fin',        channel: 'Telegram', lastMsg: 'Can you provide a proforma invoice?',               time: '2 weeks ago', unread: false },
  { id: '26', name: '+7 916 123-45-67',  channel: 'SMS',      lastMsg: 'Do you sell replacement stabilizers?',             time: '2 weeks ago', unread: false },
  { id: '27', name: '+1 555-0166',       channel: 'WhatsApp', lastMsg: 'My promo code is not working.',                     time: '2 weeks ago', unread: true  },
  { id: '28', name: '@hanna_hr',         channel: 'Telegram', lastMsg: 'Need 5 units by Friday—possible?',                  time: '2 weeks ago', unread: true  },
  { id: '29', name: '+1 555-0181',       channel: 'SMS',      lastMsg: 'Can I change switches after ordering?',             time: '3 weeks ago', unread: false },
  { id: '30', name: '+86 138 0013 8000', channel: 'WhatsApp', lastMsg: 'Does it have hot-swap sockets?',                    time: '3 weeks ago', unread: false },
];

export const messages: MessageItem[] = [
  // Conversation 1 (keyboard purchase)
  { id: 'm1',  convoId: '1', sender: 'user',  text: 'Hi, I want to order the mechanical keyboard.',                                                        time: '10:40 AM' },
  { id: 'm2',  convoId: '1', sender: 'agent', text: 'Hello! I can help with that. Are you looking for the tactile or linear switches?',                     time: '10:40 AM' },
  { id: 'm3',  convoId: '1', sender: 'user',  text: 'Tactile, please. Is the Pro version in stock?',                                                       time: '10:42 AM' },
  { id: 'm4',  convoId: '1', sender: 'agent', text: 'Yes—Pro is in stock in black and silver. Which case color do you prefer?',                              time: '10:43 AM' },
  { id: 'm5',  convoId: '1', sender: 'user',  text: 'Black case. Also, do you have ISO-DE layout?',                                                        time: '10:44 AM' },
  { id: 'm6',  convoId: '1', sender: 'agent', text: 'ISO-DE is available. Want PBT keycaps or the standard ABS set?',                                       time: '10:45 AM' },
  { id: 'm7',  convoId: '1', sender: 'user',  text: 'PBT please. Can you add a wrist rest too?',                                                            time: '10:46 AM' },
  { id: 'm8',  convoId: '1', sender: 'agent', text: 'Absolutely. I’ll add the matching wrist rest. Ready to check out?',                                    time: '10:47 AM' },

  // Conversation 2 (payment confirmation)
  { id: 'm9',  convoId: '2', sender: 'user',  text: 'Hey, just paid the invoice for order #4920.',                                                          time: '09:14 AM' },
  { id: 'm10', convoId: '2', sender: 'agent', text: 'Thanks! I see the payment—your order is confirmed.',                                                    time: '09:15 AM' },
  { id: 'm11', convoId: '2', sender: 'user',  text: 'Great. Can you send the tracking when it ships?',                                                      time: '09:16 AM' },
  { id: 'm12', convoId: '2', sender: 'agent', text: 'Will do. You’ll get an automated message as soon as the label is created.',                             time: '09:17 AM' },

  // Conversation 3 (shipping ETA)
  { id: 'm13', convoId: '3', sender: 'user',  text: 'Hi—order placed yesterday. When will it ship?',                                                        time: 'Yesterday' },
  { id: 'm14', convoId: '3', sender: 'agent', text: 'Orders placed before 2 PM ship same day; otherwise next business day. What’s your order number?',       time: 'Yesterday' },
  { id: 'm15', convoId: '3', sender: 'user',  text: 'Order #4917.',                                                                                         time: 'Yesterday' },
  { id: 'm16', convoId: '3', sender: 'agent', text: 'Thanks—#4917 is queued for dispatch today. You’ll get tracking later this afternoon.',                  time: 'Yesterday' },

  // Conversation 4 (VAT invoice)
  { id: 'm17', convoId: '4', sender: 'user',  text: 'Can you issue an invoice with VAT ID for our company?',                                                time: '08:01 AM' },
  { id: 'm18', convoId: '4', sender: 'agent', text: 'Yes. Please send your company name, address, and VAT ID.',                                             time: '08:02 AM' },
  { id: 'm19', convoId: '4', sender: 'user',  text: 'ACME GmbH, Musterstr. 1, 10115 Berlin, DE123456789.',                                                  time: '08:03 AM' },
  { id: 'm20', convoId: '4', sender: 'agent', text: "Perfect—I'll generate the VAT invoice and attach it to the order confirmation.",                       time: '08:04 AM' },

  // Conversation 6 (address change)
  { id: 'm21', convoId: '6', sender: 'user',  text: 'Need to change delivery address for order #4902.',                                                      time: 'Sun' },
  { id: 'm22', convoId: '6', sender: 'agent', text: 'Sure—please send the new address. If the label is already created, we may need a carrier reroute.',    time: 'Sun' },
  { id: 'm23', convoId: '6', sender: 'user',  text: 'New address: 12 Rue de Rivoli, 75001 Paris.',                                                          time: 'Sun' },
  { id: 'm24', convoId: '6', sender: 'agent', text: 'Got it. I’ll update it now and confirm once the system accepts the change.',                           time: 'Sun' },

  // Conversation 8 (switch type confirmation)
  { id: 'm25', convoId: '8', sender: 'user',  text: 'Order #4921—can you confirm I selected linear switches?',                                               time: 'Sat' },
  { id: 'm26', convoId: '8', sender: 'agent', text: 'Checking… you selected tactile. Want me to change it to linear before fulfillment?',                   time: 'Sat' },
  { id: 'm27', convoId: '8', sender: 'user',  text: 'Yes, please change to linear.',                                                                         time: 'Sat' },
  { id: 'm28', convoId: '8', sender: 'agent', text: 'Done. Updated to linear switches. Everything else stays the same.',                                     time: 'Sat' },

  // Conversation 11 (tracking stuck)
  { id: 'm29', convoId: '11', sender: 'user',  text: 'Tracking only shows “label created”. Any update?',                                                     time: 'Thu' },
  { id: 'm30', convoId: '11', sender: 'agent', text: 'That usually means the carrier hasn’t scanned it yet. Can you share the tracking number?',            time: 'Thu' },
  { id: 'm31', convoId: '11', sender: 'user',  text: '1Z999AA10123456784',                                                                                    time: 'Thu' },
  { id: 'm32', convoId: '11', sender: 'agent', text: 'Thanks. I’ll ping the warehouse—if it missed pickup, we’ll get it scanned today.',                    time: 'Thu' },

  // Conversation 12 (cancellation)
  { id: 'm33', convoId: '12', sender: 'user',  text: 'Please cancel my order #4899.',                                                                         time: 'Thu' },
  { id: 'm34', convoId: '12', sender: 'agent', text: 'I can help. Has it shipped yet? If not, we can cancel immediately.',                                  time: 'Thu' },
  { id: 'm35', convoId: '12', sender: 'user',  text: 'No tracking yet.',                                                                                      time: 'Thu' },
  { id: 'm36', convoId: '12', sender: 'agent', text: 'Great—cancellation requested. You’ll receive a confirmation and refund notice shortly.',              time: 'Thu' },

  // Conversation 16 (damaged package)
  { id: 'm37', convoId: '16', sender: 'user',  text: 'My package arrived damaged. The box is torn.',                                                          time: 'Tue' },
  { id: 'm38', convoId: '16', sender: 'agent', text: 'Sorry about that. Can you send photos of the box + the keyboard so we can file a claim?',            time: 'Tue' },
  { id: 'm39', convoId: '16', sender: 'user',  text: 'Sure, sending now.',                                                                                    time: 'Tue' },
  { id: 'm40', convoId: '16', sender: 'agent', text: 'Thanks—once received, we’ll offer replacement or refund, whichever you prefer.',                      time: 'Tue' },

  // Conversation 17 (double key)
  { id: 'm41', convoId: '17', sender: 'user',  text: 'Key “A” sometimes double registers. Any fix?',                                                         time: 'Tue' },
  { id: 'm42', convoId: '17', sender: 'agent', text: 'If it’s hot-swap, try reseating the switch. Also test with another USB port/cable.',                 time: 'Tue' },
  { id: 'm43', convoId: '17', sender: 'user',  text: 'It is hot-swap. I’ll reseat it.',                                                                       time: 'Tue' },
  { id: 'm44', convoId: '17', sender: 'agent', text: 'If it persists, we can send a replacement switch or start an RMA.',                                   time: 'Tue' },

  // Conversation 23 (Bluetooth pairing help)
  { id: 'm45', convoId: '23', sender: 'user',  text: 'Need help pairing via Bluetooth.',                                                                      time: 'Last week' },
  { id: 'm46', convoId: '23', sender: 'agent', text: 'Sure—hold Fn + 1 for 3 seconds until the LED blinks, then pair “KB Pro” in your Bluetooth settings.', time: 'Last week' },
  { id: 'm47', convoId: '23', sender: 'user',  text: 'Got it. Does it support switching devices?',                                                           time: 'Last week' },
  { id: 'm48', convoId: '23', sender: 'agent', text: 'Yes—Fn + 1/2/3 to switch between the three saved devices.',                                            time: 'Last week' },
];

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