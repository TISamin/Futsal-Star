# Wall Ball Futsal — Game Design Document

## Overview

**Wall Ball Futsal** is a fast-paced, arcade-style **2.5D 5-a-side indoor futsal game** built in **Unity** and published to **itch.io** as a **WebGL browser game**. The defining mechanic is that **all walls are bouncy** — there are no throw-ins, corners, or goal kicks. The ball ricochets off walls like a billiard ball, enabling creative angle passes and trick shots.

---

## Core Concept

- **5v5 futsal** on a small indoor court.
- **Bouncy walls** replace all dead-ball situations. The ball never goes out of play.
- **Goalkeepers cannot use their hands.** They are just outfield players positioned near the goal — they block with their body only.
- **No tackling.** Defenders use a **jockey** system (body-blocking) to dislodge the ball based on physics.
- **Highest score wins** at the end of a customizable timer (default: 3 minutes).
- The game targets a **single player vs AI** experience (with potential for local 2-player later).

---

## Visual Style & Perspective

- **Full 3D project with billboarded 2D sprites:** The court, walls, and ball exist in 3D space using Unity's 3D physics (Rigidbody, SphereCollider, CapsuleCollider). **All players are 2D flat sprites rendered on quads** that always rotate to face the camera (billboard-style). This gives the 2.5D aesthetic while allowing the ball to have real vertical arcs for lob passes.
- **Fixed camera:** A single, zoomed-out camera shows the **entire court at all times**. No camera movement, no scrolling. The player can see all 10 players, the ball, and the walls at a glance.
- **Top-down angled view:** The camera is positioned above and slightly behind/above center court, angled downward to give depth while keeping the full pitch visible.

---

## Court & Environment

- A rectangular indoor futsal court.
- **Walls on all four sides** — the ball bounces off them with realistic angle reflections.
- **Two goals** — one on each short side of the court, embedded in the wall. The ball passes through the goal opening to score.
- No out-of-bounds. No corners. No throw-ins. No goal kicks. Play is continuous.
- After a goal is scored, the ball resets to the center for a kick-off by the conceding team.

---

## Players

- **10 players total:** 5 per team (including the goalkeeper).
- **Goalkeeper:** Positioned near the goal but has **no special hand-save ability**. They block with their body only, just like any outfield player.
- Players are represented as **2D sprites** — simple colored team uniforms (e.g., Red Team vs Blue Team) with directional animation frames (facing up, down, left, right, diagonals).
- Each player has a small **circular collider** at their feet for physics interactions with the ball and other players.

---

## Controls (Keyboard)

| Action             | Key(s)                              | Notes                                                        |
|--------------------|-------------------------------------|--------------------------------------------------------------|
| **Move**           | `W` / `A` / `S` / `D`              | 8-directional movement (including diagonals)                 |
| **Sprint**         | `Left Shift` (hold)                | Faster movement; **disables auto-jockey** while sprinting    |
| **Shoot**          | `Space` (hold & release)           | Opens **Aiming Window** (indicator turns **Red**). WASD aims at Near/Far post. Indicator stretches as power charges up. Distance-based accuracy. |
| **Short Pass**     | `L` (hold & release)               | Opens **Aiming Window** (indicator turns **Yellow**). WASD sets pass direction. Indicator stretches as power charges up. |
| **Lob Pass**       | `I` (hold & release)               | Opens **Aiming Window** (indicator turns **Cyan**). WASD sets lob direction. (Fixed power, no charge scaling). |
| **Switch Player**  | `J`                                | Manual override: jumps to nearest teammate to the ball (defense only). |
| **Auto-Chase Ball**| `K` (hold)                         | Auto-sprints the currently controlled player directly toward the ball, regardless of offense/defense. |

> **Layout rationale:** Left hand on WASD + Shift for movement, right hand on I / J / K / L / Space for actions.

---

## Ball Physics

- The ball is a **physics-driven 3D object** (Unity Rigidbody with SphereCollider). It exists in full 3D space, allowing real vertical arcs for lob passes.
- **Wall bounces:** The ball reflects off walls at realistic angles (angle of incidence = angle of reflection), with slight energy loss per bounce.
- **Passing & Aiming Window:** When `Space`, `L`, or `I` is held, a brief **Aiming Window** (~0.5s) opens. The player carries their movement momentum, but WASD now controls the aim direction instead of movement.
- **Colored Power Indicator:** While aiming, an indicator stretches out to show charge level (power):
  - **Red** = Shooting (`Space`)
  - **Yellow** = Passing (`L`)
  - **Cyan** = Lobbing (`I`)
