using UnityEngine;
using System.Collections;

public class RoboMovEnemy : MonoBehaviour
{
    [SerializeField] public float velocidad = 5f;
    [SerializeField] public float tiempoDeGiro = 1f;
    [SerializeField] public float velocidadGiroMinima = 10f;
    [SerializeField] public float velocidadGiroMaxima = 30f;
    [SerializeField] public float distanciaRebote = 0.5f;
    [SerializeField] public float distanciaRebotePlayer = 1f;

    [SerializeField] private float tiempoAturdido = 1f;

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
    }

    private void Update()
    {
        if (girando || empujado || aturdido)
            return;

        if (player == null)
            return;

        Vector3 direccion =
            player.transform.position - transform.position;

        direccion.y = 0f;

        if (direccion.sqrMagnitude < 0.01f)
            return;

        Quaternion rotacionObjetivo =
            Quaternion.LookRotation(
                direccion.normalized
            );

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                rotacionObjetivo,
                velocidadGiroActual * Time.deltaTime
            );
    }

    private void FixedUpdate()
    {
        if (girando || empujado || aturdido)
            return;

        rb.MovePosition(
            rb.position +
            transform.forward *
            velocidad *
            Time.fixedDeltaTime
        );
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (
            empujado &&
            collision.gameObject.layer == wallLayer
        )
        {
            golpeoParedDuranteEmpuje = true;
            cancelarEmpuje = true;
        }

        if (girando || empujado || aturdido)
            return;

        // CHOQUE CON PARED
        if (collision.gameObject.layer == wallLayer)
        {
            Vector3 normalPared =
                collision.contacts[0].normal;

            rb.MovePosition(
                rb.position +
                normalPared *
                distanciaRebote
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
                // Si existe una pared entre el robot y el player, buscar una dirección alternativa
                if (IsWallBetween(transform.position, player.transform.position))
                {
                    Vector3 altDir = FindAlternateDirectionTowardsPlayer(direccion);
                    if (altDir != Vector3.zero)
                    {
                        Quaternion rotacionObjetivo =
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
                    else
                    {
                        // Si no se encuentra ruta alternativa, usar rotación aleatoria (comportamiento previo)
                        float anguloAleatorio =
                            Random.Range(
                                90f,
                                270f
                            );

                        Quaternion rotacionAleatoria =
                            Quaternion.AngleAxis(
                                anguloAleatorio,
                                Vector3.up
                            );

                        Vector3 nuevaDireccion =
                            rotacionAleatoria *
                            transform.forward;

                        nuevaDireccion.y = 0f;

                        if (
                            nuevaDireccion.sqrMagnitude >
                            0.01f
                        )
                        {
                            Quaternion rotacionObjetivo =
                                Quaternion.LookRotation(
                                    nuevaDireccion.normalized
                                );

                            StartCoroutine(
                                GirarGradualmente(
                                    rotacionObjetivo
                                )
                            );

                            return;
                        }
                    }
                }

                // Si no hay pared entre el robot y el player, girar directamente hacia el player (comportamiento original)
                Quaternion rotacionObjetivoDirecto =
                    Quaternion.LookRotation(
                        direccion.normalized
                    );

                StartCoroutine(
                    GirarGradualmente(
                        rotacionObjetivoDirecto
                    )
                );
            }
        }

        // CHOQUE CON PLAYER
        else if (
            collision.gameObject.layer ==
            playerLayer
        )
        {
            Vector3 normalPlayer =
                collision.contacts[0].normal;

            rb.MovePosition(
                rb.position +
                normalPlayer *
                distanciaRebotePlayer
            );

            float anguloAleatorio =
                Random.Range(
                    90f,
                    270f
                );

            Quaternion rotacionAleatoria =
                Quaternion.AngleAxis(
                    anguloAleatorio,
                    Vector3.up
                );

            Vector3 nuevaDireccion =
                rotacionAleatoria *
                transform.forward;

            nuevaDireccion.y = 0f;

            if (
                nuevaDireccion.sqrMagnitude >
                0.01f
            )
            {
                Quaternion rotacionObjetivo =
                    Quaternion.LookRotation(
                        nuevaDireccion.normalized
                    );

                StartCoroutine(
                    GirarGradualmente(
                        rotacionObjetivo
                    )
                );
            }
        }
    }

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
            EnemyStats stats =
                GetComponent<EnemyStats>();

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

            float t =
                tiempo / tiempoDeGiro;

            transform.rotation =
                Quaternion.Slerp(
                    rotacionInicial,
                    rotacionObjetivo,
                    t
                );

            yield return null;
        }

        transform.rotation =
            rotacionObjetivo;

        girando = false;
    }

    // Comprueba si hay una pared (wall) entre dos posiciones usando raycast
    private bool IsWallBetween(Vector3 from, Vector3 to)
    {
        if (player == null)
            return false;

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
    // Intenta varios ángulos alrededor de la dirección hacia el player y devuelve el primer candidato
    // que tenga espacio libre hacia adelante durante una distancia de comprobación.
    private Vector3 FindAlternateDirectionTowardsPlayer(Vector3 direccionHaciaPlayer)
    {
        if (direccionHaciaPlayer.sqrMagnitude < 0.01f)
            return Vector3.zero;

        Vector3 dir = direccionHaciaPlayer.normalized;
        int wallMask = 1 << wallLayer;

        // Distancia a comprobar que sea suficiente para 'salir' del muro; se usa min(distToPlayer, 3f)
        float distToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float checkDist = Mathf.Clamp(distToPlayer, 1.5f, 4f);

        // Ángulos a probar (priorizar giros pequeños para rodear)
        float[] angles = new float[] { 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f, 180f };

        foreach (float angle in angles)
        {
            Vector3 candidate = Quaternion.Euler(0f, angle, 0f) * dir;
            Vector3 origin = transform.position + Vector3.up * 0.5f;

            // Comprobar si hay pared en dirección candidate en un rango corto (clearance check)
            if (!Physics.Raycast(origin, candidate, checkDist, wallMask))
            {
                // Además comprobar si desde un pequeño desplazamiento lateral se puede ver al player (mejora robustez)
                Vector3 probeOrigin = origin + candidate * 0.5f;
                Vector3 toPlayer = (player.transform.position - probeOrigin);
                if (!Physics.Raycast(probeOrigin, toPlayer.normalized, toPlayer.magnitude, wallMask))
                {
                    return candidate;
                }
            }
        }

        // No se encontró una dirección clara
        return Vector3.zero;
    }
}