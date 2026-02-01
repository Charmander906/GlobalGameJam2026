using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager instance;

    [Header("Fade Settings")]
    public float fadeDuration = 1.5f;

    private bool isFading = false;
    private float fadeAlpha = 0f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this.gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        InputSystem.ResetHaptics();
    }

    public void TriggerGameOver()
    {
        if (!isFading)
            StartCoroutine(FadeAndReload());
    }

    private IEnumerator FadeAndReload()
    {
        isFading = true;
        fadeAlpha = 0f;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeAlpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnGUI()
    {
        if (!isFading) return;
        Color prevColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prevColor;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
