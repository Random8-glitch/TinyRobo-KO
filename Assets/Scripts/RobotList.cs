
using UnityEngine;

[CreateAssetMenu(
    fileName = "RobotList",
    menuName = "Robots/Robot List"
)]
public class RobotList : ScriptableObject
{
    [System.Serializable]
    public class RobotData
    {
        [Tooltip("Nombre utilizado para identificar el robot.")]
        [SerializeField] private string robotName;

        [Tooltip("Prefab físico del robot.")]
        [SerializeField] private GameObject robotPrefab;

        [Tooltip("Icono mostrado en la interfaz.")]
        [SerializeField] private Sprite robotIcon;

        [Tooltip("Rango mínimo necesario para desbloquear el robot.")]
        [Min(1)]
        [SerializeField] private int rank = 1;

        public string RobotName => robotName;
        public GameObject RobotPrefab => robotPrefab;
        public Sprite RobotIcon => robotIcon;
        public int Rank => rank;
    }

    [Header("Robots disponibles")]
    [Tooltip(
        "La posición de cada robot dentro de esta lista funciona como su ID."
    )]
    [SerializeField]
    private RobotData[] robots = new RobotData[0];

    public int RobotCount
    {
        get
        {
            if (robots == null)
                return 0;

            return robots.Length;
        }
    }

    public bool IsValidRobotID(int robotID)
    {
        return robots != null &&
               robotID >= 0 &&
               robotID < robots.Length &&
               robots[robotID] != null;
    }

    public RobotData GetRobot(int robotID)
    {
        if (!IsValidRobotID(robotID))
            return null;

        return robots[robotID];
    }

    public GameObject GetRobotPrefab(int robotID)
    {
        RobotData robot = GetRobot(robotID);

        if (robot == null)
            return null;

        return robot.RobotPrefab;
    }

    public Sprite GetRobotIcon(int robotID)
    {
        RobotData robot = GetRobot(robotID);

        if (robot == null)
            return null;

        return robot.RobotIcon;
    }

    public string GetRobotName(int robotID)
    {
        RobotData robot = GetRobot(robotID);

        if (robot == null)
            return string.Empty;

        return robot.RobotName;
    }

    public int GetRobotRank(int robotID)
    {
        RobotData robot = GetRobot(robotID);

        if (robot == null)
            return 0;

        return robot.Rank;
    }

    private void OnValidate()
    {
        if (robots == null)
            return;

        for (int i = 0; i < robots.Length; i++)
        {
            if (robots[i] == null)
            {
                Debug.LogWarning(
                    $"El elemento de robot con ID {i} está vacío.",
                    this
                );

                continue;
            }

            if (robots[i].RobotPrefab == null)
            {
                Debug.LogWarning(
                    $"El robot con ID {i} no tiene un prefab asignado.",
                    this
                );
            }

            if (robots[i].RobotIcon == null)
            {
                Debug.LogWarning(
                    $"El robot con ID {i} no tiene un icono asignado.",
                    this
                );
            }

            if (robots[i].Rank < 1)
            {
                Debug.LogWarning(
                    $"El robot con ID {i} tiene un rango menor que 1.",
                    this
                );
            }
        }
    }
}
