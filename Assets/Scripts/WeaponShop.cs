using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WeaponShop : MonoBehaviour
{
    [Serializable]
    public class WeaponShopSlot
    {
        [Tooltip(
            "GameObject raíz del espacio. " +
            "Debe contener la Image y un Button como hijo."
        )]
        [SerializeField] private GameObject weaponUIObject;

        [Tooltip("Precio de compra del arma.")]
        [Min(0)]
        [SerializeField] private int price;

        [Header("Información asignada automáticamente")]
        [SerializeField] private int weaponID = -1;

        [NonSerialized] private Image weaponImage;
        [NonSerialized] private Button purchaseButton;
        [NonSerialized] private UnityAction purchaseAction;

        public GameObject WeaponUIObject => weaponUIObject;
        public int Price => price;
        public int WeaponID => weaponID;

        public Image WeaponImage => weaponImage;
        public Button PurchaseButton => purchaseButton;

        public UnityAction PurchaseAction
        {
            get => purchaseAction;
            set => purchaseAction = value;
        }

        public void SetWeaponID(int newWeaponID)
        {
            weaponID = newWeaponID;
        }

        public void FindUIComponents()
        {
            weaponImage = null;
            purchaseButton = null;

            if (weaponUIObject == null)
                return;

            // Obtiene la Image del objeto raíz.
            weaponImage =
                weaponUIObject.GetComponent<Image>();

            // Obtiene el primer Button encontrado en sus hijos.
            purchaseButton =
                weaponUIObject.GetComponentInChildren<Button>(true);
        }
    }


    // ==========================================
    // ESPACIOS DE TIENDA PARA ROBOTS
    // ==========================================

    [Serializable]
    public class RobotShopSlot
    {
        [Tooltip(
            "GameObject raíz del espacio del robot. " +
            "Debe contener la Image y un Button como hijo."
        )]
        [SerializeField] private GameObject robotUIObject;

        [Tooltip("Precio de compra del robot.")]
        [Min(0)]
        [SerializeField] private int price;

        [Header("Información asignada automáticamente")]
        [SerializeField] private int robotID = -1;

        [NonSerialized] private Image robotImage;
        [NonSerialized] private Button purchaseButton;
        [NonSerialized] private UnityAction purchaseAction;

        public GameObject RobotUIObject => robotUIObject;
        public int Price => price;
        public int RobotID => robotID;
        public Image RobotImage => robotImage;
        public Button PurchaseButton => purchaseButton;

        public UnityAction PurchaseAction
        {
            get => purchaseAction;
            set => purchaseAction = value;
        }

        public void SetRobotID(int newRobotID)
        {
            robotID = newRobotID;
        }

        public void FindUIComponents()
        {
            robotImage = null;
            purchaseButton = null;

            if (robotUIObject == null)
                return;

            robotImage = robotUIObject.GetComponent<Image>();
            purchaseButton =
                robotUIObject.GetComponentInChildren<Button>(true);
        }
    }

    [Header("Referencias opcionales")]
    [Tooltip(
        "Puede dejarse vacío. Se buscará mediante " +
        "PlayerDataManager.Instance."
    )]
    [SerializeField] private PlayerDataManager playerDataManager;

    [Tooltip(
        "Puede dejarse vacío. Se buscará en el mismo GameObject " +
        "que contiene PlayerDataManager."
    )]
    [SerializeField] private WeaponManager weaponManager;

    [Header("Referencias opcionales - Robots")]
    [Tooltip(
        "Puede dejarse vacío. Se buscará en el mismo GameObject " +
        "de PlayerDataManager o mediante RoboManager.Instance."
    )]
    [SerializeField] private RoboManager roboManager;

    [Header("Objetos de la tienda - Armas")]
    [Tooltip(
        "El orden de esta lista debe coincidir con el orden " +
        "de las armas en WeaponManager."
    )]
    [SerializeField]
    private List<WeaponShopSlot> weaponSlots =
        new List<WeaponShopSlot>();

    [Header("Objetos de la tienda - Robots")]
    [Tooltip(
        "El orden de esta lista debe coincidir con el orden " +
        "de los robots en RoboManager."
    )]
    [SerializeField]
    private List<RobotShopSlot> robotSlots =
        new List<RobotShopSlot>();

    [Header("Apariencia - Armas")]
    [Tooltip(
        "Transparencia de las armas bloqueadas por rango. " +
        "0.5 representa 50%."
    )]
    [Range(0f, 1f)]
    [SerializeField] private float lockedWeaponAlpha = 0.5f;

    [Header("Apariencia - Robots")]
    [Tooltip("Transparencia de los robots que no están activos.")]
    [Range(0f, 1f)]
    [SerializeField] private float lockedRobotAlpha = 0.5f;

    [Header("Comportamiento - Armas")]
    [Tooltip(
        "Desactiva el botón cuando el jugador no tiene " +
        "dinero suficiente."
    )]
    [SerializeField] private bool disableButtonWithoutMoney = true;

    [Header("Comportamiento - Robots")]
    [Tooltip(
        "Desactiva el botón del robot cuando no hay " +
        "dinero suficiente."
    )]
    [SerializeField] private bool disableRobotButtonWithoutMoney = true;

    private bool initialized;
    private bool subscribedToEvents;
    private bool subscribedToWeaponEvents;
    private bool subscribedToRobotEvents;

    private void Awake()
    {
        PrepareWeaponSlots();
        PrepareRobotSlots();
        ConfigureButtons();
        ConfigureRobotButtons();
        DisableAllButtons();
    }

    private void Start()
    {
        InitializeShop();
    }

    private void OnEnable()
    {
        if (!initialized)
            return;

        SubscribeToEvents();
        RefreshShop();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        RemoveButtonListeners();
        RemoveRobotButtonListeners();
    }

    private void InitializeShop()
    {
        if (!ResolveReferences())
        {
            DisableAllButtons();
            return;
        }

        // Cada catálogo funciona de manera independiente.
        // Si falta uno de los managers, el otro puede seguir funcionando.
        if (weaponSlots.Count > 0 && weaponManager == null)
        {
            Debug.LogWarning(
                "WeaponShop no encontró WeaponManager: " +
                "se desactivarán los botones de armas.",
                this
            );
        }

        if (robotSlots.Count > 0 && roboManager == null)
        {
            Debug.LogWarning(
                "WeaponShop no encontró RoboManager: " +
                "se desactivarán los botones de robots.",
                this
            );
        }

        initialized = true;

        SubscribeToEvents();
        RefreshShop();
    }

    private bool ResolveReferences()
    {
        // PlayerDataManager se utiliza para consultar y descontar dinero.
        if (playerDataManager == null && weaponManager != null)
        {
            playerDataManager =
                weaponManager.GetComponent<PlayerDataManager>();
        }

        if (playerDataManager == null && roboManager != null)
        {
            playerDataManager =
                roboManager.GetComponent<PlayerDataManager>();
        }

        if (playerDataManager == null)
        {
            playerDataManager = PlayerDataManager.Instance;
        }

        // Resolver WeaponManager y RoboManager de manera independiente.
        if (weaponManager == null && playerDataManager != null)
        {
            weaponManager =
                playerDataManager.GetComponent<WeaponManager>();
        }

        if (weaponManager == null)
        {
            weaponManager = WeaponManager.Instance;
        }

        if (roboManager == null && playerDataManager != null)
        {
            roboManager =
                playerDataManager.GetComponent<RoboManager>();
        }

        if (roboManager == null)
        {
            roboManager = RoboManager.Instance;
        }

        if (playerDataManager == null)
        {
            Debug.LogError(
                "WeaponShop no pudo encontrar PlayerDataManager.",
                this
            );

            return false;
        }

        return true;
    }

    private void PrepareWeaponSlots()
    {
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            WeaponShopSlot slot = weaponSlots[i];

            if (slot == null)
            {
                Debug.LogWarning(
                    $"El espacio de tienda {i} está vacío.",
                    this
                );

                continue;
            }

            /*
             * La posición dentro de la lista funciona
             * como el ID del arma.
             *
             * Posición 0 = arma 0
             * Posición 1 = arma 1
             * Posición 2 = arma 2
             */
            slot.SetWeaponID(i);
            slot.FindUIComponents();

            if (slot.WeaponUIObject == null)
            {
                Debug.LogWarning(
                    $"El espacio de tienda {i} no tiene " +
                    "un GameObject asignado.",
                    this
                );

                continue;
            }

            if (slot.WeaponImage == null)
            {
                Debug.LogWarning(
                    $"El objeto {slot.WeaponUIObject.name} no tiene " +
                    "un componente Image en su objeto raíz.",
                    slot.WeaponUIObject
                );
            }

            if (slot.PurchaseButton == null)
            {
                Debug.LogWarning(
                    $"El objeto {slot.WeaponUIObject.name} no tiene " +
                    "un Button entre sus hijos.",
                    slot.WeaponUIObject
                );
            }
        }
    }

    // ==========================================
    // PREPARAR ESPACIOS PARA ROBOTS
    // ==========================================

    private void PrepareRobotSlots()
    {
        for (int i = 0; i < robotSlots.Count; i++)
        {
            RobotShopSlot slot = robotSlots[i];

            if (slot == null)
            {
                Debug.LogWarning(
                    $"El espacio de robot {i} está vacío.",
                    this
                );
                continue;
            }

            // Posición 0 = robot ID 0; posición 1 = robot ID 1; etc.
            slot.SetRobotID(i);
            slot.FindUIComponents();

            if (slot.RobotUIObject == null)
            {
                Debug.LogWarning(
                    $"El espacio del robot {i} no tiene GameObject.",
                    this
                );
                continue;
            }

            if (slot.RobotImage == null)
            {
                Debug.LogWarning(
                    $"El objeto {slot.RobotUIObject.name} no tiene " +
                    "un componente Image en su objeto raíz.",
                    slot.RobotUIObject
                );
            }

            if (slot.PurchaseButton == null)
            {
                Debug.LogWarning(
                    $"El objeto {slot.RobotUIObject.name} no tiene " +
                    "un Button entre sus hijos.",
                    slot.RobotUIObject
                );
            }
        }
    }

    private void ConfigureButtons()
    {
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            WeaponShopSlot slot = weaponSlots[i];

            if (slot == null ||
                slot.PurchaseButton == null)
            {
                continue;
            }

            /*
             * Elimina únicamente el evento agregado por este script,
             * sin borrar los eventos configurados manualmente
             * desde el Inspector.
             */
            if (slot.PurchaseAction != null)
            {
                slot.PurchaseButton.onClick.RemoveListener(
                    slot.PurchaseAction
                );
            }

            int capturedWeaponID = slot.WeaponID;

            slot.PurchaseAction =
                () => TryPurchaseWeapon(capturedWeaponID);

            slot.PurchaseButton.onClick.AddListener(
                slot.PurchaseAction
            );
        }
    }

    private void ConfigureRobotButtons()
    {
        for (int i = 0; i < robotSlots.Count; i++)
        {
            RobotShopSlot slot = robotSlots[i];

            if (slot == null || slot.PurchaseButton == null)
                continue;

            // No eliminar eventos asignados manualmente en el Inspector.
            if (slot.PurchaseAction != null)
            {
                slot.PurchaseButton.onClick.RemoveListener(
                    slot.PurchaseAction
                );
            }

            int capturedRobotID = slot.RobotID;
            slot.PurchaseAction =
                () => TryPurchaseRobot(capturedRobotID);

            slot.PurchaseButton.onClick.AddListener(
                slot.PurchaseAction
            );
        }
    }

    private void RemoveButtonListeners()
    {
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            WeaponShopSlot slot = weaponSlots[i];

            if (slot == null ||
                slot.PurchaseButton == null ||
                slot.PurchaseAction == null)
            {
                continue;
            }

            slot.PurchaseButton.onClick.RemoveListener(
                slot.PurchaseAction
            );

            slot.PurchaseAction = null;
        }
    }

    private void RemoveRobotButtonListeners()
    {
        for (int i = 0; i < robotSlots.Count; i++)
        {
            RobotShopSlot slot = robotSlots[i];

            if (slot == null ||
                slot.PurchaseButton == null ||
                slot.PurchaseAction == null)
            {
                continue;
            }

            slot.PurchaseButton.onClick.RemoveListener(
                slot.PurchaseAction
            );

            slot.PurchaseAction = null;
        }
    }

    private void SubscribeToEvents()
    {
        if (!subscribedToEvents && playerDataManager != null)
        {
            playerDataManager.OnPlayerDataChanged += RefreshShop;
            subscribedToEvents = true;
        }

        if (!subscribedToWeaponEvents && weaponManager != null)
        {
            weaponManager.OnWeaponsChanged += RefreshShop;
            subscribedToWeaponEvents = true;
        }

        if (!subscribedToRobotEvents && roboManager != null)
        {
            roboManager.OnRobotsChanged += RefreshShop;
            subscribedToRobotEvents = true;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (subscribedToEvents && playerDataManager != null)
        {
            playerDataManager.OnPlayerDataChanged -= RefreshShop;
            subscribedToEvents = false;
        }

        if (subscribedToWeaponEvents && weaponManager != null)
        {
            weaponManager.OnWeaponsChanged -= RefreshShop;
            subscribedToWeaponEvents = false;
        }

        if (subscribedToRobotEvents && roboManager != null)
        {
            roboManager.OnRobotsChanged -= RefreshShop;
            subscribedToRobotEvents = false;
        }
    }

    public void RefreshShop()
    {
        if (!ResolveReferences())
        {
            DisableAllButtons();
            return;
        }

        for (int i = 0; i < weaponSlots.Count; i++)
        {
            RefreshWeaponSlot(weaponSlots[i]);
        }

        for (int i = 0; i < robotSlots.Count; i++)
        {
            RefreshRobotSlot(robotSlots[i]);
        }
    }

    private void RefreshWeaponSlot(WeaponShopSlot slot)
    {
        if (slot == null)
            return;

        if (weaponManager == null)
        {
            ClearInvalidSlot(slot);
            return;
        }

        WeaponManager.WeaponRuntimeData weapon =
            weaponManager.GetWeapon(slot.WeaponID);

        if (weapon == null)
        {
            ClearInvalidSlot(slot);
            return;
        }

        // Coloca el icono correspondiente al arma.
        if (slot.WeaponImage != null)
        {
            slot.WeaponImage.sprite =
                weapon.WeaponIcon;

            Color imageColor =
                slot.WeaponImage.color;

            /*
             * Si no está activa por rango,
             * se muestra con 50% de opacidad.
             */
            imageColor.a = weapon.Activo
                ? 1f
                : lockedWeaponAlpha;

            slot.WeaponImage.color =
                imageColor;
        }

        if (slot.PurchaseButton == null)
            return;

        bool hasEnoughMoney =
            playerDataManager.Money >= slot.Price;

        bool canPurchase =
            weapon.Activo &&
            !weapon.Comprado;

        if (disableButtonWithoutMoney)
        {
            canPurchase &=
                hasEnoughMoney;
        }

        /*
         * El botón queda desactivado cuando:
         *
         * - El arma ya fue comprada.
         * - El rango todavía no la activa.
         * - No hay suficiente dinero, si la opción está habilitada.
         */
        slot.PurchaseButton.interactable =
            canPurchase;
    }

    // ==========================================
    // ACTUALIZAR ROBOTS DE LA TIENDA
    // ==========================================

    private void RefreshRobotSlot(RobotShopSlot slot)
    {
        if (slot == null)
            return;

        if (roboManager == null)
        {
            ClearInvalidRobotSlot(slot);
            return;
        }

        RoboManager.RobotRuntimeData robot =
            roboManager.GetRobot(slot.RobotID);

        if (robot == null)
        {
            ClearInvalidRobotSlot(slot);
            return;
        }

        if (slot.RobotImage != null)
        {
            slot.RobotImage.sprite = robot.RobotIcon;

            Color imageColor = slot.RobotImage.color;
            imageColor.a = robot.Activo ? 1f : lockedRobotAlpha;
            slot.RobotImage.color = imageColor;
        }

        if (slot.PurchaseButton == null)
            return;

        bool hasEnoughMoney =
            playerDataManager.Money >= slot.Price;

        bool canPurchase = robot.Activo && !robot.Comprado;

        if (disableRobotButtonWithoutMoney)
        {
            canPurchase &= hasEnoughMoney;
        }

        slot.PurchaseButton.interactable = canPurchase;
    }

    public void TryPurchaseWeapon(int weaponID)
    {
        if (!ResolveReferences())
            return;

        if (weaponManager == null)
        {
            Debug.LogWarning("No se encontró WeaponManager.", this);
            return;
        }

        WeaponShopSlot slot =
            GetSlotByWeaponID(weaponID);

        if (slot == null)
        {
            Debug.LogWarning(
                $"No existe un espacio de tienda para el arma " +
                $"con ID {weaponID}.",
                this
            );

            return;
        }

        WeaponManager.WeaponRuntimeData weapon =
            weaponManager.GetWeapon(weaponID);

        if (weapon == null)
        {
            Debug.LogWarning(
                $"No existe un arma con ID {weaponID}.",
                this
            );

            return;
        }

        if (weapon.Comprado)
        {
            Debug.Log(
                $"El arma {weapon.WeaponName} ya fue comprada.",
                this
            );

            RefreshWeaponSlot(slot);
            return;
        }

        if (!weapon.Activo)
        {
            Debug.LogWarning(
                $"El arma {weapon.WeaponName} requiere rango " +
                $"{weapon.Rank}.",
                this
            );

            RefreshWeaponSlot(slot);
            return;
        }

        if (playerDataManager.Money < slot.Price)
        {
            Debug.LogWarning(
                $"No hay suficiente dinero para comprar " +
                $"{weapon.WeaponName}. Precio: {slot.Price}.",
                this
            );

            RefreshWeaponSlot(slot);
            return;
        }

        /*
         * SpendMoney no acepta cantidades iguales a cero,
         * así que las armas gratuitas omiten este paso.
         */
        bool moneyWasSpent = slot.Price == 0;

        if (slot.Price > 0)
        {
            moneyWasSpent =
                playerDataManager.SpendMoney(slot.Price);
        }

        if (!moneyWasSpent)
        {
            Debug.LogWarning(
                $"No se pudo pagar el arma {weapon.WeaponName}.",
                this
            );

            RefreshWeaponSlot(slot);
            return;
        }

        bool purchased =
            weaponManager.ComprarArma(weaponID);

        if (!purchased)
        {
            /*
             * Devuelve el dinero si por alguna razón
             * WeaponManager rechaza la compra.
             */
            if (slot.Price > 0)
            {
                playerDataManager.AddMoney(slot.Price);
            }

            Debug.LogError(
                $"La compra de {weapon.WeaponName} fue rechazada. " +
                "El dinero fue devuelto.",
                this
            );

            RefreshWeaponSlot(slot);
            return;
        }

        Debug.Log(
            $"Se compró {weapon.WeaponName} por {slot.Price}.",
            this
        );

        /*
         * Desactiva inmediatamente el botón porque
         * Comprado ahora es verdadero.
         */
        RefreshShop();
    }

    // ==========================================
    // COMPRA DE ROBOTS
    // ==========================================

    public void TryPurchaseRobot(int robotID)
    {
        if (!ResolveReferences())
            return;

        if (roboManager == null)
        {
            Debug.LogWarning("No se encontró RoboManager.", this);
            return;
        }

        RobotShopSlot slot = GetSlotByRobotID(robotID);

        if (slot == null)
        {
            Debug.LogWarning(
                $"No existe un espacio de tienda para el robot ID {robotID}.",
                this
            );
            return;
        }

        RoboManager.RobotRuntimeData robot =
            roboManager.GetRobot(robotID);

        if (robot == null)
        {
            Debug.LogWarning(
                $"No existe un robot con ID {robotID}.",
                this
            );
            return;
        }

        if (robot.Comprado)
        {
            Debug.Log($"El robot {robot.RobotName} ya fue comprado.", this);
            RefreshRobotSlot(slot);
            return;
        }

        if (!robot.Activo)
        {
            Debug.LogWarning(
                $"El robot {robot.RobotName} todavía no está activo.",
                this
            );
            RefreshRobotSlot(slot);
            return;
        }

        if (playerDataManager.Money < slot.Price)
        {
            Debug.LogWarning(
                $"No hay suficiente dinero para comprar " +
                $"{robot.RobotName}. Precio: {slot.Price}.",
                this
            );
            RefreshRobotSlot(slot);
            return;
        }

        // Los robots gratuitos no necesitan SpendMoney(0).
        if (slot.Price > 0 &&
            !playerDataManager.SpendMoney(slot.Price))
        {
            Debug.LogWarning(
                $"No se pudo pagar el robot {robot.RobotName}.",
                this
            );
            RefreshRobotSlot(slot);
            return;
        }

        // El RoboManager actual permite modificar Comprado directamente.
        // No requiere añadir un método ComprarRobot a RoboManager.
        robot.Comprado = true;

        Debug.Log(
            $"Se compró {robot.RobotName} por {slot.Price}.",
            this
        );

        RefreshShop();
    }

    private RobotShopSlot GetSlotByRobotID(int robotID)
    {
        for (int i = 0; i < robotSlots.Count; i++)
        {
            RobotShopSlot slot = robotSlots[i];
            if (slot != null && slot.RobotID == robotID)
                return slot;
        }

        return null;
    }

    private WeaponShopSlot GetSlotByWeaponID(int weaponID)
    {
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            WeaponShopSlot slot = weaponSlots[i];

            if (slot != null &&
                slot.WeaponID == weaponID)
            {
                return slot;
            }
        }

        return null;
    }

    private void ClearInvalidSlot(WeaponShopSlot slot)
    {
        if (slot.WeaponImage != null)
        {
            slot.WeaponImage.sprite = null;

            Color imageColor =
                slot.WeaponImage.color;

            imageColor.a = lockedWeaponAlpha;

            slot.WeaponImage.color =
                imageColor;
        }

        if (slot.PurchaseButton != null)
        {
            slot.PurchaseButton.interactable = false;
        }
    }

    private void ClearInvalidRobotSlot(RobotShopSlot slot)
    {
        if (slot.RobotImage != null)
        {
            slot.RobotImage.sprite = null;

            Color imageColor = slot.RobotImage.color;
            imageColor.a = lockedRobotAlpha;
            slot.RobotImage.color = imageColor;
        }

        if (slot.PurchaseButton != null)
        {
            slot.PurchaseButton.interactable = false;
        }
    }

    private void DisableAllButtons()
    {
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            WeaponShopSlot slot = weaponSlots[i];

            if (slot?.PurchaseButton != null)
            {
                slot.PurchaseButton.interactable = false;
            }
        }

        for (int i = 0; i < robotSlots.Count; i++)
        {
            RobotShopSlot slot = robotSlots[i];

            if (slot?.PurchaseButton != null)
            {
                slot.PurchaseButton.interactable = false;
            }
        }
    }
}