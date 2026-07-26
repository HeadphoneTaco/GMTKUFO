# Debug notes, Sat 2026-07-25

Findings from a day of debugging Drac's animation, ground detection, and victim biting.
Untracked on purpose so it survives a `git reset --hard`. It does NOT survive `git clean -fd`,
which is how the first copy of this file was lost. Delete once the jam ships.

---

# OUTSTANDING WORK

Everything below this section is history and reference. This is the live list.

## Before a playtest build

**Would confuse a player**

- [ ] **No score on either end screen.** `EndScreenController` is on both `[UI]EndScreenA` and
      `[UI]EndScreenB` with `_scoreFormat` already set, but `_scoreText` is `{fileID: 0}` and
      neither prefab contains a TMP text object. Add one under `Canvas`, assign it. Use
      `{0:N0}` in the format string, blood is uncapped and reaches four figures.
- [ ] **`DeathSpikeFence` and `ChainlinkFence` have no `Obstacle`,** so they are decoration. A
      spike fence that does not hurt reads as a bug. Add `Obstacle` and a damage value. They then
      join the `LevelPopulator` hazard pool automatically, since it filters by component.

**Would look broken**

- [ ] **Remove `BiteDebug` and `LocomotionDebug`** from `Player_Drac` and ideally from the project.
      Both log every frame something changes.
- [ ] **Bat flight has no speed clamp.** `PSBat.Execute` does
      `linearVelocity += FlySpeed * Time.deltaTime * input` with gravity off and no cap, so one
      full bat meter is about 90 units of climb at 60 u/s. `FollowCamera._maxVerticalLag` keeps him
      on screen, but he can fly clean over the level. Clamp the velocity in `PSBat`.

**Decide with the team**

- [ ] **`EndScreenA` will always show a score of 0.** Death is defined as blood reaching zero, so
      the banked score on the death screen is 0 by construction. It needs a different number to be
      worth showing: distance reached, victims drained, or time survived. None are tracked yet.

## Next feature work

- [ ] **Lengthen the level.** `LevelPopulator` already handles distribution, so this is mostly
      authoring more walkway. See the placement section below for the current runs and gaps.
- [ ] **Kangaroo character.** 61 clips are imported and unused. `kangaroo_aggressive_mock_charge_01`
      played once on first detection, before the chase starts, would sell the roo far better than
      trot straight to sprint. One extra controller and a timer in `HazardAI`.
- [ ] **`KangarooDeath.controller` wraps `kangaroo_hit_chest_front_01`,** a flinch, not a death.
      `kangaroo_dead_pose_lft_01` and `kangaroo_idle_injured_to_dead_reaction_01` are available. The
      roo is hazard-only now, so this controller may just be dead weight.
- [ ] **`PauseController` guards all three of its calls on `GameManager.Exists`.** That works today
      only because the HUD touches `Instance` first and creates the manager. See the rule below.

---

# HOW THIS PROJECT BITES

Seven things cost real time on 2026-07-25. All of them recur.

1. **A prefab holding values for fields the script no longer declares is completely silent.** Unity
   ignores them. This happened four times: `BoxCastHalf`, `MaxHealth`, `_healSpeed`,
   `_endSceneName`. If a value looks set in the YAML but has no effect, check the script still
   declares that field.

2. **An unassigned Inspector reference fails silently, and looks exactly like broken logic.** The
   blood text, the clock, `_scoreText`, `_groundLayers`. Before debugging behaviour, confirm every
   serialized reference is actually assigned.

3. **Use `GameManager.Instance`, never test `GameManager.Exists` first.** The manager only lives in
   `MainMenu.unity`. Playing the game scene directly leaves `Exists` false forever, because it only
   becomes true once something touches `Instance` and triggers the lazy creation. `Exists` is only
   safe where doing nothing is an acceptable outcome.

