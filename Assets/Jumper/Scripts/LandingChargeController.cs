using UnityEngine;
using UnityEngine.UI;

public class LandingChargeController : MonoBehaviour
{
    [Header("UI")]
    public Image landingBar;

    [Header("Charge Settings")]
    public float chargeSpeed = 1.8f;
    public float maxCharge = 1f;
    public float perfectZoneMin = 0.75f;
    public float perfectZoneMax = 0.85f;

    [Header("Landing Effects")]
    public float minDownForce = 5f;
    public float maxDownForce = 25f;
    public float perfectForwardBoost = 10f;

    private float chargeValue = 0f;
    private bool isCharging = false;
    private bool chargingUp = true;
    private Rigidbody rb;
    private bool isInAir = false;
    private string landingQuality = "None";

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (landingBar != null)
            landingBar.fillAmount = 0f;
    }

    void Update()
    {
        CheckAirborneStatus();
        HandleLandingCharge();
    }

    void CheckAirborneStatus()
    {
        // Bodenprüfung
        bool grounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        if (!grounded && !isInAir)
        {
            isInAir = true;
        }
        else if (grounded && isInAir)
        {
            isInAir = false;
            // Reset der UI beim Aufkommen
            if (landingBar != null) landingBar.fillAmount = 0f;
        }
    }

    void HandleLandingCharge()
    {
        // Nur in der Luft darf geladen werden
        if (!isInAir) return;

        // Aufladen starten
        if (Input.GetKeyDown(KeyCode.Q))
        {
            isCharging = true;
            chargeValue = 0f;
            chargingUp = true;
        }

        // Ladebewegung
        if (isCharging)
        {
            chargeValue += (chargingUp ? 1 : -1) * Time.deltaTime * chargeSpeed;

            if (chargeValue >= maxCharge) { chargeValue = maxCharge; chargingUp = false; }
            if (chargeValue <= 0f) { chargeValue = 0f; chargingUp = true; }

            if (landingBar != null)
            {
                landingBar.fillAmount = chargeValue / maxCharge;
                landingBar.color = GetColorForCharge(chargeValue);
            }
        }

        // Taste losgelassen → Effekte anwenden
        if (Input.GetKeyUp(KeyCode.Q) && isCharging)
        {
            isCharging = false;
            landingQuality = EvaluateTimingQuality(chargeValue);

            // Downforce berechnen
            float downForce = Mathf.Lerp(minDownForce, maxDownForce, chargeValue);

            if (landingQuality == "Perfekt")
            {
                downForce *= 1.2f;
                ApplyPerfectLandingBoost();
            }
            else if (landingQuality == "Überschossen")
            {
                downForce *= 0.6f; // Weniger Kontrolle
            }

            // Downward force anwenden
            rb.AddForce(Vector3.down * downForce, ForceMode.Impulse);
        }
    }

    void ApplyPerfectLandingBoost()
    {
        // Kleiner Bonus nach vorne beim perfekten Timing
        rb.AddForce(transform.forward * perfectForwardBoost, ForceMode.Impulse);
    }

    Color GetColorForCharge(float val)
    {
        if (val >= perfectZoneMin && val <= perfectZoneMax)
            return new Color(0.2f, 1f, 0.6f); // Perfekt: hellgrün
        else if (val > perfectZoneMax)
            return Color.gray; // Überschossen
        else if (val >= 0.6f)
            return Color.green; // Gut
        else if (val >= 0.3f)
            return Color.yellow; // Normal
        else
            return Color.red; // Schlecht
    }

    string EvaluateTimingQuality(float val)
    {
        if (val >= perfectZoneMin && val <= perfectZoneMax) return "Perfekt";
        else if (val > perfectZoneMax) return "Überschossen";
        else if (val >= 0.6f) return "Gut";
        else if (val >= 0.3f) return "Normal";
        else if (val > 0f) return "Schlecht";
        else return "Grauenhaft";
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(20, 60, 300, 30), $"Landungsqualität: {landingQuality}");
    }
}
