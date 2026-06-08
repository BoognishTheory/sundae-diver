# Sundae Diver — Unity Prototype (C#)

A drop-in C# port of the HTML prototype. A banana dives off a board into an ice-cream
dish; the player dodges kitchen hazards (losing chunks), grabs toppings, and lands
matching the dish's orientation. Rated 1–3 scoops.

Everything from **Playtest 01** is baked in: static (predictable) rotation, retained
obstacle knock-spin, fully decoupled horizontal movement, a banana that physically
shrinks as it loses chunks (hitbox shrinks too), auto-fail if it arrives empty, per-level
speed scaling, and the corrected landing math.

---

## Requirements

- Unity **2021.3 LTS or newer** (2022 LTS recommended).
- A **2D** project (URP-2D or Built-in both work — sprites are generated at runtime).
- No packages, no art, no audio needed to run.

---

## Setup (about 2 minutes)

1. Create a new **2D** Unity project.
2. **Important:** open `Edit > Project Settings > Player > Other Settings > Active Input
   Handling` and set it to **Both** (or **Input Manager (Old)**). The input code uses the
   legacy `Input` class; the default "Input System Package (New)" alone will throw at runtime.
3. Copy the `Assets/Scripts` folder from this package into your project's `Assets` folder.
4. In an empty scene, create an empty GameObject (`GameObject > Create Empty`) and add the
   **`SceneBootstrap`** component to it.
5. Press **Play**.

That's it — `SceneBootstrap` builds the camera, banana, board, generator, game manager,
and UI, and wires them together. For a phone build, switch the platform to Android/iOS and
set the Game view to a portrait aspect.

> Optional: create tunable assets via `Assets > Create > SundaeDiver > Game Config` and
> `…> Level`, then drag them onto `SceneBootstrap`. If you leave those empty, sensible
> defaults and the two demo levels are created at runtime.

---

## Controls

| Action | Touch | Keyboard (editor) |
|---|---|---|
| Launch the dive | Swipe down on the board | Space / Down arrow |
| Move horizontally | Drag left/right | Left / Right arrows |
| Rotate (hold) | Hold the **CCW** / **CW** buttons | Q / Z = CCW, E / X = CW |

The bottom-right corner is reserved for the rotate buttons, so drags there won't move the banana.

---

## File overview

| Script | Role |
|---|---|
| `Types.cs` | Enums (state, dish, orientation, topping, obstacle) + topping values/sizes/colors + `LevelItem`. |
| `GameConfig.cs` | ScriptableObject — every tunable value. The single place to adjust feel. |
| `LevelData.cs` | ScriptableObject — one level (dish shape, depth, difficulty, seed). Includes the two demo levels. |
| `DeterministicRng.cs` | Seeded mulberry32 RNG → reproducible levels. |
| `BananaController.cs` | Player banana: static rotation + decaying knock-spin, decoupled movement, shrink, hits, landing accuracy. |
| `CameraRig.cs` | Follows the banana on Y (the "world scrolls past" feel). |
| `LevelGenerator.cs` | Procedural layout (see below) + runtime placeholder sprites. |
| `ScoreSystem.cs` | Live + final scoring and scoop thresholds. |
| `GameManager.cs` | State machine + the fixed-order dive loop and collisions. |
| `DiveInput.cs` | Swipe/drag + hold-to-spin + keyboard. |
| `PrototypeUI.cs` | IMGUI menu/HUD/buttons/results (no Canvas needed — replace for production). |
| `SpriteFactory.cs` | Generates placeholder circle/ellipse/rounded-rect sprites at runtime. |
| `SceneBootstrap.cs` | Entry point — builds and wires the whole scene. |

The architecture is deliberately modular so each piece can be swapped (e.g., real art,
a uGUI front-end, authored level patterns) without touching the others.

---

## Procedural level generation (the guidance you asked for)

`LevelGenerator` is seeded and built around five rules that keep levels varied without
becoming repetitive or unfair:

1. **Deterministic seed.** Same seed → same level, every time. Great for testing, daily
   challenges, and shareable levels. Change `LevelData.seed` for a different layout.
2. **Difficulty + pacing curve.** A pacing function eases the intro, runs full through the
   middle, then *calms down before the dish* so the player has room to set their landing
   orientation. Gaps tighten and hazard chance rises as pacing climbs.
3. **Reachability guarantee.** The horizontal step between consecutive rows is capped
   (`MaxLateralStep`) to what the banana can physically cross, so there's always a path —
   no impossible walls.
