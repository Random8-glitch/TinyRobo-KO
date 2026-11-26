using UnityEngine;
using System.Collections;

public class RoboMovEnemy : MonoBehaviour
{
    [Header("Velocidad")]
    [SerializeField] public float velocidad = 5f;

    [SerializeField] private float velocidadMinima = 3f;
    [SerializeField] private float velocidadMaxima = 9f;
    [SerializeField] private float aceleracion = 0.5f;

    private float velocidadActual;

    [Header("Impactos y pérdida de velocidad")]
    [SerializeField] private float velocidadImpactoMinima = 1f;
    [SerializeField] private float velocidadImpactoMaxima = 10f;

    [SerializeField] private float perdidaMaximaPared = 5f;
    [SerializeField] private float perdidaMaximaPlayer = 4f;

    [SerializeField] private float velocidadMinimaTrasImpacto = 1f;

    [SerializeField] private float multiplicadorImpactoFrontal = 1.25f;
    [SerializeField] private float multiplicadorImpactoLateral = 0.65f;

    [Header("Giro")]
    [SerializeField] public float tiempoDeGiro = 1f;
    [SerializeField] public float velocidadGiroMinima = 10f;
    [SerializeField] public float velocidadGiroMaxima = 30f;

    [SerializeField] public float distanciaRebote = 0.5f;
    [SerializeField] public float distanciaRebotePlayer = 1f;

    [SerializeField] private float tiempoAturdido = 1f;

    // Control de rumbo por IA
    private bool controlIA = false;
    private Vector3 direccionIA;

    private Rigidbody rb;
    private bool girando = false;

    private int wallLayer;
    private int playerLayer;

    private GameObject player;

    private float velocidadGiroActual;

    private bool empujado = false;
    private bool golpeoParedDuranteEmpuje = false;
    private bool aturdido = false;
    private bool cancelarEmpuje = false;

    [Header("Retroceso tras impacto")]
    [SerializeField] private float fuerzaRetrocesoMinima = 1.5f;
    [SerializeField] private float fuerzaRetrocesoMaxima = 8f;
    [SerializeField] private float duracionRetroceso = 0.18f;

    [SerializeField] private float velocidadReferenciaRebote = 5f;

    private bool retrocesoActivo;

    [SerializeField] private float duracionRebotePared = 0.25f;

    private bool empujeParedActivo;

    [Header("Control de velocidad por IA")]
    [SerializeField] private bool permitirControlVelocidadIA = true;

    [SerializeField, Min(0.01f)]
    private float desaceleracion = 4f;

    private float velocidadObjetivo;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        wallLayer = LayerMask.NameToLayer("Wall");
        playerLayer = LayerMask.NameToLayer("Player");

        player = BuscarPlayer();

        velocidadGiroActual = Random.Range(
            velocidadGiroMinima,
            velocidadGiroMaxima
        );

        // Inicializar velocidad dentro de los límites.
        velocidadMinima = Mathf.Max(0.01f, velocidadMinima);
        velocidadMinimaTrasImpacto = Mathf.Max(
            velocidadMinima,
            velocidadMinimaTrasImpacto
        );

        velocidadMaxima = Mathf.Max(
            velocidadMinima,
            velocidadMaxima
        );

        velocidadActual = Mathf.Clamp(
            velocidad,
            velocidadMinima,
            velocidadMaxima
        );

