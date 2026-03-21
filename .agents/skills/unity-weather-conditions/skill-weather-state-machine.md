# Weather State Machine

> Part of the `unity-weather-conditions` skill. See [SKILL.md](SKILL.md) for the overview.

## Weather State Machine

Model weather as a finite state machine with ScriptableObject configs per state.

### States

```
Clear <-> Cloudy <-> LightRain <-> HeavyRain -> Drying -> Clear
```

Each state defines visual and gameplay parameters:

```csharp
[CreateAssetMenu(menuName = "Racing/Weather State")]
public class WeatherStateConfig : ScriptableObject
{
    [Header("Identity")]
    public string stateName;
    public WeatherType weatherType;

    [Header("Wetness")]
    [Range(0f, 1f)] public float targetWetness;
    public float wetnessTransitionRate;     // Units per second toward target

    [Header("Rain")]
    public int rainParticleCount;           // 0 for Clear/Cloudy/Drying
    public float rainIntensity;

    [Header("Wind")]
    public float windStrengthMin;
    public float windStrengthMax;

    [Header("Lighting")]
    public float sunIntensityMultiplier;
    public Color ambientColorTint;

    [Header("Temperature")]
    public float ambientTempCelsius;

    [Header("Allowed Transitions")]
    public WeatherStateConfig[] validTransitions;
}
```

### State Machine Controller

```csharp
public class WeatherController : MonoBehaviour
{
    [SerializeField] private WeatherStateConfig initialState;
    [SerializeField] private float minStateDuration = 60f;
    [SerializeField] private float maxStateDuration = 300f;

    private WeatherStateConfig currentState;
    private float currentWetness;
    private float stateTimer;

    void Update()
    {
        // Lerp wetness toward current state's target
        currentWetness = Mathf.MoveTowards(currentWetness,
            currentState.targetWetness,
            currentState.wetnessTransitionRate * Time.deltaTime);

        // Push to global shader property
        Shader.SetGlobalFloat("_GlobalWetness", currentWetness);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            TransitionToNextState();
        }
    }
}
```

---

