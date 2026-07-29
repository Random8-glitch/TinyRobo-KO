using System;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    private const string RankKey = "PlayerRank";
    private const string MoneyKey = "PlayerMoney";

    [Header("Datos del jugador")]
    [SerializeField] private int rank = 1;
    [SerializeField] private int money = 0;

    public int Rank => rank;
    public int Money => money;

    // Permite que la interfaz se actualice cuando cambien los datos.
    public event Action OnPlayerDataChanged;

    private void Awake()
    {
        // Evita que se creen varias copias al cambiar de escena.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Este objeto no será destruido al cargar otra escena.
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    public void SetRank(int newRank)
    {
        rank = Mathf.Max(1, newRank);
        SaveData();
        OnPlayerDataChanged?.Invoke();
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
            return;

        money += amount;
        SaveData();
        OnPlayerDataChanged?.Invoke();
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0 || money < amount)
            return false;

        money -= amount;
        SaveData();
        OnPlayerDataChanged?.Invoke();

        return true;
    }

    public void SaveData()
    {
        PlayerPrefs.SetInt(RankKey, rank);
        PlayerPrefs.SetInt(MoneyKey, money);
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        rank = PlayerPrefs.GetInt(RankKey, 1);
        money = PlayerPrefs.GetInt(MoneyKey, 0);

        OnPlayerDataChanged?.Invoke();
    }

    public void ResetData()
    {
        rank = 1;
        money = 0;

        PlayerPrefs.DeleteKey(RankKey);
        PlayerPrefs.DeleteKey(MoneyKey);
        PlayerPrefs.Save();

        OnPlayerDataChanged?.Invoke();
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
            SaveData();
    }

    private void OnApplicationQuit()
    {
        SaveData();
    }
}