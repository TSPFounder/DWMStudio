# Hank NPC — integration steps

These are **additive edits** to three existing files, written as snippets rather than
full-file replacements on purpose: Codex has touched `DWM_DevCharacter.cpp` for the door
fix since I last saw it, and handing you a whole-file overwrite would silently discard
that work. Apply the snippets; don't replace the files.

---

## 1. `DWM_Dev.Build.cs` — add the UMG modules

The dialogue panel is a `UUserWidget`, so the module needs UMG (and Slate, which UMG
headers pull in). Find the `PublicDependencyModuleNames` line and add the three names if
they aren't already there:

```csharp
PublicDependencyModuleNames.AddRange(new string[] {
    "Core", "CoreUObject", "Engine", "InputCore",
    "EnhancedInput", "SQLiteCore", "SQLiteSupport",
    "UMG", "Slate", "SlateCore"          // <-- add these three
});
```

Missing this produces link errors on `UUserWidget`/`CreateWidget`, not compile errors, so
it's worth doing before the first build rather than after.

---

## 2. `DwmGameInstance.h` — declare the quest-progress query

Add next to the existing economy functions (right after `RefreshEconomyState`'s
declaration is fine):

```cpp
    /** Every community that BuyerCommunityId has paid Stone to at least once, read from
        StoneLedger. Used by the dialogue system to tell "still shopping" from "got
        everything" without duplicating quest state outside the ledger -- the ledger stays
        the single source of truth, same rule the HUD and trade panel already follow.
        Returns false if the database could not be read; OutPartners is emptied first. */
    UFUNCTION(BlueprintCallable, Category = "DWM|Economy")
    bool GetCompletedTradePartners(const FString& BuyerCommunityId, TArray<FString>& OutPartners) const;
```

## 3. `DwmGameInstance.cpp` — implement it

Append after `ExecuteConfiguredTrade`. It reuses the same open/prepare/step/destroy/close
shape as `RefreshEconomyState`, and opens **ReadOnly** — it can't affect the write path.

```cpp
bool UDwmGameInstance::GetCompletedTradePartners(const FString& BuyerCommunityId,
    TArray<FString>& OutPartners) const
{
    OutPartners.Reset();

    const FString DbPath = GetEconomyPackagePath();
    FSQLiteDatabase Db;
    if (!Db.Open(*DbPath, ESQLiteDatabaseOpenMode::ReadOnly))
    {
        // Logged rather than routed through SetEconomyStatus: this is a const query and
        // shouldn't overwrite the status line the trade flow owns.
        UE_LOG(LogTemp, Warning,
            TEXT("[DWM Economy] Trade-partner query could not open '%s': %s"),
            *DbPath, *Db.GetLastError());
        return false;
    }

    FSQLitePreparedStatement Stmt;
    if (!Stmt.Create(Db,
        TEXT("SELECT DISTINCT ToCommunityId FROM StoneLedger WHERE FromCommunityId = ?1;"),
        ESQLitePreparedStatementFlags::Persistent))
    {
        UE_LOG(LogTemp, Warning,
            TEXT("[DWM Economy] Trade-partner query creation failed: %s"), *Db.GetLastError());
        Db.Close();
        return false;
    }

    Stmt.SetBindingValueByIndex(1, BuyerCommunityId);

    for (;;)
    {
        const ESQLitePreparedStatementStepResult StepResult = Stmt.Step();
        if (StepResult == ESQLitePreparedStatementStepResult::Done)
        {
            break;
        }
        if (StepResult != ESQLitePreparedStatementStepResult::Row)
        {
            UE_LOG(LogTemp, Warning,
                TEXT("[DWM Economy] Trade-partner query failed: %s"), *Db.GetLastError());
            Stmt.Destroy();
            Db.Close();
            return false;
        }

        FString Partner;
        if (Stmt.GetColumnValueByIndex(0, Partner))
        {
            OutPartners.AddUnique(Partner);
        }
    }

    Stmt.Destroy();
    Db.Close();
    return true;
}
```

---

## 4. `DWM_DevCharacter.h` — three additions

**(a)** With the other forward declarations near the top:

```cpp
class ADwmNpcActor;
```

**(b)** In the `public:` section, next to the existing trade-terminal accessors:

```cpp
	/** Called by a nearby dialogue NPC while the player is in interaction range. */
	void SetActiveNpc(ADwmNpcActor* Npc);
	void ClearActiveNpc(ADwmNpcActor* Npc);
```

**(c)** Next to the existing `ActiveTradeTerminal` member:

