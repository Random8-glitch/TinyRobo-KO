using UnityEngine;

public class SweeperPlayer : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private float danioBase = 5f;

    [Header("Empuje")]
    [SerializeField] private float distanciaEmpuje = 3f;
    [SerializeField] private float duracionEmpuje = 0.25f;

    private int enemyLayer;

    private RoboMovPlayer player;

    // Referencia al EquiparChips del Player padre
    private EquiparChips equiparChips;

    private void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");

        player = GetComponentInParent<RoboMovPlayer>();

        equiparChips = GetComponentInParent<EquiparChips>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != enemyLayer)
            return;

        EnemyStats stats =
            other.GetComponent<EnemyStats>();

        float multiplicadorChip = ObtenerMultiplicadorChip();

        danioBase *= multiplicadorChip;

        if (stats != null)
        {
            stats.RecibirDanio(danioBase);
        }

        RoboMovEnemy enemigo =
            other.GetComponent<RoboMovEnemy>();

        if (enemigo != null)
        {
            Vector3 direccionEmpuje;

            if (player != null)
            {
                direccionEmpuje = player.transform.forward;
            }
            else
            {
                direccionEmpuje = transform.forward;
            }

            direccionEmpuje.y = 0f;
            direccionEmpuje.Normalize();

            enemigo.Empujar(
                direccionEmpuje,
                distanciaEmpuje,
                duracionEmpuje
            );
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
}