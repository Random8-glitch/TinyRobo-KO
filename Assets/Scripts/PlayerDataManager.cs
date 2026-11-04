

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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

        GameObject[] objetos = scene.GetRootGameObjects();

        foreach (GameObject objeto in objetos)
        {
            Transform playerEmpty = BuscarPlayerEmpty(objeto.transform);

            if (playerEmpty != null)
            {
                Instantiate(
                    carrozaEquipada,
                    playerEmpty.position,
                    playerEmpty.rotation
                );

                return;
            }
        }
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
}
