# ElectroCooked

An educational Unity prototype that combines timed circuit-building tasks with a school environment and calculus activities.

**Stack:** Unity 6000.6.0f1 · C# · editor automation

## Implemented systems

- Circuit orders, component pickup/carrying and workstation interactions.\n- Circuit mathematics and challenge evaluation, timers and scoring.\n- Fire hazards, extinguishers and feedback during laboratory activities.\n- Calculus questions, answer zones and student progress records.\n- Editor tools that build the school, circuit classroom and calculus classroom.

## Open the project

1. Install the editor version recorded in `ProjectSettings/ProjectVersion.txt` through Unity Hub.
2. Add this repository as a project and let Unity restore the packages in `Packages/manifest.json`.
3. Use **ElectroCooked → Construir TUDO (escola + aula)** in the editor if you need to open or generate the demo scene.
4. Enter Play mode.

For the calculus classroom, use **ElectroCooked → Construir sala de cálculo**. The menus and much of the game UI use Portuguese.

Scene-building commands generate or replace scene content; use them on a fresh clone before customizing the levels.

## Code structure

Assets/Scripts/Core, Circuit, Managers, Player, Calculus and UI; scene-generation tools are in Assets/Editor.

The repository includes Assets, Packages and ProjectSettings. Unity caches, local editor state and compiled builds are excluded; let the editor regenerate them.

## Project status

This is a development prototype and code portfolio, not a released game. The systems above were identified in the source; a fresh editor import, playthrough and platform build have not been verified during this publication pass. See [verification notes](docs/PORTFOLIO_VALIDATION.md). No performance or release-readiness claims are made.
