using UnityEngine;
using System.Collections;

public class RoboMovEnemy : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 5f;

    [Header("Giro")]
    [SerializeField] private float tiempoDeGiro = 1f;

    [Header("Seguimiento")]
    [SerializeField] private float velocidadGiroMinima = 10f;
    [SerializeField] private float velocidadGiroMaxima = 30f;

    [Header("Rebotes")]
    [SerializeField] private float distanciaRebote = 0.5f;
    [SerializeField] private float distanciaRebotePlayer = 1f;

    private Rigidbody rb;
    private bool girando = false;

    private int wallLayer;
    private int playerLayer;

    private GameObject player;

    private float velocidadGiroActual;

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
        if (girando)
            return;

        if (player == null)
            return;

        Vector3 direccion = player.transform.position - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude < 0.01f)
            return;

        Quaternion rotacionObjetivo =
            Quaternion.LookRotation(direccion.normalized);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            rotacionObjetivo,
            velocidadGiroActual * Time.deltaTime
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
        if (girando)
            return;

        // CHOQUE CON PARED
        if (collision.gameObject.layer == wallLayer)
        {
            Vector3 normalPared = collision.contacts[0].normal;

            rb.MovePosition(
                rb.position + normalPared * distanciaRebote
            );

            // Nueva velocidad de giro aleatoria
            velocidadGiroActual = Random.Range(
                velocidadGiroMinima,
                velocidadGiroMaxima
            );

            Debug.Log(
                "Nueva velocidad de giro: " +
                velocidadGiroActual
            );

            if (player == null)
                return;

            Vector3 direccion =
                player.transform.position - transform.position;

            direccion.y = 0f;

            if (direccion.sqrMagnitude > 0.01f)
            {
                Quaternion rotacionObjetivo =
                    Quaternion.LookRotation(direccion.normalized);

                StartCoroutine(
                    GirarGradualmente(rotacionObjetivo)
                );
            }
        }

        // CHOQUE CON PLAYER
        else if (collision.gameObject.layer == playerLayer)
        {
            Debug.Log("CHOQUE CON PLAYER");

            Vector3 normalPlayer = collision.contacts[0].normal;

            rb.MovePosition(
                rb.position + normalPlayer * distanciaRebotePlayer
            );

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
}