using UnityEngine;
using System.Collections;

public class AxePlayer : MonoBehaviour
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

    private int enemyLayer;
    private GameObject enemigoDentro;
    private bool criticoActivo = false;
    private bool atacando = false;
    private bool danoAplicado = false;

    private Renderer rend;
    private Color colorOriginal;
    private Material materialInstancia;

    private Quaternion rotacionReposo;
    private EquiparChips equiparChips;
    private Coroutine rutinaAtaque;

    private void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");

        rend = GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            materialInstancia = rend.material;
            colorOriginal = materialInstancia.color;
        }

        equiparChips = GetComponentInParent<EquiparChips>();

        if (hachaARotar != null)
        {
            rotacionReposo = hachaARotar.localRotation;
        }
        else
        {
            Debug.LogWarning(
                "AxePlayer: asigna el objeto que debe rotar en Hacha A Rotar.",
                this
            );
        }

        if (puntoImpacto == null)
        {
            Debug.LogWarning(
                "AxePlayer: asigna el Transform del Punto de Impacto.",
                this
            );
        }

        StartCoroutine(CicloCritico());
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyStats stats = other.GetComponentInParent<EnemyStats>();

        if (stats == null || stats.gameObject.layer != enemyLayer)
            return;

        enemigoDentro = stats.gameObject;

        if (!atacando)
        {
            rutinaAtaque = StartCoroutine(AnimacionAtaque(enemigoDentro));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyStats stats = other.GetComponentInParent<EnemyStats>();

        if (stats != null && stats.gameObject == enemigoDentro)
        {
            enemigoDentro = null;
        }
    }

    private IEnumerator AnimacionAtaque(GameObject enemigo)
    {
        if (hachaARotar == null || puntoImpacto == null || enemigo == null)
        {
            yield break;
        }

        atacando = true;
        danoAplicado = false;

        // El daño se calcula una sola vez por golpe.
        float danioGolpe = danioNormal * ObtenerMultiplicadorChip();

        if (criticoActivo)
        {
            danioGolpe *= multiplicadorCritico;
            Debug.Log("¡GOLPE CRITICO DEL HACHA!");
        }

        // 1. Preparacion del golpe.
        Quaternion rotacionA =
            rotacionReposo * Quaternion.Euler(rotacionPuntoA);

        yield return StartCoroutine(
            RotarHasta(rotacionA, velocidadPreparacion)
        );

        // 2. Golpe: avanza hasta tocar al enemigo.
        Quaternion rotacionB =
            rotacionReposo * Quaternion.Euler(rotacionPuntoB);

        yield return StartCoroutine(
            GolpearHastaContacto(rotacionB, enemigo, danioGolpe)
        );

        // 3. Regreso a la posicion de reposo.
        yield return StartCoroutine(
            RotarHasta(rotacionReposo, velocidadRegreso)
        );

        atacando = false;
        rutinaAtaque = null;

        // Si el enemigo sigue dentro, espera antes del siguiente ataque.
        if (enemigoDentro != null)
        {
            yield return new WaitForSeconds(tiempoEspera);

            if (enemigoDentro != null && !atacando)
            {
                rutinaAtaque = StartCoroutine(
                    AnimacionAtaque(enemigoDentro)
                );
            }
        }
    }

    private IEnumerator GolpearHastaContacto(
        Quaternion rotacionDestino,
        GameObject enemigo,
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

            // Detecta el contacto entre la posicion anterior y la actual.
            if (DetectarImpacto(
                enemigo,
                posicionAnterior,
                posicionActual))
            {
                AplicarDanio(enemigo, danioGolpe);
                yield break;
            }

            // Detecta si el punto ya esta dentro del enemigo.
            if (DetectarImpacto(enemigo, posicionActual))
            {
                AplicarDanio(enemigo, danioGolpe);
                yield break;
            }

            posicionAnterior = posicionActual;
            yield return null;
        }

        hachaARotar.localRotation = rotacionDestino;

        // Ultima comprobacion al terminar el movimiento.
        if (DetectarImpacto(enemigo, puntoImpacto.position))
        {
            AplicarDanio(enemigo, danioGolpe);
        }
    }

    private bool DetectarImpacto(
        GameObject enemigo,
        Vector3 posicionAnterior,
        Vector3 posicionActual)
    {
        if (enemigo == null || puntoImpacto == null)
            return false;

        // Comprueba si el punto de impacto ya esta dentro del objetivo.
        Collider[] cercanos = Physics.OverlapSphere(
            posicionActual,
            radioDeteccion
        );

        foreach (Collider col in cercanos)
        {
            if (PerteneceAlEnemigo(col, enemigo))
                return true;
        }

        // Comprueba el trayecto para evitar atravesar al enemigo
        // entre dos fotogramas.
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
                    PerteneceAlEnemigo(impacto.collider, enemigo))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool DetectarImpacto(GameObject enemigo, Vector3 posicion)
    {
        if (enemigo == null)
            return false;

        Collider[] cercanos = Physics.OverlapSphere(
            posicion,
            radioDeteccion
        );

        foreach (Collider col in cercanos)
        {
            if (PerteneceAlEnemigo(col, enemigo))
                return true;
        }

        return false;
    }

    private bool PerteneceAlEnemigo(
        Collider col,
        GameObject enemigo)
    {
        if (col == null || enemigo == null)
            return false;

        EnemyStats stats = col.GetComponentInParent<EnemyStats>();

        return stats != null && stats.gameObject == enemigo;
    }

    private void AplicarDanio(GameObject enemigo, float danio)
    {
        if (danoAplicado || enemigo == null)
            return;

        EnemyStats stats = enemigo.GetComponent<EnemyStats>();

        if (stats == null)
            return;

        danoAplicado = true;
        stats.RecibirDanio(danio);

        Debug.Log(
            criticoActivo
                ? "¡HACHA: GOLPE CRITICO APLICADO!"
                : "Hacha: impacto confirmado. Daño: " + danio
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

            Debug.Log("CRITICO DEL HACHA ACTIVADO");

            yield return new WaitForSeconds(duracionCritico);

            criticoActivo = false;

            if (materialInstancia != null)
                materialInstancia.color = colorOriginal;

            Debug.Log("CRITICO DEL HACHA TERMINADO");
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