using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    private const string RankKey = "PlayerRank";
    private const string MoneyKey = "PlayerMoney";

    [Header("Datos del jugador")]
    [SerializeField] private int rank = 1;
    [SerializeField] private int money = 0;

    [Header("UI")]
    [Tooltip("Nombre del objeto TextMeshPro que mostrará el dinero.")]
    [SerializeField] private string nombreTextoDinero = "TextoDinero";

    private TextMeshProUGUI textoDinero;

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
        // Se ejecuta cada vez que termina de cargar una escena.
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
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BuscarTextoDinero();
        ActualizarUIDinero();
    }



    private void BuscarTextoDinero()
    {
        GameObject objetoTexto = GameObject.Find(nombreTextoDinero);

        if (objetoTexto != null)
        {
            textoDinero = objetoTexto.GetComponent<TextMeshProUGUI>();

            if (textoDinero == null)
            {
                Debug.LogWarning(
                    $"El objeto '{nombreTextoDinero}' existe, pero no tiene TextMeshProUGUI."
                );
            }
        }
        else
        {
            textoDinero = null;

            Debug.LogWarning(
                $"No se encontró el objeto UI '{nombreTextoDinero}' en la escena '{SceneManager.GetActiveScene().name}'."
            );
        }
    }

    private void ActualizarUIDinero()
    {
        if (textoDinero != null)
        {
            textoDinero.text = money.ToString();
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