```cpp
	TWeakObjectPtr<ADwmNpcActor> ActiveNpc;
```

---

## 5. `DWM_DevCharacter.cpp` — include, accessors, and the `Interact()` change

**(a)** With the other includes:

```cpp
#include "DwmNpcActor.h"
```

**(b)** Add the two accessors next to the trade-terminal pair — same shape:

```cpp
void ADWM_DevCharacter::SetActiveNpc(ADwmNpcActor* Npc)
{
	ActiveNpc = Npc;
}

void ADWM_DevCharacter::ClearActiveNpc(ADwmNpcActor* Npc)
{
	if (ActiveNpc.Get() == Npc)
	{
		ActiveNpc.Reset();
	}
}
```

**(c)** Replace the body of `Interact()`. **Read the ordering note below before applying
this** — it's the part that protects the trade terminal.

```cpp
void ADWM_DevCharacter::Interact()
{
	// ORDERING MATTERS, and this order is deliberate:
	//
	// 1. An OPEN dialogue panel consumes E first, so E advances the conversation the
	//    player is already in. This branch can only be reached after the player
	//    deliberately started a conversation, so it cannot pre-empt anything.
	// 2. The trade terminal is checked BEFORE the NPC. When a terminal is in range and
	//    no dialogue is open, behaviour is byte-for-byte what it was before this change
	//    -- which is the guardrail on this work: the terminal path is proven and must not
	//    regress just because Hank now also answers to E.
	// 3. The NPC is last, so a terminal and an NPC standing near each other resolve in
	//    favour of the terminal.
	//
	// Co-locating a terminal AND an NPC at the same marker (which every community stop
	// after Mountain will want) is explicitly NOT solved here -- see
	// DWM_Coordination_Note.md, which scopes this task to Hank alone and flags the
	// multi-NPC co-location problem as follow-up work.

	if (ADwmNpcActor* TalkingNpc = ActiveNpc.Get())
	{
		if (TalkingNpc->IsDialogueOpen())
		{
			TalkingNpc->AdvanceDialogue();
			return;
		}
	}

	if (ADwmTradeTerminalActor* TradeTerminal = ActiveTradeTerminal.Get())
	{
		TradeTerminal->ExecuteTrade(this);
		return;
	}

	if (ADwmNpcActor* TalkingNpc = ActiveNpc.Get())
	{
		TalkingNpc->BeginDialogue(this);
		return;
	}

	if (GEngine)
	{
		GEngine->AddOnScreenDebugMessage(0xD0018EULL, 2.0f, FColor::Yellow,
			TEXT("Nothing to interact with."));
	}
}
```

If Codex's door fix added its own branch to `Interact()`, keep it — slot it in after the
trade terminal and before the NPC, and leave the two terminal-related lines untouched.

---

## 6. Editor steps

