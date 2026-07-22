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
                Quaternion rotacionObjetivo =
                    Quaternion.LookRotation(direccion.normalized);

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
}