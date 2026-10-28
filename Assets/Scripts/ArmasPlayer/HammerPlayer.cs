using UnityEngine;
using System.Collections;

public class HammerPlayer : MonoBehaviour
{
    [Header("Animacion del martillo")]
    [SerializeField] private Transform martilloARotar;

    [SerializeField]
    private Vector3 rotacionPuntoA =
        new Vector3(-60f, 0f, 0f);

    [SerializeField]
    private Vector3 rotacionPuntoB =
        new Vector3(60f, 0f, 0f);

    [Header("Velocidad (grados por segundo)")]
    [SerializeField, Min(1f)] private float velocidadPreparacion = 180f;
    [SerializeField, Min(1f)] private float velocidadGolpe = 360f;
    [SerializeField, Min(1f)] private float velocidadRegreso = 240f;

    [SerializeField, Min(0f)] private float tiempoEspera = 0f;

    [Header("Deteccion de impacto")]
    [Tooltip("Transform colocado en la cabeza del martillo.")]
    [SerializeField] private Transform puntoImpacto;

    [Tooltip("Radio de deteccion alrededor de la cabeza del martillo.")]
    [SerializeField, Min(0.01f)] private float radioDeteccion = 0.12f;

    [Header("Daño")]
    [SerializeField] private float danioMinimo = 5f;
    [SerializeField] private float danioMaximo = 40f;
    [SerializeField] private float tiempoCargaCompleta = 20f;

    private int enemyLayer;
    private GameObject enemigoDentro;

    private float danioActual;
    private bool cargaCompleta;
    private bool atacando;
    private bool danoAplicado;

    private Renderer rend;
    private Color colorOriginal;
    private Color colorCargaMaxima = new Color(1f, 0.5f, 0f);

    private EquiparChips equiparChips;
    private Quaternion rotacionReposo;

    private void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");
        danioActual = danioMinimo;

        rend = GetComponent<Renderer>();

        if (rend != null)
            colorOriginal = rend.material.color;

        equiparChips = GetComponentInParent<EquiparChips>();

        if (martilloARotar != null)
            rotacionReposo = martilloARotar.localRotation;
        else
            Debug.LogError("Asigna el Transform del martillo.", this);

        if (puntoImpacto == null)
            Debug.LogWarning("Asigna el Transform PuntoImpacto.", this);

        if (enemyLayer == -1)
            Debug.LogError("No existe la layer Enemy.");
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

        float aumento =
            (danioMaximo - danioMinimo) / tiempoCargaCompleta;

        danioActual += aumento * Time.deltaTime;

        if (danioActual >= danioMaximo)
        {
            danioActual = danioMaximo;
            cargaCompleta = true;
        }

        ActualizarColor();
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyStats stats = other.GetComponentInParent<EnemyStats>();

        if (stats == null || stats.gameObject.layer != enemyLayer)
            return;

        enemigoDentro = stats.gameObject;

        if (!atacando)
            StartCoroutine(AnimacionAtaque(enemigoDentro));
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyStats stats = other.GetComponentInParent<EnemyStats>();

