# AGENT_LOG.md — Agent Task Log

A running record of what was delegated, to which agent, and how it turned out. Two reasons
this matters: (1) you learn which agent to route which work to, and (2) you can trace where
any generated code came from — which backs the "read before merge" guardrail.

**How to use:** add a row when you hand off a task. Update Outcome when it comes back.
Newest at the top. Keep it terse — this is a log, not a report.

Agents: **C** Claude Code · **Cw** Claude web · **G** Gemini · **X** ChatGPT · **Co** Cowork

Outcome key: ✅ used as-is · 🔧 used after edits · 🔁 sent back / redone · ❌ discarded · ⏳ in progress

---

| Date | Agent | Task | Outcome | Reviewed? | Notes |
| --- | --- | --- | --- | --- | --- |
| 2026-06-30 | Cw | Review SCOPE.md for hidden in/out dependencies | ⏳ | — | Day 1 task |
| 2026-06-30 | — | (template row — copy this) | | | |

---

## Review checklist (before marking anything ✅ or 🔧)

- [ ] I read the whole artifact and understand it — not just skimmed.
- [ ] If it touches **auth / billing / member data**, a second agent reviewed it.
- [ ] It preserves the **network-sum-zero** ledger invariant (if economy-related).
- [ ] It stays within **frozen MVP scope** (SCOPE.md) — no smuggled scope creep.
- [ ] Tests exist / pass for anything it changed.
- [ ] I noted in the log what I had to fix (feeds back into PROMPTS.md).

## What to capture in Notes

- Which prompt template (from PROMPTS.md) was used, so good ones get reused.
- What had to be fixed — patterns here tell you which agent suits which task.
- Any decision that belongs in SCOPE.md's decisions log (cross-reference it).
