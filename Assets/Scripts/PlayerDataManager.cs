

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(-200)]
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    private const string RankKey = "PlayerRank";
    private const string MoneyKey = "PlayerMoney";

    [Header("Datos del jugador")]
    [SerializeField] private int rank = 1;
    [SerializeField] private int money = 0;

    [Header("Movimiento")]
    [SerializeField] private bool toggleMov = true;

    public bool ToggleMov => toggleMov;

    [Header("Carroza Equipada")]
    [SerializeField] private GameObject carrozaEquipada;

    [SerializeField] private int robotEquipadoID = -1;

    public GameObject CarrozaActual { get; private set; }

    public int RobotEquipadoID => robotEquipadoID;

    public event Action OnRobotEquipadoChanged;

    [Header("UI")]
    [Tooltip("Nombre del objeto TextMeshPro que mostrará el dinero.")]
    [SerializeField] private string nombreTextoDinero = "TextoDinero";

    private TextMeshProUGUI textoDinero;
    private Toggle toggleMovUI;

    public int Rank => rank;
    public int Money => money;

    public event Action OnPlayerDataChanged;

    private void Awake()
    {
        // Evita duplicados al cambiar de escena.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Mantener este objeto entre escenas.
        DontDestroyOnLoad(gameObject);

        // LoadData();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        BuscarTextoDinero();
        ActualizarUIDinero();

        BuscarToggleMov(SceneManager.GetActiveScene());

        // Identificar el robot inicial mediante su prefab.
        DetectarRobotEquipadoInicial();

        InstanciarCarroza(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BuscarTextoDinero();
        ActualizarUIDinero();

        BuscarToggleMov(scene);

        InstanciarCarroza(scene);
    }

    private void InstanciarCarroza(Scene scene)
    {
        if (carrozaEquipada == null)
            return;

        // Evitar instanciar dos veces en la misma escena.
        if (CarrozaActual != null &&
            CarrozaActual.scene == scene)
        {
            return;
        }

        GameObject[] objetos = scene.GetRootGameObjects();

        foreach (GameObject objeto in objetos)
        {
            Transform playerEmpty =
                BuscarPlayerEmpty(objeto.transform);

            if (playerEmpty != null)
            {
                CarrozaActual = Instantiate(
                    carrozaEquipada,
                    playerEmpty.position,
                    playerEmpty.rotation
                );

                return;
            }
        }

        Debug.LogWarning(
            $"No se encontró PlayerEmpty en la escena '{scene.name}'."
        );
    }

    private Transform BuscarPlayerEmpty(Transform objeto)
    {
        if (objeto.name == "PlayerEmpty")
            return objeto;

        foreach (Transform hijo in objeto)
        {
            Transform encontrado = BuscarPlayerEmpty(hijo);

            if (encontrado != null)
                return encontrado;
        }

        return null;
    }

    private void BuscarTextoDinero()
    {
        TextMeshProUGUI[] textos =
            Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();

        for (int i = 0; i < textos.Length; i++)
        {
            TextMeshProUGUI texto = textos[i];

            if (texto.gameObject.name == nombreTextoDinero &&
                texto.gameObject.scene.IsValid())
            {
                textoDinero = texto;
                return;
            }
        }

        textoDinero = null;

        Debug.LogWarning(
            $"No se encontró el objeto UI '{nombreTextoDinero}' " +
            $"en la escena '{SceneManager.GetActiveScene().name}'."
        );
    }

    private void ActualizarUIDinero()
    {
        if (textoDinero != null)
        {
            textoDinero.text = money.ToString();
        }
    }

    private void BuscarToggleMov(Scene scene)
    {
        if (toggleMovUI != null)
        {
            toggleMovUI.onValueChanged.RemoveListener(
                OnToggleMovChanged
            );
        }

        toggleMovUI = null;

        Toggle[] toggles =
            Resources.FindObjectsOfTypeAll<Toggle>();

        for (int i = 0; i < toggles.Length; i++)
        {
            Toggle toggle = toggles[i];

            if (toggle.gameObject.name == "ToggleMov" &&
                toggle.gameObject.scene == scene)
            {
                toggleMovUI = toggle;

                toggleMovUI.SetIsOnWithoutNotify(toggleMov);

                toggleMovUI.onValueChanged.AddListener(
                    OnToggleMovChanged
                );

                return;
            }
        }
    }

    private void OnToggleMovChanged(bool nuevoEstado)
    {
        toggleMov = nuevoEstado;
    }

    private void OnDestroy()
    {
        if (toggleMovUI != null)
        {
            toggleMovUI.onValueChanged.RemoveListener(
                OnToggleMovChanged
            );
        }
    }

    public void SetRank(int newRank)
    {
        rank = Mathf.Max(1, newRank);

        // SaveData();

        OnPlayerDataChanged?.Invoke();
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
            return;

        money += amount;

        // SaveData();

        ActualizarUIDinero();

        OnPlayerDataChanged?.Invoke();
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0 || money < amount)
            return false;

        money -= amount;

        // SaveData();

        ActualizarUIDinero();

        OnPlayerDataChanged?.Invoke();

        return true;
    }

    public void SaveData()
    {
        PlayerPrefs.SetInt(RankKey, rank);
        PlayerPrefs.SetInt(MoneyKey, money);

        // PlayerPrefs.Save();
    }

    public void LoadData()
    {
        rank = PlayerPrefs.GetInt(RankKey, 1);
        money = PlayerPrefs.GetInt(MoneyKey, 0);

        ActualizarUIDinero();

        OnPlayerDataChanged?.Invoke();
    }

    public void ResetData()
    {
        rank = 1;
        money = 0;

        PlayerPrefs.DeleteKey(RankKey);
        PlayerPrefs.DeleteKey(MoneyKey);

        // PlayerPrefs.Save();

        ActualizarUIDinero();

        OnPlayerDataChanged?.Invoke();
    }

    private void OnApplicationPause(bool isPaused)
    {
        // if (isPaused)
        //     SaveData();
    }

    private void OnApplicationQuit()
    {
        // SaveData();
    }


    private void DetectarRobotEquipadoInicial()
    {
        RoboManager roboManager = RoboManager.Instance;

        if (roboManager == null)
        {
            Debug.LogWarning(
                "PlayerDataManager: No se encontró RoboManager.",
                this
            );
            return;
        }

        robotEquipadoID = -1;

        if (carrozaEquipada == null)
            return;

        foreach (RoboManager.RobotRuntimeData robot in roboManager.Robots)
        {
            if (robot == null)
                continue;

            // Comparar el prefab original con los de RoboManager.
            if (robot.RobotPrefab == carrozaEquipada)
            {
                robotEquipadoID = robot.RobotID;
                return;
            }
        }

        Debug.LogWarning(
            "La carroza inicial no corresponde a ningún robot de RoboManager.",
            this
        );
    }

    public bool CambiarRobotEquipado(int nuevoRobotID)
    {
        RoboManager roboManager = RoboManager.Instance;

        if (roboManager == null)
            return false;

        RoboManager.RobotRuntimeData robot =
            roboManager.GetRobot(nuevoRobotID);

        if (robot == null)
            return false;

        // Solo pueden equiparse robots comprados.
        if (!robot.Comprado)
            return false;

        if (robot.RobotPrefab == null)
            return false;

        // No cambiar si ya está equipado.
        if (robotEquipadoID == nuevoRobotID ||
            carrozaEquipada == robot.RobotPrefab)
        {
            return false;
        }

        GameObject carrozaAnterior = CarrozaActual;

        bool existeCarrozaAnterior =
            carrozaAnterior != null;

        Vector3 posicion = Vector3.zero;
        Quaternion rotacion = Quaternion.identity;

        // Conservar posición y rotación de la carroza anterior.
        if (existeCarrozaAnterior)
        {
            posicion = carrozaAnterior.transform.position;
            rotacion = carrozaAnterior.transform.rotation;

            // Desactivar inmediatamente la carroza anterior.
            carrozaAnterior.SetActive(false);
        }

        // Guardar el nuevo prefab y su ID.
        carrozaEquipada = robot.RobotPrefab;
        robotEquipadoID = nuevoRobotID;

        if (existeCarrozaAnterior)
        {
            // Instanciar la nueva carroza en la posición anterior.
            CarrozaActual = Instantiate(
                carrozaEquipada,
                posicion,
                rotacion
            );

            // Destruir la carroza antigua.
            Destroy(carrozaAnterior);
        }
        else
        {
            // Si no existe una carroza, usar PlayerEmpty.
            CarrozaActual = null;

            InstanciarCarroza(
                SceneManager.GetActiveScene()
            );
        }

        // Avisar a los demás sistemas del cambio.
        OnRobotEquipadoChanged?.Invoke();

        Debug.Log(
            $"Robot equipado: {robot.RobotName} (ID {nuevoRobotID})",
            this
        );

        return true;
    }

}
