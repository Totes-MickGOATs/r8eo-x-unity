# Prompt Coach — Subagent Prompt Quality

## When to Use

Before dispatching any subagent with `isolation: "worktree"`.

## Prompt Quality Checklist

- [ ] Context: References specific files/systems being modified?
- [ ] Constraints: Mentions project conventions (CLAUDE.md, coding standards, namespaces, assembly defs)?
- [ ] Acceptance criteria: Clear, testable conditions for "done"?
- [ ] Scope guard: Explicitly says what NOT to do / what's out of scope?
- [ ] Commit strategy: Specifies commit message format and granularity?
- [ ] Branch workflow: Reminds about worktree/branch rules?
- [ ] Test expectations: Specifies TDD (red-green-commit)?
- [ ] Error recovery: Says what to do if compilation fails or tests break?
- [ ] Model routing: Correct model set for agent type (Explore=haiku, Plan=opus, general-purpose=sonnet)?

## Anti-Patterns

- Vague scope ("make it better")
- Missing file paths (agent wastes time searching)
- No acceptance criteria (agent doesn't know when to stop)
- Assuming context (agent starts fresh)
- Kitchen-sink prompts (too many unrelated tasks)
- Missing model param (defaults to inherited model, wasting cost or losing quality)

## Prompt Template

```
## Task: [Verb] [Object] — [1-line summary]

[2-3 sentences of context]

### Files to Modify
- `path/to/file` — [what changes]

### Acceptance Criteria
- [ ] ...

### Out of Scope
- [Thing NOT to touch]

### Commit Strategy
[Number of commits, format]

### References
- [Relevant CLAUDE.md, skill, or ADR]
```

## Project-Specific Patterns

- Always include namespace and assembly def when creating new scripts
- Reference the manifest that owns files being changed
- For physics changes: reference ADR-001 and conformance audit
- For new systems: include manifest creation in prompt
