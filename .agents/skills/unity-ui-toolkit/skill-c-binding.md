# C# Binding

> Part of the `unity-ui-toolkit` skill. See [SKILL.md](SKILL.md) for the overview.

## C# Binding

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private Button _playButton;
    private Button _settingsButton;
    private Button _quitButton;
    private VisualElement _settingsPanel;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        // Query elements by name
        _playButton = root.Q<Button>("btn-play");
        _settingsButton = root.Q<Button>("btn-settings");
        _quitButton = root.Q<Button>("btn-quit");
        _settingsPanel = root.Q<VisualElement>("settings-panel");

        // Query by class (returns first match)
        var title = root.Q<Label>(className: "title-text");

        // Query all matching elements
        var allButtons = root.Query<Button>(className: "menu-button").ToList();

        // Register callbacks
        _playButton.RegisterCallback<ClickEvent>(OnPlayClicked);
        _settingsButton.RegisterCallback<ClickEvent>(OnSettingsClicked);
        _quitButton.RegisterCallback<ClickEvent>(OnQuitClicked);

        // Hover effects via callbacks (beyond what USS can do)
        _playButton.RegisterCallback<MouseEnterEvent>(evt => PlayHoverSound());

        // Schedule delayed or repeated work
        root.schedule.Execute(() => Debug.Log("After 2 seconds")).StartingIn(2000);
        root.schedule.Execute(() => UpdateClock()).Every(1000);
    }

    private void OnDisable()
    {
        // Always unregister to avoid leaks
        _playButton.UnregisterCallback<ClickEvent>(OnPlayClicked);
        _settingsButton.UnregisterCallback<ClickEvent>(OnSettingsClicked);
        _quitButton.UnregisterCallback<ClickEvent>(OnQuitClicked);
    }

    private void OnPlayClicked(ClickEvent evt) => SceneLoader.LoadGameplay();
    private void OnSettingsClicked(ClickEvent evt)
    {
        _settingsPanel.style.display = _settingsPanel.style.display == DisplayStyle.None
            ? DisplayStyle.Flex
            : DisplayStyle.None;
    }
    private void OnQuitClicked(ClickEvent evt) => Application.Quit();
    private void PlayHoverSound() { /* audio */ }
    private void UpdateClock() { /* update time label */ }
}
```

### Key Callbacks

| Event | When |
|-------|------|
| `ClickEvent` | Click/tap on element |
| `MouseEnterEvent` / `MouseLeaveEvent` | Hover in/out |
| `FocusInEvent` / `FocusOutEvent` | Keyboard/gamepad focus |
| `ChangeEvent<T>` | Value changed (Toggle, Slider, TextField, etc.) |
| `KeyDownEvent` / `KeyUpEvent` | Keyboard input while focused |
| `NavigationMoveEvent` | Gamepad/keyboard navigation |
| `GeometryChangedEvent` | Element resized/repositioned |
| `AttachToPanelEvent` / `DetachFromPanelEvent` | Added/removed from panel |

### Value Change Handling

```csharp
var slider = root.Q<Slider>("volume-slider");
slider.RegisterValueChangedCallback(evt =>
{
    AudioListener.volume = evt.newValue;
    Debug.Log($"Volume: {evt.previousValue} -> {evt.newValue}");
});

var textField = root.Q<TextField>("player-name");
textField.RegisterValueChangedCallback(evt =>
{
    ValidatePlayerName(evt.newValue);
});
```

## ListView — Virtualized Lists

ListView is essential for inventory, leaderboards, or any scrollable list with many items:

```csharp
public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset rowTemplate;

    private List<LeaderboardEntry> _entries;

    private void OnEnable()
    {
        var listView = uiDocument.rootVisualElement.Q<ListView>("leaderboard-list");

        listView.itemsSource = _entries;
        listView.fixedItemHeight = 50;
        listView.selectionType = SelectionType.Single;

        // How to create each row's visual
        listView.makeItem = () => rowTemplate.Instantiate();

        // How to populate a row with data
        listView.bindItem = (element, index) =>
        {
            var entry = _entries[index];
            element.Q<Label>("rank").text = $"#{index + 1}";
            element.Q<Label>("name").text = entry.PlayerName;
            element.Q<Label>("score").text = entry.Score.ToString("N0");
        };

        listView.selectionChanged += OnEntrySelected;
    }

    private void OnEntrySelected(IEnumerable<object> selection)
    {
        var entry = selection.FirstOrDefault() as LeaderboardEntry;
        if (entry != null) ShowPlayerDetails(entry);
    }
}
```

## Custom Controls

Create reusable custom VisualElements:

```csharp
public class HealthBar : VisualElement
{
    // UxmlFactory enables use in UXML
    public new class UxmlFactory : UxmlFactory<HealthBar, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        private UxmlFloatAttributeDescription _maxHealth = new()
            { name = "max-health", defaultValue = 100f };
        private UxmlColorAttributeDescription _barColor = new()
            { name = "bar-color", defaultValue = Color.green };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var bar = (HealthBar)ve;
            bar.MaxHealth = _maxHealth.GetValueFromBag(bag, cc);
            bar._fill.style.backgroundColor = _barColor.GetValueFromBag(bag, cc);
        }
    }

    public float MaxHealth { get; set; } = 100f;

    private float _currentHealth;
    public float CurrentHealth
    {
        get => _currentHealth;
        set
        {
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
            float pct = MaxHealth > 0 ? _currentHealth / MaxHealth : 0;
            _fill.style.width = Length.Percent(pct * 100f);
            _fill.style.backgroundColor = Color.Lerp(Color.red, Color.green, pct);
        }
    }

    private VisualElement _fill;

    public HealthBar()
    {
        AddToClassList("health-bar");

        var background = new VisualElement();
        background.AddToClassList("health-bar__background");

        _fill = new VisualElement();
        _fill.AddToClassList("health-bar__fill");

        background.Add(_fill);
        Add(background);
    }
}
```

Use in UXML:
```xml
<HealthBar name="player-health" max-health="100" bar-color="#00FF00" />
```

## Responsive Layout — Flexbox

UI Toolkit uses Flexbox for layout, similar to CSS:

```css
/* Vertical column layout (default) */
.panel {
    flex-direction: column;
    align-items: center;      /* horizontal alignment */
    justify-content: center;  /* vertical alignment */
}

/* Horizontal row layout */
.toolbar {
    flex-direction: row;
    justify-content: space-between;
}

/* Flex grow/shrink */
.sidebar { flex-grow: 0; flex-shrink: 0; width: 250px; }  /* fixed width */
.content { flex-grow: 1; }  /* takes remaining space */

/* Wrapping */
.grid {
    flex-direction: row;
    flex-wrap: wrap;
}
.grid-item {
    width: 25%;  /* 4 columns */
    min-width: 150px;  /* wraps when too narrow */
}

/* Absolute positioning (escape flex) */
.tooltip {
    position: absolute;
    left: 100px;
    top: 50px;
}
```

