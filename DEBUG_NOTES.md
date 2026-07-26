# Debug notes, Sat 2026-07-25

Findings from a day of debugging Drac's animation, ground detection, and victim biting.
Untracked on purpose so it survives a `git reset --hard`. It does NOT survive `git clean -fd`,
which is how the first copy of this file was lost. Delete once the jam ships.

---

## Confirmed fixes (already committed, don't redo)

**Ground tiles were on the wrong layer.** All 15 `GroundTiles` prefabs sat on layer 0 (Default)
while `PlayerController._groundLayerName` is `"Ground"`. `IsGrounded()` masked to a layer nothing
belonged to, so it always returned false. `PSWalk` kicked to `PSFalling`, and `PSFalling` could
only escape via `IsGrounded()`, so the player stuck in the falling animation forever. Worked in the
test scenes because those had the layer assigned. Fixed in `99e6550`.

**Animation import settings.** Root Transform Position (Y) to Based Upon **Center of Mass**, Bake
Into Pose **off**, on all five Vampire clips. Confirmed by Sean in person. Fixed in `2613914`.

The clips disagree wildly about authored hips height, which is why this setting matters. Measured
from the FBX curves, in file units:

| clip | first frame | min | max |
|---|---|---|---|
| Idle | 107.68 | 107.19 | 109.44 |
| Running | 104.74 | 103.42 | 108.36 |
| Attack | 99.26 | 83.62 | 101.39 |
| LandingRun | 155.85 | 88.99 | 155.85 |
| Falling To Landing | 168.66 | 69.38 | 168.66 |

"Based Upon: Original" preserves that disagreement. "Feet" should normalize it but routes through
the Avatar, which is unreliable on this Blender-exported rig. "Center of Mass" ignores the foot
solve entirely, which is why it works. Any new clip needs the same two settings or Drac will pop
between heights on state changes.

**Layer numbers**, corrected from the first draft of these notes: `Ground` is layer **6**,
`Victims` is layer **7**.

---

## Known bugs, not yet fixed

**Ground ray barely reaches.** With the older collider the ray ended only 0.028 below the floor.
`_groundCheckOffset` is 0.9 and `_groundCheckDistance` is 0.3. Recheck against the current capsule
(height 2.1533, center.y -0.13665, so the bottom is 1.213 below the origin). Offset about **1.05**
starts the ray inside the capsule and gives real tolerance.

**Ground check is a single centre ray.** One raycast from the middle of a 0.5-radius capsule, so
standing half over a ledge reads as airborne. Three rays or a `CheckSphere` is the upgrade.

**`CheckForVictims` uses `Quaternion.identity`.** The overlap box doesn't rotate with Drac, so he
can bite someone standing behind him.

**Jump is unreachable.** There is a `Jump()` method on `PlayerController` but no Jump action in
`PlayerInputs.inputactions`, so nothing calls it.

**`PlayLanding()` is never called**, so `LandingRun` is wired in `Drac.controller` but dead.

---

## Dead end, do not repeat

**Do not replace controller swapping with a single parameter-driven controller.** It was tried on
2026-07-25 and never worked. `PlayerAnimator.SetHumanoid` assigning `runtimeAnimatorController`
per action is what works in this project. The refactor is still the better design, but it needs a
branch and time to test, not a jam afternoon.

Worth knowing if it's attempted later: `Drac.controller` was renamed from `Idle.controller` and
kept its guid (`010c4ae7...`), so `_idleController` points at it. That means the Animator window
shows `Drac.controller` bound even when the old swapping code is running, which makes it a
misleading confirmation that a refactor "worked."

---

## Diagnosing this class of bug

Two tools were written and then removed: `PlayerOriginDebug` (draws transform origin, collider
bounds, renderer bounds and the ground ray as gizmos, logs the offsets) and `AnimatorPoseOffset`
(shifts the root bone in `LateUpdate`, after the Animator has written the pose). Both are in git
history if wanted again.

The lesson that cost the most time: when a runtime symptom resists explanation, read the runtime,
not the files. The Animator's Parameters panel during Play distinguishes "parameters aren't being
set" from "parameters are set but the graph won't move" in one glance, and those two look
identical from outside while needing completely different fixes.

---

## Code restored from abandoned checkpoint 95c7e0d