- **Shooting Accuracy:** Shots automatically target the opponent's goal. `A` and `D` aim slightly inside the near or far post to avoid hitting the woodwork. Shots are always 100% accurate to the targeted slot and execute at maximum power instantly upon release (no charge up).
- **Lob passes:** The ball is launched in an arc in the aimed direction with real vertical (Y-axis) velocity. It lands wherever physics takes it — there is no auto-targeting to a teammate.
- **Loose ball:** The ball is a free physics object. It can be picked up by any player who touches it. There is no "magnetic" attraction — possession is purely based on collision.

---

## Possession System

- **Gaining possession:** A player gains possession automatically when the ball enters their immediate proximity. It acts as a slight snap to make receiving passes fluid and reliable.
- **Losing possession (Jockey):** When the controlled player is **walking (not sprinting)**, they are in **auto-jockey mode**. If they enter the proximity of an opponent who has the ball, the ball is **dislodged** from the attacker. The ball flies off based on physics, becoming a loose ball.
- **Losing possession (Interception):** If a defender enters the path of a pass, they will claim it via proximity. For extremely fast **shots** (speed > 45), there is no proximity claim; instead, the ball will physically bounce off any player in its path.
- **Losing possession (Missed pass):** If a pass does not reach a teammate, any player can collect the loose ball via proximity.

---

## Jockey Mechanic (Defense)

- **Auto-jockey** is active whenever the player is **not sprinting** (i.e., walking at normal speed).
- While in jockey mode and bumping into the ball carrier, the ball is knocked loose.
- While **sprinting**, the player moves faster but **cannot jockey**. Bumping into the ball carrier while sprinting has no effect (or a weaker effect).
- **No slide tackles, no standing tackles, no steal button.** Defense is entirely positional — you must walk into the attacker to strip the ball.

---

## Player Switching & Control Assignment

The human player controls **one player at a time**. Control assignment follows a **continuous nearest-to-ball rule** with manual override and edge-case protections.

### Automatic Control Assignment (Continuous Rule)

- **Human control = the teammate nearest to the ball, recalculated on every relevant event.**
- Events that trigger recalculation:
  - Ball changes possession (pass received, interception, jockey strip)
  - Ball hits a wall (recoil/bounce)
  - Ball becomes loose
  - Every ~0.5 seconds while defending (drift recalculation)
- **On offense (your team has the ball):** Control auto-transfers to whoever **receives/possesses** the ball. If you pass to a teammate, you become that teammate instantly the millisecond they touch the ball (zero-delay transition), and their AI logic is immediately locked out to prevent auto-passing.
- **On defense (opponent has the ball):** Control auto-switches to the nearest teammate to the ball. As the opponent dribbles past your midfielder, control naturally slides to your defender.
- **On loose ball:** Control auto-switches to the nearest teammate to the ball, **subject to the proximity buffer** (see below).

### Edge Case: Loose Ball Proximity Buffer

When a teammate picks up a loose ball, control does **NOT** transfer if the previously controlled player is also within touching distance of the ball. This prevents jarring mid-chase control yanking when two teammates are both converging on a 50/50 ball and one grazes it a frame before the other. Control only transfers if the new player is **unambiguously** the one who has it (i.e., the old player is far away).

### Edge Case: Hysteresis Margin (Anti-Flicker)

Auto-switch only fires if the new candidate is closer to the ball by a **minimum threshold (~1 unit)**, not just marginally closer. This prevents control flickering back and forth when the ball is sitting between two near-equidistant teammates and nudging slightly with each physics step.

### Manual Override (`J` Key)

- `J` forces an **immediate recalculation** of nearest-to-ball. It is purely a "refresh" — it does not cycle through a list or jump to second-nearest.
- On defense, this is mostly a no-op (since auto-switch is already continuous), but it provides a responsive feel if the player wants to confirm they're on the right teammate.
- **Cooldown:** ~0.3 seconds between presses to prevent spam.
- **Only works on defense** (when opponent has the ball or ball is loose). Silently ignored while your team has possession.

### AI Takeover on Switch

- When control leaves a player (via auto-switch or manual `J`), the old player **immediately evaluates its AI state** on that frame — no visible "idle" gap. It transitions directly into whichever AI state is appropriate (Jockey, GoToFormation, Support, etc.).

---

## Match Rules

