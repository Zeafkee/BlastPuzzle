# Architecture

BlastPuzzle is split into a pure C# rules layer and a Unity presentation layer. The rules decide, the
view shows. Nothing in the rules layer references `UnityEngine`.

```
┌──────────────────────────── Unity (BlastPuzzle.Runtime) ─────────────────────────────┐
│  AppFlow ── menu ⇄ level, popups, save                                               │
│  GameController ── tap → model → playback → outcome                                  │
│      │                         ▲                                                     │
│      │ Tap(pos)                │ TurnResult (what happened, in order)                │
│      ▼                         │                                                     │
│  ┌──────────────── Core (BlastPuzzle.Core, noEngineReferences) ─────────────────┐    │
│  │  BlastGame ─ Board ─ GroupFinder ─ GravityResolver ─ Shuffler ─ TierRules    │    │
│  └──────────────────────────────────────────────────────────────────────────────┘    │
│  BoardView ── TileViewPool ── TileAnimator ── FxPlayer        HudView, popups        │
└──────────────────────────────────────────────────────────────────────────────────────┘
```

## Why a model / view split

- **Testable.** 60 EditMode tests cover the rules in ~0.2 s, without Play Mode. Deadlocks, chain
  reactions and win/lose are tested as plain function calls.
- **Deterministic.** All randomness goes through `IRandomSource`. Same seed, same game. That makes bugs
  reproducible and lets a bot play thousands of games to tune levels.
- **Cheap.** A tile is a 2-byte struct in a flat array. The model allocates nothing during a turn.
- **Reusable.** Swap-based match-3 would be a new rule on top of the same board, gravity and shuffle.

## A turn, step by step

```
tap ──► GameController.TapCell
          │  BlastGame.Tap(pos, result)        ← the whole turn is resolved here, instantly
          │     ├─ group blast  or  booster activation (+ chain reaction queue)
          │     ├─ GravityResolver.Collapse → Refill
          │     ├─ deadlock? → Shuffler.Shuffle
          │     └─ goals / moves / outcome
          │  input locked
          ▼
        BoardView.PlayTurn(result)             ← purely visual, cannot change the outcome
          ├─ steps: pop / merge / rocket / bomb / disco
          ├─ booster appears
          ├─ falls + spawns (TileAnimator)
          └─ shuffle animation, tier icons refreshed
          │  input unlocked, or result popup
```

The model is always ahead of the screen. The view never asks "what should happen now?"; it replays a
`TurnResult`: lists of cleared tiles, moves and spawns, grouped into steps. `TurnResult` is one object
that is cleared and reused every turn.

## Algorithms

### Group finding — `GroupFinder`
Iterative flood fill over flat indices with an explicit `int[]` stack.

- **No recursion:** no call overhead, no stack-depth risk.
- **Stamp instead of clear:** `visited[i] == currentStamp` means "seen in this search". A new search is
  `currentStamp++`, not an O(n) array clear.
- **Row-edge guard:** in a flat array `index - 1` at `x == 0` is the last cell of the row below; the
  neighbour checks test `x` explicitly (there is a unit test for exactly this).
- Cost: O(group size) for one group, O(cells) to label every group (`ComputeGroupSizes`, used for
  the tier icons), because every cell is filled exactly once.

### Deadlock detection — `GroupFinder.HasAnyGroup`
The smallest group is two equal neighbours, so no flood fill is needed: compare every cell with its right
and upper neighbour. Each adjacent pair is checked once, O(cells), exits on the first hit. A board with a
booster on it is never dead (`HasAnyMove`).

### Gravity and refill — `GravityResolver`
Per column, two pointers walking bottom to top: `write` is the lowest free cell, `read` looks for the
next tile. In place, O(height) per column. Refill stacks new tiles above the board (`StartY = height,
height + 1, …`) so a column of new tiles falls as one stack instead of overlapping.

### Shuffle — `Shuffler`
The requirement was: no "shuffle until it happens to work".

1. One Fisher–Yates permutation of the colored blocks (boxes and boosters stay put).
2. If that leaves no group, **force** one: pick a random pair of adjacent cells and swap blocks of one
   color into them. Swaps keep the color counts exactly as they were.
3. Only if no color exists twice (e.g. 2×2 with four colors) is one block recolored.

Result: guaranteed solvable in a single O(cells) pass, and the shuffle reports its moves as a permutation
so the view can animate every block to its new cell.

### Boosters and chain reactions — `BlastGame`
A group larger than threshold A/B/C leaves a rocket / bomb / disco ball at the tapped cell. Activating a
booster walks its area; boosters it reaches are pushed onto a FIFO queue (an `int[]`, stamped so nothing
is queued twice) and go off in the same turn. Boxes take one hit per step, tracked with another stamp.

## View layer decisions

| Decision | Why |
|---|---|
| `TileAnimator`: one `Update` over a struct array, not one tween per tile | A refill moves up to 100 tiles. Structs in an array = no allocation, real acceleration, one cache-friendly loop. DOTween is kept for one-off UI/effect tweens. |
| `TileViewPool`, pre-warmed | No `Instantiate`/`Destroy` during play. |
| One shared `ParticleSystem` per effect, fed with `Emit()` | One draw call for all shards on screen. |
| Tiles positioned in the board's local space | The whole board can shake without touching a tile. |
| New tiles fade in at the top edge instead of a `SpriteMask` | A mask costs stencil state changes and breaks batching. |
| Single scene, screens are panels | Menu ⇄ level is instant; no scene load, no loading screen. |
| Three canvases (menu, HUD, popups) | A changing HUD number does not rebuild popup or menu geometry. |
| `NumberStrings` cache | Counter updates do not allocate strings. |
| One tap handler converting screen → cell | No collider or button per tile. |

## Data

- `LevelData` (ScriptableObject): size, colors, moves, goals, optional text layout. `LevelCatalog` orders them.
- `TileTheme`: tile → sprite mapping; reskinning is a data change.
- `LevelDefinitions` (editor) holds the shipped levels as code and generates the assets, so level changes
  show up as readable diffs.

## Level tuning

`AutoPlayer` is a goal-aware bot (Core, so it runs anywhere). `BlastPuzzle > Tools > Simulate Levels`
plays every level 300 times at three skill settings. Move counts were chosen so a mid-skill bot wins
about 90 % of the first levels, falling to about 65 % at level 20.

## Reproducible setup

`BlastPuzzle > Setup > Build Everything` regenerates atlases, theme, levels, prefabs and the scene from
code. `Tools/generate_sprites.py` and `Tools/generate_audio.py` regenerate all art and audio.
