using UnityEngine;

/// <summary>
/// On-screen telemetry overlay showing vehicle physics state.
/// Toggle with F2 (via RCInput).
/// </summary>
public class TelemetryHUD : MonoBehaviour
{
    public RCCar car;
    public bool showHUD = true;

    GUIStyle style;
    GUIStyle headerStyle;

    void Start()
    {
        style = new GUIStyle();
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        style.font = Font.CreateDynamicFontFromOSFont("Consolas", 14);

        headerStyle = new GUIStyle(style);
        headerStyle.normal.textColor = Color.yellow;
        headerStyle.fontSize = 16;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
            showHUD = !showHUD;
    }

    void OnGUI()
    {
        if (!showHUD || car == null) return;

        float x = 10f;
        float y = 10f;
        float lineH = 20f;

        // Shadow background
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(x - 5, y - 5, 400, 500), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var rb = car.GetComponent<Rigidbody>();

        // Vehicle state
        GUI.Label(new Rect(x, y, 400, lineH), "=== RC BUGGY TELEMETRY ===", headerStyle);
        y += lineH + 5;

        GUI.Label(new Rect(x, y, 400, lineH),
            $"Speed: {car.GetSpeedKmh():F1} km/h  ({rb.velocity.magnitude:F1} m/s)", style);
        y += lineH;

        GUI.Label(new Rect(x, y, 400, lineH),
            $"Fwd Speed: {car.GetForwardSpeedKmh():F1} km/h", style);
        y += lineH;

        GUI.Label(new Rect(x, y, 400, lineH),
            $"Throttle: {car.smoothThrottle:F2}  Engine: {car.currentEngineForce:F1} N", style);
        y += lineH;

        GUI.Label(new Rect(x, y, 400, lineH),
            $"Brake: {car.currentBrakeForce:F1} N  Reverse: {(car.reverseEngaged ? "YES" : "no")}", style);
        y += lineH;

        GUI.Label(new Rect(x, y, 400, lineH),
            $"Steering: {car.currentSteering * Mathf.Rad2Deg:F1} deg", style);
        y += lineH;

        string airState = car.isAirborne ? "AIRBORNE" : "GROUNDED";
        GUI.Label(new Rect(x, y, 400, lineH),
            $"State: {airState}  Tumble: {car.tumbleFactor:F2}  Tilt: {car.tiltAngle:F1} deg", style);
        y += lineH + 10;

        // Per-wheel data
        GUI.Label(new Rect(x, y, 400, lineH), "=== WHEELS ===", headerStyle);
        y += lineH + 5;

        var wheels = car.GetAllWheels();
        if (wheels == null) return;

        foreach (var w in wheels)
        {
            string ground = w.isOnGround ? "GND" : "AIR";
            string motor = w.isMotor ? "M" : " ";
            string steer = w.isSteer ? "S" : " ";

            GUI.Label(new Rect(x, y, 400, lineH),
                $"{w.name} [{motor}{steer}] {ground}  " +
                $"spring={w.lastSpringLen:F3}  slip={w.slipRatio:F2}  " +
                $"grip={w.gripFactor:F2}  rpm={w.wheelRpm:F0}", style);
            y += lineH;
        }

        y += 10;
        GUI.Label(new Rect(x, y, 400, lineH),
            $"Avg Slip: {car.GetSlip():F3}", style);
        y += lineH;

        GUI.Label(new Rect(x, y, 400, lineH),
            "F2: Toggle HUD  |  R: Flip car  |  WASD: Drive", style);
    }
}
