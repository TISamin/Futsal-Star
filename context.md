# Wall Ball Futsal - Project Context

**Target:** Unity 6000.x LTS, WebGL (itch.io)
**Genre:** Fast-paced, arcade-style 2.5D 5-a-side indoor futsal.
**Core Hook:** All 4 walls are bouncy (no throw-ins or corners), physics-based possession, and a unique "walk to jockey" defensive system.

## Visuals & Engine
- **Full 3D Project:** The court, ball, and physics live in full 3D space using `Rigidbody`, `SphereCollider`, and `BoxCollider`.
- **2.5D Sprites:** All 10 players are 2D flat sprites rendered on quads that always rotate to face the camera (billboarding).
- **Camera:** Fixed, zoomed-out, top-down angled perspective. The entire court is always visible.

## Controls
- **WASD:** 8-directional movement.
- **Left Shift (Hold):** Sprint. Disables defensive jockeying.
- **Space:** Power Shoot (straight).
- **L:** Short Pass (ground kick).
- **I:** Lob Pass (vertical arc, lands based purely on physics, no auto-targeting).
- **J:** Manual Player Switch override (forces recalculation of nearest-to-ball).

## Core Mechanics
- **No Tackle Button (Jockey System):** Defense is positional. If you *sprint* into the ball carrier, nothing happens. If you *walk* into them, you auto-jockey, stripping the ball which becomes a physics-based loose ball.
- **Interceptions:** Standing in the path of a pass wins the ball. Standing in the path of a shot causes a violent ball recoil.
- **Control Assignment (Continuous Nearest):** The human only controls one player. Control *automatically* and *continuously* snaps to the teammate nearest to the ball on every event (possession change, wall bounce, loose ball). 
- **Control Edge Cases:** 
  1. *Proximity Buffer:* Control doesn't yank mid-chase if two players converge on a loose ball.
  2. *Hysteresis:* Prevents rapid control flickering between equidistant players.
  3. *AI Takeover:* Instant state evaluation when a player reverts to AI.

## AI System (5-State Machine)
The AI (controlling the other 9 players) is designed to be highly competitive and smart, but using a simplified 5-state structure to avoid bugs:
1. **GoToFormation:** Fall back to the 1-2-1 diamond shape. The diamond dynamically shifts based on the ball's position. When in position, players actively shuffle to block passing lanes.
2. **ChaseBall:** Sprint to claim a loose ball.
3. **DribbleAndAttack:** Ball carrier moves to goal. Uses a **Scored Passing** function (evaluates distance, openness, and raycasts for blocked lanes). If all lanes are blocked (score < -100f), transitions to HoldBall.
4. **HoldBall:** Dribble laterally/slow down. Wait for a lane to open rather than forcing a turnover.
5. **Support:** Off-ball teammates move to open space. Features **continuous anti-clustering** baked into the movement vector to prevent teammates from standing on top of each other.
- **GK Priority Rule:** The Goalkeeper always sprints back to the goal if they wander too far, regardless of who has possession.

## Current Project Status
- **Phase 1** of the Implementation Plan is currently active.
- **Files Created:**
  - `idea.md`: Comprehensive game design document.
  - `.gemini/antigravity-ide/brain/<ID>/implementation_plan.md`: The 7-phase build plan.
  - `Assets/Scripts/BallPhysics.cs`: Created. Handles 3D Rigidbody physics, speed limits, ground vs. air custom drag, and projection of a ball shadow.
  - `Assets/Scripts/GoalDetector.cs`: Created. Detects goals and fires static events.
- **Next Steps:** The user needs to create the Unity project, set up the BouncyWall Physic Material, build the basic Court out of cubes/planes, configure the Main Camera, and attach the scripts to test the ball bouncing around. Once done, the project will move to Phase 2 (Player Movement).
