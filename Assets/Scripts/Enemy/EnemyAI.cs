using UnityEngine.AI;
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
    [Header("Persecución entre pisos")]
    [SerializeField] private bool usarNavMesh;
    [SerializeField, Min(0.05f)] private float toleranciaAltura = 1.25f;
    [Tooltip("Distancia vertical desde cada pivote hasta sus ruedas o pies.")]
    [SerializeField] private float alturaPivoteRobot;
    [SerializeField] private float alturaPivoteJugador;
    [SerializeField, Min(0.05f)] private float radioMuestreo = 0.8f;
    [SerializeField, Min(0.1f)] private float intervaloRuta = 0.45f;
    [SerializeField] private float desnivelJugador;
    private float proximaBusqueda;
    private float proximaRuta;
    private NavMeshPath ruta;
    private Vector3[] esquinas = new Vector3[0];
    private int esquina;
    private bool caminoDirecto;
    private bool rutaCompleta;

    private Vector3 BaseRobot => transform.position - Vector3.up * alturaPivoteRobot;
    private Vector3 BaseJugador => jugador.position - Vector3.up * alturaPivoteJugador;

    private void Awake()
    {
        movimiento = GetComponent<RoboMovEnemy>();
        stats = GetComponent<EnemyStats>();
        ruta = new NavMeshPath();

        if (percepcion == null)
            percepcion = GetComponent<EnemyPerception>();

        if (analisisArmas == null)
            analisisArmas = GetComponent<EnemyWeaponAnalysis>();
    }

    private void Start()
    {
        ultimaPosicion = transform.position;
        CambiarDireccionRodeo();

        movimiento.DetenerIA();
    }

    private void Update()
    {
        ComprobarJugadorActual();

        if (jugador == null ||
            stats == null ||
            movimiento == null)
        {
            if (movimiento != null) movimiento.DetenerIA();
            return;
        }

        desnivelJugador = BaseJugador.y - BaseRobot.y;
        if (usarNavMesh && Time.time >= proximaRuta)
        {
            ActualizarRuta();
            proximaRuta = Time.time + intervaloRuta;
            temporizadorDecision = 0f;
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

        if (!movimiento.IntentandoAvanzar || temporizadorEscape > 0f ||
            desplazamiento.sqrMagnitude >= distanciaMinimaAvance * distanciaMinimaAvance)
        {
            ultimaPosicion = transform.position;
            temporizadorSinAvance = 0f;
            return;
        }

        temporizadorSinAvance += Time.deltaTime;
        if (temporizadorSinAvance >= tiempoMaximoSinAvance)
        {
            ultimaPosicion = transform.position;
            if (usarNavMesh)
            {
                proximaRuta = 0f;
                temporizadorSinAvance = 0f;
            }
            else if (estadoActual != EstadoIA.Retirarse)
                ActivarRumboEscape();
        }
    }

    private void ActualizarRuta()
    {
        esquina = 1;
        caminoDirecto = false;
        rutaCompleta = false;
        if (!NavMesh.SamplePosition(BaseRobot, out NavMeshHit inicio, radioMuestreo, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(BaseJugador, out NavMeshHit fin, radioMuestreo, NavMesh.AllAreas))
            return;

        rutaCompleta = NavMesh.CalculatePath(inicio.position, fin.position, NavMesh.AllAreas, ruta)
            && ruta.status == NavMeshPathStatus.PathComplete;
        if (!rutaCompleta) return;
        esquinas = ruta.corners;
        caminoDirecto = !NavMesh.Raycast(inicio.position, fin.position, out _, NavMesh.AllAreas);
    }

    private Vector3 SiguienteTramo()
    {
        if (!rutaCompleta || esquinas.Length < 2) return Vector3.zero;
        // Solo adelantar una esquina si el siguiente tramo es transitable desde aquí.
        if (!NavMesh.SamplePosition(BaseRobot, out NavMeshHit inicio, radioMuestreo, NavMesh.AllAreas))
            return Vector3.zero;
        while (esquina + 1 < esquinas.Length &&
               !NavMesh.Raycast(inicio.position, esquinas[esquina + 1], out _, NavMesh.AllAreas))
            esquina++;
        Vector3 tramo = esquinas[esquina] - inicio.position;
        tramo.y = 0f;
        return tramo;
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
        if (Mathf.Abs(desnivelJugador) > toleranciaAltura || (usarNavMesh && !caminoDirecto))
        {
            estadoActual = EstadoIA.Perseguir;
            return;
        }
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

        bool seguirRuta = usarNavMesh && estadoActual == EstadoIA.Perseguir;
        Vector3 tramo = seguirRuta ? SiguienteTramo() : haciaJugador;
        if ((usarNavMesh && !rutaCompleta) || tramo.sqrMagnitude < 0.0001f ||
            (!usarNavMesh && Mathf.Abs(desnivelJugador) > toleranciaAltura && haciaJugador.sqrMagnitude < 1f))
        {
            movimiento.DetenerIA();
            return;
        }

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
                    rumboDeseado = tramo.normalized;
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

        bool persiguiendo = estadoActual == EstadoIA.Perseguir;
        float alcance = persiguiendo ? tramo.magnitude : float.PositiveInfinity;
        if (percepcion != null)
        {
            Vector3 baseSensor = BaseRobot;
            if (usarNavMesh && NavMesh.SamplePosition(baseSensor, out NavMeshHit apoyo, radioMuestreo, NavMesh.AllAreas))
                baseSensor = apoyo.position;
            rumboDeseado = percepcion.AjustarRumbo(rumboDeseado, alcance, usarNavMesh, baseSensor);
        }
        if (rumboDeseado.sqrMagnitude < 0.001f)
        {
            movimiento.DetenerIA();
            return;
        }

        movimiento.ConducirIA(rumboDeseado, alcance,
            usarNavMesh, alturaPivoteRobot, radioMuestreo);
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
            rumbo += haciaJugador.normalized * 0.5f;
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

            return jugadorEncontrado.transform;
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
        if (Time.time < proximaBusqueda) return;
        proximaBusqueda = Time.time + 0.1f;
        jugador = ObtenerJugadorActual();
        movimiento.AsignarObjetivoIA(jugador);
        proximaRuta = 0f;
        temporizadorDecision = 0f;
        temporizadorSinAvance = 0f;
        temporizadorEscape = 0f;
        ultimaPosicion = transform.position;
    }
}
