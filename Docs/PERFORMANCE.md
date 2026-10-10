# Performance

The goal: a puzzle game that holds 60 FPS on mid-range Android, keeps memory flat during a session
and does not wake the garbage collector while the player is playing.

> **Where the numbers come from.** Everything below was measured in the Unity Editor (Windows, DX12)
> with the tools in this repo. Editor numbers are useful for counting draw calls and allocations, but
> they are not device numbers. Frame time on a real phone still has to be measured with a
> development build and the Profiler.

## Measured

| What | Result | How |
|---|---|---|
| Rules layer allocations | **0 bytes** over 500 turns on a 10×10 / 6-color board | `AllocationTests` (Unity Test Framework `AllocatingGCMemory` constraint) |
| View layer allocations | **0 bytes on idle frames**, about **1 KB per turn** (coroutine + tween bookkeeping) | `DebugPerfProbe`, 28 bot turns, level 20 |
| Render events per frame | **5** for a full 10×10 board + HUD (world = one SRP batch, HUD = 3 UI batches) | Frame Debugger |
| Tile instances | **128**, created once; unchanged after 20 levels of automated play | `TileViewPool.CountAll` in the soak test |
| Rules test suite | 61 tests in ~0.15 s | EditMode tests |
| Main thread / render thread / GPU | ~1.5 ms / ~0.7 ms / ~1.6 ms per frame (desktop, editor) | `FrameTimingManager` |

How to reproduce: enter Play Mode, then `BlastPuzzle > Tools > Soak Test > Loop Last Level (bot)` and
`… > Log Report`.

## What was done, and why

### CPU
- **Rules are plain C#** on a flat `Tile[]` (2-byte structs). A full turn — flood fill, booster chain,
  gravity, refill, deadlock check — is a few passes over at most 100 cells.
- **One animator loop.** `TileAnimator` updates every moving tile from a single `Update` over a struct
  array. No per-tile `MonoBehaviour.Update`, no per-tile tween object.
- **One input handler.** A tap is converted to a cell with arithmetic; there are no colliders, no
  physics queries and no per-tile buttons. Physics simulation is switched off entirely.
- **Dirty-checked UI.** HUD texts are only assigned when the value changed; sprites are only assigned
  when they differ.

### Memory / GC
- **Pooling.** Tile views are pre-warmed; effects reuse a fixed set of sprites; goal fly-ins reuse 12 images.
- **No per-turn collections.** `TurnResult` is reused and its lists are sized for the largest board, so
  they never grow. Flood fill uses stamped `int[]` buffers instead of clearing or allocating.
- **No string garbage** for counters (`NumberStrings`).
- **Tween recycling** (`DOTween.Init(recycleAllByDefault: true)`), and completion callbacks are cached
  instead of allocating a closure per effect.
- **Incremental GC** enabled, so the little garbage that remains is collected in slices.
- **Audio:** short effects are ADPCM / decompress-on-load, the music loop stays Vorbis-compressed in
  memory. Everything is mono.

### GPU
- **Two sprite atlases** (world, UI) and **one unlit sprite material**: the board batches into a single
  SRP batch. No 2D lights, no post-processing, no HDR, no MSAA, no depth or opaque texture.
- **Particles:** one shared `ParticleSystem` per effect type fed with `Emit()`.
- **Overdraw:** tiles use tight sprite meshes; new tiles fade in at the board edge instead of using a
  `SpriteMask` (stencil state changes break batching).
- **UI:** three canvases (menu, HUD, popups) so a changing number never rebuilds unrelated geometry;
  hidden screens are deactivated; `raycastTarget` is off on everything that is not clickable;
  text shares one outline material.
- **ASTC 6×6** for atlases on Android.

### Build / platform
- IL2CPP + ARM64, managed stripping, engine code stripping.
- Portrait only; `Screen.safeArea` handled for both the UI (`SafeArea`) and the board (`BoardLayout`).
- `Application.targetFrameRate = 60` (`vSyncCount = 0`), frame pacing enabled on Android.
- WebGL: gzip with decompression fallback (works on itch.io).

## Known costs / next steps
- About 1 KB of garbage per turn remains (starting the turn coroutine, a few DOTween tweens for HUD
  punches and fly-ins). Replacing the coroutine with a small state machine would remove most of it.
- Input is locked while a turn plays back. Accepting taps during falls would need per-cell locking.
- Not yet profiled on a device; texture memory and frame time should be checked on a low-end Android phone.
