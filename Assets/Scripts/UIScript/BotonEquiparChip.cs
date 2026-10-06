
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BotonEquiparChip : MonoBehaviour
{
    [Header("Chip que equipará el botón")]
    [SerializeField] private ModificadorStats chip;

    private Button boton;

    private void Awake()
    {
        boton = GetComponent<Button>();
        boton.onClick.AddListener(EquiparODesequiparChip);
    }

    private void OnDestroy()
    {
        if (boton != null)
            boton.onClick.RemoveListener(EquiparODesequiparChip);
    }

    public void EquiparODesequiparChip()
    {
        if (chip == null)
        {
            Debug.LogWarning(
                "BotonEquiparChip: No hay ningún chip asignado.",
                this
            );
            return;
        }

        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
        {
            Debug.LogWarning(
                "BotonEquiparChip: No existe el Layer Player.",
                this
            );
            return;
        }

        EquiparChips equiparChips = null;

        EquiparChips[] gestores =
            FindObjectsByType<EquiparChips>(
                FindObjectsSortMode.None
            );

        foreach (EquiparChips gestor in gestores)
        {
            if (gestor.gameObject.layer == playerLayer)
            {
                equiparChips = gestor;
                break;
            }
        }

        if (equiparChips == null)
        {
            Debug.LogWarning(
                "BotonEquiparChip: No se encontró EquiparChips en el Player.",
                this
            );
            return;
        }

        // Obtener el chip actualmente equipado.
        ModificadorStats chipActual = equiparChips.ModificadorEquipado;

        // Comprobar si es el mismo ScriptableObject original.
        if (chipActual != null &&
            chipActual.Origen == chip)
        {
            equiparChips.Desequipar();

            Debug.Log(
                "Chip desequipado: " + chip.name,
                this
            );

            return;
        }

        // Crear una copia independiente.
        ModificadorStats copiaChip = Instantiate(chip);

        // Guardar de qué ScriptableObject proviene.
        copiaChip.Origen = chip;

        // Equipar el nuevo chip.
        equiparChips.Equipar(copiaChip);

        Debug.Log(
            "Chip equipado: " + chip.name,
            this
        );
    }
}