4. **Resolve the full parent chain, including rotation, before trusting any position read out of a
   scene file.** `=====GROUND=====` is rotated 180 degrees about Y. Reading tile positions straight
   from the YAML gives coordinates mirrored in X and Z, which once produced a confident report of a
   68 unit hole in the level that did not exist.

5. **Measure at runtime rather than computing from asset files.** The NPC collider was reported as a
   half-buried sphere from prefab numbers, because the transform scale was not accounted for. A
   `Debug.Log` of `collider.bounds` settled it in one Play session.

6. **Before calling a readout broken, work out how fast it should be changing.** The sunrise clock
   looked frozen. It was moving at 0.28 percent per second against a 360 second night. A slow
   correct readout and a dead one look identical.

7. **Commit working changes immediately.** Uncommitted recovery work evaporated twice in one day,
   once to a `git clean` that also destroyed the first copy of this file.

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

---

## Clock switched from radial fill to hands

### Why the fill looked frozen

Not a wiring fault. `ClockSprite`'s Image was already correct: Type **Filled**, Fill Method
Radial 360, clockwise. The problem was rate. `_sunriseDuration` on the GameManager in
`MainMenu.unity` is **360** seconds, so `NightProgress` moves 1/360 per second:

| readout | rate | after 10 seconds |
|---|---|---|
| radial fill | 0.28% per second | 2.8% |
| hour hand, 1 turn per night | 1.00 deg/sec | 10 degrees |
| minute hand, 6 turns per night | 6.00 deg/sec | 60 degrees |

A 2.8% change on a small circle over a ten second test is below the threshold of noticing. The
other candidate was `_clockFill` simply being unassigned, which fails silently, but the config was
right either way.

Lesson worth keeping: **before calling a readout broken, work out how fast it should be changing.**
A slow correct readout and a dead one look identical.

### Now driven by rotation

`_clockFill` and `_clockFillsTowardSunrise` are gone. `HUDController` now rotates hands:

- `_hourHand` with `_hourHandTurns`, default 1, one full turn across the night
- `_minuteHand` with `_minuteHandTurns`, default 6, optional, leave empty for a one hand clock
- `_handStartAngle` for where the hands sit at dusk
- `_handsRunClockwise`, on by default. Positive Z is counter clockwise in Unity, so the sweep is
  negated when this is on.

### Inspector work

There is no hand sprite in the project and `ClockSprite` has no children, so the hands need making.

No art needed. The project has no arrow sprite, and Unity's only built-in one is `DropdownArrow`,
a squat downward triangle that reads badly as a hand. An Image with **Source Image set to None**
renders as a plain white rectangle, which is all a clock hand is.

- [ ] Add a child Image under `ClockSprite` for the hour hand
- [ ] Leave Source Image as **None**, set Color to something dark
- [ ] Width about 4, height about 30, tuned to the clock face
- [ ] **Set its pivot to the end that stays still**, `(0.5, 0)` at bottom centre. With the default
      `(0.5, 0.5)` pivot the hand spins about its own middle instead of about the clock centre,
      which looks like it is orbiting.
- [ ] Set anchored position to `(0, 0)` so the pivot lands on the clock centre
- [ ] Leave `_handStartAngle` at 0. A bottom pivoted rectangle already points up.
      If `DropdownArrow` is used instead, it points down, so set `_handStartAngle` to 180.
- [ ] Assign it to `_hourHand`
- [ ] Repeat for `_minuteHand` only if a second hand is wanted
- [ ] At `_sunriseDuration` 360, the hour hand alone moves 1 degree per second. If that still reads
      as static while testing, raise `_hourHandTurns` rather than assuming it is broken.

---

## End screens, rising sun, shadow detection

Decisions: `EndScreenA` is death, `EndScreenB` is reaching the coffin, the sun is a **spotlight that
rises into position**, and shade is detected by **raycasting toward the light** rather than with
trigger volumes.

### The end screens were unreachable

`_endSceneName` was `"EndScreen"`. No scene by that name exists. The scenes are `EndScreenA` and
`EndScreenB`, so `SceneManager.LoadScene` was always going to fail. `EndScreenB` was also missing
from build settings entirely.

