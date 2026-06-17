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
                Quaternion rotacionObjetivo =
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
}