# Platform Specifics

> Part of the `unity-build-distribution` skill. See [SKILL.md](SKILL.md) for the overview.

## Platform Specifics

### Windows

- **Graphics APIs:** DX11 first, DX12 second in the list (DX12 still has edge-case issues on older hardware)
- **Default resolution:** 1920x1080 windowed, let player change
- **Anti-cheat:** Not needed for a racing sim — keep it simple

### Linux

- **Graphics API:** Vulkan primary, OpenGL ES fallback
- **Proton compatibility:** Test with Proton — many Linux users run Windows builds via Proton
- **Steam Deck:** Verified status requires controller support and 1280x800 UI scaling

### macOS

- **Code signing:** Required for distribution — use `codesign` in CI
- **Notarization:** Required for non-App Store distribution — `xcrun notarytool submit`
- **Universal Binary:** Build for both Intel and Apple Silicon (`UniversalArchitecture: true`)

