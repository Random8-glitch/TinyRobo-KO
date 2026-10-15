using UnityEngine;
using UnityEngine.UI;

public class SawPlayer : MonoBehaviour
{
    [SerializeField] private float danioPorTick = 1f;
    [SerializeField] private float boostDamage = 1.5f;
    [SerializeField] private float boostDuration = 5f;       // duración del aumento de daño
    [SerializeField] private float cooldownDuration = 10f;   // enfriamiento antes de poder volver a usar

    [Header("Visual")]
    [SerializeField] private Renderer sawRenderer;
    [SerializeField] private Color boostColor = Color.red;

    [Header("UI")]
    [SerializeField] private Image uiImage;
    [SerializeField, Range(0f, 1f)] private float boostAlpha = 0.5f;

    private int enemyLayer;

    private float originalDanio;
    private float boostTimer = 0f;
    private float cooldownTimer = 0f;
    private bool isBoostActive = false;
    private bool isOnCooldown = false;

    private Color originalColor;
    private Color originalImageColor;

    // Referencia al EquiparChips del Player padre
    private EquiparChips equiparChips;

    private void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");
        originalDanio = danioPorTick;

        if (sawRenderer == null)
            sawRenderer = GetComponentInChildren<Renderer>();

        if (sawRenderer != null)
            originalColor = sawRenderer.material.color;
        else
            Debug.LogWarning("SawPlayer: no se ha asignado Renderer para la sierra. Visual boost no estará disponible.");

        if (uiImage == null)
            uiImage = FindObjectOfType<Image>();

        if (uiImage != null)
            originalImageColor = uiImage.color;
        else
            Debug.LogWarning("SawPlayer: no se ha asignado Image de UI. Indicador de transparencia no estará disponible.");

        // Busca EquiparChips en los objetos padre
        equiparChips = GetComponentInParent<EquiparChips>();
    }

    private void Update()
    {
        // Reducir timers de cooldown
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            if (isOnCooldown && cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                Debug.Log("Boost disponible de nuevo.");
                RestoreUIVisual();
            }
        }

        // Activar boost al pulsar E (una sola pulsación)
        if (!isOnCooldown && !isBoostActive)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                ActivateBoost();
            }
        }

        // Si el boost está activo, reducir su timer
        if (isBoostActive)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f)
            {
                // Fin del boost: restaurar daño y visual de la sierra, arrancar cooldown
                isBoostActive = false;
                danioPorTick = originalDanio;
                RestoreSawVisual();
                cooldownTimer = cooldownDuration;
                isOnCooldown = true;
                Debug.Log($"Boost finalizado. Enfriamiento de {cooldownDuration} segundos iniciado.");
                // Nota: la UI vuelve al 100% de opacidad cuando el cooldown termine.
            }
        }
    }

    private void ActivateBoost()
    {
        isBoostActive = true;
        boostTimer = boostDuration;
        danioPorTick = boostDamage;
        ApplySawBoostVisual();
        ApplyUIBoostVisual();
        // Nota: cooldown se inicia al terminar el boost para cumplir "durará 5 segundos y tendrá un enfriamiento de 10"
        Debug.Log($"Boost activado: daño aumentado a {danioPorTick} durante {boostDuration} segundos.");
    }

    private void ApplySawBoostVisual()
    {
        if (sawRenderer == null)
            return;

        // Usar instancia de material para no modificar sharedMaterial
        sawRenderer.material.color = boostColor;
    }

    private void RestoreSawVisual()
    {
        if (sawRenderer == null)
            return;

        sawRenderer.material.color = originalColor;
    }

    private void ApplyUIBoostVisual()
    {
        if (uiImage == null)
            return;

        Color c = uiImage.color;
        c.a = boostAlpha;
        uiImage.color = c;
    }

    private void RestoreUIVisual()
    {
        if (uiImage == null)
            return;

        uiImage.color = originalImageColor;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer != enemyLayer)
            return;

        EnemyStats stats = other.GetComponent<EnemyStats>();

        if (stats != null)
        {
            float multiplicadorChip = ObtenerMultiplicadorChip();

            danioPorTick *= multiplicadorChip;

            stats.RecibirDanio(danioPorTick);
        }
    }

    private float ObtenerMultiplicadorChip()
    {
        // Si no existe EquiparChips, no modifica el daño
        if (equiparChips == null)
            return 1f;

        ModificadorStats modificador =
            equiparChips.ModificadorEquipado;

        // Si no hay modificador equipado
        if (modificador == null)
            return 1f;

        if (modificador.efectos == null)
            return 1f;

        float multiplicador = 1f;

        foreach (EfectoModificador efecto in modificador.efectos)
        {
            if (efecto == null)
                continue;

            // Solamente Weapon -> DañoPlus
            if (efecto.scriptObjetivo == TipoScript.Weapon &&
                efecto.atributo == AtributoModificable.DañoPlus)
            {
                multiplicador *= efecto.cantidad;
            }
        }

        return multiplicador;
    }
}