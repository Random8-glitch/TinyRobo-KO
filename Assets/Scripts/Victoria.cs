using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Victoria : MonoBehaviour
{
    public enum ModoChequeo { EnemigoEspecifico, TodosLosEnemigos }

    [Header("Modo")]
    [SerializeField] private ModoChequeo modo = ModoChequeo.TodosLosEnemigos;

    [Header("Referencias")]
    [Tooltip("Usar con Modo = EnemigoEspecifico")]
    [SerializeField] private EnemyStats enemigoEspecifico;
    [Tooltip("Referencia al cronómetro para escuchar el evento alTerminarTiempo")]
    [SerializeField] private CronometroHUD cronometroHUD;
    [Tooltip("Referencia al PlayerStats para comparar vida cuando termine el cronómetro (opcional)")]
    [SerializeField] private PlayerStats playerStats;

    [Header("UI - Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float duracionFade = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoVictoria;
    [SerializeField] private float delayAntesDeCarga = 1.5f;

    [Header("Comportamiento")]
    [SerializeField] private string victoryScene = "Victory";

    private bool yaVictoria = false;

    private void Start()
    {
        // Inicializaciones visuales
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.interactable = false;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (cronometroHUD == null)
            cronometroHUD = FindObjectOfType<CronometroHUD>();

        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();

        if (modo == ModoChequeo.EnemigoEspecifico && enemigoEspecifico == null)
            Debug.LogWarning("Victoria: modo EnemigoEspecifico seleccionado pero no se asignó 'enemigoEspecifico'.");

        if (cronometroHUD != null)
        {
            // Suscribirse al evento para actuar cuando el cronómetro termine
            cronometroHUD.alTerminarTiempo.AddListener(OnCronometroTerminado);
        }
        else
        {
            Debug.LogWarning("Victoria: no se encontró CronometroHUD en la escena. Puedes asignarlo en el inspector.");
        }
    }

    private void OnDestroy()
    {
        if (cronometroHUD != null)
        {
            cronometroHUD.alTerminarTiempo.RemoveListener(OnCronometroTerminado);
        }
    }

    private void Update()
    {
        if (yaVictoria)
            return;

        // Comprobación normal (sincronizada por frame): victory si enemigo específico muere
        if (modo == ModoChequeo.EnemigoEspecifico)
        {
            if (enemigoEspecifico == null || enemigoEspecifico.vida <= 0f)
            {
                IniciarVictoria();
            }
        }
        else // TodosLosEnemigos
        {
            EnemyStats[] enemigos = FindObjectsOfType<EnemyStats>();
            if (enemigos == null || enemigos.Length == 0)
            {
                IniciarVictoria();
            }
        }
    }

    // Método llamado cuando el cronómetro termina
    private void OnCronometroTerminado()
    {
        if (yaVictoria)
            return;

        float vidaJugador = playerStats != null ? playerStats.vida : 0f;
        float vidaEnemigos = 0f;

        if (modo == ModoChequeo.EnemigoEspecifico)
        {
            vidaEnemigos = (enemigoEspecifico != null) ? enemigoEspecifico.vida : 0f;
        }
        else // TodosLosEnemigos
        {
            EnemyStats[] enemigos = FindObjectsOfType<EnemyStats>();
            foreach (var e in enemigos)
            {
                if (e != null)
                    vidaEnemigos += e.vida;
            }
        }

        Debug.Log($"Cronómetro terminado -> Vida jugador: {vidaJugador} | Vida enemigos: {vidaEnemigos}");

        // Ejecutar victoria solo si la vida del jugador es mayor que la de los enemigos
        if (vidaJugador > vidaEnemigos)
        {
            Debug.Log("Condición por tiempo cumplida: jugador tiene más vida => VICTORIA");
            IniciarVictoria();
        }
        else
        {
            Debug.Log("Condición por tiempo: jugador no supera la vida enemiga => no hay victoria.");
        }
    }

    private void IniciarVictoria()
    {
        yaVictoria = true;
        Debug.Log("¡VICTORIA! condición cumplida.");

        // Pausar juego
        Time.timeScale = 0f;

        StartCoroutine(SecuenciaVictoria());
    }

    private IEnumerator SecuenciaVictoria()
    {
        // Reproducir sonido de victoria si existe
        if (audioSource != null && sonidoVictoria != null)
            audioSource.PlayOneShot(sonidoVictoria);

        // Esperar el delay en tiempo real (no afectado por Time.timeScale)
        yield return new WaitForSecondsRealtime(delayAntesDeCarga);

        // Fade a negro si hay CanvasGroup asignado
        if (fadeCanvasGroup != null)
            yield return StartCoroutine(FadeANegro());

        // Restaurar tiempo y cargar escena de victoria
        Time.timeScale = 1f;
        SceneManager.LoadScene(victoryScene);
    }

    private IEnumerator FadeANegro()
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionFade)
        {
            tiempoTranscurrido += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(tiempoTranscurrido / duracionFade);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    // Permite disparar la victoria manualmente desde otros scripts si se desea
    public void ForzarVictoria()
    {
        if (!yaVictoria)
            IniciarVictoria();
    }
}