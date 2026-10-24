# ElectroCooked — Educational Circuit & Calculus Game

An educational Unity prototype that mixes timed circuit-building challenges with
a school environment and calculus activities. Built as a serious-game study: the
classroom content is the gameplay, not a menu bolted onto one.

| | |
| --- | --- |
| **Engine** | Unity 6000.6.0f1 |
| **Language** | C# (runtime + editor tooling) |
| **Scope** | Educational gameplay prototype |
| **Status** | Development prototype, not a released game |
| **Live demo** | Unity project — no hosted demo |
| **Screenshots** | Pending — see [docs/CAPTURE_GUIDE.md](docs/CAPTURE_GUIDE.md) |
| **Stack** | Unity · C# |
| **License** | MIT — [LICENSE](LICENSE) |

## What it demonstrates

- Circuit orders, component pickup/carrying and workstation interactions.
- Circuit mathematics and challenge evaluation, timers and scoring.
- Fire hazards, extinguishers and feedback during laboratory activities.
- Calculus questions, answer zones and student progress records.
- Editor tools that build the school, circuit classroom and calculus classroom.

## Open the project

1. Install the editor version recorded in `ProjectSettings/ProjectVersion.txt`
   through Unity Hub.
2. Add this repository as a project and let Unity restore the packages listed in
   `Packages/manifest.json`.
3. Use **ElectroCooked → Construir TUDO (escola + aula)** in the editor to open
   or generate the demo scene. For the calculus classroom, use
   **ElectroCooked → Construir sala de cálculo**.
4. Enter Play mode.

Menus and much of the game UI are in Portuguese. Scene-building commands generate
or replace scene content, so run them on a fresh clone before customizing levels.

## Code structure

```
Assets/Scripts/   Core, Circuit, Managers, Player, Calculus, UI
Assets/Editor/    Scene-generation tools for the school and classrooms
```

The repository includes `Assets`, `Packages` and `ProjectSettings`. Unity caches,
local editor state and compiled builds are excluded and regenerate on import.

## Project status

This is a development prototype and code portfolio, not a released game. The
systems above were identified in the source; a fresh editor import, playthrough
and platform build have not been verified during this publication pass. See
[verification notes](docs/PORTFOLIO_VALIDATION.md). No performance or
release-readiness claims are made.

---

## What I would do next

- Record a short gameplay clip and the classroom activity screens following the capture guide.
- Add a playtest section with what students actually completed and where they got stuck.
- Extract the circuit simulator into a reusable component package.

## License

MIT - see [LICENSE](LICENSE). You may use, modify and redistribute this code with attribution.
