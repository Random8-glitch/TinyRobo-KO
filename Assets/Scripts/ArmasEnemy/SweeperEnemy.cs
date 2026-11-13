using UnityEngine;

public class SweeperEnemy : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private float danioBase = 5f;

    [Header("Empuje")]
    [SerializeField] private float distanciaEmpuje = 3f;
    [SerializeField] private float duracionEmpuje = 0.25f;

    private int playerLayer;

    private RoboMovEnemy enemy;
    private EquiparChips equiparChips;

    private void Start()
    {
        playerLayer = LayerMask.NameToLayer("Player");

        enemy = GetComponentInParent<RoboMovEnemy>();
        equiparChips = GetComponentInParent<EquiparChips>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != playerLayer)
            return;

        PlayerStats stats =
            other.GetComponent<PlayerStats>();

        float danioFinal = danioBase * ObtenerMultiplicadorChip();

        if (stats != null)
        {
            stats.RecibirDanio(danioFinal);
        }

        RoboMovPlayer player =
            other.GetComponent<RoboMovPlayer>();

        if (player != null)
        {
            Vector3 direccionEmpuje;

            if (enemy != null)
            {
                direccionEmpuje = enemy.transform.forward;
            }
            else
            {
                direccionEmpuje = transform.forward;
            }

            direccionEmpuje.y = 0f;
            direccionEmpuje.Normalize();

            player.Empujar(
                direccionEmpuje,
                distanciaEmpuje,
                duracionEmpuje
            );
        }
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
}
