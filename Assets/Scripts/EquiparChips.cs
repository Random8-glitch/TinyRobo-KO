
using UnityEngine;
using System.Collections.Generic;

public class EquiparChips : MonoBehaviour
{
    [Header("Modificador equipado")]
    [SerializeField]
    private ModificadorStats modificadorEquipado;

    private PlayerStats playerStats;
    private EnemyStats enemyStats;

    private RoboMovPlayer roboMovPlayer;
    private RoboMovEnemy roboMovEnemy;

    private bool esPlayer;
    private bool esEnemy;

    private WeaponManager weaponManager;

    // Referencia a la copia creada por este componente.
    private ModificadorStats copiaPropia;

    private struct EfectoAplicado
    {
        public AtributoModificable atributo;
        public float cantidad;

        public EfectoAplicado(
            AtributoModificable atributo,
            float cantidad)
        {
            this.atributo = atributo;
            this.cantidad = cantidad;
        }
    }

    private readonly List<EfectoAplicado> efectosAplicados =
        new List<EfectoAplicado>();

    public ModificadorStats ModificadorEquipado =>
        modificadorEquipado;

    private void Awake()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        esPlayer = playerLayer != -1 &&
                   gameObject.layer == playerLayer;

        esEnemy = enemyLayer != -1 &&
                  gameObject.layer == enemyLayer;

