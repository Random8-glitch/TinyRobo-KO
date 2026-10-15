using UnityEngine;
using System.Collections;

public class HammerPlayer : MonoBehaviour
{
    [Header("Ataque")]
    [SerializeField] private float tiempoEspera = 1f;

    [Header("Daño")]
    [SerializeField] private float danioMinimo = 5f;
    [SerializeField] private float danioMaximo = 40f;
    [SerializeField] private float tiempoCargaCompleta = 20f;

    private int enemyLayer;
    private GameObject enemigoDentro;

    private float danioActual;
    private bool cargaCompleta = false;

    private Renderer rend;
    private Color colorOriginal;
    private Color colorCargaMaxima = new Color(1f, 0.5f, 0f); // Naranja

    // Referencia al EquiparChips del Player padre
    private EquiparChips equiparChips;

    private void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");

        danioActual = danioMinimo;

        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            colorOriginal = rend.material.color;
        }

        // Busca EquiparChips en los objetos padre
        equiparChips = GetComponentInParent<EquiparChips>();
    }

    private void Update()
    {
        if (cargaCompleta)
            return;

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
        if (other.gameObject.layer != enemyLayer)
            return;

        enemigoDentro = other.gameObject;

        StartCoroutine(EsperarHit(other.gameObject));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == enemigoDentro)
        {
            enemigoDentro = null;
        }
    }

    private IEnumerator EsperarHit(GameObject enemigo)
    {
        yield return new WaitForSeconds(tiempoEspera);

        float danioGolpe = danioActual;

        float multiplicadorChip = ObtenerMultiplicadorChip();

        danioGolpe *= multiplicadorChip;

        ReiniciarCarga();

        if (enemigoDentro == enemigo)
        {
            Debug.Log(
                "Martillazo de " +
                Mathf.RoundToInt(danioGolpe)
            );

            EnemyStats stats = enemigo.GetComponent<EnemyStats>();

            if (stats != null)
            {
                stats.RecibirDanio(danioGolpe);
            }
        }
    }

    private float ObtenerMultiplicadorChip()
    {
        // Si no existe EquiparChips, no modifica el daño
        if (equiparChips == null)
            return 1f;

        ModificadorStats modificador =
            equiparChips.ModificadorEquipado;

        // Si no hay modificador equipado
        if (modificador == null)
            return 1f;

        if (modificador.efectos == null)
            return 1f;

        float multiplicador = 1f;

        foreach (EfectoModificador efecto in modificador.efectos)
        {
            if (efecto == null)
                continue;

            // Solamente Weapon -> DañoPlus
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
        {
            rend.material.color = colorOriginal;
        }
    }

    private void ActualizarColor()
    {
        if (rend == null)
            return;

        float porcentajeCarga =
            (danioActual - danioMinimo) /
            (danioMaximo - danioMinimo);

        rend.material.color = Color.Lerp(
            colorOriginal,
            colorCargaMaxima,
            porcentajeCarga
        );
    }
}