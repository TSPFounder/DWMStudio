# DWM engineering reference databases

## R2025b subsystem catalog

`dwm_subsystems_r2025b.sqlite` records the proposed 25 reusable community
subsystem families and their MathWorks block mappings, including the subsequent
R2025b corrections. It contains 137 distinct block entries (including shared
support blocks), 209 subsystem/block associations, 9 products, 34 source links,
and 10 correction records.

This catalog belongs in DWMStudio because it is engineering-workbench reference
data. See [project goals](../Dream_World_Maker_Project_Goals.md). Neither inspected
default branch had a tracked database folder, so this directory was added.
The catalog is not the economy ledger, a UE world package, or an application
migration. No application code automatically loads it yet.

## Scope and confidence

- Target release: MATLAB R2025b. This is not an R2011a catalog.
- Derived from the user's subsystem-list conversation, block mapping, and release
  corrections. It is not an exhaustive inventory of installed MathWorks blocks.
- No MATLAB models were executed. Every block has `runtime_tested = 0`.
- `availability_status` distinguishes documented introduction by R2025b,
  documentation reviewed without an individual release check, and candidate
  entries awaiting verification. Even documented introduction does not validate
  every parameter or behavior of a release-specific model.
- Source URLs are the documentation examined in the conversation; unversioned
  MathWorks pages can later change. Missing source relationships mean no
  individually cited page was supplied for that block, not proof of availability.
- Product assignments identify the library owner or alternatives, not a purchase
  list. Candidate assignments should be checked in the R2025b Library Browser.
- Multiple blocks per subsystem are alternatives and building components, not a
  prescription to instantiate them all together. Avoid duplicate masses,
  friction, and kinematic constraints.
- Composite/custom models remain necessary for machining, welding, extrusion,
  filter clogging, and other detailed process physics.
- The average-value three-phase converter exists in R2025b; its R2026a ENA port
  and rectifier conduction-loss feature are explicitly excluded.

## Schema

| Table/view | Purpose |
| --- | --- |
| `subsystems` | Priority, components, applications, modeling notes |
| `blocks` | Block names, target release, evidence status, limitations |
| `products`, `block_products` | Product catalog and block ownership alternatives |
| `subsystem_blocks` | Many-to-many subsystem/block associations |
| `sources`, `block_sources` | Documentation links and block provenance |
| `corrections` | Changes applied after the R2025b clarification |
| `support_blocks` | Solver, reference, interface, and reuse infrastructure |
| `fluid_domains` | IL, TL, G, MA, 2P definitions |
| `catalog_requirements` | Design-library requirements and modeling cautions |
| `metadata` | Scope, provenance, schema version, repository selection |
| `subsystem_catalog` | Joined view for convenient browsing |

Open the SQLite file in DB Browser for SQLite or another SQLite client. Examples:

```sql
-- Entire catalog in priority order.
SELECT priority, subsystem, block, products, availability_status
FROM subsystem_catalog ORDER BY priority, block;

-- Screw-drive candidates and limitations.
SELECT block, products, modeling_notes, block_notes
FROM subsystem_catalog WHERE priority = 9;

-- Work still needed to validate the release inventory.
SELECT name, availability_status, notes FROM blocks
WHERE availability_status <> 'introduced_by_R2025b_documented';

-- Evidence for a selected block.
SELECT b.name, s.title, s.url FROM blocks b
JOIN block_sources bs ON bs.block_id = b.id
JOIN sources s ON s.id = bs.source_id
WHERE b.name = 'Battery Equivalent Circuit';
```

## Rebuild and validate

The companion `dwm_subsystems_r2025b.sql` contains the complete schema, data,
and view for reviewable changes and recreation. Rebuild into a **new** file:

```sh
sqlite3 rebuilt.sqlite ".read dwm_subsystems_r2025b.sql"
sqlite3 rebuilt.sqlite "PRAGMA integrity_check; PRAGMA foreign_key_check;"
```

Expected: `integrity_check` returns `ok`; `foreign_key_check` returns no rows.
The dump temporarily disables foreign-key enforcement because SQLite dumps
tables alphabetically; it restores enforcement after loading. Clients that edit
the database must enable `PRAGMA foreign_keys=ON` on their own connections.

The database uses DELETE journal mode and schema `user_version = 1`. It is closed
before publication and needs no WAL/SHM companion files. Both the committed
database and an SQL round-trip passed integrity and foreign-key checks; all 25
subsystems have mappings and the recreated logical dump is identical.

When editing the catalog, update the database and SQL export together and repeat
those checks. MATLAB runtime validation is a separate future task.