Both end scenes already contain their `[UI]` prefab with `EndScreenController` on it, so the
screens themselves were fine. Only the routing was broken.

Now: `RunOutcome` enum in `GameState.cs`, `EndRun(RunOutcome)` picks between `_deathSceneName`
(`EndScreenA`) and `_coffinSceneName` (`EndScreenB`). The old parameterless `EndRun()` still exists
and means death, so existing callers and UnityEvent hookups keep working. `LastOutcome` is exposed
if the end screen ever wants to vary its text.

`EndScreenB` added to `EditorBuildSettings.asset`. Both end screens are now in the build.

`_endSceneName: EndScreen` is still serialized on the GameManager in `MainMenu.unity` and is now
dead YAML. The two replacements have initializers, so they work without touching the Inspector.
Fourth time this pattern has come up.

### Coffin

`Coffin.cs`. Walking in ends the run as `ReachedCoffin`.

- `_endsRun` **off for the coffin the player starts in**, or the run ends on frame one because the
  player is already inside the trigger
- `_requireExitFirst` is extra insurance for the end coffin, it must be left once before it fires
- `_fired` guards a second trigger firing while the scene load is already underway

Only `Dracula_Coffin.fbx` exists. There is no coffin prefab and none in `Game.unity`, so this needs
building before it can be tested.

### Sun

`SunController.cs`. Position and rotation lerp between two marker Transforms, driven by
`NightProgress` so it stays in step with the clock whatever `_sunriseDuration` is set to. Intensity
ramps from `_duskIntensity` to `_sunriseIntensity`, with an optional colour gradient and an
optional aim target that keeps the spotlight pointed at the player as it climbs. `_riseCurve`
shapes the climb, so an ease in keeps it down until late.

A gizmo draws the rise path when the object is selected, so it can be placed without entering play.

### Shadow detection

`ShadowSensor.cs`, on the player. Casts toward the sun on a `_checkInterval`, default 0.1s, and
smooths the result so the drain rate does not flicker when passing railings or posts. Reports to
`SetShadowAmount`, which until now was dead code with no callers anywhere.

**Layer warning.** All five building prefabs sit on layer 0 Default, the same layer as everything
else. Leaving `_occluderLayers` as Everything means victims and hazards count as shade. Layer 8 and
up are free, so a `Structures` layer for the buildings is worth adding and pointing the mask at.

### Inspector checklist

- [ ] Make a coffin prefab from `Dracula_Coffin.fbx`, add a collider with **Is Trigger on**, add
      `Coffin`
- [ ] Place the end coffin with `_endsRun` on, the start coffin with it off
- [ ] Add a Spotlight, plus two empty GameObjects for `_riseFrom` (below the horizon) and `_riseTo`
      (fully up), then add `SunController` and assign all three
- [ ] Set `_sunriseIntensity` well above the existing moonlight, which is 0.08
- [ ] Enable shadows on the spotlight, or the buildings will not cast anything to hide in
- [ ] Add `ShadowSensor` to `Player_Drac`
- [ ] Add a `Structures` layer, move the buildings onto it, set `_occluderLayers` to it
- [ ] Test both endings: blood to zero should give YOU DIED, the coffin should give GAME OVER

---

## Planned: much longer level with programmatic placement

The current `Game.unity` is a short test strip. It holds **one** NPC instance, `Character_01`, and
a handful of hazards, against a 360 second night. That is nowhere near enough content.

The plan is to make the level much longer and place hazards and victims **programmatically** rather
than by hand.

Worth deciding when that work starts:

- Spawn along a spline or a straight run of segment prefabs
- Seeded random so a run can be reproduced when something goes wrong, which matters given how many
  bugs this project has traced back to placement rather than code
- Minimum spacing rules so hazards do not stack into an unavoidable wall, and victims are not
  buried inside geometry
