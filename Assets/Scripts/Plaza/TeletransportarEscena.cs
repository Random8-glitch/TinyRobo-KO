
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class TeletransportarEscena : MonoBehaviour
{
    [Header("Escena de destino")]
    [SerializeField] private string nombreEscena;

    [Header("Detección del jugador")]
    [SerializeField] private string tagJugador = "Player";

    [Header("Configuración")]
    [SerializeField] private bool usarTrigger = true;

    [Header("Ventana de confirmación")]
    [SerializeField] private GameObject ventanaConfirmacion;
    [SerializeField] private TMP_Text textoConfirmacion;

    [SerializeField, TextArea(2, 4)]
    private string mensajeConfirmacion =
        "¿Estás seguro de que quieres viajar a esta escena?\n\nY = Confirmar | N = Cancelar";

    [Header("Botones UI (opcionales)")]
    [SerializeField] private GameObject botonConfirmar;
    [SerializeField] private GameObject botonCancelar;

    [Header("Teclas de confirmación")]
    [SerializeField] private KeyCode teclaConfirmar = KeyCode.Y;
    [SerializeField] private KeyCode teclaCancelar = KeyCode.N;

    [Header("Collider")]
    [SerializeField] private Collider colliderTeletransporte;
    [SerializeField] private float tiempoDesactivado = 2f;

    private bool ventanaAbierta = false;
    private bool yaSeActivo = false;
    private bool colliderTemporalmenteDesactivado = false;

    private Coroutine rutinaCollider;

    private bool pausaAnterior;
    private float escalaTiempoAnterior;
    private CursorLockMode bloqueoCursorAnterior;
    private bool cursorVisibleAnterior;

    private void Awake()
    {
        if (colliderTeletransporte == null)
            colliderTeletransporte = GetComponent<Collider>();

        if (ventanaConfirmacion != null)
            ventanaConfirmacion.SetActive(false);
    }

    private void Update()
    {
        if (!ventanaAbierta)
            return;

        // Permitir confirmar o cancelar con el teclado.
        if (Input.GetKeyDown(teclaConfirmar))
        {
            ConfirmarTeletransporte();
        }
        else if (Input.GetKeyDown(teclaCancelar))
        {
            CancelarTeletransporte();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!usarTrigger || ventanaAbierta || yaSeActivo ||
            colliderTemporalmenteDesactivado)
            return;

        ComprobarJugador(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (usarTrigger || ventanaAbierta || yaSeActivo ||
            colliderTemporalmenteDesactivado)
            return;

        ComprobarJugador(collision.gameObject);
    }

    private void ComprobarJugador(GameObject objeto)
    {
        if (!objeto.CompareTag(tagJugador))
            return;

        if (string.IsNullOrWhiteSpace(nombreEscena))
        {
            Debug.LogWarning(
                "No se ha asignado una escena de destino en " + gameObject.name
            );
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nombreEscena))
        {
            Debug.LogError(
                "La escena '" + nombreEscena +
                "' no está disponible. Revisa su nombre y configuración."
            );
            return;
        }

        AbrirVentanaConfirmacion();
    }

    private void AbrirVentanaConfirmacion()
    {
        if (ventanaConfirmacion == null)
        {
            Debug.LogError(
                "No se ha asignado la ventana de confirmación en " + gameObject.name
            );
            return;
        }

        ventanaAbierta = true;

        // Guardar el estado anterior del juego y del cursor.
        escalaTiempoAnterior = Time.timeScale;
        pausaAnterior = escalaTiempoAnterior == 0f;

        bloqueoCursorAnterior = Cursor.lockState;
        cursorVisibleAnterior = Cursor.visible;

        // Pausar y liberar el cursor.
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (textoConfirmacion != null)
            textoConfirmacion.text = mensajeConfirmacion;

        ventanaConfirmacion.SetActive(true);
    }

    // Se puede llamar desde el botón UI o con la tecla Y.
    public void ConfirmarTeletransporte()
    {
        if (!ventanaAbierta || yaSeActivo)
            return;

        yaSeActivo = true;
        ventanaAbierta = false;

        if (ventanaConfirmacion != null)
            ventanaConfirmacion.SetActive(false);

        RestaurarEstadoJuego();

        SceneManager.LoadScene(nombreEscena);
    }

    // Se puede llamar desde el botón UI o con la tecla N.
    public void CancelarTeletransporte()
    {
        if (!ventanaAbierta || yaSeActivo)
            return;

        ventanaAbierta = false;

        if (ventanaConfirmacion != null)
            ventanaConfirmacion.SetActive(false);

        RestaurarEstadoJuego();

        if (rutinaCollider != null)
            StopCoroutine(rutinaCollider);

        rutinaCollider = StartCoroutine(
            DesactivarColliderTemporalmente()
        );
    }

    private void RestaurarEstadoJuego()
    {
        Time.timeScale = escalaTiempoAnterior;

        Cursor.lockState = bloqueoCursorAnterior;
        Cursor.visible = cursorVisibleAnterior;
    }

    private IEnumerator DesactivarColliderTemporalmente()
    {
        colliderTemporalmenteDesactivado = true;

        if (colliderTeletransporte != null)
            colliderTeletransporte.enabled = false;

        yield return new WaitForSecondsRealtime(tiempoDesactivado);

        if (colliderTeletransporte != null)
            colliderTeletransporte.enabled = true;

        colliderTemporalmenteDesactivado = false;
        rutinaCollider = null;
    }

    private void OnDisable()
    {
        if (rutinaCollider != null)
        {
            StopCoroutine(rutinaCollider);
            rutinaCollider = null;
        }

        if (ventanaAbierta)
        {
            ventanaAbierta = false;
            RestaurarEstadoJuego();
        }
    }
}
