# CI Integration

> Part of the `unity-testing-patterns` skill. See [SKILL.md](SKILL.md) for the overview.

## CI Integration

Run tests in batch mode for CI/CD pipelines:

```bash
# Edit Mode tests
Unity -batchmode -nographics -runTests \
  -testPlatform EditMode \
  -testResults ./results/editmode-results.xml \
  -projectPath . \
  -logFile ./results/unity.log

# Play Mode tests
Unity -batchmode -nographics -runTests \
  -testPlatform PlayMode \
  -testResults ./results/playmode-results.xml \
  -projectPath . \
  -logFile ./results/unity.log
```

Exit code 0 = all passed, non-zero = failures.

### GitHub Actions Example

```yaml
- name: Run Edit Mode Tests
  uses: game-ci/unity-test-runner@v4
  with:
    testMode: EditMode
    projectPath: .
    artifactsPath: ./test-results
    githubToken: ${{ secrets.GITHUB_TOKEN }}

- name: Run Play Mode Tests
  uses: game-ci/unity-test-runner@v4
  with:
    testMode: PlayMode
    projectPath: .
    artifactsPath: ./test-results
    githubToken: ${{ secrets.GITHUB_TOKEN }}
```

