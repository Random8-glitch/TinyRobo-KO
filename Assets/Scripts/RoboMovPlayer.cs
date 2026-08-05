using UnityEngine;
using System.Collections;

public class RoboMovPlayer : MonoBehaviour
{
    [SerializeField] public float velocidad = 5f;
    [SerializeField] public float tiempoDeGiro = 1f;
    [SerializeField] public float velocidadGiroManual = 30f;
    [SerializeField] public float distanciaRebote = 0.5f;
    [SerializeField] public float distanciaReboteEnemigo = 1f;

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
        if (girando)
            return;

        rb.MovePosition(
            rb.position +
            transform.forward * velocidad * Time.fixedDeltaTime
        );
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
}