        if (esPlayer)
        {
            playerStats = GetComponent<PlayerStats>();
            roboMovPlayer = GetComponent<RoboMovPlayer>();
        }
        else if (esEnemy)
        {
            enemyStats = GetComponent<EnemyStats>();
            roboMovEnemy = GetComponent<RoboMovEnemy>();
        }
        else
        {
            Debug.LogWarning(
                "EquiparChips: Layer no válido.",
                this
            );
        }
    }

    private void Start()
    {
        ModificadorStats inicial = modificadorEquipado;
        modificadorEquipado = null;

        if (esPlayer)
        {
            weaponManager = WeaponManager.Instance;

            if (weaponManager != null)
            {
                // Si hay un estado guardado,
                // tiene prioridad sobre el Inspector.
                if (weaponManager.ChipInicializado)
                {
                    ModificadorStats guardado =
                        weaponManager.GetChipEquipado();

                    if (guardado != null)
                    {
                        EquiparInterno(guardado, false);
                    }

                    return;
                }

                // Primera inicialización del jugador.
                if (inicial != null)
                {
                    Equipar(inicial);
                }
                else
                {
                    SincronizarChip();
                }

                return;
            }

            Debug.LogWarning(
                "EquiparChips: No se encontró WeaponManager.",
                this
            );
        }

        // Funcionamiento original para enemigos
        // o jugadores sin WeaponManager.
        if (inicial != null)
        {
            Equipar(inicial);
        }
    }

    // ==========================================
    // EQUIPAR CHIP
    // ==========================================

    public void Equipar(ModificadorStats nuevoModificador)
    {
        EquiparInterno(nuevoModificador, true);
    }

    private void EquiparInterno(
        ModificadorStats nuevoModificador,
        bool sincronizar)
    {
        if (!esPlayer && !esEnemy)
            return;

        if (nuevoModificador == null)
        {
            DesequiparInterno(sincronizar);
            return;
        }

        ModificadorStats originalNuevo =
            nuevoModificador.Origen != null
                ? nuevoModificador.Origen
                : nuevoModificador;

        if (modificadorEquipado != null)
        {
            ModificadorStats originalActual =
                modificadorEquipado.Origen != null
                    ? modificadorEquipado.Origen
                    : modificadorEquipado;

            if (originalActual == originalNuevo)
                return;
        }

        // Retirar efectos anteriores.
        DesequiparInterno(false);

        // Crear una copia independiente.
        ModificadorStats copia =
            Instantiate(originalNuevo);

        copia.Origen = originalNuevo;

        copiaPropia = copia;
        modificadorEquipado = copia;

        // Aplicar efectos del nuevo chip.
        foreach (EfectoModificador efecto in copia.efectos)
        {
            if (AplicarEfecto(
                efecto.atributo,
                efecto.cantidad))
            {
                efectosAplicados.Add(
                    new EfectoAplicado(
                        efecto.atributo,
                        efecto.cantidad
                    )
                );
            }
        }

        ActualizarVidaUI();

        if (sincronizar)
        {
            SincronizarChip();
        }
    }

    // ==========================================
    // DESEQUIPAR CHIP
    // ==========================================

    public void Desequipar()
    {
        DesequiparInterno(true);
    }

    private void DesequiparInterno(bool sincronizar)
    {
        if (modificadorEquipado == null &&
            efectosAplicados.Count == 0)
        {
            if (sincronizar)
            {
                SincronizarChip();
            }

            return;
        }

        // Retirar exactamente los efectos aplicados.
        foreach (EfectoAplicado efecto in efectosAplicados)
        {
            AplicarEfecto(
                efecto.atributo,
                -efecto.cantidad
            );
        }

        efectosAplicados.Clear();

        modificadorEquipado = null;

        // Destruir únicamente la copia propia.
        if (copiaPropia != null)
        {
            Destroy(copiaPropia);
            copiaPropia = null;
        }

        ActualizarVidaUI();

        if (sincronizar)
        {
            SincronizarChip();
        }
    }

    // ==========================================
    // SINCRONIZACIÓN CON WEAPONMANAGER
    // ==========================================

    private void SincronizarChip()
    {
        // Los enemigos nunca modifican el
        // chip persistente del jugador.
        if (!esPlayer)
            return;

        if (weaponManager == null)
        {
            weaponManager = WeaponManager.Instance;
        }

        if (weaponManager == null)
            return;

        ModificadorStats original = null;

        if (modificadorEquipado != null)
        {
            original =
                modificadorEquipado.Origen != null
                    ? modificadorEquipado.Origen
                    : modificadorEquipado;
        }

        weaponManager.ActualizarChipEquipado(original);
    }

    // ==========================================
    // APLICACIÓN DE EFECTOS
    // ==========================================

    private bool AplicarEfecto(
        AtributoModificable atributo,
        float cantidad)
    {
        switch (atributo)
        {
            case AtributoModificable.VidaMax:
                if (esPlayer && playerStats != null)
                {
                    playerStats.vidaMax += cantidad;
                    return true;
                }

                if (esEnemy && enemyStats != null)
                {
                    enemyStats.vidaMax += cantidad;
                    return true;
                }
                break;

            case AtributoModificable.Vida:
                if (esPlayer && playerStats != null)
                {
                    playerStats.vida = Mathf.Clamp(
                        playerStats.vida + cantidad,
                        0f,
                        Mathf.Max(0f, playerStats.vidaMax)
                    );
                    return true;
                }

                if (esEnemy && enemyStats != null)
                {
                    enemyStats.vida = Mathf.Clamp(
                        enemyStats.vida + cantidad,
                        0f,
                        Mathf.Max(0f, enemyStats.vidaMax)
                    );
                    return true;
                }
                break;

            case AtributoModificable.Velocidad:
                if (esPlayer && roboMovPlayer != null)
                {
                    roboMovPlayer.velocidad += cantidad;
                    return true;
                }

                if (esEnemy && roboMovEnemy != null)
                {
                    roboMovEnemy.velocidad += cantidad;
                    return true;
                }
                break;

            case AtributoModificable.TiempoDeGiro:
                if (esPlayer && roboMovPlayer != null)
                {
                    roboMovPlayer.tiempoDeGiro += cantidad;
                    return true;
                }

                if (esEnemy && roboMovEnemy != null)
                {
                    roboMovEnemy.tiempoDeGiro += cantidad;
                    return true;
                }
                break;

            case AtributoModificable.VelocidadGiroManual:
                if (esPlayer && roboMovPlayer != null)
                {
                    roboMovPlayer.velocidadGiroManual += cantidad;
                    return true;
                }
                break;

            case AtributoModificable.VelocidadGiroMinima:
                if (esEnemy && roboMovEnemy != null)
                {
                    roboMovEnemy.velocidadGiroMinima += cantidad;
                    return true;
                }
                break;

            case AtributoModificable.VelocidadGiroMaxima:
                if (esEnemy && roboMovEnemy != null)
                {
                    roboMovEnemy.velocidadGiroMaxima += cantidad;
                    return true;
                }
                break;

            case AtributoModificable.ModificadorMovimiento:
                if (esPlayer && roboMovPlayer != null)
                {
                    return true;
                }
                break;

            case AtributoModificable.DistanciaRebote:
                if (esPlayer && roboMovPlayer != null)
                {
                    roboMovPlayer.distanciaRebote += cantidad;
                    return true;
                }

                if (esEnemy && roboMovEnemy != null)
                {
                    roboMovEnemy.distanciaRebote += cantidad;
                    return true;
                }
                break;

            case AtributoModificable.DistanciaReboteObjetivo:
                if (esPlayer && roboMovPlayer != null)
                {
                    roboMovPlayer.distanciaReboteEnemigo += cantidad;
                    return true;
                }

                if (esEnemy && roboMovEnemy != null)
                {
                    roboMovEnemy.distanciaRebotePlayer += cantidad;
                    return true;
                }
                break;
        }

        Debug.LogWarning(
            "No se pudo aplicar el atributo: " + atributo,
            this
        );

        return false;
    }

    private void ActualizarVidaUI()
    {
        if (esPlayer && playerStats != null)
        {
            playerStats.ActualizarTexto();
        }

        if (esEnemy && enemyStats != null)
        {
            enemyStats.ActualizarTexto();
        }
    }

    private void OnDestroy()
    {
        // No modificar WeaponManager cuando
        // desaparece el jugador al cambiar de escena.
        // Solo liberar la copia temporal.
        if (copiaPropia != null)
        {
            Destroy(copiaPropia);
            copiaPropia = null;
        }
    }
}
