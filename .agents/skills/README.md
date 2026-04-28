# BMAD Skill Entrypoints

This directory exposes local BMAD roles to Codex skill discovery.

Why this exists:
- `_bmad/**` stores the original BMAD role definitions and workflows.
- Codex `$` skill discovery does not read `_bmad/**` directly.
- `.agents/skills/**/SKILL.md` is the discovery layer that makes BMAD roles invokable.

Current mapping:
- Game BMAD roles map to `_bmad/bmgd/agents/*.md`
- Business BMAD roles map to `_bmad/bmm/agents/*.md`
- Core BMAD master maps to `_bmad/core/agents/bmad-master.md`

If BMAD roles disappear from `$` again, verify:
1. `.agents/skills/bmad-*/SKILL.md` still exists
2. The target `_bmad/.../agents/*.md` file still exists
3. Restart the Codex session so skill discovery refreshes