- Density curve across the level, easing up early and tightening toward the coffin
- Victims need layer 7 and a collider on the same object as `Victim`, which is what the current 31
  prefabs already do correctly, so spawning those prefabs directly keeps that intact
- Hazards need `Obstacle` and a solid, non-trigger collider

The existing 31 `Character_*` prefabs and the three hazard prefabs are the spawn pool. Nothing new
needs authoring, they just need distributing.

---

## Running animation broke a second time, different cause

Not the ground layer this time. All 15 ground prefabs and all 37 scene instances were still on
layer 6, `_groundLayerName` was still `Ground`, and `TagManager.asset` was unchanged.

**The ground ray had stopped reaching the floor.**

| | value |
|---|---|
| capsule height | 2.1533 |
| capsule center.y | -0.13665 |
| collider bottom, below origin | 1.2133 |
| `_groundCheckOffset` 0.9 + `_groundCheckDistance` 0.3 | 1.2000 |
| margin | **-0.0133** |

A negative margin means the ray ended 1.3cm **inside** the capsule, above the floor, so
`IsGrounded()` could only ever return true when the capsule penetrated the ground by more than
that. Unity's default contact offset is 0.01, so it was passing by a fraction of a millimetre.
Anything that changed how the player settled tipped it into permanent false, `PSWalk` kicked to
`PSFalling`, and the falling animation stuck. Exactly the symptom from this morning, reached by a
completely different route.

The margin went negative when the capsule was extended, commits `3be28f7` and `80262c9`. The note
recommending `_groundCheckOffset` 1.05 was written this morning and never applied.

`_groundCheckOffset` is now **1.05**, margin **+0.1367**, roughly ten times the old figure. The ray
still starts inside the capsule, so it cannot clip through thin ground.

### The underlying problem is a magic number

`_groundCheckOffset` has to be kept in sync with the capsule by hand, and nothing enforces it or
warns when it drifts. Changing the collider silently breaks ground detection, which then presents
as an animation bug several layers away from the cause. That has now cost time twice in one day.

Deriving the offset from the collider at Awake would end this permanently:

```
_groundCheckOffset = -(capsule.center.y - capsule.height / 2f) - 0.15f;
```

Not done, because retuning ground detection mid jam is how afternoons disappear. Worth doing before
the level gets rebuilt longer.

---

## Ground check made self sizing, after breaking a third time

The capsule changed again between the previous note and this one:

| | earlier today | now |
|---|---|---|
| `m_Height` | 2.1533 | 1.9830631 |
| `m_Center.y` | -0.13665 | -0.051531494 |
| collider bottom below origin | 1.2133 | **1.0431** |

`_groundCheckOffset` 1.05, correct for the old capsule, puts the ray **origin 0.007 below the feet**
against the new one. A ray that starts under the floor cannot detect the floor, so the fix made
things worse, not better.

### Two ways to get this wrong, and a narrow window between

```
offset too small  -> ray stops INSIDE the capsule, above the floor, never hits
offset too large  -> ray starts BELOW the feet, under the floor, never hits
```

For the current capsule the whole valid range for offset is roughly **0.74 to 1.04**. Miss it in
either direction and `IsGrounded` is false forever, `PSWalk` kicks to `PSFalling`, and it surfaces
as "the running animation is broken", which points nowhere near the collider that actually changed.

Three separate breakages in one day, three different root causes: wrong layer, ray too short, ray
starting under the floor.

### Now derived from the capsule

`PlayerController.ConfigureGroundCheck()` runs at Awake and computes both values:

```
bottom   = -(capsule.center.y - capsule.height / 2) * lossyScale.y
offset   = bottom - _groundRayInset      // ray starts this far INSIDE the capsule
distance = _groundRayInset + _groundRayReach
```

Defaults: inset 0.15, reach 0.15. Against the current capsule that gives offset 0.8931, distance
0.3, reaching 0.15 below the feet. The two old fields are still serialized and are simply
overwritten at Awake, so nothing else had to change.

