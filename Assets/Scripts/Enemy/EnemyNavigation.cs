using UnityEngine;
using UnityEngine.AI;

public class EnemyNavigation : MonoBehaviour
{
    [Header("Navegación")]
    [SerializeField] private float distanciaActualizacion = 2f;
    [SerializeField] private float intervaloActualizacion = 0.5f;
    [SerializeField] private float distanciaPuntoRuta = 1.5f;
    [SerializeField] private float distanciaBusquedaNavMesh = 5f;


    private NavMeshPath ruta;
    private Transform objetivo;

    private Vector3 ultimaPosicionObjetivo;
    private int indicePunto;

    private float temporizador;

    private bool rutaValida;

    public bool RutaValida => rutaValida;

    private void Awake()
    {
        ruta = new NavMeshPath();
    }

    public void EstablecerObjetivo(Transform nuevoObjetivo)
    {
        objetivo = nuevoObjetivo;

        if (objetivo == null)
        {
            rutaValida = false;
            return;
        }

        ActualizarRuta();
    }

    private void Update()
    {
        if (objetivo == null)
            return;

        temporizador -= Time.deltaTime;

        if (temporizador <= 0f)
        {
            Vector3 diferencia =
                objetivo.position - ultimaPosicionObjetivo;

            diferencia.y = 0f;

            bool jugadorSeMovio =
                diferencia.sqrMagnitude >=
                distanciaActualizacion * distanciaActualizacion;

            if (!rutaValida || jugadorSeMovio)
                ActualizarRuta();

            temporizador = intervaloActualizacion;
        }

        ActualizarIndicePunto();
    }

    public void ActualizarRuta()
    {
        if (objetivo == null)
        {
            rutaValida = false;
            return;
        }

        NavMeshHit inicio;
        NavMeshHit destino;

        bool inicioEncontrado = NavMesh.SamplePosition(
            transform.position,
            out inicio,
            distanciaBusquedaNavMesh,
            NavMesh.AllAreas
        );

        bool destinoEncontrado = NavMesh.SamplePosition(
            objetivo.position,
            out destino,
            distanciaBusquedaNavMesh,
            NavMesh.AllAreas
        );

        if (!inicioEncontrado || !destinoEncontrado)
        {
            rutaValida = false;
            return;
        }

        bool calculada = NavMesh.CalculatePath(
            inicio.position,
            destino.position,
            NavMesh.AllAreas,
            ruta
        );

        rutaValida =
            calculada &&
            ruta.status == NavMeshPathStatus.PathComplete &&
            ruta.corners.Length > 0;

        if (rutaValida)
        {
            indicePunto = ruta.corners.Length > 1 ? 1 : 0;
            ultimaPosicionObjetivo = objetivo.position;
        }
        else
        {
            indicePunto = 0;
        }
    }

    private void ActualizarIndicePunto()
    {
        if (!rutaValida || ruta.corners.Length == 0)
            return;

        while (indicePunto < ruta.corners.Length - 1)
        {
            float distancia = Vector3.Distance(
                transform.position,
                ruta.corners[indicePunto]
            );

            if (distancia > distanciaPuntoRuta)
                break;

            indicePunto++;
        }
    }

    public Vector3 ObtenerDireccionNavegacion()
    {
        if (!rutaValida || ruta.corners.Length == 0)
            return Vector3.zero;

        Vector3 objetivoRuta = ruta.corners[indicePunto];

        Vector3 direccion =
            objetivoRuta - transform.position;

        direccion.y = 0f;

        if (direccion.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return direccion.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        if (ruta == null || ruta.corners == null)
            return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < ruta.corners.Length - 1; i++)
        {
            Gizmos.DrawLine(
                ruta.corners[i],
                ruta.corners[i + 1]
            );
        }

        if (rutaValida && ruta.corners.Length > 0)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawSphere(
                ruta.corners[indicePunto],
                0.25f
            );
        }
    }
}

