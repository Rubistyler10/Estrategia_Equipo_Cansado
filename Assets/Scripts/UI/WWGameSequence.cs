
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WWGameSequence : MonoBehaviour
{
    [Header("Sistemas")]
    public TurnManager turnManager;
    public StrategyCameraController strategyCamera;
    public AnimalDragController dragController;

    [Header("Pantalla inicial")]
    public CanvasGroup introGroup;
    public float introDuration = 4f;
    public float introFadeOut = 0.7f;
    public float pauseBeforeCamera = 1f;

    [Header("Banner de turno")]
    public CanvasGroup turnBannerGroup;
    public Image turnBannerImage;
    public Sprite player1Banner;
    public Sprite player2Banner;

    public float bannerFadeIn = 0.4f;
    public float bannerHold = 2f;
    public float bannerFadeOut = 0.5f;

    [Header("Victoria")]
    public CanvasGroup victoryGroup;
    public Image victoryImage;
    public Sprite player1Victory;
    public Sprite player2Victory;
    public float victoryFadeIn = 2f;

    [Header("Botones de victoria")]
    public Button restartButton;
    public Button exitButton;

    private Coroutine bannerRoutine;
    private bool introFinished;
    private bool victoryStarted;
    private bool dragWasEnabled;
    private int announcedPlayer = -1;

    private void Awake()
    {
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();

        if (strategyCamera == null)
            strategyCamera =
                FindFirstObjectByType<StrategyCameraController>();

        if (dragController == null)
            dragController =
                FindFirstObjectByType<AnimalDragController>();

        if (strategyCamera != null)
            strategyCamera.SetInputLocked(true);

        if (dragController != null)
        {
            dragWasEnabled = dragController.enabled;
            dragController.enabled = false;
        }

        SetupGroup(introGroup, 1f, true);
        SetupGroup(turnBannerGroup, 0f, false);
        SetupGroup(victoryGroup, 0f, false);
    }

    private void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        yield return new WaitForSecondsRealtime(introDuration);

        if (introGroup != null)
        {
            yield return Fade(
                introGroup, 1f, 0f, introFadeOut);

            introGroup.blocksRaycasts = false;
            introGroup.interactable = false;
        }

        yield return new WaitForSecondsRealtime(
            pauseBeforeCamera);

        if (strategyCamera != null)
        {
            strategyCamera.SetInputLocked(false);

            if (turnManager != null)
                strategyCamera.FocusOnPlayer(
                    turnManager.currentPlayerId);
        }

        if (dragController != null)
            dragController.enabled = dragWasEnabled;

        introFinished = true;

        if (turnManager != null && !turnManager.IsGameOver)
        {
            announcedPlayer = turnManager.currentPlayerId;
            ShowTurnBanner(announcedPlayer);
        }
    }

    private void Update()
    {
        if (!introFinished || turnManager == null)
            return;

        if (turnManager.IsGameOver)
        {
            if (!victoryStarted)
                StartCoroutine(ShowVictory());

            return;
        }

        if (turnManager.currentPlayerId != announcedPlayer)
        {
            announcedPlayer = turnManager.currentPlayerId;
            ShowTurnBanner(announcedPlayer);
        }
    }

    private void ShowTurnBanner(int playerId)
    {
        if (turnBannerGroup == null ||
            turnBannerImage == null)
            return;

        if (bannerRoutine != null)
            StopCoroutine(bannerRoutine);

        turnBannerImage.sprite = playerId == 1
            ? player1Banner : player2Banner;

        bannerRoutine = StartCoroutine(
            AnimateTurnBanner());
    }

    private IEnumerator AnimateTurnBanner()
    {
        turnBannerGroup.alpha = 0f;

        yield return Fade(
            turnBannerGroup, 0f, 1f, bannerFadeIn);

        yield return new WaitForSecondsRealtime(
            bannerHold);

        yield return Fade(
            turnBannerGroup, 1f, 0f, bannerFadeOut);

        bannerRoutine = null;
    }

    private IEnumerator ShowVictory()
    {
        victoryStarted = true;

        if (bannerRoutine != null)
        {
            StopCoroutine(bannerRoutine);
            bannerRoutine = null;
        }

        SetupGroup(turnBannerGroup, 0f, false);

        if (strategyCamera != null)
            strategyCamera.SetInputLocked(true);

        if (dragController != null)
            dragController.enabled = false;

        if (victoryGroup == null)
            yield break;

        if (victoryImage != null)
        {
            victoryImage.sprite =
                turnManager.WinningPlayerId == 1
                    ? player1Victory
                    : player2Victory;
        }

        victoryGroup.alpha = 0f;
        victoryGroup.blocksRaycasts = true;
        victoryGroup.interactable = false;

        yield return Fade(
            victoryGroup, 0f, 1f, victoryFadeIn);

        // El banner permanece visible.
        victoryGroup.alpha = 1f;
        victoryGroup.interactable = true;
    }

    private IEnumerator Fade(
        CanvasGroup group,
        float from,
        float to,
        float duration)
    {
        if (group == null)
            yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            group.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        group.alpha = to;
    }

    private void SetupGroup(
        CanvasGroup group,
        float alpha,
        bool blocksRaycasts)
    {
        if (group == null)
            return;

        group.alpha = alpha;
        group.blocksRaycasts = blocksRaycasts;
        group.interactable = blocksRaycasts;
    }

    public void RestartGame()
    {
        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(currentScene.name);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartGame);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitGame);
    }
}