`_autoSizeGroundCheck` can be switched off to go back to hand-typed values. There is a warning if
the capsule is missing or the inset is larger than the capsule allows.

**Resizing the capsule no longer breaks ground detection.** This is worth keeping when the level
gets rebuilt longer, since more slopes and seams mean more chances for a hand-tuned margin to fail.

### If it is still broken after this

`LocomotionDebug.cs` is on hand. Drop it on `Player_Drac` and it reports, per Play session: the
resolved ground mask, the capsule bottom, the computed margin, every state change, every animator
controller swap, and on each ground-state change what is really underneath and whether it is on a
layer the mask accepts. It distinguishes "ray too short", "wrong layer" and "state machine never
reached Walk", which look identical from outside.

---

## The real cause: the player was standing next to the ground, not on it

Three rounds of ground-ray tuning were chasing the wrong thing. The ray was fine. There was simply
no ground underneath it.

**Player was at Z 9.3. The walkway collider spans Z 9.52 to 11.52.** Off the edge by **0.221**, and
`RigidbodyConstraints.FreezePositionZ` means he could never fall onto it.

### Why this was hard to see

`=====GROUND=====` is rotated **180 degrees about Y** (`m_LocalRotation: {y: 1, w: 0}`). Reading the
tiles' `m_LocalPosition` straight out of the YAML gives coordinates that are mirrored in X and Z
from where the geometry actually is. A first pass that ignored the rotation put the ground at
Z -4.82 and suggested a 68 unit hole in the level that does not exist.

**Resolve the full parent chain including rotation before trusting any position read out of a Unity
scene file.** Local coordinates under a rotated parent are actively misleading, not just incomplete.

Ground tile colliders are also offset: `size (8, 1, 2)`, `center (-4, -0.5, 0)`, so a tile covers
local X from -8 to 0 rather than being centred on its own origin. Under the 180 degree parent that
becomes world X from the tile position up to +8.

### Fix

Player Z moved from **9.3 to 10.25**, in both `Game.unity` and the `Player_Drac.prefab` variant.

| candidate | on ground | nearest edge | bite overlap Z |
|---|---|---|---|
| 9.3 (was) | no | -0.220 | 0.400 |
| 10.0 | yes | +0.480 | 0.400 |
| **10.25 (chosen)** | yes | **+0.730** | **0.400** |
| 10.52 (true centre) | yes | +1.000 | 0.180 |

10.25 rather than the true centre because the NPCs sit at Z about 9.50 and `_boxCastHalf.z` is 1.0.
At the centre the NPC collider only clips the edge of the bite box. At 10.25 it is fully inside,
with the most ground margin available without giving that up.

### Worth knowing

The NPCs at Z 9.46 to 9.52 are themselves right on or just outside the walkway edge. They have no
Rigidbody so they do not fall, but anything that later needs them grounded will hit this. When the
level is rebuilt longer, put the spawn plane at the walkway centre 10.52 and keep everything on it,
rather than having the player, the NPCs and the ground each on slightly different Z values.

---

## Follow camera and procedural population

### Camera

Cinemachine is **not installed**, it is absent from `Packages/manifest.json`. `FollowCamera.cs`
stands in: side scroller follow with directional look ahead, `LateUpdate` so it moves after the
player has finished moving. Nothing else depends on it, so swapping to a Cinemachine vcam later is
delete and rebuild, not a refactor.

`Main Camera` was at the scene root and completely static. Offset defaults to `(0, 0.08, -11.61)`,
taken from where the camera already sat relative to the player, with the horizontal lead coming
from look ahead instead of a baked-in X offset.

Notable settings: `_followY` is off by default so hops do not lurch the frame, `_speedDeadzone`
stops the lead jittering while stationary, and `_holdLeadWhenStopped` keeps the lead pointing the
way you were going rather than recentring.

### Population

`LevelPopulator.cs` scatters victims and hazards at Start, probing straight down at each candidate
slot and skipping any that has no ground. **This matters because the walkway is not continuous:**

