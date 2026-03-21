---
name: user-test-monitoring
description: User Test Monitoring Skill
---


# User Test Monitoring Skill

Use this skill when setting up user testing sessions, recording player behavior, capturing performance telemetry, or analyzing session data for actionable insights.

## Purpose

User testing reveals problems that automated tests cannot: confusing UI, unexpected player behavior, performance issues on real hardware, and friction points in the experience. Monitoring infrastructure captures this data so you can analyze it after the session instead of relying on memory or notes.

## Implementation Checklist

When setting up user test monitoring for a project:

- [ ] Input recorder implemented and tested
- [ ] Game event logger with meaningful events defined
- [ ] Performance sampler (FPS, frame time, memory) at regular intervals
- [ ] Error/crash capture with pre-crash state snapshot
- [ ] Session metadata recorded (build version, hardware, tester)
- [ ] Data saved to organized session directories
- [ ] Replay system (input-based or state-based) functional
- [ ] Analysis scripts or tools for post-session review
- [ ] Privacy controls (what is recorded, consent, anonymization)
- [ ] Pre-session checklist documented for test facilitators
- [ ] Known good baseline recorded for performance comparison


## Topic Pages

- [Test Session Setup](skill-test-session-setup.md)

