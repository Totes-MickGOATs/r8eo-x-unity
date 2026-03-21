# Command Recipes

> Part of the `changelog` skill. See [SKILL.md](SKILL.md) for the overview.

## Command Recipes

### Raw log since last tag

```bash
git log $(git describe --tags --abbrev=0 2>/dev/null || git rev-list --max-parents=0 HEAD)..HEAD --oneline --no-merges
```

### Grouped by type (feat/fix/etc)

```bash
git log $(git describe --tags --abbrev=0 2>/dev/null || git rev-list --max-parents=0 HEAD)..HEAD \
  --oneline --no-merges \
  --pretty=format:"%s" \
  | sort -t: -k1,1
```

### Between two tags

```bash
git log v0.2.0..v0.3.0 --oneline --no-merges --pretty=format:"- %s"
```

### Count commits by type

```bash
git log v0.2.0..HEAD --oneline --no-merges --pretty=format:"%s" \
  | sed 's/:.*//' | sort | uniq -c | sort -rn
```

---