        if (stats != null && stats.gameObject == enemigoDentro)
            enemigoDentro = null;
    }

    private IEnumerator AnimacionAtaque(GameObject enemigo)
    {
        if (martilloARotar == null || puntoImpacto == null ||
            enemigo == null)
            yield break;

        atacando = true;
        danoAplicado = false;

        if (tiempoEspera > 0f)
            yield return new WaitForSeconds(tiempoEspera);

        // Guarda el daño al comenzar el ataque.
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
        // Se detiene inmediatamente cuando detecta contacto.
        yield return StartCoroutine(
            GolpearHastaContacto(puntoB, enemigo, danioGolpe)
        );

        // 3. Regresa al reposo.
        yield return StartCoroutine(
            RotarHasta(rotacionReposo, velocidadRegreso)
        );

        atacando = false;

        // Repite el ataque si el enemigo sigue dentro.
        if (enemigoDentro != null)
        {
            GameObject siguienteEnemigo = enemigoDentro;
            StartCoroutine(AnimacionAtaque(siguienteEnemigo));
        }
    }

    private IEnumerator GolpearHastaContacto(
        Quaternion rotacionDestino,
        GameObject enemigo,
        float danioGolpe)
    {
        Quaternion rotacionInicial = martilloARotar.localRotation;

        float angulo = Quaternion.Angle(
            rotacionInicial,
            rotacionDestino
        );

        float duracion = angulo / Mathf.Max(velocidadGolpe, 1f);
        float tiempo = 0f;

        Vector3 posicionAnterior = puntoImpacto.position;

        if (duracion <= 0.001f)
        {
            martilloARotar.localRotation = rotacionDestino;

            if (DetectarImpacto(enemigo, posicionAnterior))
                AplicarDanio(enemigo, danioGolpe);

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

            // Comprueba el espacio recorrido por la cabeza
            // y el contacto en su posicion actual.
            if (DetectarImpacto(enemigo, posicionAnterior))
            {
                AplicarDanio(enemigo, danioGolpe);
                yield break;
            }

            if (DetectarImpacto(enemigo, posicionActual))
            {
                AplicarDanio(enemigo, danioGolpe);
                yield break;
            }

            posicionAnterior = posicionActual;

            yield return null;
        }

        martilloARotar.localRotation = rotacionDestino;

        // Ultima comprobacion al llegar al Punto B.
        if (DetectarImpacto(enemigo, puntoImpacto.position))
            AplicarDanio(enemigo, danioGolpe);
    }

    private bool DetectarImpacto(
        GameObject enemigoObjetivo,
        Vector3 posicionAnterior)
    {
        if (puntoImpacto == null || enemigoObjetivo == null)
            return false;

        Vector3 posicionActual = puntoImpacto.position;

        // Detecta el contacto alrededor de la cabeza.
        Collider[] colisiones = Physics.OverlapSphere(
            posicionActual,
            radioDeteccion,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider colision in colisiones)
        {
            if (PerteneceAlEnemigo(colision, enemigoObjetivo))
                return true;
        }

        // Barre el trayecto recorrido para evitar atravesar
        // colliders entre dos frames.
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
                if (PerteneceAlEnemigo(
                    impacto.collider, enemigoObjetivo))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool PerteneceAlEnemigo(
        Collider colision,
        GameObject enemigoObjetivo)
    {
        if (colision == null)
            return false;

        EnemyStats stats =
            colision.GetComponentInParent<EnemyStats>();

        return stats != null &&
               stats.gameObject == enemigoObjetivo;
    }

    private void AplicarDanio(GameObject enemigo, float danioGolpe)
    {
        // Impide aplicar daño más de una vez por ataque.
        if (danoAplicado || enemigo == null)
            return;

        EnemyStats stats = enemigo.GetComponent<EnemyStats>();

        if (stats == null)
            stats = enemigo.GetComponentInParent<EnemyStats>();

        if (stats == null)
        {
            Debug.LogWarning(
                "No se encontro EnemyStats en el enemigo.",
                enemigo
            );
            return;
        }

        danoAplicado = true;

        float danioFinal =
            danioGolpe * ObtenerMultiplicadorChip();

        Debug.Log(
            "Impacto confirmado. Daño: " +
            Mathf.RoundToInt(danioFinal)
        );

        stats.RecibirDanio(danioFinal);

        ReiniciarCarga();
    }

    private IEnumerator RotarHasta(
        Quaternion rotacionDestino,
        float velocidad)
    {
        Quaternion rotacionInicial = martilloARotar.localRotation;

        float angulo = Quaternion.Angle(
            rotacionInicial,
            rotacionDestino
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

    private float ObtenerMultiplicadorChip()
    {
        if (equiparChips == null)
            return 1f;

        ModificadorStats modificador =
            equiparChips.ModificadorEquipado;

        if (modificador == null || modificador.efectos == null)
            return 1f;

        float multiplicador = 1f;

        foreach (EfectoModificador efecto in modificador.efectos)
        {
            if (efecto == null)
                continue;

            if (efecto.scriptObjetivo == TipoScript.Weapon &&
                efecto.atributo == AtributoModificable.DañoPlus)
            {
                multiplicador *= efecto.cantidad;
            }
        }

        return multiplicador;
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
