# Claude Code prompt — SQLite verification spike (throwaway)

Copy everything in the box into Claude Code. It carries its own context. The goal is a
*disposable* verification test, not shipping code — delete it once the questions are answered.

```
PROJECT: Dream World Maker (DWM). UE 5.3 reads an SQLite .db that DWMStudio (.NET 8,
Microsoft.Data.Sqlite) writes. UE side uses the built-in SQLiteCore + SQLiteSupport plugins
(both enabled) via FSQLiteDatabase / FSQLitePreparedStatement. The economy ledger's
network sum must always equal 0, so correct read/write of integer/text columns matters.
Existing working code lives in DwmGameInstance.cpp (it already opens the .db in OnStart()).

WHY THIS TASK: An API-verification pass left two things UNCONFIRMED for UE 5.3 that I refuse
to build real code on top of until proven on THIS engine build:
  (1) Whether SetBindingValueByIndex / GetColumnValueByIndex are 1-based or 0-based.
      (Native SQLite is 1-based; Unreal's C++ ecosystem is mostly 0-based — unknown which wins.)
  (2) That a clean Open -> create table -> insert -> select -> Close cycle works with no
      lingering file lock (we have a known file-handle conflict with DWMStudio and want to
      confirm UE releases the handle cleanly).

TASK: Write a SINGLE throwaway verification function — call it DwmSqliteSpike() — that I can
trigger once (from an exec console command, or a temporary call in OnStart guarded by a
bool, your call — pick the lowest-friction option for UE 5.3) and read the results in the
Output Log. It must:

  1. Open a temp .db in ReadWriteCreate mode at a writable absolute path
     (e.g. FPaths::ProjectSavedDir() + TEXT("SpikeTest.db")). Log the resolved path.
  2. Create a tiny table: CREATE TABLE t (id INTEGER, name TEXT).
  3. Insert one row using a PREPARED STATEMENT with BY-INDEX binding. Bind id and name.
     Then ALSO do a second insert using BY-NAME binding ($id, $name) so I can compare.
  4. Select the rows back. Read columns BOTH by index AND by name. Log every value read.
  5. CRITICAL — answer question (1) explicitly: attempt the by-index bind/get using index 1
     first; if a value comes back wrong/empty, retry with index 0, and LOG which index base
     actually produced correct values. End with a clear line like:
     "RESULT: SetBindingValueByIndex is <1-based|0-based> on this build."
  6. Close the database. Then immediately re-Open the same file ReadOnly and read one row,
     to confirm the handle was released cleanly (no lock). Log success/failure.
  7. Wrap every FSQLite call's bool/step result in a check and log GetLastError() on failure.
     Use the ESQLitePreparedStatementStepResult enum (Row/Done/Error) correctly.

CONSTRAINTS:
  - This is THROWAWAY. Keep it in one self-contained function in one place; easy to delete.
    Do not wire it into real game flow or the real ledger. Do not touch existing schema.
  - Before writing, if any FSQLiteDatabase/FSQLitePreparedStatement signature or enum name
    is uncertain, tell me to confirm it against the local headers at
    Engine/Plugins/Runtime/Database/SQLiteCore/Source/SQLiteCore/Public/ rather than guessing.
  - Don't add the NonUFS packaging config — this spike runs in-editor only.

DELIVER:
  - The function code + exactly how to trigger it once.
  - A one-paragraph summary of what I should see in the log if everything's correct.
  - A short "DELETE THIS LATER" note listing every file/line you added, so cleanup is trivial.
```
```

## After it runs — record the answers

Put the confirmed facts where they won't get lost:

- In **AGENT_LOG.md**, update the G verification row's Notes to: "binding index = {1-based/0-based}, confirmed by spike; open/close releases handle cleanly: {yes/no}."
- If 1-based (likely), adopt **by-name binding ($id, $name) as the project default** anyway — it sidesteps the index question entirely and reads better. Note that as a convention.
- Then **delete the spike** using the "DELETE THIS LATER" list. A verification spike that gets left in the codebase becomes mystery code in three weeks.