Recovered four C# files from the checkpoint that was reset away. Deliberately left behind: the
single-controller `PlayerAnimator` refactor, the `PlayerController` field collapse, the Attack
input action, and the tangled prefab and scene edits.

| file | change |
|---|---|
| `PSIdle.cs` | automatic bite check at top of `Execute` |
| `PSWalk.cs` | same check, ordered before the ground test |
| `PSEating.cs` | `PlayAttack()`, horizontal-only snap, null-victim escape |
| `Victim.cs` | `Die()` with controller swap and despawn, dead-guard, zero-blood warning |

---

## Why attacking does nothing, measured on `mike` at 80262c9

The first draft of this checklist was written from guesses. Corrections, all measured from the
serialized assets:

**The NPC side is already done.** All 31 prefabs: layer 7, `_startBloodPoints` 100,
`_deathController` assigned, `_despawnDelay` 2. The `Victim` component and the CapsuleCollider are
both on GameObject `919132149155446097`, the same one carrying the layer override, so
`EatCastHit.GetComponent<Victim>()` resolves. Nothing to do here.

**The player fields were never serialized.** `VampirePlayerAsset.prefab` still carries
`BoxCastHalf: {x: 0, y: 0}`, a Vector2 from an older script that no longer declares that field.
Unity ignores the dead line. `_boxCastHalf`, `_victimLayerName`, `BloodDrainSpeed`, `MaxHealth`,
`_healSpeed` and `_invincibilityTime` are absent from the prefab entirely and run at C# defaults.

`_victimLayerName` empty means `LayerMask.GetMask("")` returns 0, so `OverlapBox` matches nothing
regardless of geometry. `_boxCastHalf` zero means the box has no volume. Two independent hard
stops.

**Touching a collider is not how this works.** `CheckForVictims` is a polled `Physics.OverlapBox`,
not a collision or trigger callback. Bumping the capsules together does nothing. The query has to
be switched on and given a volume.

**Geometry, measured:**

| thing | value | source |
|---|---|---|
| player collider | height 2.1533, center.y -0.13665 | Player_Drac variant override |
| player collider bottom | 1.213 below transform origin | derived |
| player resting origin | about Y 1.213 | derived |
| NPC collider | radius 0.5, height 1, center (0,0,0) | Character_01 |
| NPC collider world span | Y -0.5 to +0.5 | NPC placed at Y 0 |
| vertical gap, player origin to NPC collider top | 0.713 | derived |
| player Z / NPC Z | both 9.3, coplanar | Game.unity |

The NPC capsule is degenerate: height 1 with radius 0.5 is a sphere, centred on the origin so half
of it is below the floor. It sits at ankle height, not body height.

**Fix the NPC capsule rather than inflating the bite box.** NPC `m_Height` about 2 and
`m_Center.y` about 1 puts the collider on the body, spanning Y 0 to 2. A `_boxCastHalf.y` of 1 then
overlaps it comfortably and stays correct if the player's height changes later.

Note only one NPC is actually placed in `Game.unity`.

### Values to set on VampirePlayerAsset (the base prefab, not the Player_Drac variant)

- [ ] `_victimLayerName` to `Victims`, exact case, `LayerMask.GetMask` is case sensitive
- [ ] `_boxCastHalf` to `(1, 1, 1)` after the NPC capsule fix, or `(1, 1.5, 1)` if left as is
- [ ] `BloodDrainSpeed` to 50
- [ ] `MaxHealth` to 100, currently 0 so any hazard is instant death
- [ ] Saving the prefab drops the dead `BoxCastHalf` line and writes the real fields

### Confirm it worked

- [ ] `_boxCastHalf.y` has to exceed 0.713 to touch the NPC collider as it currently stands
- [ ] Walk into the NPC on flat ground, no button. Should enter Eating.
- [ ] If still nothing, temporarily log `_victimLayerIndex` in Awake. Zero means the layer name is
      wrong or empty, and no amount of box resizing will help.

---

## The restore was lost once, then redone

The four C# files were restored from `95c7e0d`, left uncommitted, and were back to their original
state by the time attacking was tested. `git stash list` was empty, so the pop either never landed
or was undone. Symptom was "the attacking animation isn't firing", but the real cause was that
`PSEating.Enter` had no `PlayAttack()` call and `PSIdle`/`PSWalk` had no `CheckForVictims()` at
all.

