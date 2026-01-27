---
name: conquiz-microtasks
description: Conquiz (Unity 2022.3 2D/Universal 2D). Use this to break any feature into tiny Unity steps (1–3 scripts), provide scene/inspector setup, and generate small prompts for follow-up work.
disable-model-invocation: true
---

# Conquiz Microtasks Skill

## When to use
Use this skill when implementing any part of the Conquiz game (map/quiz/phases/bots/UI), or when the user asks for a development plan or prompts.

## Output format (must follow)
For each response, output exactly these sections:

1) **Goal (1–2 lines)**
2) **Scope**: only 1 subsystem OR 1–3 C# scripts max
3) **Files**
   - List exact file paths to create/modify
4) **Code**
   - Provide complete compilable C# code blocks (Unity 2022.3)
5) **Unity setup**
   - Step-by-step: which Scene, which GameObjects, which components, what to assign in Inspector
6) **Quick test**
   - 3–6 bullet points to verify in Play Mode
7) **Next micro-prompts**
   - Provide 2–4 short prompts the user can paste next (each ≤ 2 sentences)

## Project constraints
- Unity 2022.3 LTS, 2D / Universal 2D (URP 2D).
- No external plugins.
- Prefer TextMeshPro for UI.
- Use ScriptableObjects for static data; runtime state in plain C# classes.
- Keep MonoBehaviours thin; use events for UI updates.

## Conquiz rules (MVP snapshot)
- Quiz types: MCQ (4 options) and Numeric (closest; if both exact => faster wins).
- Normal territory attack session:
  - Round 1 MCQ: if one correct and other wrong => winner immediately; else Numeric round decides.
- Phases:
  - A Base selection by quiz ranking.
  - B Settlement: each player gets 1 pick; top ranked gets +1 extra pick; repeat until all territories owned.
  - Main phase: fixed rounds; each player acts once per round; turn order shuffled each round; attack only adjacent territories.
- Base capture:
  - Base has 3 towers; hardcore/perfect rule: attacker losing any question ends the base attack attempt immediately.
  - When base captured: all territories of defeated player transfer to attacker.
- Scoring: only at match end = sum of points of owned territories.
- Base defense bonus: diminishing returns; resets to 0 on base capture; does not transfer.

## Safety rails
- If unsure, pick a reasonable default and state it explicitly.
- Never dump a giant architecture in one go. Always split into the next micro-prompts.