        velocidadObjetivo = velocidadActual;
    }

    private void Update()
    {
        if (girando || empujado || aturdido || retrocesoActivo)
            return;

        Vector3 direccion;

        if (controlIA)
        {
            direccion = direccionIA;
        }
        else
        {
            if (player == null)
                return;

            direccion =
                player.transform.position - transform.position;
        }

        direccion.y = 0f;

        if (direccion.sqrMagnitude < 0.01f)
            return;

        Quaternion rotacionObjetivo =
            Quaternion.LookRotation(direccion.normalized);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                rotacionObjetivo,
                velocidadGiroActual * Time.deltaTime
            );
    }

    // ================================
    // CONTROL DE DIRECCIÓN IA
    // ================================

    public void SetDireccionIA(Vector3 direccion)
    {
        controlIA = true;

        direccion.y = 0f;

        if (direccion.sqrMagnitude > 0.01f)
        {
            direccionIA = direccion.normalized;
        }
    }

    // ================================
    // MOVIMIENTO
    // ================================

    private void FixedUpdate()
    {
        if (girando || empujado || aturdido || retrocesoActivo)
            return;

        // Aumentar velocidad progresivamente.
        Acelerar();

        // Avance automático.
        rb.MovePosition(
            rb.position +
            transform.forward *
            velocidadActual *
            Time.fixedDeltaTime
        );
    }

    // ================================
    // SISTEMA DE ACELERACIÓN
    // ================================

    private void Acelerar()
    {
        velocidadActual = Mathf.MoveTowards(
            velocidadActual,
            velocidadMaxima,
            aceleracion * Time.fixedDeltaTime
        );
    }

    // ================================
    // PÉRDIDA DE VELOCIDAD POR IMPACTO
    // ================================

    private void PerderVelocidadPorImpacto(
        Collision collision,
        float perdidaMaxima
    )
    {
        if (collision.contactCount == 0)
            return;

        // Obtener fuerza relativa del impacto.
        float fuerzaImpacto = collision.relativeVelocity.magnitude;

        // Respaldo si Unity no proporciona velocidad suficiente.
        if (fuerzaImpacto < 0.1f)
            fuerzaImpacto = velocidadActual;

        // Calcular intensidad del impacto.
        float intensidad = Mathf.InverseLerp(
            velocidadImpactoMinima,
            velocidadImpactoMaxima, // Corregido aquí
            fuerzaImpacto
        );

        // Obtener normal del contacto.
        Vector3 normal = collision.contacts[0].normal;

        // Determinar si fue frontal o lateral.
        float frontalidad = Mathf.Abs(
            Vector3.Dot(transform.forward, normal)
        );

        float multiplicador = Mathf.Lerp(
            multiplicadorImpactoLateral,
            multiplicadorImpactoFrontal,
            frontalidad
        );

        // Calcular pérdida final.
        float perdida = Mathf.Lerp(
            0f,
            perdidaMaxima,
            intensidad
        );

        perdida *= multiplicador;

        // Aplicar pérdida de velocidad.
        velocidadActual -= perdida;

        velocidadActual = Mathf.Max(
            velocidadMinima,
            velocidadMinimaTrasImpacto,
            velocidadActual
        );
    }

    // ================================
    // COLISIONES
    // ================================

    private void OnCollisionEnter(Collision collision)
    {
        // Si está siendo empujado y golpea una pared.
        if (
            empujado &&
            collision.gameObject.layer == wallLayer
        )
        {
            golpeoParedDuranteEmpuje = true;
            cancelarEmpuje = true;

            // También pierde velocidad por el impacto.
            PerderVelocidadPorImpacto(
                collision,
                perdidaMaximaPared
            );
        }

        if (girando || empujado || aturdido)
            return;

        // ================================
        // CHOQUE CON PARED
        // ================================

        if (collision.gameObject.layer == wallLayer)
        {
            PerderVelocidadPorImpacto(
                collision,
                perdidaMaximaPared
            );

            Vector3 normalPared =
                collision.contacts[0].normal;

            rb.MovePosition(
                rb.position +
                normalPared *
                ObtenerDistanciaRebote(
                    collision,
                    distanciaRebote
                )
            );

            velocidadGiroActual = Random.Range(
                velocidadGiroMinima,
                velocidadGiroMaxima
            );

            if (player == null)
                return;

            Vector3 direccion =
                player.transform.position -
                transform.position;

            direccion.y = 0f;

            if (direccion.sqrMagnitude > 0.01f)
            {
                if (IsWallBetween(
                    transform.position,
                    player.transform.position
                ))
                {
                    Vector3 altDir =
                        FindAlternateDirectionTowardsPlayer(direccion);

                    if (altDir != Vector3.zero)
                    {
                        Quaternion rotacionObjetivo =
                            Quaternion.LookRotation(altDir.normalized);

                        StartCoroutine(
                            GirarGradualmente(rotacionObjetivo)
                        );

                        return;
                    }
                    else
                    {
                        float anguloAleatorio =
                            Random.Range(90f, 270f);

                        Quaternion rotacionAleatoria =
                            Quaternion.AngleAxis(
                                anguloAleatorio,
                                Vector3.up
                            );

                        Vector3 nuevaDireccion =
                            rotacionAleatoria * transform.forward;

                        nuevaDireccion.y = 0f;

                        if (nuevaDireccion.sqrMagnitude > 0.01f)
                        {
                            Quaternion rotacionObjetivo =
                                Quaternion.LookRotation(
                                    nuevaDireccion.normalized
                                );

                            StartCoroutine(
                                GirarGradualmente(rotacionObjetivo)
                            );

                            return;
                        }
                    }
                }

                Quaternion rotacionObjetivoDirecto =
                    Quaternion.LookRotation(direccion.normalized);

                StartCoroutine(
                    GirarGradualmente(rotacionObjetivoDirecto)
                );
            }
        }

        // ================================
        // CHOQUE CON PLAYER
        // ================================

        else if (collision.gameObject.layer == playerLayer)
        {
            PerderVelocidadPorImpacto(
                collision,
                perdidaMaximaPlayer
            );

            AplicarRetrocesoPorImpacto(collision);

            float anguloAleatorio =
                Random.Range(90f, 270f);

            Quaternion rotacionAleatoria =
                Quaternion.AngleAxis(
                    anguloAleatorio,
                    Vector3.up
                );

            Vector3 nuevaDireccion =
                rotacionAleatoria * transform.forward;

            nuevaDireccion.y = 0f;

            if (nuevaDireccion.sqrMagnitude > 0.01f)
            {
                Quaternion rotacionObjetivo =
                    Quaternion.LookRotation(
                        nuevaDireccion.normalized
                    );

                StartCoroutine(
                    GirarGradualmente(rotacionObjetivo)
                );
            }
        }
    }

    // ================================
    // BUSCAR PLAYER
    // ================================

    private GameObject BuscarPlayer()
    {
        GameObject[] objetos =
            FindObjectsByType<GameObject>(
                FindObjectsSortMode.None
            );

        foreach (GameObject obj in objetos)
        {
            if (obj.layer == playerLayer)
                return obj;
        }

        return null;
    }

    // ================================
    // EMPUJE
    // ================================

    public void Empujar(
        Vector3 direccion,
        float distancia,
        float duracion
    )
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (empujado || aturdido)
            return;

        StartCoroutine(
            EmpujeCoroutine(
                direccion.normalized,
                distancia,
                duracion
            )
        );
    }

    private IEnumerator EmpujeCoroutine(
        Vector3 direccion,
        float distancia,
        float duracion
    )
    {
        empujado = true;
        golpeoParedDuranteEmpuje = false;
        cancelarEmpuje = false;

        Vector3 inicio = transform.position;

        Vector3 destino =
            inicio + direccion * distancia;

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            if (cancelarEmpuje)
                break;

            tiempo += Time.deltaTime;

            float t = tiempo / duracion;

            rb.MovePosition(
                Vector3.Lerp(
                    inicio,
                    destino,
                    t
                )
            );

            yield return null;
        }

        empujado = false;

        if (golpeoParedDuranteEmpuje)
        {
            EnemyStats stats =
                GetComponent<EnemyStats>();

            if (stats != null)
            {
                stats.RecibirDanio(5f);
            }

            StartCoroutine(Aturdir());
        }
    }

    // ================================
    // ATURDIMIENTO
    // ================================

    private IEnumerator Aturdir()
    {
        aturdido = true;
        girando = false;

        yield return new WaitForSeconds(
            tiempoAturdido
        );

        aturdido = false;
    }

    // ================================
    // GIRO GRADUAL
    // ================================

    private IEnumerator GirarGradualmente(
        Quaternion rotacionObjetivo
    )
    {
        girando = true;

        Quaternion rotacionInicial =
            transform.rotation;

        float tiempo = 0f;

        while (tiempo < tiempoDeGiro)
        {
            if (aturdido)
            {
                girando = false;
                yield break;
            }

            tiempo += Time.deltaTime;

            float t = tiempo / tiempoDeGiro;

            transform.rotation =
                Quaternion.Slerp(
                    rotacionInicial,
                    rotacionObjetivo,
                    t
                );

            yield return null;
        }

        transform.rotation = rotacionObjetivo;

        girando = false;
    }

    // ================================
    // DETECCIÓN DE PAREDES
    // ================================

    private bool IsWallBetween(Vector3 from, Vector3 to)
    {
        if (player == null)
            return false;

        Vector3 origin = from + Vector3.up * 0.5f;
        Vector3 dir = to - origin;

        float dist = dir.magnitude;

        if (dist <= 0.01f)
            return false;

        int wallMask = 1 << wallLayer;

        if (Physics.Raycast(
            origin,
            dir.normalized,
            out RaycastHit hit,
            dist,
            wallMask
        ))
        {
            return true;
        }

        return false;
    }

    // ================================
    // BUSCAR DIRECCIÓN ALTERNATIVA
    // ================================

    private Vector3 FindAlternateDirectionTowardsPlayer(
        Vector3 direccionHaciaPlayer
    )
    {
        if (direccionHaciaPlayer.sqrMagnitude < 0.01f)
            return Vector3.zero;

        Vector3 dir = direccionHaciaPlayer.normalized;
        int wallMask = 1 << wallLayer;

        float distToPlayer = Vector3.Distance(
            transform.position,
            player.transform.position
        );

        float checkDist = Mathf.Clamp(
            distToPlayer,
            1.5f,
            4f
        );

        float[] angles = new float[]
        {
            30f, -30f, 60f, -60f,
            90f, -90f, 135f, -135f, 180f
        };

        foreach (float angle in angles)
        {
            Vector3 candidate =
                Quaternion.Euler(0f, angle, 0f) * dir;

            Vector3 origin =
                transform.position + Vector3.up * 0.5f;

            if (!Physics.Raycast(
                origin,
                candidate,
                checkDist,
                wallMask
            ))
            {
                Vector3 probeOrigin =
                    origin + candidate * 0.5f;

                Vector3 toPlayer =
                    player.transform.position - probeOrigin;

                if (!Physics.Raycast(
                    probeOrigin,
                    toPlayer.normalized,
                    toPlayer.magnitude,
                    wallMask
                ))
                {
                    return candidate;
                }
            }
        }

        return Vector3.zero;
    }

    private void AplicarRetrocesoPorImpacto(Collision collision)
    {
        if (collision.contactCount == 0 || rb == null)
            return;

        Vector3 normal = collision.contacts[0].normal;
        normal.y = 0f;

        if (normal.sqrMagnitude < 0.01f)
            return;

        normal.Normalize();

        float velocidadImpacto =
            collision.relativeVelocity.magnitude;

        if (velocidadImpacto < 0.1f)
            velocidadImpacto = velocidadActual;

        float intensidad = Mathf.InverseLerp(
            velocidadImpactoMinima,
            velocidadImpactoMaxima,
            velocidadImpacto
        );

        float fuerza = Mathf.Lerp(
            fuerzaRetrocesoMinima,
            fuerzaRetrocesoMaxima,
            intensidad
        );

        rb.AddForce(
            normal * fuerza,
            ForceMode.Impulse
        );

        StartCoroutine(DesactivarRetroceso());
    }

    private IEnumerator DesactivarRetroceso()
    {
        retrocesoActivo = true;

        yield return new WaitForSeconds(
            duracionRetroceso
        );

        retrocesoActivo = false;
    }

    private float ObtenerDistanciaRebote(
        Collision collision,
        float distanciaBase
    )
    {
        float velocidadImpacto = collision.relativeVelocity.magnitude;

        if (velocidadImpacto < 0.1f)
            velocidadImpacto = velocidadActual;

        float multiplicador =
            velocidadImpacto / velocidadReferenciaRebote;

        return distanciaBase * Mathf.Max(1f, multiplicador);
    }

    private IEnumerator RetrocesoGradualContraPared(
        Vector3 normalPared,
        float distancia
    )
    {
        if (empujeParedActivo)
            yield break;

        empujeParedActivo = true;

        normalPared.y = 0f;

        if (normalPared.sqrMagnitude < 0.01f)
        {
            empujeParedActivo = false;
            yield break;
        }

        normalPared.Normalize();

        Vector3 inicio = rb.position;
        Vector3 destino = inicio + normalPared * distancia;
        float tiempo = 0f;

        while (tiempo < duracionRebotePared)
        {
            tiempo += Time.fixedDeltaTime;

            float progreso = Mathf.Clamp01(
                tiempo / duracionRebotePared
            );

            float progresoSuavizado =
                progreso * progreso * (3f - 2f * progreso);

            rb.MovePosition(
                Vector3.Lerp(
                    inicio,
                    destino,
                    progresoSuavizado
                )
            );

            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(destino);
        empujeParedActivo = false;
    }

    private void AjustarVelocidad()
    {
        float velocidadCambio;

        if (velocidadObjetivo > velocidadActual)
        {
            velocidadCambio = aceleracion;
        }
        else
        {
            velocidadCambio = desaceleracion;
        }

        velocidadActual = Mathf.MoveTowards(
            velocidadActual,
            velocidadObjetivo,
            velocidadCambio * Time.fixedDeltaTime
        );

        velocidadActual = Mathf.Max(
            velocidadMinima,
            velocidadActual
        );
    }

    public void AcelerarPorIA()
    {
        if (!permitirControlVelocidadIA)
            return;

        velocidadObjetivo = velocidadMaxima;
    }

    public void DesacelerarPorIA()
    {
        if (!permitirControlVelocidadIA)
            return;

        velocidadObjetivo = velocidadMinima;
    }

    public void EstablecerVelocidadObjetivoIA(
        float nuevaVelocidadObjetivo
    )
    {
        if (!permitirControlVelocidadIA)
            return;

        velocidadObjetivo = Mathf.Clamp(
            nuevaVelocidadObjetivo,
            velocidadMinima,
            velocidadMaxima
        );
    }
}
