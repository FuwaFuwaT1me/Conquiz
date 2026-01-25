# CLAUDE.md — Conquiz (Unity mobile quiz strategy)

## Role
You are a senior Unity 2022.3 LTS (2D / Universal 2D) engineer + technical game designer.
Help implement the game described below. Always produce small, testable increments.

## Output rules (VERY IMPORTANT)
- Keep responses small: 1–3 scripts OR 1 subsystem per answer.
- Always include:
  1) Goal of this step (1–2 lines)
  2) Files to create/modify (explicit paths)
  3) C# code (complete, compilable)
  4) Unity Editor setup steps (what GameObjects, where to attach scripts, what to assign in Inspector)
  5) Quick test checklist (how to verify it works in Play Mode)
- No external plugins. Use Unity built-in APIs + TextMeshPro for UI.
- Prefer ScriptableObjects for static data, and plain C# classes for runtime state.
- Do not generate huge monolithic systems. If needed, split into follow-up steps.
- If something is ambiguous, pick a reasonable default and state it clearly.

## Tech constraints
- Unity: 2022.3 LTS
- Render pipeline: Universal 2D (URP 2D). Do not depend on 2D lights unless requested.
- Platform: mobile (Android first), but MVP runs in Editor.
- Networking not in MVP (offline + bots). Design code so online can be added later.

## Project folder conventions
Use this structure:
Assets/_Project/
  Scenes/
  Scripts/
    Core/
    Map/
    Quiz/
    UI/
    Bots/
  ScriptableObjects/
  Prefabs/
  Data/
  Art/
  UI/

## Game summary (MVP)
A mobile quiz-based territory conquest strategy for 2–4 players (later more).
Initially offline vs bots. Map is world/region themed; territories are cells (start with hex grid overlay).

### Questions (MVP types)
1) MCQ (4 options)
2) Numeric approximate input (closest wins; if both exact -> faster wins)

### Session rules — normal territory attack (1v1)
Round 1 (MCQ):
- If one correct and the other incorrect => winner decided immediately, session ends.
- If tie (both correct OR both wrong) => go to Round 2.

Round 2 (Numeric):
- Winner is closer to correct value.
- If both exact => faster response time wins.

### Phases
Phase A — Base selection:
- Players do quiz ranking (MCQ + Numeric).
- Pick bases in order of ranking. Bases may be adjacent.

Phase B — Settlement draft until all territories are owned:
- Each settlement round starts with quiz ranking.
- Each player gets minimum 1 pick of a neutral territory.
- Top ranked player gets +1 extra pick (so 2 picks).
- Repeat until no neutral territories remain.
- Then main phase begins.

Main phase:
- Fixed number of rounds (Quick/Long).
- Each round each player acts once; turn order shuffled each round.
- Action (MVP): Attack an adjacent territory only.

### Capture rules
Normal territory:
- One successful session capture; if attacker wins, territory becomes attacker’s.

Base:
- Each base has 3 towers.
- Attacking base is hardcore/perfect: if attacker loses ANY question, base attack attempt ends immediately.
- One attack action attempts to destroy exactly one tower.
- Tower is destroyed only on a perfect win per the strict rule (implement as: attacker must win and not drop a question; if MCQ tie triggers numeric, attacker must also win numeric; any loss ends attempt).
- When towersRemaining reaches 0 => base captured and ALL territories of defeated player transfer to attacker (including base). Defeated player effectively cannot compete further, hence base capture must be hard/rare.

### Scoring
- Final score computed only at end of match: sum of points of owned territories.
- Successful base defense grants diminishing defense bonus points to that base (e.g., +6, +3, +2, +1...).
- Defense bonus resets to 0 when base is captured and does not transfer on normal territory capture.

### Boosters
Future feature: limited boosters per match that simplify gameplay. Not in MVP, but keep architecture extensible.

## Implementation approach (vertical slice)
Build a playable prototype first:
- Small map (5–9 cells), 2 players (human vs bot)
- Quiz UI + session logic
- Phases A/B
- Adjacent attacks
- Base towers + total territory transfer
- End-of-game scoring screen

## Code style
- Use namespaces: Conquiz.Core, Conquiz.Map, Conquiz.Quiz, Conquiz.UI, Conquiz.Bots
- Use events for UI updates (C# events or UnityEvent if necessary)
- Keep MonoBehaviours thin; put logic into plain C# classes where possible.
- Clear naming, comments only where needed.