| run | X range | length |
|---|---|---|
| player's run | -163.6 to -99.6 | 64 |
| | -91.6 to -83.6 | 8 |
| | -75.6 to 6.4 | 82 |
| | 8.4 to 16.4 | 8 |
| | 76.4 to 104.4 | 28 |
| | 120.4 to 176.4 | 56 |

Across -164 to 176 at a 4 unit step that is 86 slots, of which **61 have ground and 25 do not**. A
fixed spacing rule with no probe would have dropped roughly a third of the level into empty space.

Seeded by default, so the same layout comes back every run while pacing is being tuned. Victims are
rolled before hazards, so a slot that could take either becomes a victim.

### Hazard prefabs, watch this

`Obstacle` is on **`GatorHazard.prefab`** and **`KangarooNPC.prefab`** only. `DeathSpikeFence` and
`ChainlinkFence` do **not** have it, so spawning those places harmless scenery. Add `Obstacle` to
them, or leave them out of the hazard pool.

The hazard prefabs also moved since the earlier notes. Current paths:

```
Assets/_Project/Prefabs/Characters/GatorHazard.prefab      (was Characters/Hazards/GatorHazarad)
Assets/_Project/Prefabs/Characters/KangarooNPC.prefab      (was Characters/NPCAnimals/KangarooNPC)
```

### Setup

- [ ] Add `FollowCamera` to `Main Camera`. Target can stay empty, it finds the player.
- [ ] Create an empty GameObject, add `LevelPopulator`
- [ ] Create two CoreUtils prefab buckets: **Create > CoreUtils > Bucket > Prefab Bucket**
- [ ] Victim bucket: set its Sources to `Assets/_Project/Prefabs/Characters/NPCs`
- [ ] Hazard bucket: Sources can be as broad as `Assets/_Project/Prefabs`, the component filter
      sorts it out
- [ ] Assign both to `_victimBucket` and `_hazardBucket`
- [ ] **Set `_groundLayers` to the Ground layer only.** Left empty nothing spawns, set to Everything
      things spawn on top of each other.
- [ ] `_laneZ` is 10.25 and must match the player, who is frozen in Z
- [ ] Expect roughly 16 to 21 spawns at the default density. Raise `_victimChance` or drop `_step`
      for more.

### Buckets instead of hand-assigned arrays

`LevelPopulator` now takes two `CoreUtils.AssetBuckets.PrefabBucket` references rather than
`GameObject[]`. A bucket auto-populates from folders set in its Sources list, and the package's
`AssetBucketWatcher` keeps it up to date as prefabs are added or removed, so new victims appear in
the pool with no code or Inspector work.

`CoreUtils` is `autoReferenced` and the project has no asmdefs, so `Assembly-CSharp` reaches it
with just `using CoreUtils.AssetBuckets;`. Runtime access is `bucket.Items`, a `GameObject[]`.

**Contents are filtered by component, not trusted by folder.** Victims must carry `Victim`, hazards
must carry `Obstacle`, and the search includes children because on these prefab variants the
gameplay component usually sits on a child rather than the root. That means:

- The hazard bucket can point at the whole prefab tree. `DeathSpikeFence` and `ChainlinkFence` have
  no `Obstacle`, so they are skipped rather than spawned as scenery that does nothing.
- The victim bucket cannot accidentally spawn `Player_Drac`, since it has no `Victim`.
- Adding `Obstacle` to the fences later makes them join the hazard pool automatically.

Both filters log what they kept and what they skipped, so a prefab silently missing its component
shows up in the console rather than as a hazard that mysteriously does no damage.

### Camera vertical follow, for bat form

The camera has to track vertically or the bat leaves the screen, but plain vertical following makes
every hop lurch the frame. `FollowCamera` uses a **vertical deadzone**: it holds a focus height and
only moves it when the player leaves the band around it.

