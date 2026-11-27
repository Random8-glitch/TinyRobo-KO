using UnityEngine;
using UnityEngine.AI;

public class EnemyPerception : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] private float distanciaDeteccion = 4f;
    [SerializeField] private float radioDeteccion = 0.4f;
    [SerializeField] private float alturaDeteccion = 0.5f;

    [Header("Capas")]
    [SerializeField] private LayerMask capasObstaculos;
    [SerializeField] private string nombreCapaTrampa = "Trap";
    [SerializeField] private string nombreCapaPared = "Wall";

    [Header("Penalizaciones")]
    [SerializeField] private float penalizacionPared = 4f;
    [SerializeField] private float penalizacionTrampa = 6f;
    [SerializeField] private float penalizacionOtros = 4f;
    [Header("Direcciones de búsqueda")]
    [SerializeField]
    private float[] angulosBusqueda =
        { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f, 180f };

    private int layerTrampa;
    private int layerPared;

    private void Awake()
    {
        layerTrampa = LayerMask.NameToLayer(nombreCapaTrampa);
        layerPared = LayerMask.NameToLayer(nombreCapaPared);

        // Se añaden las capas aunque capasObstaculos tenga
        // alguna capa configurada desde el Inspector.
        if (layerPared >= 0)
            capasObstaculos |= 1 << layerPared;

        if (layerTrampa >= 0)
            capasObstaculos |= 1 << layerTrampa;
    }

    public float ObtenerDistanciaJugador(Transform jugador)
    {
        if (jugador == null) return Mathf.Infinity;
        Vector3 distancia = jugador.position - transform.position;
        distancia.y = 0f;
        return distancia.magnitude;
    }

    public bool HayObstaculoDelante()
    {
        return BuscarImpacto(transform.forward, distanciaDeteccion, out _);
    }

    public Vector3 AjustarRumbo(Vector3 rumboDeseado)
    {
        return AjustarRumbo(rumboDeseado, distanciaDeteccion, false, transform.position);
    }

    public Vector3 AjustarRumbo(Vector3 rumboDeseado, float tramo, bool sobreMalla, Vector3 baseRobot)
    {
        rumboDeseado.y = 0f;
        if (rumboDeseado.sqrMagnitude < 0.001f) return transform.forward;
        rumboDeseado.Normalize();
        float alcance = Mathf.Min(distanciaDeteccion, tramo);
        float mejor = float.NegativeInfinity;
        Vector3 resultado = sobreMalla ? Vector3.zero : rumboDeseado;
        foreach (float angulo in angulosBusqueda)
        {
            Vector3 direccion = Quaternion.AngleAxis(angulo, Vector3.up) * rumboDeseado;
            if (sobreMalla && NavMesh.Raycast(baseRobot, baseRobot + direccion * alcance,
                                              out NavMeshHit borde, NavMesh.AllAreas) &&
                borde.distance + 0.02f < alcance)
                continue;

            bool bloqueado = BuscarImpacto(direccion, alcance, out RaycastHit impacto);
            float puntos = 3f * Vector3.Dot(rumboDeseado, direccion);
            if (bloqueado)
            {
                int capa = impacto.collider.gameObject.layer;
                if (capa == layerTrampa) puntos -= penalizacionTrampa;
                else if (capa == layerPared) puntos -= penalizacionPared;
                else puntos -= penalizacionOtros;
                puntos -= (alcance - impacto.distance) * 0.5f;
            }
            else
            {
                puntos += 3f;
                if (angulo == 0f) return direccion;
            }

            if (puntos > mejor)
            {
                resultado = direccion;
                mejor = puntos;
            }
        }
        return resultado;
    }

    private bool BuscarImpacto(Vector3 direccion, float alcance, out RaycastHit masCercano)
    {
        masCercano = default;
        Vector3 origen = transform.position + Vector3.up * alturaDeteccion;
        float distancia = float.PositiveInfinity;
        foreach (RaycastHit hit in Physics.SphereCastAll(origen, radioDeteccion, direccion,
                 alcance, capasObstaculos, QueryTriggerInteraction.Collide))
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform) ||
                hit.distance >= distancia) continue;
            distancia = hit.distance;
            masCercano = hit;
        }
        bool bloqueado = masCercano.collider != null;
        Debug.DrawRay(origen, direccion * alcance, bloqueado ? Color.red : Color.green);
        return bloqueado;
    }

    public bool HayTrampaCerca(Vector3 rumbo, float alcance)
    {
        if (layerTrampa < 0)
            return false;

        rumbo.y = 0f;

        if (rumbo.sqrMagnitude < 0.001f)
            rumbo = transform.forward;

        rumbo.Normalize();

        float distancia = Mathf.Min(distanciaDeteccion, alcance);

        Vector3 origen =
            transform.position + Vector3.up * alturaDeteccion;

        int mascaraTrampa = 1 << layerTrampa;

        foreach (RaycastHit hit in Physics.SphereCastAll(
            origen,
            radioDeteccion,
            rumbo,
            distancia,
            mascaraTrampa,
            QueryTriggerInteraction.Collide))
        {
            if (hit.collider == null ||
                hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            Debug.DrawRay(
                origen,
                rumbo * hit.distance,
                Color.yellow
            );

            return true;
        }

        Debug.DrawRay(
            origen,
            rumbo * distancia,
            Color.green
        );

        return false;
    }
}
