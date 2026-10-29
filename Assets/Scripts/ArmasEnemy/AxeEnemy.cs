using UnityEngine;
using System.Collections;

public class AxeEnemy : MonoBehaviour
{
    [Header("Ataque")]
    [SerializeField] private float tiempoEspera = 1f;
    [SerializeField] private float danioNormal = 10f;
    [SerializeField] private float multiplicadorCritico = 3f;

    [Header("Animacion del Hacha")]
    [SerializeField] private Transform hachaARotar;
    [SerializeField] private Vector3 rotacionPuntoA = new Vector3(-45f, 0f, 0f);
    [SerializeField] private Vector3 rotacionPuntoB = new Vector3(70f, 0f, 0f);
    [SerializeField] private float velocidadPreparacion = 180f;
    [SerializeField] private float velocidadGolpe = 360f;
    [SerializeField] private float velocidadRegreso = 240f;

    [Header("Punto de Impacto")]
    [SerializeField] private Transform puntoImpacto;
    [SerializeField] private float radioDeteccion = 0.2f;
    [SerializeField] private bool dibujarPuntoImpacto = true;

    [Header("Estado Critico")]
    [SerializeField] private float tiempoMinCritico = 5f;
    [SerializeField] private float tiempoMaxCritico = 10f;
    [SerializeField] private float duracionCritico = 3f;

    private int playerLayer;
    private GameObject playerDentro;

    private bool criticoActivo = false;
    private bool atacando = false;
    private bool danoAplicado = false;

    private Renderer rend;
    private Color colorOriginal;
    private Material materialInstancia;

    private Quaternion rotacionReposo;

    private void Start()
    {
        playerLayer = LayerMask.NameToLayer("Player");

        rend = GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            materialInstancia = rend.material;
            colorOriginal = materialInstancia.color;
        }

        if (hachaARotar != null)
        {
            rotacionReposo = hachaARotar.localRotation;
        }
        else
        {
            Debug.LogWarning(
                "AxeEnemy: asigna el objeto que debe rotar en Hacha A Rotar.",
                this
            );
        }

        if (puntoImpacto == null)
        {
            Debug.LogWarning(
                "AxeEnemy: asigna el Transform del Punto de Impacto.",
                this
            );
        }

        StartCoroutine(CicloCritico());
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerStats stats = other.GetComponentInParent<PlayerStats>();

        if (stats == null || stats.gameObject.layer != playerLayer)
            return;

        playerDentro = stats.gameObject;

        if (!atacando)
        {
            StartCoroutine(AnimacionAtaque(playerDentro));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerStats stats = other.GetComponentInParent<PlayerStats>();

        if (stats != null && stats.gameObject == playerDentro)
        {
            playerDentro = null;
        }
    }

    private IEnumerator AnimacionAtaque(GameObject player)
    {
        if (hachaARotar == null || puntoImpacto == null || player == null)
            yield break;

        atacando = true;
        danoAplicado = false;

        // Calcula el daño al comenzar cada ataque.
        float danioGolpe = danioNormal;

        if (criticoActivo)
        {
            danioGolpe *= multiplicadorCritico;
            Debug.Log("¡GOLPE CRITICO DEL HACHA ENEMIGA!");
        }

        // 1. Preparar el golpe.
        Quaternion rotacionA =
            rotacionReposo * Quaternion.Euler(rotacionPuntoA);

        yield return StartCoroutine(
            RotarHasta(rotacionA, velocidadPreparacion)
        );

        // 2. Ejecutar el golpe y detenerse al impactar.
        Quaternion rotacionB =
            rotacionReposo * Quaternion.Euler(rotacionPuntoB);

        yield return StartCoroutine(
            GolpearHastaContacto(rotacionB, player, danioGolpe)
        );

        // 3. Regresar a la posición de reposo.
        yield return StartCoroutine(
            RotarHasta(rotacionReposo, velocidadRegreso)
        );

        atacando = false;

        // Repetir el ataque si el jugador sigue dentro del área.
        if (playerDentro != null)
        {
            yield return new WaitForSeconds(tiempoEspera);

            if (playerDentro != null && !atacando)
            {
                StartCoroutine(AnimacionAtaque(playerDentro));
            }
        }
    }

