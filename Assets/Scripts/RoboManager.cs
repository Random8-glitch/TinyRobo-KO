
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Lista original de robots del jugador")]
    [SerializeField] private RobotList robotList;

    [Header("Lista original de robots enemigos")]
    [SerializeField] private RobotList robotListEnemigos;

    [Header("Datos de robots durante la partida")]
    [SerializeField]
    private List<RobotRuntimeData> robots =
        new List<RobotRuntimeData>();

    [Header("Datos de robots enemigos")]
    [SerializeField]
    private List<RobotRuntimeData> robotsEnemigos =
        new List<RobotRuntimeData>();

    [Header("Enemigo instanciado")]
    [SerializeField] private GameObject enemigoActual;

    public IReadOnlyList<RobotRuntimeData> Robots => robots;

    public IReadOnlyList<RobotRuntimeData> RobotsEnemigos =>
        robotsEnemigos;

    public GameObject EnemigoActual => enemigoActual;

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

        CrearListaDeEnemigos();
    }

    private void OnEnable()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void Start()
    {
        // Crear el enemigo también en la escena inicial.
        InstanciarEnemigo(SceneManager.GetActiveScene());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstanciarEnemigo(scene);
    }

    // ==========================================
    // ROBOTS DEL JUGADOR
    // ==========================================

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

    // ==========================================
    // LISTA DE ROBOTS ENEMIGOS
    // ==========================================

    private void CrearListaDeEnemigos()
    {
        robotsEnemigos.Clear();

        if (robotListEnemigos == null)
        {
            Debug.LogError(
                "No se ha asignado un RobotList de enemigos.",
                this
            );
            return;
        }

        for (int robotID = 0;
             robotID < robotListEnemigos.RobotCount;
             robotID++)
        {
            RobotList.RobotData robotData =
                robotListEnemigos.GetRobot(robotID);

            if (robotData == null)
            {
                Debug.LogWarning(
                    $"El enemigo con ID {robotID} no es válido.",
                    this
                );
                continue;
            }

            // Los enemigos no necesitan ser comprados.
            // Todos están disponibles para la selección,
            // siempre que su rango sea permitido.
            RobotRuntimeData runtimeData =
                new RobotRuntimeData(
                    robotID,
                    robotData,
                    true,
                    true
                );

            robotsEnemigos.Add(runtimeData);
        }
    }

    public RobotRuntimeData GetEnemigo(int robotID)
    {
        for (int i = 0; i < robotsEnemigos.Count; i++)
        {
            if (robotsEnemigos[i] != null &&
                robotsEnemigos[i].RobotID == robotID)
            {
                return robotsEnemigos[i];
            }
        }

        return null;
    }

    // ==========================================
    // SELECCIÓN ALEATORIA DE ENEMIGOS
    // ==========================================

    private RobotRuntimeData SeleccionarEnemigoAleatorio()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning(
                "No se encontró PlayerDataManager.",
                this
            );
            return null;
        }

        int rangoJugador = PlayerDataManager.Instance.Rank;

        List<RobotRuntimeData> enemigosDisponibles =
            new List<RobotRuntimeData>();

        foreach (RobotRuntimeData enemigo in robotsEnemigos)
        {
            if (enemigo == null)
                continue;

            if (enemigo.RobotPrefab == null)
                continue;

            // Solo enemigos con rango igual o menor.
            if (enemigo.Rank > rangoJugador)
                continue;

            enemigosDisponibles.Add(enemigo);
        }

        if (enemigosDisponibles.Count == 0)
        {
            Debug.LogWarning(
                $"No hay enemigos disponibles para " +
                $"el rango {rangoJugador}.",
                this
            );

            return null;
        }

        int indiceAleatorio = UnityEngine.Random.Range(
            0,
            enemigosDisponibles.Count
        );

        return enemigosDisponibles[indiceAleatorio];
    }

    // ==========================================
    // INSTANCIACIÓN DEL ENEMIGO
    // ==========================================

    private void InstanciarEnemigo(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        // Evitar duplicar el enemigo en la misma escena.
        if (enemigoActual != null &&
            enemigoActual.scene == scene)
        {
            return;
        }

        // Buscar EnemyEmpty en la escena.
        GameObject[] objetos = scene.GetRootGameObjects();

        Transform enemyEmpty = null;

        foreach (GameObject objeto in objetos)
        {
            enemyEmpty = BuscarEnemyEmpty(objeto.transform);

            if (enemyEmpty != null)
                break;
        }

        // Si la escena no tiene EnemyEmpty,
        // no necesita crear enemigos.
        if (enemyEmpty == null)
            return;

        // Elegir un enemigo según el rango del jugador.
        RobotRuntimeData enemigoSeleccionado =
            SeleccionarEnemigoAleatorio();

        if (enemigoSeleccionado == null)
            return;

        // Crear una copia del prefab seleccionado.
        enemigoActual = Instantiate(
            enemigoSeleccionado.RobotPrefab,
            enemyEmpty.position,
            enemyEmpty.rotation
        );

        // Asociar la copia a la escena correspondiente.
        if (enemigoActual.scene != scene)
        {
            SceneManager.MoveGameObjectToScene(
                enemigoActual,
                scene
            );
        }

        Debug.Log(
            $"Enemigo generado: " +
            $"{enemigoSeleccionado.RobotName} " +
            $"(ID {enemigoSeleccionado.RobotID}, " +
            $"Rango {enemigoSeleccionado.Rank})",
            this
        );
    }

    // ==========================================
    // BÚSQUEDA DE ENEMYEMPTY
    // ==========================================

    private Transform BuscarEnemyEmpty(Transform objeto)
    {
        if (objeto.name == "EnemyEmpty")
            return objeto;

        foreach (Transform hijo in objeto)
        {
            Transform encontrado =
                BuscarEnemyEmpty(hijo);

            if (encontrado != null)
                return encontrado;
        }

        return null;
    }
}