While `Victim.cs` was reverted, the NPC prefabs still carried `_deathController` and
`_despawnDelay`. Those fields did not exist on the reverted script, so they sat as dead YAML
exactly like `BoxCastHalf` did on the player. Restoring the script makes them live again with the
right values already set. Worth remembering: a prefab holding values for fields the current script
does not declare is silent, and it is the same failure twice in one day.

**Lesson: commit the restore immediately. Do not leave it in the working tree.** Twice now
uncommitted recovery work has evaporated between one test and the next.

### Geometry, corrected by measurement

An earlier draft of these notes claimed the NPC collider spanned Y -0.5 to +0.5 and warned that
the bite box only barely reached it. That was wrong. It was derived from the prefab values
(`radius 0.5, height 1, center 0`) without accounting for the NPC's transform scale.

`BiteDebug` measured the real thing at runtime:

```
[BiteDebug] victim 'Character_01' layer=7 (Victims) inMask=True  collider bounds Y 0.600..1.000
```

Bounds 0.4 units tall means the NPC is scaled to about 0.4, and the collider sits at chest height
on a small figure rather than being a half-buried sphere at the ankles. With `_boxCastHalf` at
`(1, 1, 1)` and the player's origin resting near Y 1.2, the box spans roughly 0.2 to 2.2 and
overlaps comfortably. **No NPC capsule change is needed.** Disregard the earlier advice to set
`m_Height` 2 and `m_Center.y` 1.

The general lesson, again: prefab numbers are local, and a scaled transform makes them lie. Read
`collider.bounds` at runtime instead of doing arithmetic on the YAML.

---

## Bite behaviour, decided 2026-07-25

Confirmed working end to end. Trace from the logs: `PSWalk` / `Running` at frame 65, overlap finds
`Character_01` with `hasVictim=True`, `PSEating` / `Attacking` at frame 175, back to `PSIdle` at
frame 342. That is 167 frames for a 100 blood victim at drain speed 50, so about 2 seconds, which
matches.

**Two fixes after first playtest:**

The player could walk out of the bite and the victim would still die. `PSEating.Execute` only
checked `_drainFinished`, so entering the state committed to the full drain no matter what.

The cause of the drift was the snap in `Enter`, which set the player to the victim's exact x while
they already shared a z. Two colliders in the same place interpenetrate, and physics resolves that
by shoving them apart, pushing the player out of the bite it had just started.

Decisions:

- **The snap is gone.** The overlap query already proved the victim is in reach, so no
  repositioning is needed, and removing it removes the shove.
- **The player is locked in place for the drain.** `Execute` zeroes x velocity every frame.
  Horizontal only, so a bite started mid air still falls. Input was already ignored, since this
  state's `ChangeDI` is deliberately empty. Biting is now a roughly 2 second commitment, which
  makes it a real risk near hazards.

### Known quirk in BiteDebug

`OverlapBox` skips disabled colliders, and `Die()` disables the victim's collider. The first
version of the tool reported that as "in range on all axes, so the miss is the layer mask", which
is wrong and misleading. Fixed to report a drained victim explicitly. Delete `BiteDebug.cs` before
shipping.

---

## Bite standoff, added after second playtest

Attack animation played with a visible gap. Cause: `_boxCastHalf.x` is 1.0, so the bite can
trigger from a full unit away, before the two colliders ever touch.

`PSEating.Enter` now pulls the player in horizontally to `PlayerController.BiteStandoff`, on
whichever side they approached from. Y and Z are untouched.

**Floor is about 0.7.** Player capsule radius 0.5, NPC capsule radius 0.5 but scaled to roughly
0.4, so about 0.2 in world units. Set `BiteStandoff` below their sum and the colliders
interpenetrate, physics resolves it by shoving the player out, and the bite breaks the same way it
did when the old code snapped to the victim's exact x. Default is 0.75, tune upward freely and
downward carefully.

`BiteStandoff` has a `= 0.75f` initializer, so it holds without re-saving the prefab. Same trick as
`_jumpForce = 8f`. Fields absent from the YAML keep their initializer.

---

## Hazard damage: gator, kangaroo, spike fence

