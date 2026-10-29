using UnityEngine;
using System.Collections;

public class HammerEnemy : MonoBehaviour
{
    [Header("Animacion del martillo")]
    [SerializeField] private Transform martilloARotar;

    [Tooltip("Rotacion adicional desde reposo para preparar el golpe.")]
    [SerializeField]
    private Vector3 rotacionPuntoA =
        new Vector3(-60f, 0f, 0f);

    [Tooltip("Rotacion adicional desde reposo para golpear.")]
    [SerializeField]
    private Vector3 rotacionPuntoB =
        new Vector3(60f, 0f, 0f);

    [Header("Velocidad (grados por segundo)")]
    [SerializeField, Min(1f)] private float velocidadPreparacion = 180f;
    [SerializeField, Min(1f)] private float velocidadGolpe = 360f;
    [SerializeField, Min(1f)] private float velocidadRegreso = 240f;

    [Header("Deteccion de impacto")]
    [Tooltip("Transform situado en la cabeza del martillo.")]
    [SerializeField] private Transform puntoImpacto;

    [Tooltip("Radio de deteccion de la cabeza del martillo.")]
    [SerializeField, Min(0.01f)] private float radioDeteccion = 0.12f;

    [Header("Ataque")]
    [SerializeField, Min(0f)] private float tiempoEspera = 1f;

    [Header("Daño")]
    [SerializeField] private float danioMinimo = 5f;
    [SerializeField] private float danioMaximo = 40f;
    [SerializeField] private float tiempoCargaCompleta = 20f;

    private int playerLayer;
    private GameObject playerDentro;

    private float danioActual;
    private bool cargaCompleta;
    private bool atacando;
    private bool danoAplicado;

    private Renderer rend;
    private Color colorOriginal;
    private Color colorCargaMaxima = new Color(1f, 0.5f, 0f);

    private Quaternion rotacionReposo;

    private void Start()
    {
        playerLayer = LayerMask.NameToLayer("Player");
        danioActual = danioMinimo;

        rend = GetComponent<Renderer>();

        if (rend != null)
            colorOriginal = rend.material.color;

        if (martilloARotar != null)
            rotacionReposo = martilloARotar.localRotation;
        else
            Debug.LogError(
                "HammerEnemy: asigna el Transform del martillo.", this
            );

        if (puntoImpacto == null)
            Debug.LogError(
                "HammerEnemy: asigna el Transform PuntoImpacto.", this
            );

        if (playerLayer == -1)
            Debug.LogError("No existe la layer Player.");
    }

    private void Update()
    {
        if (cargaCompleta)
            return;

        if (tiempoCargaCompleta <= 0f)
        {
            danioActual = danioMaximo;
            cargaCompleta = true;
            ActualizarColor();
            return;
        }

        float aumentoPorSegundo =
            (danioMaximo - danioMinimo) / tiempoCargaCompleta;

        danioActual += aumentoPorSegundo * Time.deltaTime;

        if (danioActual >= danioMaximo)
        {
            danioActual = danioMaximo;
            cargaCompleta = true;
        }

        ActualizarColor();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerStats stats = other.GetComponentInParent<PlayerStats>();

        if (stats == null || stats.gameObject.layer != playerLayer)
            return;

        playerDentro = stats.gameObject;

        if (!atacando)
            StartCoroutine(AnimacionAtaque(playerDentro));
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerStats stats = other.GetComponentInParent<PlayerStats>();

        if (stats != null && stats.gameObject == playerDentro)
            playerDentro = null;
    }

    private IEnumerator AnimacionAtaque(GameObject player)
    {
        if (martilloARotar == null || puntoImpacto == null ||
            player == null)
            yield break;

        atacando = true;
        danoAplicado = false;

        if (tiempoEspera > 0f)
            yield return new WaitForSeconds(tiempoEspera);

        float danioGolpe = danioActual;

        Quaternion puntoA =
            rotacionReposo * Quaternion.Euler(rotacionPuntoA);

        Quaternion puntoB =
            rotacionReposo * Quaternion.Euler(rotacionPuntoB);

        // 1. Reposo -> Punto A.
        yield return StartCoroutine(
            RotarHasta(puntoA, velocidadPreparacion)
        );

        // 2. Punto A -> Punto B.
        // La animacion se detiene si detecta contacto.
        yield return StartCoroutine(
            GolpearHastaContacto(puntoB, player, danioGolpe)
        );

        // 3. Regreso al reposo.
        yield return StartCoroutine(
            RotarHasta(rotacionReposo, velocidadRegreso)
        );

        atacando = false;

        // Repetir el ataque si el jugador sigue dentro.
        if (playerDentro != null)
        {
            GameObject siguientePlayer = playerDentro;
            StartCoroutine(AnimacionAtaque(siguientePlayer));
        }
    }

