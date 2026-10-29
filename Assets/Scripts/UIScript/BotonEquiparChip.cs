using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BotonEquiparChip : MonoBehaviour
{
    [Header("Chip que equipará el botón")]
    [SerializeField] private ModificadorStats chip;

    [Header("Colores del botón")]
    [SerializeField] private Color colorEquipado = Color.green;

    private Button boton;
    private ColorBlock coloresOriginales;

    private void Awake()
    {
        boton = GetComponent<Button>();

        // Guardar los colores originales del botón.
        coloresOriginales = boton.colors;

        boton.onClick.AddListener(EquiparODesequiparChip);
    }

    private void Start()
    {
        ActualizarColor();
    }

    private void OnEnable()
    {
        ActualizarColor();
    }

    private void OnDestroy()
    {
        if (boton != null)
        {
            boton.onClick.RemoveListener(
                EquiparODesequiparChip
            );
        }
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
                "BotonEquiparChip: No se encontró " +
                "EquiparChips en el Player.",
                this
            );
            return;
        }

        ModificadorStats chipActual =
            equiparChips.ModificadorEquipado;

        // Comprobar si el chip pulsado ya está equipado.
        if (chipActual != null)
        {
            ModificadorStats originalActual =
                chipActual.Origen != null
                    ? chipActual.Origen
                    : chipActual;

            if (originalActual == chip)
            {
                equiparChips.Desequipar();

                ActualizarColor();

                Debug.Log(
                    "Chip desequipado: " + chip.name,
                    this
                );

                return;
            }
        }

        // Equipar el chip seleccionado.
        equiparChips.Equipar(chip);

        // Actualizar el color de este botón.
        ActualizarColor();

        // Actualizar los demás botones de chips.
        BotonEquiparChip[] botones =
            FindObjectsByType<BotonEquiparChip>(
                FindObjectsSortMode.None
            );

        foreach (BotonEquiparChip otroBoton in botones)
        {
            if (otroBoton != this)
            {
                otroBoton.ActualizarColor();
            }
        }

        Debug.Log(
            "Chip equipado: " + chip.name,
            this
        );
    }

    private void ActualizarColor()
    {
        if (boton == null)
            return;

        if (chip == null)
        {
            RestaurarColorOriginal();
            return;
        }

        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
        {
            RestaurarColorOriginal();
            return;
        }

        EquiparChips[] gestores =
            FindObjectsByType<EquiparChips>(
                FindObjectsSortMode.None
            );

        foreach (EquiparChips gestor in gestores)
        {
            if (gestor.gameObject.layer != playerLayer)
                continue;

            ModificadorStats equipado =
                gestor.ModificadorEquipado;

            if (equipado == null)
            {
                RestaurarColorOriginal();
                return;
            }

            ModificadorStats originalEquipado =
                equipado.Origen != null
                    ? equipado.Origen
                    : equipado;

            if (originalEquipado == chip)
            {
                // El chip está equipado: botón verde.
                ColorBlock colores = coloresOriginales;
                colores.normalColor = colorEquipado;
                colores.highlightedColor = colorEquipado;
                colores.selectedColor = colorEquipado;

                boton.colors = colores;
                return;
            }
        }

        // El chip no está equipado.
        RestaurarColorOriginal();
    }

    private void RestaurarColorOriginal()
    {
        if (boton != null)
        {
            boton.colors = coloresOriginales;
        }
    }
}
