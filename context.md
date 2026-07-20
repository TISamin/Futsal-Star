# Wall Ball Futsal - Project Context & Status

This document provides a complete technical summary of the project. A new chat window can read this file to get immediately caught up on the architecture, visual setup, completed work, and pending features.

---

## 1. Project Specifications
- **Target Platform:** WebGL (itch.io)
- **Unity Version:** Unity 6 (6000.x LTS)
- **Genre:** Fast-paced, physics-based 2.5D 5-a-side indoor futsal.
- **Core Mechanics:** 
  - Bouncy walls (no throw-ins, out of bounds, or corners).
  - Physics-based dribbling and collision-based ball possession.
  - "Walk-to-jockey" defensive system (no slide tackles; walking into the ball carrier triggers a tackle, sprinting does not).
  - Continuous Nearest-to-Ball player control switching (human control snaps automatically, with hysteresis and buffers to prevent yanking/flickering).

---

## 2. Codebase Architecture

The project has a singleton-heavy structure that separates match rules, player spawning, control systems, and AI:

1. **[MatchManager.cs](file:///d:/Projects/nexproject/My%20project/Assets/Scripts/MatchManager.cs)**: Drives the overall game flow state machine (`Warmup` -> `Playing` -> `GoalReset` -> `Finished`), score tracking, and match clock.
2. **[TeamManager.cs](file:///d:/Projects/nexproject/My%20project/Assets/Scripts/TeamManager.cs)**: Spawns the 10 players programmatically (1 GK, 1 DF, 2 MF, 1 FW per team) using cylinders as placeholder visuals and attaches controllers. Exposes the static cached `AllPlayers` array.
3. **[PlayerBase.cs](file:///d:/Projects/nexproject/My%20project/Assets/Scripts/PlayerBase.cs)**: Core movement velocity and kicking configurations (Shoot, Pass, Lob).
4. **[PlayerController.cs](file:///d:/Projects/nexproject/My%20project/Assets/Scripts/PlayerController.cs)**: Translates human input into player movements and kick actions.
5. **[AIController.cs](file:///d:/Projects/nexproject/My%20project/Assets/Scripts/AIController.cs)**: Drives AI behaviors via a 5-state machine (`GoToFormation`, `ChaseBall`, `DribbleAndAttack`, `HoldBall`, `Support`) and anti-clustering avoidance vectors.
6. **[ControlAssignment.cs](file:///d:/Projects/nexproject/My%20project/Assets/Scripts/ControlAssignment.cs)**: Continuously evaluates which team-member is closest to the ball, switching human control to them, and controls the floating overhead green indicator.
7. **[BallPossession.cs](file:///d:/Projects/nexproject/My%20project/Assets/Scripts/BallPossession.cs)**: Tracks which player has possession, locking the ball to the player's front facing offset, and manages proximity pickups.
8. **[BallPhysics.cs](file:///d:/Projects/nexproject/My%20project/Assets/Scripts/BallPhysics.cs)**: Handles customized drag (ground vs. air), speed clamps, and project custom shadow projections.
9. **[UIManager.cs](file:///d:/Projects/nexproject/My%20project/Assets/Scripts/UIManager.cs)**: Updates the score overlay, match timer, warmup countdown, and handles the animated goal overlay.

---

## 3. Work Accomplished (Phases 1-7 Completed)

The full development lifecycle from `implementation.md` is completed:

- **Performance & WebGL Optimizations:** 
  - Created `WebGLOptimizer.cs` to lock target frame rate to 60 FPS and turn off VSync.
  - Eliminated garbage allocations by caching all spawned players in `TeamManager.AllPlayers`, replacing update-loop `FindObjectsByType` calls in `AIController.cs`, `ControlAssignment.cs`, and `BallPossession.cs`.
  - Configured WebGL default window settings in `ProjectSettings.asset` to `1280x720` (16:9 ratio).
- **Visual Polish:**
  - **Court Markings (`CourtMarkings.cs`):** Generates court lines, center circle, kick-off dot, and penalty area lines programmatically.
  - **Dynamic Styling:** Automatically finds and styles the floor ("Pitch") to deep Futsal Green and goal objects to Team Red / Team Blue.
  - **Overhead Indicator:** Renders a clean, bright green pulsing disc hovering above the active human-controlled player.
  - **Goal Text Animation:** Implements a punch-scale text bounce overlay (`1.0` -> `1.4` -> `1.0` scale) whenever a goal is scored.
  - **Ball Trail (`BallTrail.cs`):** Renders a white fading trail behind the ball when shot/passed above 15 units/s. Subscribes to Goal Events to clear the trail, preventing visual streaks across the pitch when resetting positions.
- **Template & Publishing Settings:**
  - Placed a custom itch.io WebGL template inside `Assets/WebGLTemplates/WallBallFutsal/` with HTML progress bars and specific `codeUrl` fixes for Unity 6 compatibility.
- **Git Integration:**
  - Configured a custom Unity `.gitignore` to skip `Library/` and `Temp/` folders.
  - Initialized repo and pushed code to GitHub: `https://github.com/TISamin/Futsal-Star.git`.

---

## 4. Pending / Proposed Feature: Skippable Cutscene & Looping Music

To fulfill the request for an introductory skippable MP4 cutscene and background music looping seamlessly across scenes:

### The WebGL Autoplay Challenge
Web browsers block video/audio from playing automatically unless a user interacts with the page first. To bypass this, we introduce a new flow:
`Startup` (user clicks to proceed) → `Cutscene` (video plays, can be skipped) → `MainMenu` (looping music starts) → `Match` (looping music continues).

### Proposed Implementation Plan:
1. **Asset Organization:**
   - **`Assets/StreamingAssets/`**: Create this folder and place `cutscene.mp4` here (Unity WebGL requires this directory to stream videos).
   - **`Assets/Audio/`**: Create this folder and place `music.mp3` here.
2. **`Startup` Scene:**
   - A basic starting scene displaying "Click anywhere to Start". 
   - A simple click loads the `Cutscene` scene and unlocks browser audio/video restrictions.
3. **`Cutscene` Scene:**
   - Add a `VideoPlayer` rendering to the Main Camera.
   - Attach **`CutsceneController.cs`**:
     - Plays the MP4 from streaming assets.
     - Listens for video completion (`loopPointReached` event).
     - Listens for `Space` or `Escape` keys to skip the video.
     - Transitions to the `MainMenu` scene once finished or skipped.
4. **Persistent `AudioManager.cs`:**
   - A persistent script (using `DontDestroyOnLoad`) that manages a looping `AudioSource` playing the background music.
   - Initialized when `MainMenu` loads, playing the soundtrack seamlessly through menu navigation and matches.
5. **Build Scene Order:**
   - `Scene 0`: `Assets/Scenes/Startup.unity`
   - `Scene 1`: `Assets/Scenes/Cutscene.unity`
   - `Scene 2`: `Assets/Scenes/MainMenu.unity`
   - `Scene 3`: `Assets/Scenes/Match.unity`
