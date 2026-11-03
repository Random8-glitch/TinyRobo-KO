
using UnityEngine;

public class EnemyPerception : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] private float distanciaDeteccion = 4f;
    [SerializeField] private float radioDeteccion = 0.4f;
    [SerializeField] private float alturaDeteccion = 0.5f;

    [Header("Capas")]
    [Tooltip("Agrega aquí todas las capas que el enemigo debe evitar.")]
    [SerializeField] private LayerMask capasObstaculos;

    [Tooltip("Capa que identifica las trampas.")]
    [SerializeField] private string nombreCapaTrampa = "Trap";

    [Tooltip("Capa que identifica las paredes.")]
    [SerializeField] private string nombreCapaPared = "Wall";

    [Header("Penalizaciones")]
    [SerializeField] private float penalizacionPared = 4f;
    [SerializeField] private float penalizacionTrampa = 6f;
    [SerializeField] private float penalizacionOtros = 4f;

    [Header("Direcciones de búsqueda")]
    [SerializeField]
    private float[] angulosBusqueda =
    {
        0f, 30f, -30f, 60f, -60f,
        90f, -90f, 120f, -120f, 180f
    };

    private int layerTrampa = -1;
    private int layerPared = -1;

    private void Awake()
    {
        layerTrampa = LayerMask.NameToLayer(nombreCapaTrampa);
        layerPared = LayerMask.NameToLayer(nombreCapaPared);

        // Si no se asignaron capas manualmente,
        // agrega automáticamente Wall y Trap.
        if (capasObstaculos.value == 0)
        {
            if (layerPared != -1)
                capasObstaculos |= 1 << layerPared;

            if (layerTrampa != -1)
                capasObstaculos |= 1 << layerTrampa;
        }
    }

    public float ObtenerDistanciaJugador(Transform jugador)
    {
        if (jugador == null)
            return Mathf.Infinity;

        Vector3 posicionEnemigo = transform.position;
        Vector3 posicionJugador = jugador.position;

        posicionEnemigo.y = 0f;
        posicionJugador.y = 0f;

        return Vector3.Distance(
            posicionEnemigo,
            posicionJugador
        );
    }

    public bool HayObstaculoDelante()
    {
        return HayObstaculoEnDireccion(
            transform.forward,
            out _
        );
    }

    public Vector3 AjustarRumbo(Vector3 rumboDeseado)
    {
        rumboDeseado.y = 0f;

        if (rumboDeseado.sqrMagnitude < 0.001f)
            return transform.forward;

        rumboDeseado.Normalize();

        Vector3 mejorDireccion = rumboDeseado;
        float mejorPuntuacion = float.NegativeInfinity;

        foreach (float angulo in angulosBusqueda)
        {
            Vector3 direccion =
                Quaternion.Euler(0f, angulo, 0f) * rumboDeseado;

            direccion.y = 0f;
            direccion.Normalize();

            bool bloqueado = HayObstaculoEnDireccion(
                direccion,
                out RaycastHit obstaculo
            );

            float alineacion = Vector3.Dot(
                rumboDeseado,
                direccion
            );

            float puntuacion = alineacion * 3f;

            if (bloqueado)
            {
                int layer = obstaculo.collider.gameObject.layer;

                if (layer == layerTrampa)
                {
                    puntuacion -= penalizacionTrampa;
                }
                else if (layer == layerPared)
                {
                    puntuacion -= penalizacionPared;
                }
                else
                {
                    puntuacion -= penalizacionOtros;
                }

                // Penaliza más los obstáculos cercanos.
                puntuacion -=
                    (distanciaDeteccion - obstaculo.distance) * 0.5f;
            }
            else
            {
                // Favorece las direcciones despejadas.
                puntuacion += 3f;
            }

            if (puntuacion > mejorPuntuacion)
            {
                mejorPuntuacion = puntuacion;
                mejorDireccion = direccion;
            }

            Debug.DrawRay(
                transform.position + Vector3.up * alturaDeteccion,
                direccion * distanciaDeteccion,
                bloqueado ? Color.red : Color.green
            );
        }

        return mejorDireccion.normalized;
    }

    private bool HayObstaculoEnDireccion(
        Vector3 direccion,
        out RaycastHit mejorImpacto
    )
    {
        mejorImpacto = default;

        Vector3 origen =
            transform.position + Vector3.up * alturaDeteccion;

        RaycastHit[] impactos = Physics.SphereCastAll(
            origen,
            radioDeteccion,
            direccion,
            distanciaDeteccion,
            capasObstaculos,
            QueryTriggerInteraction.Collide
        );

        float distanciaMasCercana = Mathf.Infinity;
        bool encontrado = false;

        foreach (RaycastHit impacto in impactos)
        {
            if (impacto.collider == null)
                continue;

            // Ignorar colliders del propio robot.
            if (impacto.transform == transform ||
                impacto.transform.IsChildOf(transform))
                continue;

            if (impacto.distance < distanciaMasCercana)
            {
                distanciaMasCercana = impacto.distance;
                mejorImpacto = impacto;
                encontrado = true;
            }
        }

        return encontrado;
    }
}
