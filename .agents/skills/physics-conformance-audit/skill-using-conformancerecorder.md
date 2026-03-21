# Using ConformanceRecorder

> Part of the `physics-conformance-audit` skill. See [SKILL.md](SKILL.md) for the overview.

## Using ConformanceRecorder

```csharp
using R8EOX.Debug.Audit;

// Start a conformance run
ConformanceRecorder.BeginRun();

// Record individual checks
ConformanceRecorder.Record(
    category: "B",
    checkId: "B6",
    checkName: "Normal force sum at rest",
    expected: 14.715,  // mg = 1.5 * 9.81
    actual: measuredForceSum
);

ConformanceRecorder.Record(
    category: "A",
    checkId: "A3",
    checkName: "Wheel rotation vs distance traveled",
    expected: 2.0 * Mathf.PI * 0.166,  // one rotation = circumference
    actual: measuredDistance,
    metadata: "{\"wheelIndex\": 0, \"rotations\": 1}"
);

// End the run -- flushes to DB and logs summary
ConformanceRecorder.EndRun();

// Query results from the latest run
var summaries = ConformanceRecorder.GetLatestRunSummary();
foreach (var s in summaries)
{
    Debug.Log($"Category {s.Category}: {s.Passed}/{s.Total} passed, worst: {s.WorstTier}");
}
```

---

