using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportSimple : MonoBehaviour
{
    [Header("Escena de destino")]
    [SerializeField] private string nombreEscena;

    [Header("Detección del jugador")]
    [SerializeField] private string tagJugador = "Player";

    [Header("Configuración")]
    [SerializeField] private bool usarTrigger = true;

    private bool yaSeActivo = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!usarTrigger || yaSeActivo)
            return;

        ComprobarJugador(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (usarTrigger || yaSeActivo)
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

        yaSeActivo = true;
        SceneManager.LoadScene(nombreEscena);
    }
}