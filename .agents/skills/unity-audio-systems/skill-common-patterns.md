# Common Patterns

> Part of the `unity-audio-systems` skill. See [SKILL.md](SKILL.md) for the overview.

## Common Patterns

### Singleton AudioManager

```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] AudioMixer _mixer;
    [SerializeField] AudioPool _sfxPool;
    [SerializeField] MusicManager _music;

    // Sound library: map enum to clips
    [System.Serializable]
    public struct SoundEntry
    {
        public SoundId id;
        public AudioClip[] clips; // Multiple variants for randomization
        public float volume;
        public float pitchVariation;
    }

    [SerializeField] SoundEntry[] _sounds;
    Dictionary<SoundId, SoundEntry> _soundLookup;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _soundLookup = new Dictionary<SoundId, SoundEntry>();
        foreach (var entry in _sounds)
            _soundLookup[entry.id] = entry;
    }

    public void PlaySFX(SoundId id, Vector3 position)
    {
        if (!_soundLookup.TryGetValue(id, out SoundEntry entry)) return;
        AudioClip clip = entry.clips[Random.Range(0, entry.clips.Length)];
        float pitch = 1f + Random.Range(-entry.pitchVariation, entry.pitchVariation);
        _sfxPool.Play(clip, position, entry.volume, pitch);
    }

    public void PlayMusic(AudioClip clip) => _music.PlayTrack(clip);
    public void SetMasterVolume(float vol) => SetVolume("MasterVolume", vol);
    public void SetMusicVolume(float vol) => SetVolume("MusicVolume", vol);
    public void SetSFXVolume(float vol) => SetVolume("SFXVolume", vol);

    void SetVolume(string param, float linear)
    {
        float dB = linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
        _mixer.SetFloat(param, dB);
    }
}

public enum SoundId
{
    ButtonClick, ButtonHover,
    FootstepDirt, FootstepMetal, FootstepWood,
    GunFire, GunReload, GunEmpty,
    Explosion, ImpactLight, ImpactHeavy,
    PickupCoin, PickupHealth,
    PlayerHurt, PlayerDeath
}
```

### Random Pitch Variation

Prevents repetitive SFX from sounding mechanical:

```csharp
// On any AudioSource play call, vary pitch slightly
_source.pitch = Random.Range(0.92f, 1.08f);
_source.PlayOneShot(clip);
_source.pitch = 1f; // Reset for next play
```
