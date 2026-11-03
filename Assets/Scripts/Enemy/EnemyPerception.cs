using UnityEngine;

public class EnemyPerception : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] private float distanciaDeteccion = 4f;
    [SerializeField] private float radioDeteccion = 0.4f;
    [SerializeField] private float alturaDeteccion = 0.5f;

    [Header("Capas")]
    [SerializeField] private LayerMask capasObstaculos;

    [Header("Evitación")]
    [SerializeField] private float penalizacionPared = 4f;
    [SerializeField] private float penalizacionTrampa = 6f;

    [Header("Direcciones de búsqueda")]
    [SerializeField]
    private float[] angulosBusqueda =
    {
        0f, 30f, -30f, 60f, -60f,
        90f, -90f, 120f, -120f, 180f
    };

    private void Awake()
    {
        // Si no se asignaron capas manualmente, intenta encontrarlas.
        if (capasObstaculos.value == 0)
        {
            int wall = LayerMask.NameToLayer("Wall");
            int trap = LayerMask.NameToLayer("Trap");

            if (wall != -1)
                capasObstaculos |= 1 << wall;

            if (trap != -1)
                capasObstaculos |= 1 << trap;
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

        return Vector3.Distance(posicionEnemigo, posicionJugador);
    }

    public bool HayObstaculoDelante()
    {
        return HayObstaculoEnDireccion(transform.forward, out _);
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

            float alineacion = Vector3.Dot(rumboDeseado, direccion);

            // Cuanto más alineada esté con el rumbo deseado, mejor.
            float puntuacion = alineacion * 3f;

            if (bloqueado)
            {
                int layer = obstaculo.collider.gameObject.layer;

                int trapLayer = LayerMask.NameToLayer("Trap");

                if (layer == trapLayer)
                    puntuacion -= penalizacionTrampa;
                else
                    puntuacion -= penalizacionPared;

                // Penaliza especialmente los obstáculos cercanos.
                puntuacion -=
                    (distanciaDeteccion - obstaculo.distance) * 0.5f;
            }
            else
            {
                // Favorece las direcciones completamente despejadas.
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