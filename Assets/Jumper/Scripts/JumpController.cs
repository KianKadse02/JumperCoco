using UnityEngine;
using UnityEngine.UI;

public class JumpChargeController : MonoBehaviour
{
    [Header("UI")]
    public Image chargeBar;

    [Header("Charge Settings")]
    public float chargeSpeed = 1.5f;
    public float maxCharge = 1f;
    public float perfectZoneMin = 0.75f;
    public float perfectZoneMax = 0.85f;

    [Header("Jump Settings")]
    public float minJumpForce = 5f;
    public float maxJumpForce = 15f;
    public float momentumMultiplier = 2f;

    private float chargeValue = 0f;
    private bool isCharging = false;
    private bool chargingUp = true;
    private Rigidbody rb;
    private string jumpQuality = "None";

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (chargeBar != null)
            chargeBar.fillAmount = 0f;
    }

    void Update()
    {
        HandleCharge();
    }

    void HandleCharge()
    {
        // Aufladen starten
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            isCharging = true;
            chargeValue = 0f;
            chargingUp = true;
        }

        // Aufladen
        if (isCharging)
        {
            chargeValue += (chargingUp ? 1 : -1) * Time.deltaTime * chargeSpeed;

            if (chargeValue >= maxCharge) { chargeValue = maxCharge; chargingUp = false; }
            if (chargeValue <= 0f) { chargeValue = 0f; chargingUp = true; }

            if (chargeBar != null)
            {
                chargeBar.fillAmount = chargeValue / maxCharge;
                chargeBar.color = GetColorForCharge(chargeValue);
            }
        }

        // Sprung beim Loslassen
        if (Input.GetKeyUp(KeyCode.Space) && isCharging)
        {
            isCharging = false;

            float jumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, chargeValue);
            jumpQuality = EvaluateTimingQuality(chargeValue);

            // Anpassung basierend auf Qualität
            if (jumpQuality == "Perfekt") jumpForce *= 1.15f;
            if (jumpQuality == "Grauenhaft") jumpForce *= 0.5f;

            // Sprung ausführen
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            rb.AddForce(transform.forward * jumpForce * momentumMultiplier * chargeValue, ForceMode.Impulse);
        }
    }

    // Farbe abhängig vom Ladezustand
    Color GetColorForCharge(float val)
    {
        if (val >= perfectZoneMin && val <= perfectZoneMax)
            return new Color(0.2f, 1f, 0.6f); // Hellgrün (Perfekt)
        else if (val > perfectZoneMax)
            return Color.gray; // Überschossen
        else if (val >= 0.6f)
            return Color.green; // Gut
        else if (val >= 0.3f)
            return Color.yellow; // Normal
        else if (val > 0f)
            return Color.red; // Schlecht
        else
            return Color.white; // Standard
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

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 300, 30), $"Sprungqualität: {jumpQuality}");
    }
}
