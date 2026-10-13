
using UnityEngine;
using System.Collections;

public class AxePlayer : MonoBehaviour
{
    [Header("Ataque")]
    [SerializeField] private float tiempoEspera = 1f;
    [SerializeField] private float danioNormal = 10f;
    [SerializeField] private float multiplicadorCritico = 3f;

    [Header("Estado Crítico")]
    [SerializeField] private float tiempoMinCritico = 5f;
    [SerializeField] private float tiempoMaxCritico = 10f;
    [SerializeField] private float duracionCritico = 3f;

    private int enemyLayer;
    private GameObject enemigoDentro;

    private bool criticoActivo = false;

    private Renderer rend;
    private Color colorOriginal;

    // Referencia al EquiparChips del Player padre
    private EquiparChips equiparChips;

    private void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");

        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            colorOriginal = rend.material.color;
        }

        // Busca EquiparChips en los objetos padre
        equiparChips = GetComponentInParent<EquiparChips>();

        StartCoroutine(CicloCritico());
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

        if (enemigoDentro == enemigo)
        {
            // Daño base del hacha
            float danio = danioNormal;

            // 1. Aplicar multiplicador del chip
            float multiplicadorChip = ObtenerMultiplicadorChip();

            danio *= multiplicadorChip;

            // 2. Aplicar crítico después del chip
            if (criticoActivo)
            {
                danio *= multiplicadorCritico;
                Debug.Log("¡GOLPE CRÍTICO!");
            }
            else
            {
                Debug.Log("Enemigo golpeado");
            }

            EnemyStats stats = enemigo.GetComponent<EnemyStats>();

            if (stats != null)
            {
                stats.RecibirDanio(danio);
            }
        }
    }

    // Busca únicamente efectos Weapon -> DañoPlus
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

            if (rend != null)
            {
                rend.material.color = Color.red;
            }

            Debug.Log("CRÍTICO ACTIVADO");

            yield return new WaitForSeconds(duracionCritico);

            criticoActivo = false;

            if (rend != null)
            {
                rend.material.color = colorOriginal;
            }

            Debug.Log("CRÍTICO TERMINADO");
        }
    }
}