    private IEnumerator GolpearHastaContacto(
        Quaternion rotacionDestino,
        GameObject player,
        float danioGolpe)
    {
        Quaternion rotacionInicial = hachaARotar.localRotation;

        float distancia = Quaternion.Angle(
            rotacionInicial,
            rotacionDestino
        );

        float duracion = distancia / Mathf.Max(velocidadGolpe, 0.01f);
        float tiempo = 0f;

        Vector3 posicionAnterior = puntoImpacto.position;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            float progreso = duracion > 0f
                ? Mathf.Clamp01(tiempo / duracion)
                : 1f;

            hachaARotar.localRotation = Quaternion.Slerp(
                rotacionInicial,
                rotacionDestino,
                progreso
            );

            Vector3 posicionActual = puntoImpacto.position;

            // Detectar contacto durante el desplazamiento.
            if (DetectarImpacto(
                player,
                posicionAnterior,
                posicionActual))
            {
                AplicarDanio(player, danioGolpe);
                yield break;
            }

            // Comprobar si el punto ya está dentro del jugador.
            if (DetectarImpacto(player, posicionActual))
            {
                AplicarDanio(player, danioGolpe);
                yield break;
            }

            posicionAnterior = posicionActual;

            yield return null;
        }

        hachaARotar.localRotation = rotacionDestino;

        // Comprobación final.
        if (DetectarImpacto(player, puntoImpacto.position))
        {
            AplicarDanio(player, danioGolpe);
        }
    }

    private bool DetectarImpacto(
        GameObject player,
        Vector3 posicionAnterior,
        Vector3 posicionActual)
    {
        if (player == null)
            return false;

        // Detectar colliders que ya están dentro del radio.
        Collider[] cercanos = Physics.OverlapSphere(
            posicionActual,
            radioDeteccion
        );

        foreach (Collider col in cercanos)
        {
            if (PerteneceAlJugador(col, player))
                return true;
        }

        // Detectar colisiones entre la posición anterior y la actual.
        Vector3 desplazamiento = posicionActual - posicionAnterior;
        float distancia = desplazamiento.magnitude;

        if (distancia > 0.001f)
        {
            RaycastHit[] impactos = Physics.SphereCastAll(
                posicionAnterior,
                radioDeteccion,
                desplazamiento.normalized,
                distancia
            );

            foreach (RaycastHit impacto in impactos)
            {
                if (impacto.collider != null &&
                    PerteneceAlJugador(impacto.collider, player))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool DetectarImpacto(GameObject player, Vector3 posicion)
    {
        if (player == null)
            return false;

        Collider[] cercanos = Physics.OverlapSphere(
            posicion,
            radioDeteccion
        );

        foreach (Collider col in cercanos)
        {
            if (PerteneceAlJugador(col, player))
                return true;
        }

        return false;
    }

    private bool PerteneceAlJugador(Collider col, GameObject player)
    {
        if (col == null || player == null)
            return false;

        PlayerStats stats = col.GetComponentInParent<PlayerStats>();

        return stats != null && stats.gameObject == player;
    }

    private void AplicarDanio(GameObject player, float danio)
    {
        if (danoAplicado || player == null)
            return;

        PlayerStats stats = player.GetComponent<PlayerStats>();

        if (stats == null)
            return;

        danoAplicado = true;
        stats.RecibirDanio(danio);

        Debug.Log(
            criticoActivo
                ? "¡GOLPE CRITICO APLICADO AL JUGADOR!"
                : "Hacha enemiga: impacto confirmado. Daño: " + danio
        );
    }

    private IEnumerator RotarHasta(
        Quaternion rotacionDestino,
        float velocidad)
    {
        if (hachaARotar == null)
            yield break;

        while (Quaternion.Angle(
            hachaARotar.localRotation,
            rotacionDestino) > 0.5f)
        {
            hachaARotar.localRotation = Quaternion.RotateTowards(
                hachaARotar.localRotation,
                rotacionDestino,
                velocidad * Time.deltaTime
            );

            yield return null;
        }

        hachaARotar.localRotation = rotacionDestino;
    }

    private IEnumerator CicloCritico()
    {
        while (true)
        {
            float espera = Random.Range(
                tiempoMinCritico,
                tiempoMaxCritico
            );

            yield return new WaitForSeconds(espera);

            criticoActivo = true;

            if (materialInstancia != null)
                materialInstancia.color = Color.red;

            Debug.Log("CRITICO ACTIVADO");

            yield return new WaitForSeconds(duracionCritico);

            criticoActivo = false;

            if (materialInstancia != null)
                materialInstancia.color = colorOriginal;

            Debug.Log("CRITICO TERMINADO");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!dibujarPuntoImpacto || puntoImpacto == null)
            return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            puntoImpacto.position,
            radioDeteccion
        );
    }
}