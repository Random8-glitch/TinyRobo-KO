
using UnityEngine;

[RequireComponent(typeof(RoboMovEnemy))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyAI : MonoBehaviour
{
    public enum EstadoIA
    {
        Perseguir,
        Combatir,
        Retirarse,
        Rodear,
        Reposicionarse
    }

    [Header("Análisis y percepción")]
    [SerializeField] private EnemyPerception percepcion;
    [SerializeField] private EnemyWeaponAnalysis analisisArmas;

    [Header("Objetivo")]
    [SerializeField] private Transform jugador;

    [Header("Retirada")]
    [Range(0f, 100f)]
    [SerializeField] private float porcentajeVidaRetirada = 30f;
    [SerializeField] private float distanciaReanudarAtaque = 10f;

    [Header("Combate")]
    [SerializeField] private float distanciaIdeal = 5f;
    [SerializeField] private float distanciaPersecucion = 7f;

    [Header("Maniobras")]
    [SerializeField] private bool rodearJugador = true;
    [SerializeField] private float intensidadRodeo = 0.7f;
    [SerializeField] private float intensidadRetiradaDiagonal = 0.35f;
    [SerializeField] private float tiempoCambioRodeo = 3f;

    [Header("Decisiones")]
    [SerializeField] private float intervaloDecision = 0.25f;

    [Header("Estado actual")]
    [SerializeField] private EstadoIA estadoActual;

    private RoboMovEnemy movimiento;
    private EnemyStats stats;

    private float temporizadorDecision;
    private float temporizadorRodeo;

    private int direccionRodeo = 1;

    private EnemyNavigation navegacion;

    private void Awake()
    {
        movimiento = GetComponent<RoboMovEnemy>();
        stats = GetComponent<EnemyStats>();

        if (percepcion == null)
            percepcion = GetComponent<EnemyPerception>();

        if (analisisArmas == null)
            analisisArmas = GetComponent<EnemyWeaponAnalysis>();

        navegacion = GetComponent<EnemyNavigation>();
    }

    private void Start()
    {
        if (jugador == null)
        {
            GameObject objetoJugador =
                GameObject.FindGameObjectWithTag("Player");

            if (objetoJugador != null)
                jugador = objetoJugador.transform;
        }

        if (navegacion != null && jugador != null)
        {
            navegacion.EstablecerObjetivo(jugador);
        }

        CambiarDireccionRodeo();
    }

    private void Update()
    {
        if (jugador == null || stats == null || movimiento == null)
            return;

        temporizadorDecision -= Time.deltaTime;
        temporizadorRodeo -= Time.deltaTime;

        if (temporizadorRodeo <= 0f)
            CambiarDireccionRodeo();

        if (temporizadorDecision <= 0f)
        {
            EvaluarSituacion();
            temporizadorDecision = intervaloDecision;
        }

        EjecutarEstado();
    }

    // Decide qué comportamiento debe utilizar el enemigo.
    private void EvaluarSituacion()
    {
        float distancia = DistanciaAlJugador();
        float porcentajeVida = ObtenerPorcentajeVida();

        // Si está retirándose, continúa hasta alcanzar
        // la distancia necesaria para volver a combatir.
        if (estadoActual == EstadoIA.Retirarse)
        {
            if (distancia < distanciaReanudarAtaque)
                return;
        }

        // Comienza a retirarse si tiene poca vida
        // y se encuentra cerca del jugador.
        if (porcentajeVida <= porcentajeVidaRetirada &&
            distancia < distanciaReanudarAtaque)
        {
            estadoActual = EstadoIA.Retirarse;
            return;
        }

        // Persecución si el jugador está lejos.
        if (distancia > distanciaPersecucion)
        {
            estadoActual = EstadoIA.Perseguir;
        }
        // Reposicionamiento si el jugador está demasiado cerca.
        else if (distancia < distanciaIdeal * 0.6f)
        {
            estadoActual = EstadoIA.Reposicionarse;
        }
        // Combate a distancia intermedia.
        else
        {
            estadoActual = EstadoIA.Combatir;
        }
    }

    // Ejecuta el comportamiento correspondiente al estado actual.
    private void EjecutarEstado()
    {
        Vector3 haciaJugador = jugador.position - transform.position;
        haciaJugador.y = 0f;

        if (haciaJugador.sqrMagnitude < 0.01f)
            return;

        Vector3 rumboDeseado;

        switch (estadoActual)
        {
            case EstadoIA.Perseguir:

                if (navegacion != null && navegacion.RutaValida)
                {
                    Vector3 direccionRuta =
                        navegacion.ObtenerDireccionNavegacion();

                    if (direccionRuta.sqrMagnitude > 0.001f)
                        rumboDeseado = direccionRuta;
                    else
                        rumboDeseado = haciaJugador.normalized;
                }
                else
                {
                    // Si no existe una ruta válida,
                    // utiliza la persecución convencional.
                    rumboDeseado = haciaJugador.normalized;
                }

                break;

            case EstadoIA.Combatir:
                rumboDeseado = CalcularRumboCombate(haciaJugador);
                break;

            case EstadoIA.Retirarse:
                rumboDeseado = CalcularRumboRetirada(haciaJugador);
                break;

            case EstadoIA.Rodear:
                rumboDeseado = CalcularRumboRodeo(haciaJugador);
                break;

            case EstadoIA.Reposicionarse:
                rumboDeseado = CalcularRumboReposicion(haciaJugador);
                break;

            default:
                rumboDeseado = haciaJugador.normalized;
                break;
        }

        // Ajusta la orientación según las armas disponibles.
        if (estadoActual == EstadoIA.Combatir &&
            analisisArmas != null)
        {
            rumboDeseado = analisisArmas.ObtenerRumboDeAtaque(
                jugador,
                rumboDeseado
            );
        }

        // Evita paredes y trampas.
        if (percepcion != null)
            rumboDeseado = percepcion.AjustarRumbo(rumboDeseado);

        movimiento.SetDireccionIA(rumboDeseado);
    }

    // Comportamiento de combate.

    private Vector3 CalcularRumboCombate(Vector3 haciaJugador)
    {
        float distancia = haciaJugador.magnitude;

        Vector3 direccionJugador = haciaJugador.normalized;

        // Siempre conserva cierta intención de acercarse al jugador.
        Vector3 rumbo = direccionJugador * 0.4f;

        // Si está demasiado lejos, aumenta la persecución.
        if (distancia > distanciaIdeal + 1f)
        {
            rumbo += direccionJugador * 0.8f;
        }
        // Si está demasiado cerca, intenta recuperar distancia.
        else if (distancia < distanciaIdeal - 1f)
        {
            rumbo -= direccionJugador * 0.8f;
        }

        // El rodeo es una maniobra secundaria, no la dirección principal.
        if (rodearJugador && TieneArmasLaterales())
        {
            rumbo += ObtenerDireccionLateral(haciaJugador)
                * (intensidadRodeo * 0.5f);
        }

        if (rumbo.sqrMagnitude < 0.01f)
            rumbo = direccionJugador;

        return rumbo.normalized;
    }


    // Se aleja del jugador mientras conserva
    // un pequeño componente diagonal.
    private Vector3 CalcularRumboRetirada(Vector3 haciaJugador)
    {
        Vector3 rumbo = -haciaJugador.normalized;

        if (rodearJugador && TieneArmasLaterales())
        {
            rumbo += ObtenerDireccionLateral(haciaJugador)
                * intensidadRetiradaDiagonal;
        }

        return rumbo.normalized;
    }

    // Maniobra para rodear al jugador.
    private Vector3 CalcularRumboRodeo(Vector3 haciaJugador)
    {
        Vector3 rumbo = ObtenerDireccionLateral(haciaJugador);
        float distancia = haciaJugador.magnitude;

        if (distancia < distanciaIdeal)
        {
            rumbo -= haciaJugador.normalized * 0.5f;
        }
        else if (distancia > distanciaIdeal + 1f)
        {
            rumbo += haciaJugador.normalized * 0.5f;
        }

        return rumbo.normalized;
    }

    // Se aleja para recuperar una distancia de combate adecuada.
    private Vector3 CalcularRumboReposicion(Vector3 haciaJugador)
    {
        Vector3 rumbo = -haciaJugador.normalized;

        rumbo += ObtenerDireccionLateral(haciaJugador)
            * intensidadRodeo;

        return rumbo.normalized;
    }

    // Calcula la dirección lateral según el sentido de rodeo.
    private Vector3 ObtenerDireccionLateral(Vector3 haciaJugador)
    {
        Vector3 lateral = Vector3.Cross(
            Vector3.up,
            haciaJugador.normalized
        );

        return lateral * direccionRodeo;
    }

    private bool TieneArmasLaterales()
    {
        return analisisArmas != null &&
               analisisArmas.TieneArmasLaterales();
    }

    // Obtiene la distancia horizontal al jugador.
    private float DistanciaAlJugador()
    {
        if (jugador == null)
            return Mathf.Infinity;

        if (percepcion != null)
            return percepcion.ObtenerDistanciaJugador(jugador);

        Vector3 posicionEnemigo = transform.position;
        Vector3 posicionJugador = jugador.position;

        posicionEnemigo.y = 0f;
        posicionJugador.y = 0f;

        return Vector3.Distance(
            posicionEnemigo,
            posicionJugador
        );
    }

    // Calcula el porcentaje de vida restante.
    private float ObtenerPorcentajeVida()
    {
        if (stats.vidaMax <= 0f)
            return 100f;

        return stats.vida / stats.vidaMax * 100f;
    }

    // Cambia aleatoriamente el sentido de rodeo.
    private void CambiarDireccionRodeo()
    {
        direccionRodeo = Random.value < 0.5f ? -1 : 1;
        temporizadorRodeo = tiempoCambioRodeo;
    }

    public EstadoIA ObtenerEstadoActual()
    {
        return estadoActual;
    }
}
