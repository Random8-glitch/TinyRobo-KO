using UnityEngine;
using System.Collections;

public class RoboMovPlayer : MonoBehaviour
{
    [SerializeField] public float velocidad = 5f;
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

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        equiparChips = GetComponent<EquiparChips>();
        playerDataManager = PlayerDataManager.Instance;

        wallLayer = LayerMask.NameToLayer("Wall");
        enemyLayer = LayerMask.NameToLayer("Enemy");
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
        // Recuperar la referencia si el manager se inicializa después.
        if (playerDataManager == null)
            playerDataManager = PlayerDataManager.Instance;

        // ToggleMov activo: movimiento automático original.
        // ToggleMov desactivado: movimiento manual con W y S.
        if (playerDataManager != null && !playerDataManager.ToggleMov)
            MovimientoManual();
        else
            MovimientoAutomatico();
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("Choqué con: " + collision.gameObject.name);

        if (girando)
            return;

        // CHOQUE CON PARED
        if (collision.gameObject.layer == wallLayer)
        {
            Vector3 normalPared = collision.contacts[0].normal;

            rb.MovePosition(
                rb.position + normalPared * distanciaRebote
            );

            GameObject enemigo = BuscarEnemigo();

            if (enemigo == null)
                return;

            Vector3 direccion = enemigo.transform.position - transform.position;
            direccion.y = 0f;

            if (direccion.sqrMagnitude > 0.01f)
            {
                // Declarar una única vez la rotación objetivo para evitar sombras de variables
                Quaternion rotacionObjetivo;

                // Si hay una pared entre el jugador y el enemigo, buscar dirección alternativa
                if (IsWallBetween(transform.position, enemigo.transform.position))
                {
                    Vector3 altDir = FindAlternateDirectionTowardsEnemy(direccion);
                    if (altDir != Vector3.zero)
                    {
                        rotacionObjetivo = Quaternion.LookRotation(altDir.normalized);
                        StartCoroutine(GirarGradualmente(rotacionObjetivo));
                        return;
                    }
                    else
                    {
                        // Si no se encuentra alternativa, girar en ángulo aleatorio para intentar liberarse
                        float anguloAleatorio = Random.Range(60f, 180f);
                        Quaternion rotacionAleatoria = Quaternion.AngleAxis(anguloAleatorio, Vector3.up);
                        Vector3 nuevaDireccion = rotacionAleatoria * transform.forward;
                        nuevaDireccion.y = 0f;

                        if (nuevaDireccion.sqrMagnitude > 0.01f)
                        {
                            rotacionObjetivo = Quaternion.LookRotation(nuevaDireccion.normalized);
                            StartCoroutine(GirarGradualmente(rotacionObjetivo));
                            return;
                        }
                    }
                }

                // Si no hay pared entre el jugador y el enemigo, girar directamente hacia el enemigo
                rotacionObjetivo = Quaternion.LookRotation(direccion.normalized);
                StartCoroutine(GirarGradualmente(rotacionObjetivo));
            }
        }