Decisions: health pool with invincibility frames, kangaroo is hazard only and not biteable,
hazards stay solid and damage on collision.

### What was already there

`Obstacle.cs` existed and did the job, but was only present in `PlayerTestInContext.unity`. It was
on no prefab and not in `Game.unity`, so nothing ever dealt damage.

### Code changes

**`Obstacle.cs`** now handles `OnCollisionEnter` as well as `OnTriggerEnter`, so hazards work
solid or as triggers without editing the script. Knockback is built from the **contact point**
rather than this object's pivot, which matters for the fence: its pivot sits far from where the
player actually hits it, so pivot-based knockback fired the wrong way. The direction is also
normalized now. The old version multiplied the raw position difference by push strength, so
knockback grew with distance and a distant pivot launched the player across the level.

**`PlayerController.TakeDamage`** now returns early when invincible, so a hazard can no longer
shove the player around during the window meant to protect them. It sets horizontal velocity
instead of adding to it, because the player is usually running into the hazard and adding partly
cancelled the push. It starts `InvincibilityTime()`, which was written but never called, so there
were no i-frames at all. The death test is now `<=` rather than `<`, since the old form left the
player alive on exactly zero health, which read in game as one extra free hit.

**Knockback was being erased the frame it was applied.** `PSWalk.Execute` assigns
`v.x = WalkSpeed * DirectionalInput.x` and `PSIdle.Execute` assigns `v.x = 0`, both every frame.
Only `PSFalling` is additive, so the bounce only ever worked mid air. `PlayerController` now
exposes `IsKnockedBack`, a short window after a hit, and both states skip their horizontal
override while it is true. Without this a hazard drains health and looks like it does nothing.

`_knockbackTime` defaults to 0.25 and `_invincibilityTime` to 1.0. Both have initializers, so they
hold without re-saving the prefab.

### Inspector checklist

**Player, this one is required or nothing works:**

- [ ] `MaxHealth` on VampirePlayerAsset. It is serialized as **0**, so an initializer cannot fix
      it and it must be set by hand. At 0 the first hit always calls `EndRun()`. Try 100.

**Each of the three hazards** (`GatorHazarad`, `KangarooNPC`, `DeathSpikeFence`):

- [ ] Add an `Obstacle` component
- [ ] Set `_damage`, default 25, so four hits at 100 health
- [ ] `_pushStrength` default 6, `_yMultiplier` default 1, `_minUpward` default 0.4
- [ ] Keep the collider **not** a trigger, since these are solid
- [ ] Keep them on layer 0 Default. Do not put them on Victims (7) or `CheckForVictims` will pick
      them up and bounce the player in and out of Eating

**Colliders, currently missing:**

- [ ] `GatorHazarad` has **no collider at all**. It is a variant of `ALLIGATOR_DEMO.fbx` with
      nothing added. Add a Box or Capsule.
- [ ] `KangarooNPC` has **no collider at all**. Same situation, variant of `kangaroo cotw.fbx`.
- [ ] `DeathSpikeFence` already has a BoxCollider with `isTrigger: 0`, which is correct for solid.
      Nothing to change.

Note the player's Rigidbody is what makes `OnCollisionEnter` fire. The hazards do not need one.

### Still outstanding

- `_healSpeed` on `PlayerController` is serialized and never read. Either wire up regeneration or
  delete the field.
- There is no hit reaction animation or flash, so during i-frames the only feedback is the
  knockback itself.
- `ChainlinkFence` was left alone. If it should hurt too, it needs the same treatment.

---

## In-run HUD

Decisions: blood is score only and stays separate from `MaxHealth`, the blood total is a number
rather than a bar because it is uncapped, the clock is a radial fill.

### What was already there

`[UI]Game.prefab` is in `Game.unity` already, with `BatSlider`, `HealthSlider`, `ClockSprite`,
`GameScreen`, `PauseScreen`. Only `PauseController` was attached. Nothing drove any of the three
readouts, and the prefab contains **no TMP text at all**, so there was no way to display a number.

### Code changes

**`HUDController.cs`**, new, in `_Project.Code.UI`. Put it on `GameScreen`. Bat meter and clock are
polled in `Update` because neither has a change event. Blood uses `GameManager.OnScoreChanged`, and
also pulls the current value in `OnEnable`, since that event only fires on change and the readout
would otherwise sit blank until the first bite. The events are static, so `OnDisable` unsubscribes.

