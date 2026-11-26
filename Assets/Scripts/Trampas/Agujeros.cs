using UnityEngine;

public class Agujeros : MonoBehaviour
{
    [Header("Posiciones de teletransporte")]
    [SerializeField] private Transform[] posicionesDestino;
    [SerializeField] private bool elegirDestinoAleatorio = true;

    [Header("Objetos que pueden teletransportarse")]
    [SerializeField] private LayerMask capasTeleportables;

    [Header("Daño")]
    [SerializeField] private bool aplicarDanio = true;
    [SerializeField, Min(0f)] private float danioTeletransporte = 20f;

    [Header("Configuración")]
    [SerializeField] private bool conservarRotacion = false;
    [SerializeField, Min(0f)] private float tiempoEntreTeletransportes = 0.5f;

    [Header("Cámara del jugador")]
    [SerializeField] private AgitarCamara agitarCamara;

    private float tiempoUltimoTeletransporte = -Mathf.Infinity;

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time <
            tiempoUltimoTeletransporte +
            tiempoEntreTeletransportes)
        {
            return;
        }

        Rigidbody rigidbodyObjetivo =
            other.GetComponentInParent<Rigidbody>();

        Transform objetivo;

        if (rigidbodyObjetivo != null)
        {
            objetivo = rigidbodyObjetivo.transform;
        }
        else
        {
            objetivo = other.transform.root;
        }

        if (!PuedeTeletransportarse(objetivo))
            return;

        Transform destino = ObtenerDestino();

        if (destino == null)
            return;

        Teletransportar(
            objetivo,
            rigidbodyObjetivo,
            destino
        );

        AplicarDanio(objetivo);
        AgitarCamaraSiEsJugador(objetivo);

        tiempoUltimoTeletransporte = Time.time;
    }

    private bool PuedeTeletransportarse(Transform objetivo)
    {
        int mascaraObjeto = 1 << objetivo.gameObject.layer;

        return (capasTeleportables.value & mascaraObjeto) != 0;
    }

    private Transform ObtenerDestino()
    {
        if (posicionesDestino == null ||
            posicionesDestino.Length == 0)
        {
            Debug.LogWarning(
                "Agujeros no tiene posiciones de destino configuradas.",
                this
            );

            return null;
        }

        if (elegirDestinoAleatorio)
        {
            int indice = Random.Range(
                0,
                posicionesDestino.Length
            );

            return posicionesDestino[indice];
        }

        foreach (Transform destino in posicionesDestino)
        {
            if (destino != null)
                return destino;
        }

        return null;
    }

    private void Teletransportar(
        Transform objetivo,
        Rigidbody rigidbodyObjetivo,
        Transform destino
    )
    {
        Quaternion rotacionInicial = Quaternion.identity;

        if (rigidbodyObjetivo != null)
        {
            rigidbodyObjetivo.position = destino.position;
            rigidbodyObjetivo.rotation = rotacionInicial;

            rigidbodyObjetivo.linearVelocity = Vector3.zero;
            rigidbodyObjetivo.angularVelocity = Vector3.zero;
        }
        else
        {
            objetivo.position = destino.position;
            objetivo.rotation = rotacionInicial;
        }

        Physics.SyncTransforms();
    }

    private void AplicarDanio(Transform objetivo)
    {
        if (!aplicarDanio || danioTeletransporte <= 0f)
            return;

        PlayerStats playerStats =
            objetivo.GetComponentInParent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.RecibirDanio(danioTeletransporte);
        }

        EnemyStats enemyStats =
            objetivo.GetComponentInParent<EnemyStats>();

        if (enemyStats != null)
        {
            enemyStats.RecibirDanio(danioTeletransporte);
        }
    }

    private void AgitarCamaraSiEsJugador(Transform objetivo)
    {
        PlayerStats playerStats =
            objetivo.GetComponentInParent<PlayerStats>();

        if (playerStats == null)
            return;

        AgitarCamara camara = agitarCamara;

        if (camara == null && Camera.main != null)
        {
            camara = Camera.main.GetComponent<AgitarCamara>();
        }

        if (camara != null)
        {
            camara.Agitar();
        }
    }
}
