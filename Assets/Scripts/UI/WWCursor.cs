
using UnityEngine;

public class WWCursor : MonoBehaviour
{
    public Texture2D cursorTexture;

    [Header("Punto de clic dentro de la imagen")]
    public Vector2 hotSpot = new Vector2(72f, 18f);

    private void Start()
    {
        if (cursorTexture != null)
        {
            Cursor.SetCursor(
                cursorTexture,
                hotSpot,
                CursorMode.Auto
            );
        }
    }

    private void OnDisable()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
