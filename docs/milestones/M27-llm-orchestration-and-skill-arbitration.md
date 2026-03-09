# Milestone M27: LLM Orchestration and Skill Arbitration

## Summary
Build the runtime agent orchestration layer that decides how HeyAlan reacts to customer messages.

This milestone introduces the internal LLM-driven control loop that reads conversation history, structured conversation state, customer history, agent configuration, and enabled skills, then chooses whether to answer directly, ask follow-up questions, call a tool, update conversation state, or request human handoff.

This milestone owns runtime decision-making. It does not own provider-specific write logic, checkout persistence semantics, or order status synchronization beyond invoking the correct downstream services.

## Dependencies and Preconditions
- [ ] M23 catalog cache and agent product assignment behavior are available.
- [ ] M24 agent skill enablement and credential readiness are available.
- [ ] M25 skill execution contracts and safety policies are available.
- [ ] M26 structured conversation state and customer identity resolution are available.

## User Decisions (Locked)
- [x] The LLM runtime is the authority for deciding whether to answer, ask, use a skill, or hand off.
- [x] The LLM must operate through approved skills for external side effects.
- [x] Structured conversation state is part of the runtime context and may be updated after each turn.
- [x] Invalid or unsafe model output must fall back to deterministic non-destructive behavior.
- [x] Human handoff is a first-class runtime outcome.
- [x] Prompt strategy and response generation are internal implementation details; public chat endpoints remain unchanged in this milestone.

## Public API and Contract Changes
- [ ] Add internal `IAgentRuntimeOrchestrator`:
  - [ ] accept inbound message event + runtime context,
  - [ ] return orchestrated outcome (`reply`, `ask_follow_up`, `skill_call`, `handoff`, `no_reply`),
  - [ ] persist safe state updates.
- [ ] Add internal `IAgentContextBuilder`:
  - [ ] gather conversation transcript summary,
  - [ ] gather conversation state snapshot,
  - [ ] gather customer history snapshot,
  - [ ] gather enabled and ready skills,
  - [ ] gather agent profile/personality/channel readiness.
- [ ] Add internal `IAgentReplyPolicy`:
  - [ ] validate model output shape,
  - [ ] reject forbidden operations,
  - [ ] choose fallback action on invalid output.
- [ ] Define internal runtime contracts:
  - [ ] `AgentRuntimeContext`
  - [ ] `AgentRuntimeDecision`
  - [ ] `AgentRuntimeStatePatch`
  - [ ] `AgentRuntimeHandoffRequest`
- [ ] Keep existing channel-facing transport contracts unchanged.

## Authoritative Runtime Responsibilities
- [ ] The orchestrator must decide one of:
  - [ ] answer product/company question directly,
  - [ ] ask customer for missing order information,
  - [ ] invoke a read-only skill,
  - [ ] invoke a state-changing skill through approved policy path,
  - [ ] mark conversation for human handoff,
  - [ ] suppress reply when ownership or policy forbids response.
- [ ] The orchestrator must never:
  - [ ] call external providers directly,
  - [ ] bypass M25 confirmation/idempotency rules,
  - [ ] mutate state when conversation is human-owned unless the caller is an authorized operator path.

## Gate A - Runtime Contracts and Context Assembly
- [ ] Define `AgentRuntimeContext` shape and context builder pipeline.
- [ ] Include in runtime context:
  - [ ] recent message window,
  - [ ] conversation state snapshot,
  - [ ] customer history summary,
  - [ ] enabled skill descriptors,
  - [ ] catalog/product constraints,
  - [ ] agent personality/system instructions.
- [ ] Add redaction and truncation rules so context remains bounded and secret-safe.
- [ ] Define `AgentRuntimeDecision` machine-readable result schema.

### Gate A Acceptance Criteria
- [ ] Runtime context is deterministic, bounded, and secret-safe.
- [ ] Decision outputs are machine-readable and suitable for downstream execution.

## Gate B - Orchestrator Core and Decision Validation
- [ ] Implement `IAgentRuntimeOrchestrator`.
- [ ] Add model invocation boundary separated from prompt assembly and output validation.
- [ ] Implement decision validation rules:
  - [ ] allowed action names only,
  - [ ] required fields by action type,
  - [ ] state patch schema validation,
  - [ ] no direct provider operation outside skill calls.
- [ ] Implement deterministic fallback policy:
  - [ ] invalid output -> safe clarification reply or no-op,
  - [ ] unavailable dependency -> explain limitation or ask for alternative path,
  - [ ] policy violation -> block action and mark for review where appropriate.

### Gate B Acceptance Criteria
- [ ] Unsafe or malformed model outputs do not produce side effects.
- [ ] Orchestrator can reliably choose and validate one runtime action per inbound turn.

## Gate C - Skill Arbitration and State Mutation
- [ ] Wire orchestrator to enabled skill descriptors from M24 and execution boundary from M25.
- [ ] Support read-only skill execution for product lookup and validation steps.
- [ ] Support state-changing skill execution only through M25 policy requirements.
- [ ] Apply allowed state patches back through `IConversationStateService`.
- [ ] Persist outcome metadata needed for future troubleshooting:
  - [ ] selected action,
  - [ ] invoked skill key if any,
  - [ ] fallback reason if any,
  - [ ] whether human attention was requested.

### Gate C Acceptance Criteria
- [ ] Tool selection is driven by skill readiness and policy metadata.
- [ ] Successful runtime decisions can update conversation state without bypassing the state service.

## Gate D - Incoming Message Integration
- [ ] Integrate orchestrator into incoming message processing after message persistence.
- [ ] Enforce ownership rule:
  - [ ] agent-owned conversation may auto-respond,
  - [ ] human-owned conversation must not auto-respond.
- [ ] Route successful reply outcomes into existing outbound channel pipeline.
- [ ] Preserve idempotent handling for retried inbound message processing.

### Gate D Acceptance Criteria
- [ ] New inbound messages can trigger the runtime loop end to end.
- [ ] Human-owned conversations do not receive duplicate automated replies.

## Gate E - Testing and Regression Coverage
- [ ] Unit tests:
  - [ ] context assembly and redaction rules,
  - [ ] decision validation,
  - [ ] fallback behavior on invalid model output,
  - [ ] ownership-based reply suppression,
  - [ ] allowed state patch application.
- [ ] Integration tests:
  - [ ] product Q&A direct reply path,
  - [ ] missing-info follow-up question path,
  - [ ] read-only skill invocation path,
  - [ ] blocked write path when policy requirements are missing,
  - [ ] handoff outcome path.
- [ ] Regression tests:
  - [ ] M25 skill safety guarantees remain intact,
  - [ ] M26 state integrity remains intact,
  - [ ] existing inbound/outbound message persistence remains unchanged.

### Gate E Acceptance Criteria
- [ ] Runtime decisioning is covered across direct reply, follow-up, skill usage, fallback, and handoff branches.
- [ ] Existing message pipeline behavior is not regressed.

## Implementation Sequence
- [ ] 1) Gate A: runtime contract and context builder foundation.
- [ ] 2) Gate B: orchestrator core and validation/fallback policy.
- [ ] 3) Gate C: skill arbitration and state mutation integration.
- [ ] 4) Gate D: incoming message pipeline integration.
- [ ] 5) Gate E: tests and regression verification.

## Handoff and Operational Notes
- [ ] If runtime configuration for LLM providers introduces new secrets or settings, keep them environment-managed and out of API surfaces.
- [ ] This milestone should add observability for runtime decisions, but must not log raw secrets or excessive customer PII.
- [ ] Public chat endpoint contracts remain unchanged in this milestone.

