# 🍌 Sundae Diver

> A one-thumb arcade diver. Launch a banana off the high board, thread it through a chaotic
> kitchen, and stick the landing in the dish below. The cleaner your dive, the bigger your
> sundae — rated 1–3 scoops.

Made by **TBD Studios**.

---

## Gameplay

- **Swipe down** to launch off the board.
- **Drag** to steer left/right as you fall.
- **Hold CW / CCW** to rotate the banana.
- **Dodge** kitchen hazards — each hit takes a bite out of the banana.
- **Collect** toppings on the way down.
- **Land matching the container**: lie *flat* for the long dish, go *upright* for the tall glass.
- Lose every chunk before you reach the dish and it's an auto-splat.

## The 3-scoop payoff

Your score combines three things: how intact the banana arrives, the toppings you grabbed, and
how cleanly you matched the landing orientation. Clear the thresholds and your run is rendered
as an actual sundae — on landing, an ice-cream **splat wipes the screen**, your sundae is built
from exactly what you collected, and the splat **slides off to reveal it**. *(That reveal was
Ali's idea.)*

Official collectibles per level: **3 whipped cream · 3 cherries · 2 peanuts · 2 hot fudge**.

## Status

Early prototype / vertical slice. The core loop, seeded procedural levels, scoring, and the
splat → sundae reveal are implemented with a prefab-driven art pipeline (with generated
placeholders so it always runs). Two demo levels ship: **Banana Split** (flat dish) and
**Sundae Glass** (upright glass).

## Tech

- **Unity 6** (works on 2021.3 LTS+), 2D.
- **C#.** Code-driven *kinematic* movement for a predictable arcade feel, with Unity 2D
  **triggers** for pickups/hits so real fitted colliders are the hitboxes.
- **Seeded procedural generation** — reproducible levels from a per-level seed.
- **ScriptableObject-tunable** config and levels; one place to balance feel and difficulty.

## Project layout

```
Assets/
  Scripts/    gameplay code (state machine, banana, generator, scoring, reveal, UI)
  Prefabs/    banana health states, collectibles, obstacles, dishes, sundae pieces
  ...
ProjectSettings/, Packages/   Unity project config
```

See the in-repo setup guide for the full script breakdown and the scene-wiring checklist.

## Getting started

1. Open the project in **Unity 6** (or 2021.3 LTS+).
2. `Edit > Project Settings > Player > Other Settings > Active Input Handling` = **Both**.
3. Open the main scene and press **Play**.

## Controls

| Action | Touch | Editor (keyboard) |
|---|---|---|
| Launch | Swipe down | Space / ↓ |
| Move | Drag left/right | ← / → |
| Rotate | Hold CW / CCW | E·X (CW), Q·Z (CCW) |

## Roadmap

- Replace the prototype IMGUI HUD with a uGUI / TextMeshPro front-end.
- Authored **pattern-library** level generation (shuffled "bag" of hand-made beats).
- Audio + juice (camera shake and a drip sound on the splat-in).
- Expand the level tree with more containers and orientation targets.

---

*Sundae Diver is a work in progress. Everything here is subject to change as it gets tastier.*
