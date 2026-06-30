# PROMPTS.md — Agent Prompt Library

Reusable prompts for the DWM multi-agent workflow. Agents don't share context between
sessions, so every prompt should carry its own context. Copy a template, fill the
`{{blanks}}`, and paste. When a prompt works well, refine it here so the next use is better.

## The agents

| Tag | Agent | Use for |
| --- | --- | --- |
| **C** | Claude Code | Repo-touching implementation: code, schema, tests, refactors |
| **Cw** | Claude (web) | Architecture review, security review, design, spec drafting |
| **G** | Gemini | API/library verification, research, multimodal (screenshot/log) diagnosis |
| **X** | ChatGPT | Alternative implementations, narrative/flavor text, member-facing copy |
| **Co** | Cowork | Multi-step research + synthesis producing a durable artifact (see notes) |

> **Guardrail:** Read and understand every generated artifact before it ships — especially
> anything touching auth, billing, or member data. Security-sensitive code gets a
> second-agent review as a rule, not an option.

---

## Standing context block (paste at the top of most prompts)

```
PROJECT: Dream World Maker (DWM) — cooperative simulation/world-building platform.
Two environments: DWM (production world) and DWM_Dev (UE 5.3 engineering sandbox).
ECONOMY: authoritative in DWMStudio/SQLite (Microsoft.Data.Sqlite). UE reads via
FSQLiteDatabase/FSQLitePreparedStatement (SQLiteCore + SQLiteSupport plugins enabled).
Stone (LETS) mutual-credit ledger: 1 hr labor = 1 Stone; minted at exchange;
network sum ALWAYS = 0 (the core invariant). Dollar Vault depletes → Cascading Failure.
STACK: .NET 8, CommunityToolkit.Mvvm ([RelayCommand]/[ObservableProperty]); UE 5.3 for MVP.
MVP SCOPE: 5 communities (Mountain, Hillside, Valley, Suburb, City), single-player, local,
one promoted mechanism (Mountain wind turbine via Simscape). Scope is FROZEN — see SCOPE.md.
WORKFLOW RULE: DWMStudio must fully close before UE opens the .db (file-handle conflict).
```

---

## C — Claude Code (implementation)

**Implement a feature**
```
{{standing context}}
TASK: Implement {{feature}} in {{file/module}}.
CONSTRAINTS: Follow existing patterns (abstract interface + tool-specific impl;
project references not DLL HintPaths). Preserve the network-sum-zero invariant.
Add/extend tests. Don't touch anything outside {{scope}}.
DELIVER: the code + a one-paragraph summary of what changed and what I must review.
```

**Write tests for an invariant**
```
{{standing context}}
TASK: Write tests proving the Stone ledger network sum stays exactly 0 across {{operations}}.
Cover: normal trade, concurrent trades, failed/rolled-back trade, edge amounts.
DELIVER: the tests + a list of any invariant paths still untested.
```

## Cw — Claude web (review / architecture)

**Security review (mandatory for auth/billing/member-data)**
```
{{standing context}}
TASK: Security-review this {{auth/billing/webhook}} code. Look for: missing validation,
state that can desync, replay/double-submit, secrets in the wrong place, error paths that
leak. This is a second-agent review — assume the first pass missed something.
DELIVER: prioritized findings (must-fix vs. nice-to-have), each with the why.
[paste code]
```

**Design / data-model review**
```
{{standing context}}
TASK: Review this {{schema/API/design}} for {{goal}}. Check it against the ledger
invariant and the append-only requirement. Flag hidden coupling and future-pain.
DELIVER: what's sound, what's risky, what you'd change and why.
```

## G — Gemini (verification / research / diagnosis)

**Verify an API/library**
```
TASK: Verify the current {{library/API}} surface for {{use}} as of today.
Specifically: {{methods/version}}. Flag any version-specific gotchas (esp. UE 5.3).
DELIVER: confirmed signatures/steps + source links + gotchas. Say if anything is uncertain.
```

**Diagnose from a screenshot/log**
```
TASK: Diagnose this {{UE error / perf profile / build failure}}.
CONTEXT: {{what I was doing}}, UE 5.3, {{relevant setup}}.
DELIVER: most-likely cause, how to confirm, the fix. Rank if multiple.
[attach screenshot/log]
```

## X — ChatGPT (alternatives / content)

**Alternative implementation (for diffing)**
```
{{standing context}}
TASK: Independently implement {{thing}} so I can diff against my approach.
Optimize for {{clarity/speed}}. Note the tradeoffs of your approach vs. alternatives.
```

**Flavor text / member copy**
```
CONTEXT: DWM community "{{biome}}" — produces {{sells}}, needs {{buys}}.
Tone: grounded, hopeful, real-world-resilience (not whimsical fantasy).
TASK: Write {{marker flavor text / trade hint / onboarding email}}, {{length}}.
```

## Co — Cowork (multi-step synthesis → artifact)

Use Cowork when the task is **multi-step, spans multiple sources/files, and produces a
durable artifact** — the briefing cost is worth it. Not for one-shot lookups (use G) or
quick drafts (use X). Cowork needs briefing each session; point it at the repo + SCOPE.md.

```
{{standing context}}
TASK: {{e.g. "Compare Lemon Squeezy, Paddle, and Stripe for a solo VA LLC selling a
$25/mo digital subscription. Capabilities, fees, tax handling, setup effort."}}
DELIVER: a decision memo I can act on — recommendation + the reasoning + a comparison table.
```

---

## Prompt-writing notes (what works here)

- Always paste the standing context — agents start cold every session.
- Ask for "what I must review" / "what's still untested" — keeps the guardrail honest.
- For G, always ask it to flag uncertainty and cite sources (it's verifying, not guessing).
- Keep one prompt = one task. Bundled prompts produce shallow output across all parts.
- When a prompt works, paste the improved version back here. This file should get better.