**`PlayerController`** gained `BatTimeNormalized` and `HealthNormalized`, both guarded against a
zero max so they cannot divide by zero. The HUD reads these instead of using reflection.

**`GameManager`** gained `_startingBlood`, default 100, applied in a new shared `ResetRunState`.
`StartRun` and the new `StartRunInCurrentScene` both use it.

### The testing trap this would have hit

`GameManager.Update` returns early unless `CurrentState == Playing`, `CurrentState` starts at
`MainMenu`, and `StartRun` calls `LoadScene`, so it cannot be used from inside the game scene.
Pressing Play directly in `Game.unity` therefore means the clock never ticks and `AddBlood` rejects
every drain, which reads as a completely broken HUD even when it is wired correctly.

`StartRunInCurrentScene()` does the same reset without loading a scene. `RunTestControls` now calls
it from `Start`, gated on `_autoStartRunOnPlay`, default on. Turn it off before shipping, or when
testing the real MainMenu to Game flow.

### Inspector checklist

**On `GameScreen` inside `[UI]Game.prefab`:**

- [ ] Add `HUDController`
- [ ] `_batSlider` to `BatSlider`
- [ ] `_clockFill` to the Image on `ClockSprite`
- [ ] `_bloodText` to a new TMP text, see below
- [ ] `_player` can stay empty, it finds the player at Awake
- [ ] `_healthSlider` leave empty unless the top right bar should show health instead

**Two prefab changes that the script cannot do:**

- [ ] `ClockSprite`'s Image `Type` is **Simple**. Set it to **Filled**. Fill Method is already
      Radial 360 and Fill Amount is already 1, but with Type Simple the fill amount does nothing
      and the clock will sit static.
- [ ] There is no TMP text in the prefab. Add one inside the top right bar frame for the blood
      total, then assign it to `_bloodText`.

**Worth checking:**

- [ ] `BatSlider` Direction is `Left To Right` (0). The mockup shows a vertical bar, so it likely
      wants `Bottom To Top`.
- [ ] `HealthSlider` currently has nothing driving it. Either point `_healthSlider` at it or accept
      it stays empty.
- [ ] `_startingBlood` on GameManager, which lives in `MainMenu.unity`, defaults to 100.

### Still outstanding

- No hit feedback beyond knockback, so i-frames are invisible.
- `HealthSlider` is undriven by default given blood and health are separate.
- Nothing calls `EndRun` from gameplay yet except `RunTestControls.SleepNow`. The coffin is still
  the missing piece.

---

## HUD fixes after first wiring

### Why the blood text never updated

`GameManager` exists only in `MainMenu.unity`. Playing `Game.unity` directly means
`GameManager.Exists` is **false** and stays false, because `Exists` only becomes true once
something touches `Instance` and triggers the CoreUtils lazy creation.

Everything in the first HUD pass was guarded on `Exists`, so all of it silently did nothing:

- `HUDController.OnEnable` skipped its initial score pull, leaving the text blank
- `HUDController.Update` skipped the clock entirely
- `RunTestControls.Start` returned before starting the run, so `CurrentState` stayed `MainMenu`
- which made `AddBlood` reject every drain, so `OnScoreChanged` never fired either

Four symptoms, one cause. The warning was already written down in the `PSEating` comment:
"Guarding it would skip scoring entirely in that case, because Exists stays false until something
touches Instance." It got ignored on the way in.

**Rule for this project: to use `GameManager` in the game scene, touch `Instance`, never test
`Exists` first.** `Exists` is only safe where doing nothing is an acceptable outcome, such as
`EndScreenController` falling back to a score of 0.

`HUDController` now goes through a `GM` property that caches `GameManager.Instance` on first use.
`RunTestControls.Start` does the same. `OnDisable` still avoids `Instance`, since touching it
during application quit can resurrect a destroyed singleton.

Same pattern still present in `PauseController`, which guards all three of its calls on `Exists`.
It works today only because the HUD or RunTestControls creates the manager first. Worth cleaning up
if pausing ever misbehaves.

### Blood bar