        // CHOQUE CON ENEMIGO
        else if (collision.gameObject.layer == enemyLayer)
        {
            //Debug.Log("CHOQUE CON ENEMIGO");

            Vector3 normalEnemigo = collision.contacts[0].normal;

            rb.MovePosition(
                rb.position + normalEnemigo * distanciaReboteEnemigo
            );

            float anguloAleatorio = Random.Range(90f, 270f);

            Quaternion rotacionAleatoria =
                Quaternion.AngleAxis(anguloAleatorio, Vector3.up);

            Vector3 nuevaDireccion =
                rotacionAleatoria * transform.forward;

            nuevaDireccion.y = 0f;

            if (nuevaDireccion.sqrMagnitude > 0.01f)
            {
                Quaternion rotacionObjetivo =
                    Quaternion.LookRotation(nuevaDireccion.normalized);

                StartCoroutine(GirarGradualmente(rotacionObjetivo));
            }
        }
    }

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

        Vector3 inicio =
            transform.position;

        Vector3 destino =
            inicio +
            direccion * distancia;

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            if (cancelarEmpuje)
                break;

            tiempo += Time.deltaTime;

            float t =
                tiempo / duracion;

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
            PlayerStats stats =
                GetComponent<PlayerStats>();

            if (stats != null)
            {
                stats.RecibirDanio(5f);
            }

            StartCoroutine(
                Aturdir()
            );
        }
    }

    private IEnumerator Aturdir()
    {
        aturdido = true;
        girando = false;

        yield return new WaitForSeconds(
            tiempoAturdido
        );

        aturdido = false;
    }

    // Comprueba si hay una pared (wall) entre dos posiciones usando raycast
    private bool IsWallBetween(Vector3 from, Vector3 to)
    {
        Vector3 origin = from + Vector3.up * 0.5f;
        Vector3 dir = (to - origin);
        float dist = dir.magnitude;
        if (dist <= 0.01f) return false;

        int wallMask = 1 << wallLayer;
        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, wallMask))
        {
            return true;
        }

        return false;
    }

    // Busca una dirección alternativa que permita avanzar sin chocar inmediatamente con una pared.
    // Intenta varios ángulos alrededor de la dirección hacia el enemigo y devuelve el primer candidato
    // que tenga espacio libre hacia adelante durante una distancia de comprobación.
    private Vector3 FindAlternateDirectionTowardsEnemy(Vector3 direccionHaciaEnemy)
    {
        if (direccionHaciaEnemy.sqrMagnitude < 0.01f)
            return Vector3.zero;

        Vector3 dir = direccionHaciaEnemy.normalized;
        int wallMask = 1 << wallLayer;

        float distToEnemy = Vector3.Distance(transform.position, BuscarEnemigo()?.transform.position ?? (transform.position + dir * 2f));
        float checkDist = Mathf.Clamp(distToEnemy, 1.5f, 4f);

        float[] angles = new float[] { 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f, 180f };

        foreach (float angle in angles)
        {
            Vector3 candidate = Quaternion.Euler(0f, angle, 0f) * dir;
            Vector3 origin = transform.position + Vector3.up * 0.5f;

            // Comprobar si hay pared en dirección candidate en un rango corto (clearance check)
            if (!Physics.Raycast(origin, candidate, checkDist, wallMask))
            {
                // Además comprobar si desde un pequeño desplazamiento lateral se puede ver al enemy
                Vector3 probeOrigin = origin + candidate * 0.5f;
                GameObject enemigo = BuscarEnemigo();
                Vector3 toEnemy = (enemigo != null) ? (enemigo.transform.position - probeOrigin) : Vector3.zero;

                if (enemigo == null || (toEnemy.magnitude > 0.01f && !Physics.Raycast(probeOrigin, toEnemy.normalized, toEnemy.magnitude, wallMask)))
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
        // Al volver al modo automático, reiniciar la aceleración manual.
        velocidadManualActual = 0f;
        direccionManualAnterior = 0;

        if (girando)
            return;

        // Velocidad normal del robot.
        float velocidadActual = velocidad;

        // Consultar el chip equipado.
        ModificadorStats chip = equiparChips != null
            ? equiparChips.ModificadorEquipado
            : null;

        if (chip != null)
        {
            float modificador = 0f;
            bool permiteModificarMovimiento = false;

            foreach (EfectoModificador efecto in chip.efectos)
            {
                // Comprobar que el chip permite modificar
                // el movimiento del robot.
                if (efecto.scriptObjetivo == TipoScript.RoboMov &&
                    efecto.atributo == AtributoModificable.ModificadorMovimiento)
                {
                    modificador += efecto.cantidad;
                    permiteModificarMovimiento = true;
                }
            }

            // W y S solamente funcionan con el chip adecuado.
            if (permiteModificarMovimiento)
            {
                // W: Aumentar velocidad.
                if (Input.GetKey(KeyCode.W) &&
                    !Input.GetKey(KeyCode.S))
                {
                    velocidadActual += modificador;
                }

                // S: Disminuir velocidad.
                else if (Input.GetKey(KeyCode.S) &&
                         !Input.GetKey(KeyCode.W))
                {
                    velocidadActual -= modificador;
                }
            }
        }

        // Evitar velocidades negativas.
        velocidadActual = Mathf.Max(0f, velocidadActual);

        // Movimiento original del robot.
        rb.MovePosition(
            rb.position +
            transform.forward * velocidadActual * Time.fixedDeltaTime
        );
    }

    // Movimiento manual: W avanza, S retrocede, A y D giran en Update().
    private void MovimientoManual()
    {
        // Respetar los bloqueos por choque, empuje y aturdimiento.
        if (girando || empujado || aturdido)
        {
            velocidadManualActual = 0f;
            direccionManualAnterior = 0;
            return;
        }

        int direccion = 0;

        // W tiene prioridad si W y S se pulsan simultáneamente.
        if (Input.GetKey(KeyCode.W))
            direccion = 1;
        else if (Input.GetKey(KeyCode.S))
            direccion = -1;

        // Sin tecla de avance o retroceso, se detiene de inmediato.
        if (direccion == 0)
        {
            velocidadManualActual = 0f;
            direccionManualAnterior = 0;
            return;
        }

        // Al invertir el sentido se vuelve a acelerar desde cero.
        if (direccion != direccionManualAnterior)
            velocidadManualActual = 0f;

        direccionManualAnterior = direccion;

        float velocidadMaxima = Mathf.Max(0f, velocidad);
        float duracion = Mathf.Max(0.01f, tiempoAceleracionManual);

        // De 0 a velocidadMaxima en tiempoAceleracionManual segundos.
        velocidadManualActual = Mathf.MoveTowards(
            velocidadManualActual,
            velocidadMaxima,
            velocidadMaxima / duracion * Time.fixedDeltaTime
        );

        rb.MovePosition(
            rb.position +
            transform.forward * (direccion * velocidadManualActual) * Time.fixedDeltaTime
        );
    }
}