1. **Build** (close the editor first — new C++ classes need a full compile).
2. **Create the panel widget:** Content Browser → Add → **User Interface → Widget
   Blueprint** → pick **`DwmDialogueWidget`** as the parent class (use the "All Classes"
   search if it isn't in the common list). Name it `WBP_DwmDialogue`.
   - Lay out a border with two `TextBlock`s (speaker, body) and a `Button` with its own
     `TextBlock` label.
   - Bind the speaker text to `CurrentLine.Speaker` and the body to `CurrentLine.Body`.
   - On the button's `OnClicked`, call **`Request Advance`**.
   - For the button label: use `CurrentLine.AdvancePrompt` when it's non-empty, otherwise
     "Continue" if `bHasNextLine` is true and "Done" if it's false.
3. **Build the Anim Blueprint** (this is what makes idle/walk *blend* instead of cut).
   The C++ publishes the state; the blend graph itself has to be built in the editor —
   there's no way to author a state machine from code.

   a. Content Browser → Add → **Animation → Animation Blueprint**.
      - **Parent Class:** `DwmNpcAnimInstance` (search for it — it won't be in the
        default shortlist).
      - **Skeleton:** Hank's skeleton (the Yarrawah one, not UE5 Manny).
      - Name it `ABP_Hank`.

   b. Open it. In **AnimGraph**, right-click → **Add State Machine**, name it `Locomotion`,
      and wire its output into **Output Pose**.

   c. Double-click the state machine. Create two states:
      - **`Idle`** — inside it, drag in the idle sequence and connect to Output Animation
        Pose. Tick **Loop**.
      - **`Walk`** — same, with the walk cycle. Tick **Loop**.
      - Drag from Entry into `Idle` so idle is the default.

   d. Create transitions in both directions (drag from the edge of one state to the other):
      - **Idle → Walk:** click the transition node, and in its rule graph wire
        **`Is Moving`** (a variable this C++ class publishes) straight into **Result**.
      - **Walk → Idle:** same, but put a **NOT Boolean** between `Is Moving` and `Result`.
      - Select each transition and set **Duration** to about **0.2** — that number *is*
        the blend. Zero here reproduces the single-node cutting this step exists to fix.

   e. If you want a distinct pose while he's talking or watching the turbine, add more
      states and drive their transitions from **`Is Talking`** or **`Activity`** (both are
      published as variables too). Optional — two states is enough to look right.

   f. **For montages to work** (the turbine glance and the talk gesture), the AnimGraph
      needs a slot: add a **Slot 'DefaultSlot'** node between the state machine and Output
      Pose. Without it, `Montage_Play` runs but nothing shows.

   g. Compile and save.

4. **Create Hank's Blueprint:** Add → Blueprint Class → parent **`DwmNpcActor`** → name it
   `BP_Hank`. Open it and set:
   - `NpcMesh` → Hank's skeletal mesh, **civilian outfit preset** (per
     DWM_MVP_Storyline.md — the default military/survival gear "would misread for a
     community leader").
   - **`NpcMesh` → Anim Class → `ABP_Hank`.** This one setting is what selects blended
     mode. The actor checks for it at BeginPlay; if it's empty it silently falls back to
     single-node playback instead, so a missed assignment shows up as cutting rather than
     as an error.
   - **Montages** (Anim Blueprint mode uses these, not the sequences below): create each
     from its sequence — right-click the animation → **Create → Create AnimMontage** —
     then assign:
     - `Gesture At Turbine Montage` → the look-up/point/inspect clip for the "glances up
       at it" beat.
     - `Talk Montage` → a talking gesture played per dialogue line.
     - Both optional. Without them the loop still runs, just without gestures.
   - **Sequences** — only used in the single-node fallback. Fill them in anyway if you
     want the fallback to look right, or leave them empty once `ABP_Hank` is assigned:
     - `Idle Animation`, `Walk Animation`, `Gesture At Turbine Animation`, `Talk Animation`.
   - `Dialogue Widget Class` → `WBP_DwmDialogue`.
   - Leave `Dialogue By State`, `Required Seller Community Ids`, and `Buyer Community Id`
     at their defaults — already populated with Hank's copy and the four communities.
4. **Place `BP_Hank`** in the Mountain level at his marker, and set up the loop:
   - **Rotate him so his +X (the red arrow) points at the turbine.** `Turbine Watch
     Offset` defaults to 400cm straight forward, so getting the facing right means the
     offset needs no editing.
   - Set **`Turbine Actor`** (instance-only, in the level Details panel) to the placed
     wind turbine. This is what he turns to face while watching. Optional, but without it
     he just keeps facing whichever way he walked.
   - `Wander Radius` defaults to 250cm around his marker; `Wander Chance` 0.5 means about
     half his trips are a wander instead of a turbine check. Set the radius to 0 for the
     plain two-point shuttle with no wandering.
5. **PIE-test**, in this order:
   - Stand back and just watch for ~30s: **he idles, walks out toward the turbine, pauses
     and looks up at it, then walks back** — and sometimes wanders instead. No T-pose at
     any point, and the walk cycle plays while he's moving.
   - **Check the Output Log for one line at startup**: "driving animation through its Anim
     Blueprint (blended)" confirms `ABP_Hank` was picked up. "falling back to single-node
     playback" means the Anim Class assignment didn't take — fix that before judging how
     the blending looks.
   - Watch the moment he starts and stops walking: the pose should **ease** between idle
     and walk over ~0.2s, not snap. If it snaps, the transition Duration is still 0.
   - Check his feet: if they skate, lower `Walk Speed`; if he moonwalks, raise it. If he
     sinks into or floats above the terrain, adjust `Ground Offset`.
   - Walk up → "Press E: Talk to Hank" appears.
   - E → **he stops walking and turns to face you**, approach line shows. E again → the
     "What exactly do we need?" brief. E → panel closes and **he resumes his loop**.
   - Walk away and back, E → "How's it going down there?"
   - Walk away and back twice more → ambient lines, cycling (not the same one repeating).
   - **Then walk to a trade terminal and press E once** — confirm the trade still settles
     and balances still move. That's the guardrail check.