    private IEnumerator GolpearHastaContacto(
        Quaternion rotacionDestino,
        GameObject player,
        float danioGolpe)
    {
        Quaternion rotacionInicial = martilloARotar.localRotation;

        float angulo = Quaternion.Angle(
            rotacionInicial, rotacionDestino
        );

        float duracion = angulo / Mathf.Max(velocidadGolpe, 1f);
        float tiempo = 0f;

        Vector3 posicionAnterior = puntoImpacto.position;

        if (duracion <= 0.001f)
        {
            martilloARotar.localRotation = rotacionDestino;

            if (DetectarImpacto(player, posicionAnterior))
                AplicarDanio(player, danioGolpe);

            yield break;
        }

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            float progreso = Mathf.Clamp01(tiempo / duracion);

            martilloARotar.localRotation = Quaternion.Slerp(
                rotacionInicial,
                rotacionDestino,
                progreso
            );

            Vector3 posicionActual = puntoImpacto.position;

            // Comprueba la posicion actual y el trayecto recorrido.
            if (DetectarImpacto(
                player, posicionAnterior, posicionActual))
            {
                AplicarDanio(player, danioGolpe);

                // Detiene el golpe en el punto de contacto.
                yield break;
            }

            posicionAnterior = posicionActual;

            yield return null;
        }

        // Comprobacion final al llegar al Punto B.
        if (DetectarImpacto(
            player, puntoImpacto.position, puntoImpacto.position))
        {
            AplicarDanio(player, danioGolpe);
        }
    }

    private bool DetectarImpacto(
        GameObject playerObjetivo,
        Vector3 posicionAnterior,
        Vector3 posicionActual)
    {
        if (puntoImpacto == null || playerObjetivo == null)
            return false;

        // 1. Detecta colliders que tocan la esfera de la cabeza.
        Collider[] colisiones = Physics.OverlapSphere(
            posicionActual,
            radioDeteccion,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider colision in colisiones)
        {
            if (PerteneceAlJugador(colision, playerObjetivo))
                return true;
        }

        // 2. Barre el trayecto entre el frame anterior y el actual.
        Vector3 desplazamiento = posicionActual - posicionAnterior;
        float distancia = desplazamiento.magnitude;

        if (distancia > 0.0001f)
        {
            RaycastHit[] impactos = Physics.SphereCastAll(
                posicionAnterior,
                radioDeteccion,
                desplazamiento.normalized,
                distancia,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide
            );

            foreach (RaycastHit impacto in impactos)
            {
                if (PerteneceAlJugador(
                    impacto.collider, playerObjetivo))
                    return true;
            }
        }

        return false;
    }

    private bool DetectarImpacto(
        GameObject playerObjetivo,
        Vector3 posicionActual)
    {
        return DetectarImpacto(
            playerObjetivo, posicionActual, posicionActual
        );
    }

    private bool PerteneceAlJugador(
        Collider colision,
        GameObject playerObjetivo)
    {
        if (colision == null || playerObjetivo == null)
            return false;

        PlayerStats stats =
            colision.GetComponentInParent<PlayerStats>();

        return stats != null &&
               stats.gameObject == playerObjetivo;
    }

    private void AplicarDanio(GameObject player, float danioGolpe)
    {
        if (danoAplicado || player == null)
            return;

        PlayerStats stats = player.GetComponent<PlayerStats>();

        if (stats == null)
            stats = player.GetComponentInParent<PlayerStats>();

        if (stats == null)
        {
            Debug.LogWarning(
                "No se encontro PlayerStats en el jugador.", player
            );
            return;
        }

        danoAplicado = true;

        Debug.Log(
            "Martillazo enemigo: " + Mathf.RoundToInt(danioGolpe)
        );

        stats.RecibirDanio(danioGolpe);

        ReiniciarCarga();
    }

    private IEnumerator RotarHasta(
        Quaternion rotacionDestino,
        float velocidad)
    {
        Quaternion rotacionInicial = martilloARotar.localRotation;

        float angulo = Quaternion.Angle(
            rotacionInicial, rotacionDestino
        );

        float duracion = angulo / Mathf.Max(velocidad, 1f);

        if (duracion <= 0.001f)
        {
            martilloARotar.localRotation = rotacionDestino;
            yield break;
        }

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            float progreso = Mathf.Clamp01(tiempo / duracion);

            martilloARotar.localRotation = Quaternion.Slerp(
                rotacionInicial,
                rotacionDestino,
                progreso
            );

            yield return null;
        }

        martilloARotar.localRotation = rotacionDestino;
    }

    private void ReiniciarCarga()
    {
        danioActual = danioMinimo;
        cargaCompleta = false;

        if (rend != null)
            rend.material.color = colorOriginal;
    }

    private void ActualizarColor()
    {
        if (rend == null)
            return;

        float rango = danioMaximo - danioMinimo;

        float porcentaje = rango > 0f
            ? (danioActual - danioMinimo) / rango
            : 1f;

        rend.material.color = Color.Lerp(
            colorOriginal,
            colorCargaMaxima,
            Mathf.Clamp01(porcentaje)
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (puntoImpacto == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            puntoImpacto.position,
            radioDeteccion
        );
    }
}

