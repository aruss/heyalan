# System Architecture Requirements: Omnichannel Messaging Platform

## 1. Core Technology Stack (Open-Source / Self-Hosted)
* **Message Broker:** RabbitMQ (AMQP 0-9-1).
* **Primary Datastore:** PostgreSQL.
* **State & Cache:** Redis.

## 2. Inbound Topology (Ingress)
* **Channel Gateways:** Dedicated HTTP workers handling incoming webhooks from external channels (Telegram, WhatsApp, Signal, Webchat, etc.).
* **Multi-Tenancy Webhooks:** Endpoints must map to `/api/v1/webhooks/{tenant_id}/{channel}`.
* **Normalization:** Gateways transform proprietary channel payloads into a standardized internal JSON schema. `tenant_id` and channel metadata must be appended.
* **Ingest Queue:** Normalized payloads are published to a unified `inbound_events` queue.

## 3. Core Processing (Central Business Logic)
* **Identity Resolution:** Map disparate channel identifiers (e.g., Telegram chat ID, WhatsApp phone number) to a global `customer_id`.
* **Idempotency & Ordering:** Deduplicate inbound webhook retries using Redis idempotency keys. Guarantee sequential processing per `customer_id`.
* **State Management:** Track active conversation context, channel preferences, and bot-to-human (merchant) handoff states in Redis/PostgreSQL.

## 4. Outbound Topology (Egress)
* **Broker Exchange:** Central logic publishes all outbound messages to a single AMQP Topic Exchange (e.g., `outbound_exchange`).
* **Routing Keys:** Append multi-tenant and channel routing variables formatted as: `egress.{tenant_id}.{channel}`.
* **Dedicated Queues:** AMQP bindings route messages into channel-specific queues.
* **Isolated Egress Workers:** Channel-specific workers consume exclusively from their dedicated queues. Workers handle payload denormalization, external API rate limits, and transmission. Default shared workers bind via wildcards (`egress.*.{channel}`).

## 5. Multi-Tenancy Implementation
* **Database Isolation:** Enforce PostgreSQL Row-Level Security (RLS) on all tenant-specific tables. Core logic must execute `SET LOCAL app.current_tenant_id = '{tenant_id}'` per transaction block.
* **State Namespace:** Redis keys must enforce strict tenant prefixes: `state:{tenant_id}:{resource}:{id}`.
* **Worker Isolation (High SLA):** Capable of spinning up dedicated egress workers for enterprise tenants by binding specifically to `egress.{specific_tenant_id}.#`.

## 6. Admin Dashboard Gateway
* **Protocol:** WebSockets.
* **Integration:** WebSocket server acts as a client to the message bus, subscribing to a fanout or topic exchange to stream bi-directional, real-time conversation updates to the merchant UI.