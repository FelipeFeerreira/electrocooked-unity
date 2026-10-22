# Capture guide — screenshots and GIF

This is an educational game, so the screenshot has to show the teaching loop, not
just a classroom. Show the task, the interaction and the feedback.

## Setup

1. Open the project in the editor version recorded in
   `ProjectSettings/ProjectVersion.txt`.
2. Run **ElectroCooked → Construir TUDO (escola + aula)** and the calculus room
   with **ElectroCooked → Construir sala de cálculo**.
3. Set the Game view to **16:9**, 1600×900, gizmos off.
4. Put the UI in Portuguese, since that is the intended audience.

## Required shots

| File | Frame |
| --- | --- |
| `docs/screenshots/01-circuit-order.png` | Circuit order panel with the required components and the timer running |
| `docs/screenshots/02-workstation.png` | Player carrying a component to the workstation |
| `docs/screenshots/03-calculus.png` | Calculus question with the answer zone and progress record |
| `docs/screenshots/04-hazard.png` | Fire hazard and extinguisher feedback in the laboratory |

## GIF (10–12 s)

- Sequence: pick up a component → mount it → score an order → a fire starts and is
  put out.
- 1280×720, 15 fps, under 5 MB. Save as `docs/screenshots/loop.gif`.

## Paste into the README

Right after the intro paragraph:

```markdown
![ElectroCooked gameplay loop](docs/screenshots/loop.gif)

| Circuit order | Workstation | Calculus | Hazard |
| --- | --- | --- | --- |
| ![](docs/screenshots/01-circuit-order.png) | ![](docs/screenshots/02-workstation.png) | ![](docs/screenshots/03-calculus.png) | ![](docs/screenshots/04-hazard.png) |
```

## Honesty rule

These are classroom prototypes with fictional student data. If the seeded progress
records are illustrative, say it in the caption, the same way the other projects
label their demo data.
