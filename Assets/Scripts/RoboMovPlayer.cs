using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoboMovPlayer : MonoBehaviour
{
    [Header("Velocidad automática")]
    [SerializeField] public float velocidad = 5f;

    [SerializeField] private float velocidadMinima = 3f;
    [SerializeField] private float velocidadMaxima = 9f;
    [SerializeField] private float aceleracion = 0.5f;

    private float velocidadActual;

    [Header("Impactos y pérdida de velocidad")]
    [SerializeField] private float velocidadImpactoMinima = 1f;
    [SerializeField] private float velocidadImpactoMaxima = 10f;

    [SerializeField] private float perdidaMaximaPared = 5f;
    [SerializeField] private float perdidaMaximaEnemigo = 4f;

    [SerializeField] private float velocidadMinimaTrasImpacto = 1f;

    [SerializeField] private float multiplicadorImpactoFrontal = 1.25f;
    [SerializeField] private float multiplicadorImpactoLateral = 0.65f;

    [Header("Giro")]
    [SerializeField] public float tiempoDeGiro = 1f;
    [SerializeField] public float velocidadGiroManual = 30f;

    [SerializeField] public float distanciaRebote = 0.5f;
    [SerializeField] public float distanciaReboteEnemigo = 1f;

    private EquiparChips equiparChips;
    private PlayerDataManager playerDataManager;

    [Header("Movimiento manual")]
    [SerializeField] private float tiempoAceleracionManual = 1f;

    private float velocidadManualActual = 0f;
    private int direccionManualAnterior = 0;

    private Rigidbody rb;
    private bool girando = false;

    private int wallLayer;
    private int enemyLayer;

    private bool empujado = false;
    private bool golpeoParedDuranteEmpuje = false;
    private bool aturdido = false;
    private bool cancelarEmpuje = false;

    [SerializeField] private float tiempoAturdido = 1f;
    
    [SerializeField] private float fuerzaRetrocesoMinima = 1.5f;
    [SerializeField] private float fuerzaRetrocesoMaxima = 8f;
    [SerializeField] private float duracionRetroceso = 0.18f;

    private bool retrocesoActivo;

    [SerializeField] private float velocidadReferenciaRebote = 5f;
    [SerializeField] private float duracionRebotePared = 0.25f;

    private bool empujeParedActivo;

    [Header("Modificador de movimiento")]
    [SerializeField] private float tiempoAceleracionModificador = 0.25f;
    [SerializeField] private float tiempoDesaceleracionModificador = 0.5f;

    private float modificadorMovimientoActual;

    [Header("Interfaz")]
    [SerializeField] private TMP_Text textoVelocidad;

    [Header("Stamina del modificador")]
    [SerializeField] private string nombreBarraStamina =
        "BarraStamina";

    private Slider barraStamina;
    [SerializeField] private float staminaMaxima = 100f;
    [SerializeField] private float consumoStamina = 25f;
    [SerializeField] private float regeneracionStamina = 15f;

    private float staminaActual;

    private void Start()
    {
        GameObject objetoBarraStamina =
            GameObject.Find(nombreBarraStamina);

        if (objetoBarraStamina != null)
        {
            barraStamina =
                objetoBarraStamina.GetComponent<Slider>();
        }

        if (barraStamina != null)
        {
            staminaActual = staminaMaxima;
            barraStamina.maxValue = staminaMaxima;
            barraStamina.value = staminaActual;
            barraStamina.gameObject.SetActive(false);
        }

        rb = GetComponent<Rigidbody>();

        equiparChips = GetComponent<EquiparChips>();
        playerDataManager = PlayerDataManager.Instance;

        wallLayer = LayerMask.NameToLayer("Wall");
        enemyLayer = LayerMask.NameToLayer("Enemy");

        velocidadActual = Mathf.Clamp(
            velocidadMinima,
            0f,
            velocidadMaxima
        );

        staminaActual = staminaMaxima;

        if (barraStamina != null)
        {
            barraStamina.maxValue = staminaMaxima;
            barraStamina.value = staminaActual;
            barraStamina.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (girando || empujado || aturdido)
            return;

        float giro = 0f;

        if (Input.GetKey(KeyCode.A))
            giro = -1f;
        else if (Input.GetKey(KeyCode.D))
            giro = 1f;

        transform.Rotate(
            Vector3.up,
            giro * velocidadGiroManual * Time.deltaTime
        );
    }

    private void FixedUpdate()
    {
        if (retrocesoActivo || empujeParedActivo)
            return;

        // Recuperar referencia si el manager se inicializa después.
        if (playerDataManager == null)
            playerDataManager = PlayerDataManager.Instance;

        // ToggleMov activo: movimiento automático.
        // ToggleMov desactivado: movimiento manual.
        if (playerDataManager != null && !playerDataManager.ToggleMov)
            MovimientoManual();
        else
            MovimientoAutomatico();
    }

    // ================================
    // COLISIONES
    // ================================

    private void OnCollisionEnter(Collision collision)
    {
        if (girando)
            return;

        // CHOQUE CON PARED
        if (collision.gameObject.layer == wallLayer)
        {
            if (playerDataManager == null)
                playerDataManager = PlayerDataManager.Instance;

            bool movimientoAutomatico =
                playerDataManager == null ||
                playerDataManager.ToggleMov;

            PerderVelocidadPorImpacto(
                collision,
                perdidaMaximaPared
            );

            Vector3 normalPared = collision.contacts[0].normal;

            if (movimientoAutomatico)
            {
                StartCoroutine(
                    RetrocesoGradualContraPared(
                        normalPared,
                        ObtenerDistanciaRebote(
                            collision,
                            distanciaRebote
                        )
                    )
                );
            }
            else
            {
                // Movimiento manual original:
                // rebote instantáneo y distancia fija.
                rb.MovePosition(
                    rb.position +
                    normalPared * distanciaRebote
                );
            }

            GameObject enemigo = BuscarEnemigo();

            if (enemigo == null)
                return;

            Vector3 direccion =
                enemigo.transform.position -
                transform.position;

            direccion.y = 0f;

            if (direccion.sqrMagnitude > 0.01f)
            {
                Quaternion rotacionObjetivo;

                if (IsWallBetween(
                    transform.position,
                    enemigo.transform.position
                ))
                {
                    Vector3 altDir =
                        FindAlternateDirectionTowardsEnemy(
                            direccion
                        );

                    if (altDir != Vector3.zero)
                    {
                        rotacionObjetivo =
                            Quaternion.LookRotation(
                                altDir.normalized
                            );

                        StartCoroutine(
                            GirarGradualmente(
                                rotacionObjetivo
                            )
                        );

                        return;
                    }
                }

                rotacionObjetivo =
                    Quaternion.LookRotation(
                        direccion.normalized
                );

                StartCoroutine(
                    GirarGradualmente(
                        rotacionObjetivo
                )
                );
            }
        }

        // CHOQUE CON ENEMIGO
        else if (collision.gameObject.layer == enemyLayer)
        {
            PerderVelocidadPorImpacto(
                collision,
                perdidaMaximaEnemigo
            );

            AplicarRetrocesoPorImpacto(collision);

            float anguloAleatorio = Random.Range(90f, 270f);

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
    // PÉRDIDA DE VELOCIDAD POR IMPACTO
    // ================================

    private void PerderVelocidadPorImpacto(
        Collision collision,
        float perdidaMaxima
    )
    {
        // No afectar el movimiento manual.
        if (playerDataManager != null && !playerDataManager.ToggleMov)
            return;

        if (collision.contactCount == 0)
            return;

        // Velocidad relativa del impacto.
        float fuerzaImpacto = collision.relativeVelocity.magnitude;

        // Respaldo por si MovePosition no proporciona
        // una velocidad relativa suficiente.
        if (fuerzaImpacto < 0.1f)
            fuerzaImpacto = velocidadActual;

        // Normalizar intensidad del impacto.
        float intensidad = Mathf.InverseLerp(
            velocidadImpactoMinima,
            velocidadImpactoMaxima,
            fuerzaImpacto
        );

        // Dirección de la superficie de contacto.
        Vector3 normal = collision.contacts[0].normal;

        // Calcular si el impacto fue frontal o lateral.
        float frontalidad = Mathf.Abs(
            Vector3.Dot(transform.forward, normal)
        );

        // Interpolar entre impacto lateral y frontal.
        float multiplicador = Mathf.Lerp(
            multiplicadorImpactoLateral,
            multiplicadorImpactoFrontal,
            frontalidad
        );

        // Calcular pérdida de velocidad.
        float perdida = Mathf.Lerp(
            0f,
            perdidaMaxima,
            intensidad
        );

        perdida *= multiplicador;

        // Aplicar pérdida.
        velocidadActual -= perdida;

        velocidadActual = Mathf.Max(
            velocidadMinimaTrasImpacto,
            velocidadActual
        );
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

    // ================================
    // ACELERACIÓN AUTOMÁTICA
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
    // BUSCAR ENEMIGO
    // ================================

    private GameObject BuscarEnemigo()
    {
        GameObject[] objetos =
            FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in objetos)
        {
            if (obj.layer == enemyLayer)
                return obj;
        }

        return null;
    }

    // ================================
    // GIRO GRADUAL
    // ================================

    private IEnumerator GirarGradualmente(Quaternion rotacionObjetivo)
    {
        girando = true;

        Quaternion rotacionInicial = transform.rotation;
        float tiempo = 0f;

        while (tiempo < tiempoDeGiro)
        {
            tiempo += Time.deltaTime;

            float t = tiempo / tiempoDeGiro;

            transform.rotation = Quaternion.Slerp(
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

        Vector3 destino = inicio + direccion * distancia;

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
            PlayerStats stats = GetComponent<PlayerStats>();

            if (stats != null)
                stats.RecibirDanio(5f);

            StartCoroutine(Aturdir());
        }
    }

    private IEnumerator Aturdir()
    {
        aturdido = true;
        girando = false;

        yield return new WaitForSeconds(tiempoAturdido);

        aturdido = false;
    }

    // ================================
    // DETECCIÓN DE PAREDES
    // ================================

    private bool IsWallBetween(Vector3 from, Vector3 to)
    {
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

    private Vector3 FindAlternateDirectionTowardsEnemy(
        Vector3 direccionHaciaEnemy
    )
    {
        if (direccionHaciaEnemy.sqrMagnitude < 0.01f)
            return Vector3.zero;

        Vector3 dir = direccionHaciaEnemy.normalized;
        int wallMask = 1 << wallLayer;

        float distToEnemy = Vector3.Distance(
            transform.position,
            BuscarEnemigo()?.transform.position ??
            (transform.position + dir * 2f)
        );

        float checkDist = Mathf.Clamp(distToEnemy, 1.5f, 4f);

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

            if (!Physics.Raycast(origin, candidate, checkDist, wallMask))
            {
                Vector3 probeOrigin = origin + candidate * 0.5f;

                GameObject enemigo = BuscarEnemigo();

                Vector3 toEnemy = (enemigo != null)
                    ? enemigo.transform.position - probeOrigin
                    : Vector3.zero;

                if (enemigo == null ||
                    (toEnemy.magnitude > 0.01f &&
                    !Physics.Raycast(
                        probeOrigin,
                        toEnemy.normalized,
                        toEnemy.magnitude,
                        wallMask
                    )))
                {
                    return candidate;
                }
            }
        }

        return Vector3.zero;
    }

    // ================================
    // MODOS DE MOVIMIENTO
    // ================================

    private void MovimientoAutomatico()
    {
        velocidadManualActual = 0f;
        direccionManualAnterior = 0;

        if (girando || empujado || aturdido)
            return;

        Acelerar();

        float modificadorChip = 0f;

        ModificadorStats chip = equiparChips != null
            ? equiparChips.ModificadorEquipado
            : null;

        if (chip != null)
        {
            foreach (EfectoModificador efecto in chip.efectos)
            {
                if (
                    efecto.scriptObjetivo == TipoScript.RoboMov &&
                    efecto.atributo ==
                    AtributoModificable.ModificadorMovimiento
                )
                {
                    modificadorChip += efecto.cantidad;
                }
            }
        }

        bool tieneModificadorMovimiento =
            modificadorChip > 0f;

        if (barraStamina != null)
        {
            barraStamina.gameObject.SetActive(
                tieneModificadorMovimiento
            );
        }

        bool acelerando =
            Input.GetKey(KeyCode.W) &&
            !Input.GetKey(KeyCode.S);

        bool desacelerando =
            Input.GetKey(KeyCode.S) &&
            !Input.GetKey(KeyCode.W);

        if (tieneModificadorMovimiento)
        {
            if (acelerando)
            {
                staminaActual = Mathf.MoveTowards(
                    staminaActual,
                    0f,
                    consumoStamina *
                    Time.fixedDeltaTime
                );
            }
            else
            {
                staminaActual = Mathf.MoveTowards(
                    staminaActual,
                    staminaMaxima,
                    regeneracionStamina *
                    Time.fixedDeltaTime
                );
            }
        }
        else
        {
            staminaActual = staminaMaxima;
        }

        if (barraStamina != null)
        {
            barraStamina.value = staminaActual;
        }

        float modificadorObjetivo = 0f;

        if (acelerando &&
            staminaActual > 0f &&
            tieneModificadorMovimiento)
        {
            // Aumento temporal de velocidad.
            modificadorObjetivo = modificadorChip;
        }
        else if (desacelerando &&
                 tieneModificadorMovimiento)
        {
            // Disminución temporal de velocidad.
            modificadorObjetivo = -modificadorChip;
        }

        float tiempoCambio;

        if (
            Mathf.Abs(modificadorObjetivo) >
            Mathf.Abs(modificadorMovimientoActual)
        )
        {
            tiempoCambio = tiempoAceleracionModificador;
        }
        else
        {
            tiempoCambio = tiempoDesaceleracionModificador;
        }

        tiempoCambio = Mathf.Max(
            0.01f,
            tiempoCambio
        );

        float diferenciaModificador =
            Mathf.Abs(
                modificadorObjetivo -
                modificadorMovimientoActual
            );

        float velocidadCambio =
            diferenciaModificador /
            tiempoCambio *
            Time.fixedDeltaTime;

        modificadorMovimientoActual = Mathf.MoveTowards(
            modificadorMovimientoActual,
            modificadorObjetivo,
            velocidadCambio
        );

        float velocidadFinal =
            velocidadActual +
            modificadorMovimientoActual;

        // No se limita por arriba para permitir superar
        // velocidadMaxima mientras haya stamina.
        velocidadFinal = Mathf.Max(
            velocidadMinimaTrasImpacto,
            velocidadFinal
        );

        ActualizarTextoVelocidad(velocidadFinal);

        rb.MovePosition(
            rb.position +
            transform.forward *
            velocidadFinal *
            Time.fixedDeltaTime
        );
    }

    // ================================
    // MOVIMIENTO MANUAL ORIGINAL
    // ================================

    private void MovimientoManual()
    {
        if (girando || empujado || aturdido)
        {
            velocidadManualActual = 0f;
            direccionManualAnterior = 0;
            modificadorMovimientoActual = 0f;
            return;
        }

        modificadorMovimientoActual = 0f;

        int direccion = 0;

        if (Input.GetKey(KeyCode.W))
            direccion = 1;
        else if (Input.GetKey(KeyCode.S))
            direccion = -1;

        if (direccion == 0)
        {
            velocidadManualActual = 0f;
            direccionManualAnterior = 0;
            return;
        }

        if (direccion != direccionManualAnterior)
            velocidadManualActual = 0f;

        direccionManualAnterior = direccion;

        float velocidadMaximaManual = Mathf.Max(0f, velocidad);
        float duracion = Mathf.Max(0.01f, tiempoAceleracionManual);

        velocidadManualActual = Mathf.MoveTowards(
            velocidadManualActual,
            velocidadMaximaManual,
            velocidadMaximaManual / duracion * Time.fixedDeltaTime
        );

        rb.MovePosition(
            rb.position +
            transform.forward *
            velocidadManualActual *
            Time.fixedDeltaTime
        );
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

            // Suaviza el inicio y el final del desplazamiento.
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

    private void ActualizarTextoVelocidad(float velocidadMostrada)
    {
        if (textoVelocidad == null)
            return;

        textoVelocidad.text =
            $"Velocidad: {velocidadMostrada:0.00}";
    }
}
