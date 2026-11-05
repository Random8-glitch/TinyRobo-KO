
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BotonEquiparRobo : MonoBehaviour
{
    [Header("Robot que representa el botón")]
    [Min(0)]
    [SerializeField] private int robotID;

    private static readonly Color ColorEquipado = Color.green;

    private Button boton;
    private ColorBlock coloresOriginales;

    private bool estadoInicializado;
    private bool ultimoEquipado;
    private bool ultimoDisponible;

    private void Awake()
    {
        boton = GetComponent<Button>();

        // Guardar los colores originales.
        coloresOriginales = boton.colors;

        boton.onClick.AddListener(EquiparRobot);
    }

    private void Start()
    {
        ActualizarEstado();
    }

    private void OnEnable()
    {
        ActualizarEstado();
    }

    private void Update()
    {
        // Detectar cambios de compra o equipamiento,
        // incluso si otros scripts modificaron los datos.
        ActualizarEstado();
    }

    private void OnDestroy()
    {
        if (boton != null)
        {
            boton.onClick.RemoveListener(
                EquiparRobot
            );
        }
    }


    public void EquiparRobot()
    {
        RoboManager roboManager = RoboManager.Instance;

        PlayerDataManager playerData =
            PlayerDataManager.Instance;

        if (roboManager == null || playerData == null)
            return;

        RoboManager.RobotRuntimeData robot =
            roboManager.GetRobot(robotID);

        if (robot == null)
        {
            Debug.LogWarning(
                $"BotonEquiparRobo: No existe el robot ID {robotID}.",
                this
            );
            return;
        }

        // Impedir equipar robots no comprados.
        if (!robot.Comprado)
        {
            Debug.LogWarning(
                $"El robot {robot.RobotName} no está comprado.",
                this
            );
            return;
        }

        // No hacer nada si ya está equipado.
        if (playerData.RobotEquipadoID == robotID)
            return;

        if (robot.RobotPrefab == null)
        {
            Debug.LogWarning(
                $"El robot {robot.RobotName} no tiene prefab.",
                this
            );
            return;
        }

        // Solicitar el cambio de vehículo.
        bool cambiado =
            playerData.CambiarRobotEquipado(robotID);

        if (!cambiado)
            return;

        // ==========================================
        // DESACTIVAR MOVIMIENTO DEL NUEVO VEHÍCULO
        // ==========================================

        GameObject nuevoVehiculo = playerData.CarrozaActual;

        if (nuevoVehiculo != null)
        {
            RoboMovPlayer roboMov =
                nuevoVehiculo.GetComponentInChildren<RoboMovPlayer>(true);

            if (roboMov != null)
            {
                // Desactivar únicamente este componente
                // de la instancia recién creada.
                roboMov.enabled = false;
            }
        }

        // Actualizar estado visual del botón.
        ActualizarEstado();

        Debug.Log(
            $"BotonEquiparRobo: Se equipó {robot.RobotName}.",
            this
        );
    }


    private void ActualizarEstado()
    {
        if (boton == null)
            return;

        RoboManager roboManager = RoboManager.Instance;

        PlayerDataManager playerData =
            PlayerDataManager.Instance;

        RoboManager.RobotRuntimeData robot = null;

        if (roboManager != null)
        {
            robot = roboManager.GetRobot(robotID);
        }

        // Comprobar si este botón representa
        // al robot actualmente equipado.
        bool equipado =
            playerData != null &&
            playerData.RobotEquipadoID == robotID;

        // Un robot solo está disponible para equipar
        // cuando está comprado y tiene prefab.
        bool disponible =
            robot != null &&
            robot.Comprado &&
            robot.RobotPrefab != null;

        // No repetir cambios visuales innecesarios.
        if (estadoInicializado &&
            ultimoEquipado == equipado &&
            ultimoDisponible == disponible)
        {
            return;
        }

        estadoInicializado = true;
        ultimoEquipado = equipado;
        ultimoDisponible = disponible;

        // El botón no funciona cuando:
        // - El robot no está comprado.
        // - No tiene prefab.
        // - Ya está equipado.
        boton.interactable =
            disponible && !equipado;

        ColorBlock colores = coloresOriginales;

        if (equipado)
        {
            // Mostrar verde el robot equipado.
            colores.normalColor = ColorEquipado;
            colores.highlightedColor = ColorEquipado;
            colores.selectedColor = ColorEquipado;

            // Necesario porque el botón está desactivado.
            colores.disabledColor = ColorEquipado;
        }

        boton.colors = colores;
    }
}
