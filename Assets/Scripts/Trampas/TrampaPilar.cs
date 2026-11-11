using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrampaPilar : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private bool activarSiempre = false;

    [Tooltip("Tiempo entre activaciones")]
    [SerializeField] private float tiempoEntreActivaciones = 3f;

    [Tooltip("Tiempo que espera el pilar después de detectar al objetivo")]
    [SerializeField] private float retrasoAntesDeActivar = 1f;

    [Header("Movimiento del pilar")]
    [SerializeField] private Transform pilar;

    [Tooltip("Desplazamiento local desde la posicion inicial")]
    [SerializeField]
    private Vector3 desplazamientoAtaque =
        new Vector3(0, 3, 0);

    [SerializeField] private float velocidadAtaque = 8f;
    [SerializeField] private float velocidadRegreso = 5f;

    [Tooltip("Tiempo que permanece en la posicion de ataque")]
    [SerializeField] private float tiempoGolpe = 0.3f;

    [Header("Deteccion de impacto")]
    [SerializeField] private Collider colliderImpacto;

    [SerializeField] private LayerMask capasObjetivo;

    [SerializeField] private float danio = 20f;

    [Tooltip("Nombre del metodo de daño que reciben los objetivos")]
    [SerializeField] private string metodoDanio = "RecibirDanio";

    private Vector3 posicionInicial;
    private bool enMovimiento = false;
    private bool enCooldown = false;

    [Header("Deteccion de colision")]
    [SerializeField] private Collider colliderPilar;
    [SerializeField] private LayerMask capasColision;
    [SerializeField] private float toleranciaImpacto = 0.05f;

    private bool impactoDetectado = false;

    [Header("Agitacion de camara")]
    [SerializeField] private AgitarCamara camara;

    [SerializeField] private float duracionAgitacion = 0.2f;
    [SerializeField] private float intensidadAgitacion = 0.15f;

    private void Start()
    {
        if (pilar == null)
        {
            Debug.LogError(
                "TrampaPilar: No se asigno el pilar.",
                this
            );

            enabled = false;
            return;
        }

        posicionInicial = pilar.localPosition;

        if (activarSiempre)
        {
            StartCoroutine(CicloAutomatico());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activarSiempre || enMovimiento || enCooldown)
            return;

        if (!EsObjetivoValido(other))
            return;

        StartCoroutine(ActivacionPorTrigger());
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enMovimiento || impactoDetectado)
            return;

        int capa = collision.gameObject.layer;

        if ((capasColision.value & (1 << capa)) == 0)
            return;

        impactoDetectado = true;

        AplicarDanio();

        if (camara != null)
        {
            camara.Agitar();
        }
    }

    private bool EsObjetivoValido(Collider objetivo)
    {
        int capa = objetivo.gameObject.layer;

        return (capasObjetivo.value & (1 << capa)) != 0;
    }

    private IEnumerator ActivacionPorTrigger()
    {
        enCooldown = true;

        float retraso = Mathf.Max(
            0f,
            retrasoAntesDeActivar
        );

        yield return new WaitForSeconds(retraso);

        yield return StartCoroutine(EjecutarTrampa());

        yield return new WaitForSeconds(tiempoEntreActivaciones);

        enCooldown = false;
    }

    private IEnumerator CicloAutomatico()
    {
        while (true)
        {
            yield return StartCoroutine(EjecutarTrampa());

            yield return new WaitForSeconds(tiempoEntreActivaciones);
        }
    }

    private IEnumerator EjecutarTrampa()
    {
        enMovimiento = true;
        impactoDetectado = false;

        Vector3 posicionAtaque =
            posicionInicial + desplazamientoAtaque;

        yield return StartCoroutine(
            MoverPilar(
                posicionAtaque,
                velocidadAtaque,
                true
            )
        );

        yield return new WaitForSeconds(tiempoGolpe);

        impactoDetectado = false;

        yield return StartCoroutine(
            MoverPilar(
                posicionInicial,
                velocidadRegreso,
                false
            )
        );

        enMovimiento = false;
    }

    private IEnumerator MoverPilar(
        Vector3 destino,
        float velocidad,
        bool detectarImpacto)
    {
        while (Vector3.Distance(
            pilar.localPosition,
            destino
        ) > 0.01f)
        {
            if (detectarImpacto && impactoDetectado)
                break;

            pilar.localPosition = Vector3.MoveTowards(
                pilar.localPosition,
                destino,
                velocidad * Time.deltaTime
            );

            yield return null;
        }

        if (!impactoDetectado || !detectarImpacto)
        {
            pilar.localPosition = destino;
        }
    }

    private void AplicarDanio()
    {
        if (colliderImpacto == null)
        {
            Debug.LogWarning(
                "TrampaPilar: No se asignó el collider de impacto.",
                this
            );

            return;
        }

        Bounds limites = colliderImpacto.bounds;

        Collider[] objetivos = Physics.OverlapBox(
            limites.center,
            limites.extents,
            colliderImpacto.transform.rotation,
            capasObjetivo
        );

        HashSet<Transform> objetivosGolpeados =
            new HashSet<Transform>();

        foreach (Collider objetivo in objetivos)
        {
            Transform raiz = objetivo.transform.root;

            if (objetivosGolpeados.Contains(raiz))
                continue;

            objetivosGolpeados.Add(raiz);

            objetivo.gameObject.SendMessageUpwards(
                metodoDanio,
                danio,
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (colliderImpacto != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawWireCube(
                colliderImpacto.bounds.center,
                colliderImpacto.bounds.size
            );
        }
    }
}

