
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonHoverEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Objeto visual que se anima")]
    public RectTransform visual;

    [Header("Escala")]
    [Range(1f, 1.3f)]
    public float hoverScale = 1.08f;

    [Range(0.8f, 1f)]
    public float pressedScale = 0.96f;

    [Header("Velocidad de transicion")]
    [Min(1f)]
    public float smoothSpeed = 14f;

    [Header("Comportamiento")]
    public bool soloSiInteractuable = true;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private Button button;
    private bool pointerInside;
    private bool pointerPressed;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (visual == null)
            visual = transform as RectTransform;

        if (visual != null)
        {
            originalScale = visual.localScale;
            targetScale = originalScale;
        }
    }

    private void OnEnable()
    {
        pointerInside = false;
        pointerPressed = false;

        if (visual != null)
        {
            targetScale = originalScale;
            visual.localScale = originalScale;
        }
    }

    private void Update()
    {
        if (visual == null)
            return;

        bool puedeInteractuar =
            !soloSiInteractuable ||
            button == null ||
            button.IsActive() && button.IsInteractable();

        float multiplier = 1f;

        if (puedeInteractuar)
        {
            if (pointerPressed)
                multiplier = pressedScale;
            else if (pointerInside)
                multiplier = hoverScale;
        }

        targetScale = originalScale * multiplier;

        // Suavizado independiente del framerate.
        float t = 1f - Mathf.Exp(
            -smoothSpeed * Time.unscaledDeltaTime
        );

        visual.localScale = Vector3.Lerp(
            visual.localScale,
            targetScale,
            t
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        pointerPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            pointerPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerPressed = false;
    }

    private void OnDisable()
    {
        pointerInside = false;
        pointerPressed = false;

        if (visual != null)
            visual.localScale = originalScale;
    }
}
