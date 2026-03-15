# Release Pipeline — Non-Code Deliverables

> **Status:** SKELETON — flesh out when approaching release (Phase 5).

## Purpose

A release is more than a build. This skill tracks every non-code deliverable needed to ship and announce a version of R8EO-X.

---

## Release Deliverables Checklist

### 1. Changelog

- **What:** Formatted list of player-facing changes since last release
- **Owner:** Automated (agent-assisted)
- **Template:** See `changelog` skill (`SKILL.md` → Keep a Changelog template)
- **When:** Generated at tag time, included in GitHub Release and store page

### 2. Store Descriptions

- **What:** Game description, feature list, system requirements for each storefront
- **Owner:** Project lead (human)
- **Platforms:**
  - **Steam** — Short description (300 char), About This Game (HTML), system requirements
  - **itch.io** — Game description (Markdown), tags, classification
- **Template:** TBD — create `resources/release/store-description-template.md` when needed

### 3. Screenshots and Trailer

- **What:** 5-10 screenshots at store-required resolutions, 30-60s gameplay trailer
- **Owner:** Project lead (human)
- **Specs:**
  - Steam: 1920x1080 screenshots (min 5), 616x353 capsule image, trailer MP4
  - itch.io: Cover image (630x500), screenshots (any resolution)
- **Template:** TBD — create screenshot capture script when game is visually ready

### 4. Community Announcements

- **What:** Release announcement for Discord, social media, forums
- **Owner:** Project lead (human), draft assisted by agent
- **Template:** See `changelog` skill (`SKILL.md` → Discord/Community Announcement template)
- **Channels:**
  - Discord server announcement channel
  - Twitter/X post
  - Reddit (r/radiocontrol, r/indiegames, r/unity3d)

### 5. Website Updates

- **What:** Landing page, download links, version badge
- **Owner:** Project lead (human)
- **Template:** TBD — depends on hosting choice (GitHub Pages, custom domain)

### 6. Press Kit

- **What:** Game description, logos, high-res screenshots, developer bio, contact info
- **Owner:** Project lead (human)
- **Format:** [presskit()](https://dopresskit.com/) or equivalent
- **Template:** TBD — create `resources/release/presskit/` when approaching launch

---

## Multi-Platform Build Matrix

| Platform | Format | Priority | Status |
|----------|--------|----------|--------|
| Windows x64 | `.exe` (IL2CPP) | P0 — primary | Planned |
| Linux x64 | AppImage or tar.gz | P1 — secondary | Planned |
| WebGL | Browser build | P2 — demo only | Stretch |
| macOS | `.app` (Apple Silicon) | P2 — if demand exists | Stretch |

### Build Pipeline Notes

- CI builds via GitHub Actions (see `unity-build-distribution` skill)
- Windows is the primary target — all QA gates must pass on Windows
- Linux builds share the same codebase, minimal platform-specific code expected
- WebGL may require reduced asset quality and feature gating
- See `VERSIONING.md` for version number convention and build types

---

## Release Workflow (High-Level)

1. **Feature freeze** — no new features, only bug fixes
2. **QA pass** — full test suite green, manual playthrough of all milestones
3. **Generate changelog** — `changelog` skill recipes
4. **Tag release** — `git tag -a vX.Y.Z -m "Release vX.Y.Z — Milestone Name"`
5. **Build** — CI produces platform builds from tag
6. **Upload** — push builds to storefronts
7. **Announce** — post to all community channels
8. **Monitor** — watch for crash reports and community feedback
9. **Hotfix** — if critical bugs found, branch from tag, fix, re-release

---

## Related Skills

- `changelog` — automated changelog generation from conventional commits
- `unity-build-distribution` — IL2CPP, Addressables, Steam integration, GameCI
- `branch-workflow` — tag conventions, hotfix branch workflow
