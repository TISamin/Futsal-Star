# Wall Ball Futsal

A fast-paced, physics-based 2.5D arcade futsal game built with Unity. The game features bouncy walls, a custom "walk-to-jockey" defensive system, continuous player control assignment, and a competitive 5-state AI team.

---

## 1. Engine & Frameworks
- **Game Engine:** Unity 6 (6000.0 LTS)
- **Target Platform:** WebGL (playable in modern web browsers)
- **Rendering Pipeline:** Universal Render Pipeline (URP)
- **Input System:** Unity Input System (New Input System package)

---

## 2. Installation & Running Instructions

### Play in the Browser (Recommended)
This game is designed to run directly in your web browser. 
- You can play the published WebGL build on **itch.io** (or self-host the build folder). No installation is required.

### Run in Unity Editor (Development)
To run or edit the source project locally:
1. Install **Unity Hub** and the **Unity 6 (6000.x LTS)** editor. Make sure you install the **WebGL Build Support** module.
2. Clone the Git repository to your local machine:
   ```bash
   git clone https://github.com/TISamin/Futsal-Star.git
   ```
3. Open Unity Hub, click **Add**, select the cloned repository folder, and open the project.
4. Open the `Assets/Scenes/MainMenu.unity` scene and press the **Play** button in the editor.

---

## 3. How to Play

### Game Controls
- **Movement (8-Directional):** Use `W`, `A`, `S`, `D` or the **Arrow Keys**.
- **Sprint (Hold):** Press **Left Shift**. *(Sprinting allows you to move faster but disables defensive jockeying)*.
- **Power Shoot:** Press **Space**. *(Fires a straight, high-powered kick)*.
- **Pass (Short Ground Kick):** Press **L**.
- **Lob Pass (Air Kick):** Press **I**. *(Launches the ball in a high vertical arc based on physics, without auto-targeting)*.
- **Manual Player Switch:** Press **J**. *(Forces the system to override and pick a new active player)*.
- **Auto-Sprint Toggle:** Press **K**.

### Rules & Mechanics
- **Bouncy Walls:** The court has no out-of-bounds, throw-ins, or corners. The walls are completely bouncy; use them to bounce passes or shoot from angles!
- **Walk-to-Jockey Defense:** Defense is positional. If you *sprint* into the ball carrier, you cannot steal the ball. You must *walk* (release Shift) into the opponent to trigger a jockey move, causing them to lose possession.
- **Continuous Control Switching:** The game automatically yanks your control to the teammate closest to the ball. You don't need to manually switch players unless you want to override the system.

---

## 4. Git Repository Link
The project source code is hosted on GitHub:
- **Repository URL:** [https://github.com/TISamin/Futsal-Star.git](https://github.com/TISamin/Futsal-Star.git)
