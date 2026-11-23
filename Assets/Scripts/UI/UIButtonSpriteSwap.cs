
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonSpriteSwap : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Imagen que cambia")]
    public Image imagenVisual;

    [Header("Sprites")]
    public Sprite spriteNormal;
    public Sprite spriteHover;

    private bool pointerInside;

    private void OnEnable()
    {
        pointerInside = false;
        CambiarSprite(spriteNormal);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        CambiarSprite(spriteHover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        CambiarSprite(spriteNormal);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        CambiarSprite(spriteHover);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CambiarSprite(pointerInside ? spriteHover : spriteNormal);
    }

    private void CambiarSprite(Sprite sprite)
    {
        if (imagenVisual != null && sprite != null)
            imagenVisual.sprite = sprite;
    }
}