That is what lets one camera serve both forms. On foot the player stays inside the band and the
view holds still. In bat form he climbs out of it and the camera comes along.

Defaults: `_deadzoneUp` 1.5, `_deadzoneDown` 3, `_verticalSmoothTime` 0.35. Down is larger than up
so falling reads as falling. `_clampToStartHeight` stops the camera sinking into the ground when
the player drops into one of the gaps.

### Bat flight has no speed cap, and the camera has to defend against it

`PSBat.Execute` does `RB.linearVelocity += FlySpeed * Time.deltaTime * DirectionalInput` with
gravity switched off and **no clamp**. That is acceleration, not velocity, so holding a direction
compounds:

| holding up for | speed | climbed |
|---|---|---|
| 0.5s | 10 u/s | 2.6 |
| 1.0s | 20 u/s | 10.2 |
| 2.0s | 40 u/s | 40.3 |
| 3.0s, the full bat meter | **60 u/s** | **90.5** |

Smooth damping lags by roughly speed times smooth time, so at 60 u/s the camera would sit about
**21 units** behind and the bat would be long gone off the top of the screen. `_maxVerticalLag`,
default 6, hard clamps how far the camera may trail, so smoothing handles the feel and the clamp
guarantees the player stays visible.

**Worth fixing in gameplay rather than papering over in the camera:** `PSBat` should clamp its
velocity to a terminal speed. 90 units of climb on one bat meter is almost certainly not the
intent, and it will fight level design as well as the camera.

---

## Drac turns to face movement

There was no facing logic anywhere in the project. `Player_Drac` sat at a fixed 90 degree yaw, so
running left played the run animation while still facing right.

`PlayerController.UpdateFacing()`, called from `Update`:

- Driven by `DirectionalInput.x`, not velocity, so the turn starts the instant the stick moves
  rather than after the body has picked up speed
- Facing is **held** when input returns to neutral, so stopping does not snap him to a default
- `_turnSpeed` 720 degrees per second, so the 180 degree flip takes half a second. 0 snaps instantly
- `_turnInputDeadzone` 0.1 stops a stick resting slightly off centre flipping him back and forth
- `_targetYaw` is seeded from the authored rotation in `Awake`. Left at zero he would swing round
  to face the camera on the first frame

Defaults `_yawFacingRight` 90 and `_yawFacingLeft` -90. The authored quaternion
`(w 0.7071, y 0.7071)` is exactly 90 degrees, so the right-facing default matches how he is placed.

### Why rotating the root is safe here

Checked every consumer before doing it:

- `IsGrounded` casts `Vector3.down`, unaffected by yaw
- `CheckForVictims` passes `Quaternion.identity` to `OverlapBox`, so the bite volume stays axis
  aligned and does not shrink or swing as he turns
- `FollowCamera` is not parented to the player and works in world space
- `Obstacle` knockback is built from world positions and contact points
- Rigidbody rotation is frozen, so assigning `transform.rotation` directly is not fought by physics

The one knock-on: because the overlap box does not rotate, biting still reaches equally in both
directions regardless of facing. That was already logged as a known quirk and is arguably the
behaviour you want for an automatic bite.

### Note, the player prefab moved

`Player_Drac.prefab` is now at `Assets/_Project/Prefabs/Player_Drac.prefab`. It was previously under
`Prefabs/Characters/`. Several prefabs have shifted folders today, so paths in older notes may be
stale. Search by name rather than trusting a recorded path.

---

## Hazard AI

`HazardAI.cs`. Idle or patrol, notice the player by X distance, chase, give up, go home. Works on
anything with an `Obstacle`, fences included, which is the point.

Moves on **X only**, holding its authored Y and Z, matching the plane the player is frozen to.
Everything is per instance, so a croc lurks slowly while a roo bounds.

### Setup

- `GatorHazard`: `_idleMode` **Stationary**, slow `_chaseSpeed`, around 3
- `KangarooHazard`: `_idleMode` **Patrol**, `_patrolRange` 6 to 10, faster `_chaseSpeed`
- Fences, once they have an `Obstacle`: either mode. Stationary is the safer joke, a patrolling
  fence is the better one