| Rule                  | Value                        |
|-----------------------|------------------------------|
| **Match duration**    | 3 minutes (customizable)    |
| **Win condition**     | Highest score at full time   |
| **Draw**              | Allowed (no extra time/pens) |
| **Kick-off**          | After each goal, conceding team kicks off from center |
| **Fouls**             | None                         |
| **Cards**             | None                         |
| **Offsides**          | None                         |
| **Substitutions**     | None                         |

---

## AI Behavior (All AI-Controlled Players)

The player controls **one player at a time** on their team (see **Player Switching** section). The other **4 teammates are AI-controlled**, as are all **5 opponents**. The AI should be **as smart and competitive as realistically possible** — it should feel like playing against a skilled team, not bots. Teammate AI behaves identically to opponent AI in quality.

### 5-State Machine

| State | When Active | Behavior |
|-------|-------------|----------|
| `GoToFormation` | Ball is far from this player | Return to assigned position in 1-2-1 diamond |
| `ChaseBall` | This player is closest to a loose ball | Sprint toward the ball to claim possession |
| `DribbleAndAttack` | This player has the ball | Move toward opponent's goal; evaluate shoot or pass using **scoring function** (see below). If all pass lanes blocked → transition to `HoldBall` |
| `HoldBall` | Has ball, all pass lanes are blocked, not in shooting range | Dribble laterally / slow down. Re-evaluate every frame until a lane opens or shooting range is reached. Prevents forced turnovers into covered lanes. |
| `Support` | A teammate has the ball | Move forward into open space to offer a passing option. **Continuous anti-clustering** is baked into the movement vector every frame (repulsion from nearby teammates within ~3 units). |

### Scored Passing (AI Decision-Making)

The AI does **not** simply pass to the nearest teammate. It evaluates all teammates with a **scoring function**:

- **Proximity to opponent's goal** (closer = higher score)
- **Openness** — distance to nearest defender (more open = higher score)
- **Backward penalty** — backward passes are heavily penalized
- **Lane clearness** — one raycast per candidate; if a defender blocks the lane, the candidate is **disqualified** (score = -999)

The AI picks the teammate with the highest score. **Critical rule:** If the best score is still below a floor threshold (all lanes blocked), the AI does **NOT** pass. It transitions to `HoldBall` state and waits for lanes to open. This prevents AI turnovers that look like bugs.

### Shooting Evaluation

When in shooting range, the AI evaluates:
- Distance to goal
- Angle to goal (near post vs far post)
- Goalkeeper position — aim at the side the GK is **not** covering
- Number of defenders in the direct line

### Defensive AI Behavior

- **First defender (nearest to ball carrier):** Closes down at **walking speed** (auto-jockey active) to pressure and strip the ball. Does not sprint recklessly.
- **Remaining defenders:** Fall back toward their own goal, maintain shape, and cover passing lanes.

### Anti-Clustering (Continuous)

Anti-clustering is **not** a one-time entry check. It runs **every frame** as part of the `Support` state's movement calculation. Each player in `Support` adds a repulsion vector away from any teammate within ~3 units, proportional to how close they are. This prevents two players from converging on the same spot when both are trying to "move forward."

### Goalkeeper AI (Priority Override)

The goalkeeper has a **hard priority rule** that overrides all other AI states:

> **IF** the goalkeeper does **NOT** have possession of the ball **AND** is further than ~3 units from the goalpost vicinity → **SPRINT back to goal immediately.** This fires regardless of which team has possession.

When near the goal:
- Moves laterally to position themselves in the lateral third (top, middle, or bottom) that the ball is at
- Reacts to shots by moving to block with body
- Never ventures past the penalty area equivalent unless personally dribbling the ball

---

## Game Flow

1. **Main Menu** → Start Match (with optional timer setting).
2. **Kick-off** → Ball at center, conceding/home team starts.
3. **Continuous play** → Ball bounces off walls, never stops unless a goal is scored.
4. **Goal scored** → Quick "GOAL!" text flash on screen (~1.5 seconds), ball instantly resets to center, conceding team kicks off. No slow-motion or extended celebration.
5. **Full time** → Final score displayed, option to play again or return to menu.

---

## Target Platform

- **Primary:** WebGL build uploaded to **itch.io** (playable in browser).
- **Engine:** Unity (C#).
- **Input:** Keyboard only (no mouse, no gamepad — at least for v1).

---

## Future Considerations (Not in v1)

- Local 2-player (Player 2 uses Arrow Keys + numpad).
- Team selection / jersey color picker.
- Multiple AI difficulty levels.
- Sound effects and music.
- Animated 2D sprite sheets (run cycles, kick animations).
- Tournament / season mode.
