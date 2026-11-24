using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class WWVictoryBanner : MonoBehaviour
{
    [Header("Sistema existente")]
    public TurnManager turnManager;

    [Header("Banners de victoria")]
    public Sprite victoriaJugador1;
    public Sprite victoriaJugador2;

    [Header("Transicion")]
    [Min(0f)] public float duracionFadeIn = 2f;

    private CanvasGroup grupo;
    private Image imagen;
    private bool mostrado;

    private void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        imagen = GetComponent<Image>();
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();

        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
    }

    private void Update()
    {
        if (mostrado || turnManager == null || !turnManager.IsGameOver)
            return;

        mostrado = true;
        imagen.sprite = turnManager.WinningPlayerId == 1
            ? victoriaJugador1 : victoriaJugador2;

        if (imagen.sprite == null)
        {
            Debug.LogWarning("Falta asignar el sprite de victoria.");
            return;
        }

        // La imagen ocupa la pantalla y bloquea clics sobre la partida.
        grupo.blocksRaycasts = true;
        StartCoroutine(MostrarVictoria());
    }

    private IEnumerator MostrarVictoria()
    {
        if (duracionFadeIn <= 0f)
        {
            grupo.alpha = 1f;
            yield break;
        }

        float tiempo = 0f;
        while (tiempo < duracionFadeIn)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / duracionFadeIn);
            grupo.alpha = t * t * (3f - 2f * t);
            yield return null;
        }

        grupo.alpha = 1f;
        // Permanece visible, hasta que otro sistema reinicie o cambie de escena.
    }
}
