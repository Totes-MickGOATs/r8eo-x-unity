# R8EO-X — Game Design Document

**Version:** 0.1 (Pre-production)
**Engine:** Unity 6 (URP)
**Platform:** PC (Steam / Desktop)
**Mode:** Single-player first

---

## Topic Pages

| Page | Contents |
|------|----------|
| [Physics, Camera & Input](./gdd-physics-camera-input.md) | §4 Car physics (buggy specs, ground, air), §6 Camera, §7 Input |
| [Technical Architecture & Phases](./gdd-technical-architecture.md) | §11 Scene hierarchy, vehicle setup, physics params, air system; §12 Dev phases; §13 Open questions |

---

## 1. Concept

A realistic RC buggy racing simulation set on clay/dirt tracks. The player controls 1/10th or 1/8th scale electric buggy cars from a chase-camera perspective. The game captures the unique feel of RC racing: twitchy throttle, snap oversteer on dirt, and precise air control using inertia and gyroscopic effects from spinning wheels.

**Core fantasy:** _You are a skilled RC racer who can feel every bump, every slip, and stick the perfect landing from a triple jump._

---

## 2. Core Pillars

| Pillar | Description |
|--------|-------------|
| **Authentic Feel** | Physics tuned to real RC buggy behavior, not arcade approximation |
| **Track Mastery** | Tracks reward learning optimal lines, jump approach angles, berm use |
| **Air Craft** | Air control via inertia/gyro is a skill ceiling, not a gimmick |
| **Accessible Depth** | Easy to pick up (chase cam, forgiving defaults); hard to master |

---

## 3. Player Experience Goals

- First 5 minutes: driving feels immediately responsive and fun even before mastery
- First hour: player discovers that throttle timing on jumps changes landing angle
- Long-term: mastering optimal lines and air correction shaves seconds per lap

---

## 5. Track Design

### 5.1 Track Anatomy

```
[Starting Grid] -> [Technical Low-Speed Section] -> [Jump Section]
  -> [High-Speed Sweeper] -> [Rhythm Lane] -> [Jump-Into-Corner] -> [Finish Line]
```

### 5.2 Track Features

| Feature | Purpose |
|---------|---------|
| **Berms** | Banked corners — faster with proper entry angle |
| **Rhythm Section** | Alternating bumps — doubles vs singles is a skill choice |
| **Table Top** | Flat-top jump — safe landing, but single vs table choice |
| **On/Off Camber** | Tests weight transfer and line selection |
| **Ruts** | Develops along optimal line — rewards learning the groove |

### 5.3 Racing Line System

The "groove" — the optimal, highest-grip path — is worn into clay tracks:
- Implemented as a spline-based path through the track
- Driving on the groove gives a grip bonus
- Visual indicator (darker, packed dirt texture) shows over time
- AI uses this path as its navigation line

---

## 8. Game Modes (Phase 1)

| Mode | Description |
|------|-------------|
| **Time Trial** | Race alone, beat your best lap time |
| **Practice** | Free roam the track with no pressure |
| **Race (vs AI)** | Race a field of AI drivers |

### 8.1 Progression (Future)

- Unlock tracks by completing races
- Upgrade car parts (motor, suspension, tires, differential)
- 1/10th class -> 1/8th class progression

---

## 9. AI Design

Phase 1 AI follows the optimal racing line (spline) with speed lookup table per section and throttle control for landing corrections.

| Difficulty | Line Adherence | Speed Factor | Mistakes |
|-----------|---------------|-------------|---------|
| Beginner | 70% | 0.75x | Frequent |
| Intermediate | 90% | 0.90x | Occasional |
| Expert | 97% | 1.00x | Rare |
| Champion | 99% | 1.05x | Almost never |

---

## 10. Audio Design (Future)

| Sound | Description |
|-------|-------------|
| Motor | Electric whine pitch-shifted to RPM |
| Tire slip | Dirt spray sound when breaking traction |
| Suspension | Soft thud on landing, spring creak |
| Air | Wind rush that increases with speed |
| Collision | Plastic-on-dirt tumble sounds |