The top right bar is now driven by blood, filling from 0 to `_bloodBarMax`, default **1000**, so a
starting 100 shows as a tenth full. The score itself stays uncapped: past 1000 the bar sits full
and the number keeps climbing. Assign `HealthSlider` to `_bloodSlider`.

`_healthSlider` was removed from the HUD, since blood and health are separate and only blood has a
readout. `PlayerController.HealthNormalized` is left in place for whenever health gets its own
display.

### Correction to the earlier checklist

The earlier note suggested setting `BatSlider` Direction to `Bottom To Top` to match the vertical
bar in the mockup. **That is wrong.** Bottom To Top makes the slider too wide and it spills outside
the frame art. Keep it `Left To Right`.

---

## Blood and health merged into one pool

### The "+99" was rounding, not draining

`Score` was `Mathf.FloorToInt(_score)`. `Victim.DrainBlood` awards `drainRate` per frame plus the
remainder on the final call, which sums to exactly 100 in real arithmetic but lands a hair under it
in floating point. `_score` reached about 199.99998 and floored to 199, so a full victim read as a
gain of 99. Now `Mathf.RoundToInt`, and the change test in `TickDaylightDrain` matches.

### One resource, not two

Blood is now the health pool. There is no `MaxHealth`, `_currentHealth`, `_healSpeed` or
`HealthNormalized` on `PlayerController` any more.

- `GameManager.RemoveBlood(amount)` subtracts, clamps at zero, fires `OnScoreChanged`, and calls
  `EndRun()` when it reaches zero
- `PlayerController.TakeDamage` spends blood via that, keeping knockback and i-frames, and has no
  death check of its own
- `Obstacle._damage` is the blood cost, no second field
- The daylight drain after sunrise now also ends the run at zero, which it did not before. Blood
  being health makes that consistent: bleeding out in the sun should end things.
- Blood is still uncapped upward, so the bar fills to `_bloodBarMax` (1000) and the number keeps
  climbing past it

### Dead YAML left behind, expected

`MaxHealth` and `_healSpeed` are still serialized in `VampirePlayerAsset.prefab` and will sit there
until the prefab is next saved. Unity ignores fields the script no longer declares. This is the
same harmless situation as the old `BoxCastHalf` Vector2, and it is worth recognising rather than
chasing: **a prefab holding values for fields the current script does not declare is silent.** That
has now come up three times in this project.

### Retest after this change

- [ ] Drain a full victim. The total should go 100 to 200, not 199.
- [ ] Take a hazard hit. The number should drop by that hazard's `_damage`.
- [ ] Let blood reach zero from hazards. The run should end.
- [ ] Survive to sunrise and stand in the sun until blood hits zero. The run should end there too.
- [ ] Confirm i-frames still hold, so one fence touch costs one hit and not several.

---

## Console cleanup

### CS0067 on CountInputManager.OnEat

Removed. Nothing raised or subscribed to it, and there is no Eat action in `PlayerInputs` to raise
it from, so it could never have fired. Biting is automatic on proximity by design, so an eat input
is not planned.

`CountController` lives on `Count.prefab`, which appears only in `CountTestScene`. It is a separate
player implementation from `Player_Drac` and is not in `Game.unity`.

### The UnityEditor.Graphs NullReferenceException is not ours

Every frame of that stack is inside `UnityEditor.Graphs`, thrown from `Graph.OnEnable`, with no
project code anywhere in it. That is the Animator window failing to rebuild its graph after a
domain reload. Editor only, does not affect builds. It also appears at the top of log captures from
before any of the recent changes, so it is not a regression.

Checked and ruled out the usual real cause: all 11 `.controller` assets were scanned for internal
`fileID` references pointing at objects that do not exist in the file. **Zero dangling references
across all of them.** If it becomes annoying, close the Animator window before letting scripts
recompile.

### Two latent bugs spotted in CountInputManager, not fixed

Someone else's file and only reachable from the test scene, so left alone. Worth passing on:

- `OnDisable` calls `_playerInputs.Enable()` where it almost certainly means `Disable()`. Input
  stays live after the component is switched off.
- `OnJumpPerformed` does `obj.ReadValue<bool>()`, but `Transform` is a **Button** action, whose
  value type is float. `ReadValue<bool>` on it throws `InvalidOperationException` at runtime. It
  has not surfaced because `Count.prefab` is only in `CountTestScene`.
