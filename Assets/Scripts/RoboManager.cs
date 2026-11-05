
using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class RoboManager : MonoBehaviour
{
    [Serializable]
    public class RobotRuntimeData
    {
        [Header("Datos del robot")]
        [SerializeField] private int robotID;
        [SerializeField] private string robotName;
        [SerializeField] private GameObject robotPrefab;
        [SerializeField] private Sprite robotIcon;
        [SerializeField] private int rank;

        [Header("Estado del robot")]
        [SerializeField] private bool comprado;
        [SerializeField] private bool activo;

        public int RobotID => robotID;
        public string RobotName => robotName;
        public GameObject RobotPrefab => robotPrefab;
        public Sprite RobotIcon => robotIcon;
        public int Rank => rank;

        public bool Comprado
        {
            get => comprado;
            set => comprado = value;
        }

        public bool Activo
        {
            get => activo;
            set => activo = value;
        }

        public RobotRuntimeData(
            int robotID,
            RobotList.RobotData robotData,
            bool comprado,
            bool activo)
        {
            this.robotID = robotID;

            robotName = robotData.RobotName;
            robotPrefab = robotData.RobotPrefab;
            robotIcon = robotData.RobotIcon;
            rank = robotData.Rank;

            this.comprado = comprado;
            this.activo = activo;
        }
    }

    public static RoboManager Instance { get; private set; }

    [Header("Lista original de robots")]
    [SerializeField] private RobotList robotList;

    [Header("Datos de robots durante la partida")]
    [SerializeField]
    private List<RobotRuntimeData> robots =
        new List<RobotRuntimeData>();

    public IReadOnlyList<RobotRuntimeData> Robots => robots;

    public event Action OnRobotsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CrearListaDeRobots();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void CrearListaDeRobots()
    {
        robots.Clear();

        if (robotList == null)
        {
            Debug.LogError(
                "No se ha asignado un RobotList.",
                this
            );
            return;
        }

        for (int robotID = 0;
             robotID < robotList.RobotCount;
             robotID++)
        {
            RobotList.RobotData robotData =
                robotList.GetRobot(robotID);

            if (robotData == null)
            {
                Debug.LogWarning(
                    $"El robot con ID {robotID} no es válido.",
                    this
                );
                continue;
            }

            // Los dos primeros robots están comprados.
            bool compradoInicial = robotID < 2;

            // Los tres primeros robots están activos.
            bool activoInicial = robotID < 3;

            RobotRuntimeData runtimeData =
                new RobotRuntimeData(
                    robotID,
                    robotData,
                    compradoInicial,
                    activoInicial
                );

            robots.Add(runtimeData);
        }

        OnRobotsChanged?.Invoke();
    }

    public RobotRuntimeData GetRobot(int robotID)
    {
        for (int i = 0; i < robots.Count; i++)
        {
            if (robots[i] != null &&
                robots[i].RobotID == robotID)
            {
                return robots[i];
            }
        }

        return null;
    }

    public bool EstaComprado(int robotID)
    {
        RobotRuntimeData robot = GetRobot(robotID);

        return robot != null && robot.Comprado;
    }

    public bool EstaActivo(int robotID)
    {
        RobotRuntimeData robot = GetRobot(robotID);

        return robot != null && robot.Activo;
    }
}
