using UnityEngine;

public class SawEnemy : MonoBehaviour
{
    [SerializeField] private float danioPorTick = 1f;

    private int playerLayer;

    // Referencia al EquiparChips del Enemy padre
    private EquiparChips equiparChips;

    private void Start()
    {
        playerLayer = LayerMask.NameToLayer("Player");

        // Busca EquiparChips en los objetos padre
        equiparChips = GetComponentInParent<EquiparChips>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer != playerLayer)
            return;

        PlayerStats stats = other.GetComponent<PlayerStats>();

        if (stats != null)
        {
            float multiplicadorChip = ObtenerMultiplicadorChip();
            float danioFinal = danioPorTick * multiplicadorChip;

            stats.RecibirDanio(danioFinal);
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