- **Set `_groundLayers` to the Ground layer on every one.** Left empty with `_stopAtLedges` on,
  every probe fails and the hazard never moves at all. There is a warning on Start for this.

### Balance, against a walk speed of 5

| chase speed | result |
|---|---|
| 3 | loses 2 u/s, escapable on foot |
| 4, the default | loses 1 u/s, escapable on foot |
| 6 | gains 1 u/s, needs bat or mist to escape |

At `_maxChaseTime` 5 and chase speed 6, a chaser closes 5 units over a full chase. `_leashFromHome`
25 means it can never follow more than 25 units from where it spawned, so a chase cannot drag a
hazard across the whole level and leave a hole in the layout.

### Why the ledge probe matters here

The main walkway has **five gaps**. Without `_stopAtLedges` a chasing hazard walks straight into one
and is gone from the level for the rest of the run. It now stops at the lip instead, which still
reads as menacing.

`_loseRange` 18 is deliberately larger than `_sightRange` 12. Equal values make a player standing
right on the boundary flicker in and out of being chased every frame.

### Known gaps, deliberate

- **No animation.** The animals have only `AlligatorIdle` and `KangarooIdle` controllers, with no
  walk or run clip, so there is nothing to swap to. Movement will slide rather than stride until
  someone authors a locomotion clip.
- **No Rigidbody on the hazards.** Moving a collider with no Rigidbody makes Unity rebuild static
  physics data each frame. Fine at this count. If a longer level with many hazards starts to chug,
  add a Kinematic Rigidbody to each hazard prefab. Collision damage still works either way, because
  the player carries the Rigidbody that generates the contact.

### Hazard animation

`HazardAI` now swaps the Animator's controller per state, the same one-controller-per-action pattern
the player uses. It only assigns on an actual change, because reassigning
`runtimeAnimatorController` restarts the state machine, and doing it every frame would pin the clip
on frame one forever.

Fields: `_idleController`, `_patrolController`, `_chaseController`. Patrol falls back to the chase
controller if left empty, and `Returning` reuses the patrol one.

### The clips that exist

Both FBXs are richer than expected. The alligator has 2 clips, the kangaroo has **61**.

| use | clip | loops already |
|---|---|---|
| gator idle | `AmericanAlligator_Idle` | yes |
| gator chase | `AmericanAlligator_Trot_F` | **no** |
| roo idle | `kangaroo_idle_01` | yes |
| roo patrol | `kangaroo_trot_fwd_01` | yes |
| roo chase | `kangaroo_sprint_fwd_01` | **no** |
| roo alerted, optional | `kangaroo_trot_alerted_stomp` | yes |

**Tick Loop Time on `AmericanAlligator_Trot_F` and `kangaroo_sprint_fwd_01`** in the FBX import
settings. Without it the chase animation plays once and freezes on its last frame, which reads as
the hazard breaking the moment it starts chasing.

**`KangarooIdle.controller` is misnamed.** It wraps `kangaroo_trot_alerted_stomp`, not an idle. It
is a reasonable patrol or alerted controller as it stands, but it is not an idle, and using it as
one will look wrong. `AlligatorIdle.controller` genuinely does wrap the idle.

Controllers still to create: gator chase, roo idle, roo chase. One state each, matching the existing
`AlligatorIdle` and `KangarooIdle` assets.

### Watch root motion

Both animals import as **Humanoid** (`animationType: 3`), and neither hazard prefab overrides
`m_ApplyRootMotion`.

`HazardAI.Step()` moves the transform directly. If Apply Root Motion is on, the clip **also** moves
the object, so the two fight and the hazard drifts, moves at roughly double speed, or slides away
from where the AI thinks it is. **Confirm Apply Root Motion is off on both hazard Animators.** This
is the same class of problem that cost time on the player's animation earlier.
