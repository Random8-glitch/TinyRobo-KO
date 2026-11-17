using System.Collections;
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
    private Transform jugador;

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
    [SerializeField, Min(0.05f)] private float intervaloDecision = 0.25f;

    [Header("Antibloqueo")]
    [SerializeField, Min(0.1f)] private float tiempoMaximoSinAvance = 1.5f;
    [SerializeField, Min(0.01f)] private float distanciaMinimaAvance = 0.15f;
    [SerializeField, Min(0.1f)] private float duracionRumboEscape = 1.2f;
    [SerializeField, Range(0f, 1f)] private float fuerzaPersecucionEscape = 0.75f;

    [Header("Estado actual")]
    [SerializeField] private EstadoIA estadoActual = EstadoIA.Perseguir;

    private RoboMovEnemy movimiento;
    private EnemyStats stats;

    private float temporizadorDecision;
    private float temporizadorRodeo;
    private float temporizadorSinAvance;
    private float temporizadorEscape;

    private int direccionRodeo = 1;
    private Vector3 ultimaPosicion;
    private bool escapePorLaIzquierda = true;
    private Coroutine rutinaBuscarJugador;

    private void Awake()
    {
        movimiento = GetComponent<RoboMovEnemy>();
        stats = GetComponent<EnemyStats>();

        if (percepcion == null)
            percepcion = GetComponent<EnemyPerception>();

        if (analisisArmas == null)
            analisisArmas = GetComponent<EnemyWeaponAnalysis>();
    }

    private void Start()
    {
        ultimaPosicion = transform.position;
        CambiarDireccionRodeo();

        rutinaBuscarJugador = StartCoroutine(
            EsperarJugadorInstanciado()
        );
    }

    private void Update()
    {
        ComprobarJugadorActual();

        if (jugador == null ||
            stats == null ||
            movimiento == null)
        {
            return;
        }

        ActualizarDeteccionDeBloqueo();

        temporizadorDecision -= Time.deltaTime;
        temporizadorRodeo -= Time.deltaTime;
        temporizadorEscape -= Time.deltaTime;

        if (temporizadorRodeo <= 0f)
            CambiarDireccionRodeo();

        if (temporizadorDecision <= 0f)
        {
            EvaluarSituacion();
            temporizadorDecision = intervaloDecision;
        }

        EjecutarEstado();
    }

    private void ActualizarDeteccionDeBloqueo()
    {
        Vector3 desplazamiento =
            transform.position - ultimaPosicion;

        desplazamiento.y = 0f;

        if (desplazamiento.sqrMagnitude <
            distanciaMinimaAvance * distanciaMinimaAvance)
        {
            temporizadorSinAvance += Time.deltaTime;
        }
        else
        {
            temporizadorSinAvance = 0f;
        }

        ultimaPosicion = transform.position;

        if (temporizadorSinAvance >= tiempoMaximoSinAvance &&
            temporizadorEscape <= 0f)
        {
            ActivarRumboEscape();
        }
    }

    private void ActivarRumboEscape()
    {
        temporizadorSinAvance = 0f;
        temporizadorEscape = duracionRumboEscape;
        escapePorLaIzquierda = !escapePorLaIzquierda;

        direccionRodeo = escapePorLaIzquierda ? -1 : 1;
    }

    private void EvaluarSituacion()
    {
        float distancia = DistanciaAlJugador();
        float porcentajeVida = ObtenerPorcentajeVida();

        if (estadoActual == EstadoIA.Retirarse &&
            distancia < distanciaReanudarAtaque)
        {
            return;
        }

        if (porcentajeVida <= porcentajeVidaRetirada &&
            distancia < distanciaReanudarAtaque)
        {
            estadoActual = EstadoIA.Retirarse;
            return;
        }

        if (distancia > distanciaPersecucion)
        {
            estadoActual = EstadoIA.Perseguir;
        }
        else if (distancia < distanciaIdeal * 0.6f)
        {
            estadoActual = EstadoIA.Reposicionarse;
        }
        else
        {
            estadoActual = EstadoIA.Combatir;
        }
    }

    private void EjecutarEstado()
    {
        Vector3 haciaJugador =
            jugador.position - transform.position;

        haciaJugador.y = 0f;

        if (haciaJugador.sqrMagnitude < 0.01f)
            return;

        Vector3 rumboDeseado;

        if (temporizadorEscape > 0f)
        {
            rumboDeseado =
                CalcularRumboEscape(haciaJugador);
        }
        else
        {
            switch (estadoActual)
            {
                case EstadoIA.Perseguir:
                    rumboDeseado =
                        haciaJugador.normalized;
                    break;

                case EstadoIA.Combatir:
                    rumboDeseado =
                        CalcularRumboCombate(haciaJugador);
                    break;

                case EstadoIA.Retirarse:
                    rumboDeseado =
                        CalcularRumboRetirada(haciaJugador);
                    break;

                case EstadoIA.Rodear:
                    rumboDeseado =
                        CalcularRumboRodeo(haciaJugador);
                    break;

                case EstadoIA.Reposicionarse:
                    rumboDeseado =
                        CalcularRumboReposicion(haciaJugador);
                    break;

                default:
                    rumboDeseado =
                        haciaJugador.normalized;
                    break;
            }
        }

        if (estadoActual == EstadoIA.Combatir &&
            analisisArmas != null &&
            temporizadorEscape <= 0f)
        {
            rumboDeseado = analisisArmas.ObtenerRumboDeAtaque(
                jugador,
                rumboDeseado
            );
        }

        if (percepcion != null)
        {
            rumboDeseado =
                percepcion.AjustarRumbo(rumboDeseado);
        }

        movimiento.SetDireccionIA(rumboDeseado);
    }

    private Vector3 CalcularRumboEscape(
        Vector3 haciaJugador)
    {
        Vector3 direccionJugador =
            haciaJugador.normalized;

        Vector3 lateral = Vector3.Cross(
            Vector3.up,
            direccionJugador
        );

        lateral *= escapePorLaIzquierda ? -1f : 1f;

        Vector3 rumbo =
            lateral +
            direccionJugador * fuerzaPersecucionEscape;

        rumbo.y = 0f;

        if (rumbo.sqrMagnitude < 0.01f)
            return direccionJugador;

        return rumbo.normalized;
    }

    private Vector3 CalcularRumboCombate(
        Vector3 haciaJugador)
    {
        float distancia = haciaJugador.magnitude;
        Vector3 direccionJugador = haciaJugador.normalized;

        Vector3 rumbo = direccionJugador * 0.5f;

        if (distancia > distanciaIdeal + 1f)
        {
            rumbo += direccionJugador;
        }
        else if (distancia < distanciaIdeal - 1f)
        {
            rumbo -= direccionJugador;
        }

        if (rodearJugador &&
            TieneArmasLaterales())
        {
            rumbo += ObtenerDireccionLateral(haciaJugador)
                * intensidadRodeo;
        }

        if (rumbo.sqrMagnitude < 0.01f)
            rumbo = direccionJugador;

        return rumbo.normalized;
    }

    private Vector3 CalcularRumboRetirada(
        Vector3 haciaJugador)
    {
        Vector3 rumbo =
            -haciaJugador.normalized;

        if (rodearJugador &&
            TieneArmasLaterales())
        {
            rumbo += ObtenerDireccionLateral(haciaJugador)
                * intensidadRetiradaDiagonal;
        }

        return rumbo.normalized;
    }

    private Vector3 CalcularRumboRodeo(
        Vector3 haciaJugador)
    {
        Vector3 rumbo =
            ObtenerDireccionLateral(haciaJugador);

        float distancia = haciaJugador.magnitude;

        if (distancia < distanciaIdeal)
        {
            rumbo -= haciaJugador.normalized * 0.5f;
        }
        else if (distancia > distanciaIdeal + 1f)
        {
            rumbo += haciaJugador.normalized         * 0.5f;
        }

        return rumbo.normalized;
    }

    private Vector3 CalcularRumboReposicion(
        Vector3 haciaJugador)
    {
        Vector3 rumbo =
            -haciaJugador.normalized;

        rumbo += ObtenerDireccionLateral(haciaJugador)
            * intensidadRodeo;

        return rumbo.normalized;
    }

    private Vector3 ObtenerDireccionLateral(
        Vector3 haciaJugador)
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

    private float DistanciaAlJugador()
    {
        if (jugador == null)
            return Mathf.Infinity;

        if (percepcion != null)
            return percepcion.ObtenerDistanciaJugador(jugador);

        Vector3 posicionEnemigo =
            transform.position;

        Vector3 posicionJugador =
            jugador.position;

        posicionEnemigo.y = 0f;
        posicionJugador.y = 0f;

        return Vector3.Distance(
            posicionEnemigo,
            posicionJugador
        );
    }

    private float ObtenerPorcentajeVida()
    {
        if (stats.vidaMax <= 0f)
            return 100f;

        return stats.vida / stats.vidaMax * 100f;
    }

    private void CambiarDireccionRodeo()
    {
        direccionRodeo =
            Random.value < 0.5f ? -1 : 1;

        temporizadorRodeo =
            Mathf.Max(tiempoCambioRodeo, 0.1f);
    }

    public EstadoIA ObtenerEstadoActual()
    {
        return estadoActual;
    }

    private IEnumerator EsperarJugadorInstanciado()
    {
        while (jugador == null)
        {
            jugador = ObtenerJugadorActual();

            if (jugador == null)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }

        rutinaBuscarJugador = null;
    }

    private Transform ObtenerJugadorActual()
    {
        PlayerStats[] jugadores =
            FindObjectsByType<PlayerStats>(
                FindObjectsSortMode.None
            );

        foreach (PlayerStats jugadorEncontrado in jugadores)
        {
            if (jugadorEncontrado == null ||
                !jugadorEncontrado.gameObject.activeInHierarchy)
            {
                continue;
            }

            return jugadorEncontrado.transform.root;
        }

        return null;
    }

    private void ComprobarJugadorActual()
    {
        if (jugador != null &&
            jugador.gameObject.activeInHierarchy)
        {
            return;
        }

        jugador = null;

        if (rutinaBuscarJugador == null)
        {
            rutinaBuscarJugador = StartCoroutine(
                EsperarJugadorInstanciado()
            );
        }
    }
}
