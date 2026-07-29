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

    [Header("Objetos de la tienda")]
    [Tooltip(
        "El orden de esta lista debe coincidir con el orden " +
        "de las armas en WeaponManager."
    )]
    [SerializeField]
    private List<WeaponShopSlot> weaponSlots =
        new List<WeaponShopSlot>();

    [Header("Apariencia")]
    [Tooltip(
        "Transparencia de las armas bloqueadas por rango. " +
        "0.5 representa 50%."
    )]
    [Range(0f, 1f)]
    [SerializeField] private float lockedWeaponAlpha = 0.5f;

    [Header("Comportamiento")]
    [Tooltip(
        "Desactiva el botón cuando el jugador no tiene " +
        "dinero suficiente."
    )]
    [SerializeField] private bool disableButtonWithoutMoney = true;

    private bool initialized;
    private bool subscribedToEvents;

    private void Awake()
    {
        PrepareWeaponSlots();
        ConfigureButtons();
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
    }

    private void InitializeShop()
    {
        if (!ResolveReferences())
        {
            DisableAllButtons();
            return;
        }

        initialized = true;

        SubscribeToEvents();
        RefreshShop();
    }

    private bool ResolveReferences()
    {
        /*
         * Primero intenta utilizar referencias asignadas
         * manualmente desde el Inspector.
         */

        if (playerDataManager == null &&
            weaponManager != null)
        {
            playerDataManager =
                weaponManager.GetComponent<PlayerDataManager>();
        }

        /*
         * Si no fue asignado manualmente, utiliza la instancia
         * persistente de PlayerDataManager.
         */

        if (playerDataManager == null)
        {
            playerDataManager =
                PlayerDataManager.Instance;
        }

        /*
         * WeaponManager está en el mismo GameObject
         * que PlayerDataManager.
         */

        if (weaponManager == null &&
            playerDataManager != null)
        {
            weaponManager =
                playerDataManager.GetComponent<WeaponManager>();
        }

        if (playerDataManager == null)
        {
            Debug.LogError(
                "WeaponShop no pudo encontrar PlayerDataManager.",
                this
            );

            return false;
        }

        if (weaponManager == null)
        {
            Debug.LogError(
                "WeaponShop no pudo encontrar WeaponManager en el " +
                "GameObject de PlayerDataManager.",
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

    private void SubscribeToEvents()
    {
        if (subscribedToEvents ||
            playerDataManager == null)
        {
            return;
        }

        playerDataManager.OnPlayerDataChanged +=
            RefreshShop;

        subscribedToEvents = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (!subscribedToEvents ||
            playerDataManager == null)
        {
            return;
        }

        playerDataManager.OnPlayerDataChanged -=
            RefreshShop;

        subscribedToEvents = false;
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
    }

    private void RefreshWeaponSlot(WeaponShopSlot slot)
    {
        if (slot == null)
            return;

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

    public void TryPurchaseWeapon(int weaponID)
    {
        if (!ResolveReferences())
            return;

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
    }
}