4. **Anti-repetition.** Consecutive items avoid landing in the same lane, so you don't get
   a boring straight column.
5. **Risk / reward.** High-value toppings (cherry, fudge) are biased toward the edges /
   off the safe center line; the safe central pickups are low-value nuts. Greed costs risk.

There's also an **intro clearance** (calm launch) and **landing clearance** (empty approach
to the dish) so the start and finish always feel fair.

**Production upgrade path (recommended for the full game):** replace per-row random
placement with an authored **pattern library** — small hand-designed "beats" (e.g., a
zig-zag gauntlet, a topping arc tucked behind a hazard) stored as ScriptableObjects, each
tagged with a difficulty and which dish/orientation it suits. Assemble a level by drawing
patterns from a **shuffled bag** (every pattern appears before any repeats) along the same
pacing curve. That gives you hand-crafted feel *and* procedural variety, and it's the
single biggest lever against "mundane" levels. The current generator's hooks (lane picking,
pacing, reachability) are where that system slots in — see the comments at the top of
`LevelGenerator.cs`.

This also satisfies your **level-tree** note: `LevelData` is the per-level node, and the
dish shape it carries drives both the landing target and (later) which patterns are eligible.

---

## Scoring — and the banana-value question you raised

You asked how to weigh the **banana itself vs. collectibles vs. the landing**. Here's the
framework I'd start from (all set in `GameConfig`, so it's one place to tune):

- **Intact banana — the controllable core (`intactMax = 600`).** This is the skill of
  *surviving the descent*. It's a guaranteed, fully-controllable payout, so it should be
  the largest single fixed bucket.
- **Toppings — the variable greed reward (per-level total ≈ match the banana).** Aim for
  each level's total topping value to land roughly equal to `intactMax` (~600–700). That
  makes "play it safe and stay intact" and "take risks for toppings" two viable paths to a
  similar ceiling — which is what makes the risk/reward placement meaningful.
- **Landing — the finishing bonus (`landingMax = 500`).** Smaller than the other two, but
  large enough that a sloppy landing visibly costs you a scoop. It rewards the one moment of
  precision at the end without letting a great landing rescue a banana that got shredded.

So a clean run is roughly **banana 600 / toppings ~650 / landing 500 ≈ 1,750 max**, and the
scoop thresholds (45% / 68% / 88%) mean 3 scoops requires doing well on *all three* axes.
These are starting numbers — the right move is to play several runs and adjust `intactMax`,
the per-level topping total, and `landingMax` until the three paths feel balanced. Two things
worth confirming once you've played the Unity build:

- **Auto-fail timing.** Right now it triggers the instant the banana hits zero chunks
  (mid-air "splat"). The alternative is to only fail if it *arrives* at the dish empty.
  Immediate felt cleaner in the prototype, but it's a one-line change in `BananaController`/`GameManager`.
- **Hitbox shrink.** A smaller banana is a smaller target, so losing chunks is partly
  self-correcting. If that feels too forgiving, reduce `minScale` or decouple the hitbox
  from the visual.

---

## Tuning guide (`GameConfig`)

| Field | What it does |
|---|---|
| `fallStart / fallAccel / fallMax` | Descent speed curve (scaled per level by `speedMul`). |
| `moveSpeed / moveFriction` | Horizontal responsiveness and damping. |
| `rotateRate` | Static rotation speed (deg/sec) — the core feel of the rotate buttons. |
| `knockSpinKick / knockSpinDamp` | Strength and decay of obstacle-induced spin. |
| `maxChunks / minScale / invulnTime` | Banana health, smallest size, and i-frames. |
| `bananaRadius` | Base collision radius (scales with size). |
| `intactMax / landingMax / hitPenalty` | Scoring weights (see above). |
| `scoopThresholds` | % of level max needed for 1 / 2 / 3 scoops. |
| `orthoSize / bananaScreenYFactor` | Camera zoom and where the banana sits on screen. |

Per-level difficulty lives on `LevelData`: `speedMul`, `obstacleRate`, `gap`, `seed`.

---

## Next steps toward production

- Replace `PrototypeUI` (IMGUI) with a uGUI/TextMeshPro Canvas; `GameManager` already
  exposes everything the UI reads.
- Swap the runtime placeholder sprites for real art (banana, kitchen hazards, dishes,
  background) and add audio/particles.
- Build the authored **pattern-library** generator described above.
- Expand the **level tree** with more container shapes and orientation targets.
