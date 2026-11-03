using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ContadorEscenas : MonoBehaviour
{
    [Header("UI (elige TMP o UI Text)")]
    [SerializeField] private TMP_Text textoTMP;
    [SerializeField] private Text textoUI;

    [Header("Prefijo clave PlayerPrefs")]
    [SerializeField] private string prefsKeyPrefix = "SceneOpenCount_";

    [Header("Desbloqueo persistente")]
    [SerializeField] private string bossUnlockKeyPrefix = "BossUnlocked_";

    [Header("Reinicio automatico")]
    [Tooltip("Si es mayor que 0, el contador vuelve a 0 al alcanzar este valor.")]
    [SerializeField] private int maxOpenCountBeforeReset = 0;

    [Header("Hito - activar boton")]
    [Tooltip("Numero de aperturas necesarias para desbloquear el boton.")]
    [SerializeField] private int milestoneCount = 0;

    [SerializeField] private Button botonAActivar;

    [Tooltip("Desactiva el boton al iniciar si aun no se ha alcanzado el hito.")]
    [SerializeField] private bool disableButtonOnStart = true;

    private readonly Dictionary<string, bool> milestonesTriggered =
        new Dictionary<string, bool>();

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (botonAActivar == null)
            return;

        string sceneName = SceneManager.GetActiveScene().name;
        string unlockKey = bossUnlockKeyPrefix + sceneName;

        bool bossUnlocked = PlayerPrefs.GetInt(unlockKey, 0) == 1;

        if (bossUnlocked)
        {
            botonAActivar.gameObject.SetActive(true);
            botonAActivar.interactable = true;
        }
        else if (disableButtonOnStart)
        {
            botonAActivar.interactable = false;
            botonAActivar.gameObject.SetActive(false);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(HandleSceneLoadedNextFrame(scene.name));
    }

    private IEnumerator HandleSceneLoadedNextFrame(string sceneName)
    {
        yield return new WaitForEndOfFrame();

        TryIncrementForScene(sceneName);
    }

    private void TryIncrementForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        string key = prefsKeyPrefix + sceneName;
        string unlockKey = bossUnlockKeyPrefix + sceneName;

        int count = PlayerPrefs.GetInt(key, 0);
        bool bossUnlocked = PlayerPrefs.GetInt(unlockKey, 0) == 1;

        // Incrementar el contador de aperturas.
        count++;

        // Comprobar y guardar el hito ANTES del reinicio.
        if (!bossUnlocked && milestoneCount > 0 &&
            count >= milestoneCount)
        {
            PlayerPrefs.SetInt(unlockKey, 1);
            bossUnlocked = true;

            Debug.Log(
                $"Jefe desbloqueado en la escena '{sceneName}'."
            );

            StartCoroutine(ActivateButtonNextFrame());
        }

        // Reiniciar el contador al alcanzar el maximo.
        if (maxOpenCountBeforeReset > 0 &&
            count >= maxOpenCountBeforeReset)
        {
            Debug.Log(
                $"ContadorEscenas: la escena '{sceneName}' " +
                $"alcanzó el máximo ({maxOpenCountBeforeReset}). " +
                "Reiniciando contador a 0."
            );

            count = 0;
        }

        PlayerPrefs.SetInt(key, count);
        PlayerPrefs.Save();

        UpdateUI(count);

        Debug.Log(
            $"ContadorEscenas: escena '{sceneName}', " +
            $"contador actual: {count}, " +
            $"jefe desbloqueado: {bossUnlocked}."
        );
    }

    private IEnumerator ActivateButtonNextFrame()
    {
        yield return new WaitForEndOfFrame();

        if (botonAActivar == null)
        {
            Debug.LogWarning(
                "ContadorEscenas: botonAActivar no está asignado."
            );
            yield break;
        }

        // Si el boton asignado pertenece a otra escena,
        // buscar uno con el mismo nombre en la escena activa.
        if (botonAActivar.gameObject.scene != SceneManager.GetActiveScene())
        {
            GameObject found = GameObject.Find(botonAActivar.gameObject.name);

            if (found != null)
            {
                Button encontrado = found.GetComponent<Button>();

                if (encontrado != null)
                    botonAActivar = encontrado;
            }
        }

        botonAActivar.gameObject.SetActive(true);
        botonAActivar.interactable = true;

        Debug.Log(
            $"ContadorEscenas: botón '{botonAActivar.name}' activado."
        );
    }

    private void UpdateUI(int count)
    {
        string display;

        if (maxOpenCountBeforeReset > 0)
            display = $"{count} / {maxOpenCountBeforeReset}";
        else
            display = $"{count}";

        if (textoTMP != null)
            textoTMP.text = display;
        else if (textoUI != null)
            textoUI.text = display;
    }

    public int GetOpenCount(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return 0;

        return PlayerPrefs.GetInt(prefsKeyPrefix + sceneName, 0);
    }

    public void ResetCount(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        PlayerPrefs.SetInt(prefsKeyPrefix + sceneName, 0);
        PlayerPrefs.Save();

        if (SceneManager.GetActiveScene().name == sceneName)
        {
            UpdateUI(0);

            if (botonAActivar != null &&
                disableButtonOnStart &&
                PlayerPrefs.GetInt(
                    bossUnlockKeyPrefix + sceneName, 0) != 1)
            {
                botonAActivar.interactable = false;
                botonAActivar.gameObject.SetActive(false);
            }
        }

        milestonesTriggered[sceneName] = false;
    }

    public bool IsBossUnlocked(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        return PlayerPrefs.GetInt(
            bossUnlockKeyPrefix + sceneName, 0) == 1;
    }

    public void ResetBossUnlock(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        PlayerPrefs.DeleteKey(bossUnlockKeyPrefix + sceneName);
        PlayerPrefs.Save();

        if (SceneManager.GetActiveScene().name == sceneName)
        {
            if (botonAActivar != null)
            {
                botonAActivar.interactable = false;
                botonAActivar.gameObject.SetActive(false);
            }
        }
    }
}
