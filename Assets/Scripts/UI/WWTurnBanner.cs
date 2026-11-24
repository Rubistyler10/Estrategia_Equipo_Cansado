using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class WWTurnBanner : MonoBehaviour
{
    [Header("Sistema de turnos existente")]
    public TurnManager turnManager;

    [Header("Imagen del anuncio")]
    public Image bannerImage;
    public Sprite bannerJugador1;
    public Sprite bannerJugador2;

    [Header("Tiempos (segundos)")]
    [Min(0f)] public float esperaInicial = 1f;
    [Min(0f)] public float fadeIn = 0.4f;
    [Min(0f)] public float visibleDurante = 2f;
    [Min(0f)] public float fadeOut = 0.5f;

    private CanvasGroup grupo;
    private int ultimoJugador;
    private bool terminado;

    private void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();
    }

    private void Start()
    {
        if (turnManager == null)
        {
            Debug.LogWarning("WWTurnBanner: asigna TurnManager en el Inspector.");
            enabled = false;
            return;
        }

        ultimoJugador = turnManager.currentPlayerId;
        StartCoroutine(MostrarAlComienzo());
    }

    private IEnumerator MostrarAlComienzo()
    {
        yield return new WaitForSecondsRealtime(esperaInicial);

        if (!terminado && !turnManager.IsGameOver)
            yield return MostrarBanner(ultimoJugador);
    }

    private void Update()
    {
        if (terminado || turnManager == null)
            return;

        if (turnManager.IsGameOver)
        {
            terminado = true;
            StopAllCoroutines();
            grupo.alpha = 0f; // La pantalla de victoria tiene prioridad.
            return;
        }

        if (turnManager.currentPlayerId != ultimoJugador)
        {
            ultimoJugador = turnManager.currentPlayerId;
            StopAllCoroutines();
            StartCoroutine(MostrarBanner(ultimoJugador));
        }
    }

    private IEnumerator MostrarBanner(int jugador)
    {
        if (bannerImage == null)
            yield break;

        Sprite elegido = jugador == 1 ? bannerJugador1 : bannerJugador2;
        if (elegido == null)
        {
            Debug.LogWarning("WWTurnBanner: falta el sprite del jugador " + jugador);
            yield break;
        }

        bannerImage.sprite = elegido;
        grupo.alpha = 0f;

        yield return Fundido(0f, 1f, fadeIn);
        yield return new WaitForSecondsRealtime(visibleDurante);
        yield return Fundido(1f, 0f, fadeOut);
    }

    private IEnumerator Fundido(float desde, float hasta, float segundos)
    {
        if (segundos <= 0f)
        {
            grupo.alpha = hasta;
            yield break;
        }

        float transcurrido = 0f;
        while (transcurrido < segundos)
        {
            transcurrido += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(transcurrido / segundos);
            t = t * t * (3f - 2f * t);
            grupo.alpha = Mathf.Lerp(desde, hasta, t);
            yield return null;
        }

        grupo.alpha = hasta;
    }
}
