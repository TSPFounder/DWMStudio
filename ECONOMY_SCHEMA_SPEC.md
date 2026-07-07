# DWM Economy Schema — Design Spec (Day 3 draft, for review)

Companion to `economy_schema.sql`. This is the ECONOMY LEDGER schema — communities,
resources, and the Stone (LETS) ledger. It is separate from `WorldPackageExporter.cs`'s
schema (`WorldInfo`/`Blocks`/`Parameters`/`SimSamples`), which is the engineering/mechanism
package format for individual Simscape-driven objects (the pendulum, the turbine). The two
may live in the same `.db` or a separate one — recommend separate files for now; they change
at different rates and serve different consumers (UE economy HUD vs. the mechanism reader).

## Tables

**Communities** — the five MVP communities. `CommunityId` is a short stable slug (used as a
foreign key everywhere), not the display name, so renaming a community later doesn't break
every table that references it.

**Resources** — the tradeable goods. Same slug-vs-display-name pattern.

**CommunityResources** — what each community produces or needs, and a starting quantity.
`Role` is `'Produces'` or `'Needs'`. A community can appear multiple times (multiple
resources), which is why the primary key is the full triple.

**StoneLedger** — the mutual-credit transaction log. **Append-only. Never UPDATE or DELETE a
row** — a correction is a new offsetting row (equal amount, swapped From/To), same as
real LETS practice. Design choice: a **single-row transfer model**
(`FromCommunityId → ToCommunityId, Amount`) rather than double-entry debit/credit pairs.
Every row moves value from one community to another; nothing is created or destroyed by a
row, so **network sum = 0 falls out structurally** — the invariant test is just
`SUM(Amount) = 0` summed correctly across the table (see test note below), not something the
app logic has to maintain by writing matched pairs. `ResourceId` and `Quantity` are nullable
so the ledger can also record non-trade Stone movements later if ever needed, but every MVP
row will have them populated (1 Stone per hour of labor, applied to a specific trade).

**DollarVault** — the finite conventional-currency pool. Modeled as a ledger too
(`DollarVaultLedger`), same append-only reasoning, so the running balance is always derivable
and auditable rather than a single mutable number that can drift from its history.
`CascadingFailureThreshold` lives on a tiny singleton `DollarVaultConfig` table.

## Invariant test hook (Appendix B.4)

```sql
-- Network sum must always be exactly 0.
SELECT COALESCE(SUM(credit), 0) - COALESCE(SUM(debit), 0) AS network_sum FROM (
  SELECT Amount AS credit, 0 AS debit FROM StoneLedger
  UNION ALL
  SELECT 0 AS credit, Amount AS debit FROM StoneLedger
);
-- Simpler equivalent, since every row both credits ToCommunityId and debits FromCommunityId
-- by the same Amount: this is 0 by construction for ANY set of rows. The real invariant test
-- is per-community balances summing to 0 across the whole network:
SELECT SUM(bal) AS network_sum FROM (
  SELECT CommunityId,
         COALESCE((SELECT SUM(Amount) FROM StoneLedger WHERE ToCommunityId = c.CommunityId), 0)
       - COALESCE((SELECT SUM(Amount) FROM StoneLedger WHERE FromCommunityId = c.CommunityId), 0)
         AS bal
  FROM Communities c
) t;
```
Both should always return 0. The xUnit test (Day 4, Appendix B.4) asserts this after every
write path: a normal trade, concurrent trades, a failed/rolled-back trade, and edge amounts.

## Starter resource list — PLEASE REVIEW (worldbuilding decision, not just schema)

Design goal: every community produces something at least one other community needs, and
needs something it doesn't produce — so a trade has an obvious, motivated reason to happen
(this is what the Day-27 scripted demo trade needs). Proposed set, drafted from your biome
notes (City's software keeping Mountain's turbines running is carried over from the Primer;
the rest are my proposals for you to confirm or rename):

| Community | Produces | Needs |
| --- | --- | --- |
| **Mountain** | Timber, Wind-Power (from the turbine mechanism) | Grain, Manufactured Tools, Skilled Labor, Software Services |
| **Hillside** | Orchard Fruit, Wool | Timber, Wind-Power, Textiles |
| **Valley** | Grain, Water | Wool, Manufactured Tools |
| **Suburb** | Skilled Labor (hours), Textiles | Grain, Wind-Power |
| **City** | Manufactured Tools, Software/Maintenance Services | Timber, Orchard Fruit, Water |

This creates natural cross-links: Grain (Valley) is needed by both Mountain and Suburb;
Timber (Mountain) is needed by both Hillside and City; Wind-Power (Mountain) is needed by
both Hillside and Suburb; Manufactured Tools (City) is needed by both Mountain and Valley.
That redundancy is deliberate — it means the Day-27 demo trade isn't the ONLY possible trade,
so the economy doesn't look scripted even though the demo path is. Every one of the ten
resources now has both a producer and at least one consumer — including a small closed loop
(Hillside's Wool becomes Suburb's Textiles, which Hillside buys back) and Mountain needing
City's Software Services, which is the MVP economy's version of the Primer's canonical
"City software keeps Mountain turbines… running" interdependency.

Verified by actually executing the DDL + seed (not just inspected): every resource has
≥1 producer and ≥1 consumer, and the network-sum-zero invariant query returns 0.

**Please confirm or rename before this is seeded** — resource names are worldbuilding and
should be yours. Quantities in the seed script are round starting numbers (100 units
produced, 20 needed) purely as placeholders — real balance-tuning is a later pass, not a
Day-3 concern.

## What's deliberately NOT in this schema

- No pricing/exchange-rate table — Stone value is fixed (1 hr = 1 Stone ≈ $25 local
  purchasing power) per the Primer; no floating market to model for MVP.
- No multi-currency — Dollar Vault and Stone Ledger are separate, not exchangeable 1:1 in
  the schema (they represent different things: finite outside-resource money vs. mutual
  credit). Keep them structurally separate so one can never accidentally offset the other.
- No user/account tables — that's Phase A (post-MVP), not MVP economy data.
