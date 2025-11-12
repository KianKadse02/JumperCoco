using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class SimpleDashController : MonoBehaviour
{
    public Rigidbody rb;
    public InputActionReference jumpAction;
    public InputActionReference dashAction;
    public RectTransform dashBar;
    public RectTransform dashPointer;

    [Header("Dash Settings")]
    public float minDashForce = 5f;
    public float maxDashForce = 15f;
    public float dashDuration = 0.2f;
    public float pointerSpeed = 300f;

    private bool isJumping = false;
    private bool isDashActive = false;
    private float pointerX = 0f;
    private bool pointerRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        dashBar.gameObject.SetActive(false);

        jumpAction.action.started += ctx => OnJump();
        dashAction.action.performed += ctx => OnDash();
    }

    void Update()
    {
        if (isDashActive)
            UpdatePointer();
    }

    void OnJump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 7f, rb.linearVelocity.z); // einfacher Mario-Style-Sprung
        StartCoroutine(ShowDashBar());
    }

    IEnumerator ShowDashBar()
    {
        dashBar.gameObject.SetActive(true);
        pointerX = 0f;
        pointerRight = true;
        isDashActive = true;
        yield return null;
    }

    void UpdatePointer()
    {
        float halfWidth = dashBar.rect.width / 2;
        pointerX += (pointerRight ? 1 : -1) * pointerSpeed * Time.deltaTime;
        if (pointerX > halfWidth) pointerRight = false;
        if (pointerX < -halfWidth) pointerRight = true;

        dashPointer.anchoredPosition = new Vector2(pointerX, 0);
    }

    void OnDash()
    {
        if (!isDashActive) return;

        float halfWidth = dashBar.rect.width / 2;
        float normalized = Mathf.InverseLerp(-halfWidth, halfWidth, pointerX);
        float dashForce = Mathf.Lerp(minDashForce, maxDashForce, 1f - Mathf.Abs(0.5f - normalized) * 2f);

        Vector3 dashDir = transform.forward; // aktuell nur vorwärts
        rb.AddForce(dashDir * dashForce, ForceMode.VelocityChange);

        isDashActive = false;
        dashBar.gameObject.SetActive(false);
    }
}
