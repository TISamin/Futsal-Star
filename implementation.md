# Wall Ball Futsal — Implementation Plan

Build a 2.5D 5-a-side futsal game in Unity with bouncy walls, physics-based possession, jockey defense, and deploy to itch.io as a WebGL browser game. Full design spec in [idea.md](file:///d:/Projects/nexproject/idea.md).

## Decisions (Resolved)

| Decision | Choice |
|----------|--------|
| **Unity Version** | Unity 6000.x LTS (Unity 6 LTS) |
| **Project Type** | Full 3D project with 2D sprites as billboarded quads |
| **Physics Engine** | 3D physics (Rigidbody, SphereCollider, CapsuleCollider) |
| **AI Teammates** | Player controls 1 player at a time; AI controls the other 4 teammates |
| **Lob Pass** | Pure physics arc — launches in facing direction, lands wherever physics takes it |
| **Goal Celebration** | Quick "GOAL!" text flash (~1.5s) + instant ball reset to center |
| **Score Display** | Always visible at the top of the screen during gameplay |
| **AI Complexity** | 5-state machine with scored passing, bank passing, anti-clustering, GK priority override |
| **Player Switching** | Continuous nearest-to-ball auto-assignment with proximity buffer, hysteresis, and J as manual refresh |
| **Ball Carrier Penalty**| 15% speed reduction (0.85x multiplier) for the player holding the ball |
| **Boring Ball Safety**  | 4-second timeout forces nearest player on each team to sprint and chase the ball |

> [!NOTE]
> **Art Assets:** For v1, we will use **placeholder sprites** (colored shapes with team colors and directional indicators). You can replace these with proper pixel art or illustrated sprites later without changing any code.

> [!IMPORTANT]
> **Unity Beginner Workflow:** The user has never used Unity before. Therefore, for **every phase** that requires Unity Editor tasks (creating objects, assigning scripts, tagging things), the AI must provide **exact, step-by-step instructions** (e.g., "Right-click in the Hierarchy, select 3D Object -> Plane", "Click Add Component and search for `BallPhysics`"). Do not assume the user knows how to use the Inspector, Prefabs, or PhysicMaterials.

---

## Proposed Changes

We will build this in **7 phases**, each producing a playable milestone. All code is C# scripts inside the Unity project.

---

### Phase 1 — Project Setup & Court

Set up the Unity project (3D Core template), build the court geometry, configure the camera, and get the ball bouncing off walls.

#### Unity Project (3D Core Template)
- Located at: `d:\Projects\nexproject\My project`
- Target platform: WebGL
- Unity 6000.x LTS

#### `My project/Assets/Prefabs/Court.prefab`
- A flat green **Plane** for the pitch floor
- **4 wall colliders** (BoxCollider) with a **PhysicMaterial** set to high bounciness (~0.85) and zero friction
- **2 goal openings** — gaps in the short-side walls with trigger colliders behind them to detect goals
- Court dimensions roughly **42 x 24 Unity units** (1.5x larger than original) with **2x wider goals** (±3.0 Z)
- Visual wall meshes (thin boxes) so the walls are visible to the player
- Visual wall meshes (thin boxes) so the walls are visible to the player

#### `My project/Assets/Scripts/BallPhysics.cs` ✅ Created
- Rigidbody (3D) with SphereCollider, Continuous collision detection
- Caps max speed to prevent tunneling through walls
- `Kick(Vector3 direction, float power)` — ground-level hard kick
- `Pass(Vector3 direction, float power)` — ground-level softer kick
- `Lob(Vector3 direction, float horizontalPower, float verticalPower)` — adds real Y-axis velocity for vertical arcs
- Ball shadow: dark circle projected on the ground, shrinks as ball gains height
- Custom drag: higher on ground, lower in air
- Boring ball timer: checks if the ball has gone untouched for 4 seconds, triggering the safety sprint override.

#### `My project/Assets/Scripts/GoalDetector.cs` ✅ Created
- OnTriggerEnter on goal trigger zones (box triggers behind the goal openings)
- Fires a static `OnGoalScored` event with the scoring team ID
- Identifies scoring team based on `defendingTeamId` configuration

#### Camera Setup (Main Camera in Scene)
- Position: centered above the court, angled ~60° downward
- Projection: **Perspective** (gives depth to the 2.5D look — sprites will billboard)
- Field of view tuned so the entire court fits on screen with a small margin

**Milestone:** A ball spawns at center, you can click to kick it, and it bounces off all 4 walls. Lob kicks arc vertically and land. Scoring through the goal opening triggers a reset.

---

### Phase 2 — Player Controller & Movement ✅ Completed

Get a single controllable player moving around the court with 8-directional input, sprinting, and sprite billboarding.

#### `My project/Assets/Scripts/PlayerBase.cs` ✅ Created
- Base component shared by all players (human and AI)
- Holds: team ID, player role (GK/Defender/Midfielder/Forward), facing direction, walk/sprint speed
- Exposes `isSprinting` and `HasBall` properties (with 15% speed penalty logic)
- `MoveTo(Vector3 direction, bool sprint)` method
- Reference to Rigidbody and CapsuleCollider

#### `My project/Assets/Scripts/PlayerController.cs` ✅ Created
- Reads WASD input → 8-directional movement on the XZ plane
- Walk speed (~5 units/s) and Sprint speed (~8 units/s) when Left Shift held
- Rigidbody-based movement (no CharacterController) for physics interactions
- Stores `facingDirection` (one of 8 directions) based on last movement input
- Cannot walk outside court bounds (walls block naturally)

#### `My project/Assets/Scripts/SpriteBillboard.cs` ✅ Created
- Attached to every player's sprite quad child object
- Every LateUpdate, rotates the quad to face the camera
- Selects correct sprite frame based on `facingDirection` (8 frames)

**Milestone:** A single player moves around the court in 8 directions, sprints with Shift, and always faces the camera as a 2D sprite.

---

### Phase 3 — Ball Possession & Kicking

Connect the player to the ball. Implement possession, the directional arrow, shooting, short pass, and long pass.

#### [NEW] `My project/Assets/Scripts/BallPossession.cs`
- Singleton/manager that tracks which player (if any) currently "has" the ball
- When a player's collider touches the ball and no one else has possession → that player gains possession
- While possessed, the ball follows slightly in front of the player (dribbling offset in the facing direction)
- Fires C# events (`OnPossessionGained`, `OnPossessionLost`) for UI and AI to react to
- `ReleaseBall(Vector3 impulse)` to dislodge ball with physics

#### [MODIFY] `My project/Assets/Scripts/PlayerController.cs`
- Add input handling for:
  - `Space` → Shoot: opens Aiming Window (Red indicator). A/D selects near/far post (slightly inside posts). Shots execute instantly at max power upon release.
  - `L` → Short Pass: opens Aiming Window (Yellow indicator). WASD selects direction. Executes instantly at max power upon release.
  - `I` → Lob Pass: opens Aiming Window (Cyan indicator). WASD selects direction. Executes instantly at max power upon release.
  - `K` → Auto-Sprint to Ball: forces the active player to sprint directly to the loose ball.
- Directional arrow: Indicator changes color based on aim state. Player maintains pre-aim momentum while aiming.
#### [MODIFY] `My project/Assets/Scripts/BallPhysics.cs`
- Add drag/friction when ball is rolling on the ground so it eventually slows down

**Milestone:** The player picks up the ball by walking into it, sees a directional arrow, and can shoot / short pass / lob pass. The ball bounces off walls after being kicked. Lob passes arc realistically and land based on physics.

---

### Phase 4 — Defense: Jockey, Interception & Player Switching

Implement the defensive mechanics: auto-jockey when walking, shot interception with recoil, and the full player switching/control assignment system.

#### [NEW] `My project/Assets/Scripts/JockeySystem.cs`
- Attached to all players (human and AI)
- When a player is **walking (not sprinting)** and enters proximity of the ball carrier:
  - Ball is dislodged: `BallPossession.ReleaseBall()`
  - Ball becomes a loose ball — NOT given to either team
- When a player is **sprinting**, proximity with the ball carrier has **no jockey effect**

#### [NEW] `My project/Assets/Scripts/BallInterception.cs` (Merged into BallPossession)
- Detects when a non-possessing player intercepts a moving ball:
  - **Pass interception** (speed < 45): intercepting player automatically claims possession via proximity snap.
  - **Shot interception** (speed > 45): no proximity claim; ball **recoils** with strong physical bounce-back impulse upon contact.

#### [NEW] `My project/Assets/Scripts/ControlAssignment.cs`
- Central manager for the **continuous nearest-to-ball** control assignment system
- **Auto-assignment triggers:**
  - Ball changes possession (pass received, interception, jockey strip)
  - Ball hits a wall (recoil/bounce)
  - Ball becomes loose
  - Every ~0.5 seconds while defending (drift recalculation)
- **On offense:** Control auto-transfers to whoever receives/possesses the ball (zero-delay instant transition). AI is locked out from auto-passing.
- **On defense / loose ball:** Control auto-switches to nearest teammate to the ball
- **Loose Ball Proximity Buffer:** Does NOT transfer control if the previously controlled player is also within touching distance of the ball (prevents mid-chase yanking on 50/50 balls)
- **Hysteresis Margin (~1 unit):** Auto-switch only fires if the new candidate is closer by at least ~1 unit. Prevents flickering between near-equidistant teammates
- **Manual override (`J` key):** Forces immediate recalculation. Purely a "refresh" — no cycling, no second-nearest. 0.3s cooldown. Defense-only; silently ignored on offense
- **AI Takeover:** On switch, old player immediately evaluates its AI state on that frame — no idle gap

**Milestone:** Walk into the ball carrier to strip the ball (physics-based loose ball). Intercept a shot and see the ball recoil. Control auto-switches to nearest teammate. J forces a refresh. No flickering on 50/50 balls.

---

### Phase 5 — Team Setup & Smart AI

Populate both teams with 5 players each. Implement the **5-state AI** for all AI-controlled players (teammates and opponents).

#### [NEW] `My project/Assets/Scripts/TeamManager.cs`
- Spawns 5 players per team in formation positions (1 GK + 4 outfield in a 1-2-1 diamond)
- Tracks which player is currently user-controlled (integrates with `ControlAssignment.cs`)
- Assigns team colors and roles

#### [NEW] `My project/Assets/Scripts/AIController.cs`
**5-state machine** driving all AI players:

| State | When Active | Behavior |
|-------|-------------|----------|
| `GoToFormation` | Ball is far from this player | Return to assigned position in 1-2-1 diamond |
| `ChaseBall` | Closest to a loose ball | Sprint toward the ball to claim possession |
| `DribbleAndAttack` | Has the ball | Move toward opponent's goal; evaluate shoot or pass via scoring function. If all lanes blocked → `HoldBall` |
| `HoldBall` | Has ball, all lanes blocked, not in shooting range | Dribble laterally / slow down. Re-evaluate every frame until a lane opens or shooting range reached. If backward lane is open, choose backward pass over holding indefinitely |
| `Support` | Teammate has the ball | Move forward into open space. **Continuous anti-clustering** baked into movement vector every frame |

**State transitions:** Re-evaluated every ~0.2s (not every frame, for performance). Driven by ball position, possession state, distance to ball, distance to goal.

**Anti-clustering in Support:** Each frame, each player in `Support` adds a repulsion vector away from any teammate within ~3 units, proportional to proximity. This is part of the movement calculation, not a post-hoc correction.

#### [NEW] `My project/Assets/Scripts/AIPassingEvaluator.cs`
**Scored passing function** — evaluates all teammates, picks the best:
- **Proximity to opponent's goal** (closer = higher score)
- **Openness** — distance to nearest defender (more open = higher score)
- **Backward penalty** — backward passes are penalized, but not disqualified.
- **Lane clearness** — one raycast per candidate; direct blocked lane = checks for **bank pass** off side walls. If both direct and bank are blocked = disqualified.

**Critical rule:** If the best score is below -100f (all options completely blocked), the AI does **NOT** pass. It transitions to `HoldBall` and waits.

#### [NEW] `My project/Assets/Scripts/AIShootingEvaluator.cs`
**Shot evaluation** — when in shooting range:
- Distance to goal
- Angle to goal (near post vs far post)
- Goalkeeper position — aim at the side the GK is NOT covering
- Number of defenders in the direct line

#### [NEW] `My project/Assets/Scripts/FormationData.cs`
- Defines home positions for each player slot (GK, Left-Back, Right-Back, Left-Forward, Right-Forward) in the 1-2-1 diamond
- Players return toward these positions when ball is far

#### Goalkeeper AI (Priority Override in `AIController.cs`)
**Hard override rule:**
- IF goalkeeper does NOT have possession AND is further than ~3 units from goalpost vicinity **AND the ball is not loose in the goal vicinity (5 units)** → SPRINT back to goal immediately.
- If the ball is loose in the goal area, the GK is allowed to transition to `ChaseBall`.
- When near the goal:
  - Moves laterally to position themselves in the lateral third (top, middle, or bottom) that the ball is at
  - Reacts to shots by moving to block with body
  - Never ventures past penalty area equivalent unless personally dribbling

#### Defensive AI (in `AIController.cs`)
- **First defender (nearest to ball carrier):** Closes down at walking speed (auto-jockey active). Does not sprint recklessly.
- **Remaining defenders:** Fall back toward own goal, maintain shape, cover passing lanes.

**Milestone:** Full 5v5 match plays out. You control one red team player, control auto-switches properly. AI plays smart: evaluates passes, holds ball when blocked, doesn't cluster, GK stays home. Goals scored by both sides.

---

### Phase 6 — Match Manager, HUD & Menus

Build the game flow: main menu, scoreboard, timer, goal events, and match-end screen.

#### [NEW] `My project/Assets/Scripts/MatchManager.cs`
- Countdown timer (default 3 minutes, customizable from menu)
- Tracks score for both teams
- Handles kick-off sequence after each goal (ball at center, conceding team starts)
- Fires `MatchEnd` event when timer reaches 0
- Subscribes to `GoalDetector.OnGoalScored`

#### [NEW] `My project/Assets/Scripts/UIManager.cs`
- **HUD (always visible during gameplay):**
  - Score: `RED 0 - 0 BLUE` at **top center** of screen
  - Timer: countdown `MM:SS` directly below the score
  - Clean, minimal font — readable at a glance
- **Goal overlay:** Quick "GOAL!" text flash on screen for ~1.5 seconds, then instant reset
- **Full time overlay:** Final score, "RED WINS" / "BLUE WINS" / "DRAW", and buttons for "Play Again" / "Main Menu"

#### [NEW] `My project/Scenes/MainMenu.unity`
- Title: "Wall Ball Futsal"
- "Play" button → loads match scene
- Match duration input field (default: 3 minutes, customizable)
- Simple controls reference panel showing the keybindings

#### [NEW] `My project/Scenes/Match.unity`
- The main gameplay scene with court, players, ball, HUD canvas

**Milestone:** Complete game loop. Main menu → Play → 3-minute match with always-visible scoreboard → Quick "GOAL!" text on score → Full time screen → Play Again or Main Menu.

---

### Phase 7 — Polish & WebGL Deployment

Final polish pass, WebGL build settings, and upload to itch.io.

#### Performance & WebGL Optimization
- Set target frame rate to 60 FPS
- Compress textures for WebGL
- Minimize draw calls (sprite atlasing)
- Test in browser (Chrome, Firefox)
- Set WebGL Player Settings: resolution, WebGL template, compression (Brotli)
- AI state evaluation throttled to every ~0.2s (not every frame)

#### Visual Polish
- Court markings (center circle, penalty areas) as a texture on the floor plane
- Ball trail / motion blur effect (optional particle trail behind fast-moving ball)
- **Player highlight circle** under the user-controlled player (so you always know who you're controlling)
- Subtle screen flash or text animation on goal

#### [NEW] `My project/Assets/WebGLTemplates/` (optional)
- Custom HTML template for itch.io embed with proper sizing

#### itch.io Upload
- Build: `File → Build Settings → WebGL → Build`
- Zip the output folder
- Upload to itch.io as an HTML/WebGL game
- Set viewport size to match the game resolution (e.g., 960×540 or 1280×720)

**Milestone:** Game is live on itch.io and playable in a web browser.

---

## File Summary

| File | Phase | Purpose |
|------|-------|---------|
| `BallPhysics.cs` | 1 ✅ | Ball rigidbody, bouncing, kick/pass/lob methods, shadow, boring-ball timer |
| `GoalDetector.cs` | 1 ✅ | Detect goals via trigger zones, fire events |
| `PlayerBase.cs` | 2 ✅ | Shared player component (team, role, movement, facing, carrier penalty) |
| `PlayerController.cs` | 2 ✅ | Human input: WASD, Shift, K-key auto-sprint, Aiming Window logic |
| `SpriteBillboard.cs` | 2 ✅ | Rotate sprite quad to face camera, 8-directional frame selector |
| `ShootingHelper.cs` | 3 ✅ | Static utility for calculating goal targets, distance-based accuracy curves, and deviation |
| `BallPossession.cs` | 3 ✅ | Possession tracking, dribble offset, events, release cooldown |
| `JockeySystem.cs` | 4 | Auto-jockey ball strip on walk-contact |
| `BallInterception.cs` | 4 | Pass interception + shot recoil |
| `ControlAssignment.cs` | 4 | Continuous nearest-to-ball control, proximity buffer, hysteresis, J override |
| `TeamManager.cs` | 5 | Spawn teams, assign roles and colors |
| `AIController.cs` | 5 | 5-state machine for all AI players, GK priority override, exemption logic |
| `AIPassingEvaluator.cs` | 5 | Scored passing: lane raycasts, openness, backward rescue, bank passes |
| `AIShootingEvaluator.cs` | 5 | Shot evaluation: GK position, angle, defender count |
| `FormationData.cs` | 5 | 1-2-1 diamond home positions |
| `MatchManager.cs` | 6 | Timer, score, kick-offs, match flow |
| `UIManager.cs` | 6 | HUD (score/timer), goal text, end screen |

---

## Verification Plan

### Automated Tests
- Not applicable for Unity prototype — verification is manual/playtesting.

### Manual Verification
Each phase has a **milestone** checkpoint:

| Phase | Verification |
|-------|-------------|
| 1 | Ball bounces off all 4 walls. Lob kicks arc vertically. Scoring through goal resets ball. |
| 2 | Player moves in 8 directions, sprints, sprite always faces camera. |
| 3 | Pick up ball, see arrow, shoot/pass/lob all work. Lob lands based on physics. Wall-bounce passes work. |
| 4 | Jockey strips ball on walk-contact. Shot interception causes recoil. Control auto-switches correctly. No flicker on 50/50 balls. J refreshes. |
| 5 | 5v5 match runs. AI evaluates passes (no blocked-lane turnovers), holds ball when needed, doesn't cluster, GK stays home. Goals scored by both sides. |
| 6 | Full game loop: Menu → Match → HUD always visible → Quick "GOAL!" text → Full Time → Replay. |
| 7 | WebGL build loads in browser. Runs at 60fps. Uploaded and playable on itch.io. |

### User Playtesting
- After each phase, you (the user) will **Play** in the Unity Editor and report:
  - Does the movement feel good?
  - Is the ball physics satisfying?
  - Is the AI competitive and fun to play against?
  - Does player switching feel natural (no jarring yanks or flickers)?
  - Any bugs or weird behaviors?
