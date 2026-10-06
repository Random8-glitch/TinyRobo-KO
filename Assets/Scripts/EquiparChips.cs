using UnityEngine;
using System.Collections.Generic;

public class EquiparChips : MonoBehaviour
{
    [Header("Modificador equipado")]
    [SerializeField] private ModificadorStats modificadorEquipado;

    private PlayerStats playerStats;
    private EnemyStats enemyStats;

    private RoboMovPlayer roboMovPlayer;
    private RoboMovEnemy roboMovEnemy;

    private bool esPlayer;
    private bool esEnemy;

    // Guarda los efectos realmente aplicados.
    private struct EfectoAplicado
    {
        public AtributoModificable atributo;
        public float cantidad;

        public EfectoAplicado(AtributoModificable atributo, float cantidad)
        {
            this.atributo = atributo;
            this.cantidad = cantidad;
        }
    }

    private readonly List<EfectoAplicado> efectosAplicados =
        new List<EfectoAplicado>();

    public ModificadorStats ModificadorEquipado => modificadorEquipado;

    private void Awake()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        esPlayer = gameObject.layer == playerLayer;
        esEnemy = gameObject.layer == enemyLayer;

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
                "GestorModificadores: Layer no válido.",
                this
            );
        }
    }

    private void Start()
    {
        // Aplica el modificador asignado desde el Inspector.
        ModificadorStats inicial = modificadorEquipado;
        modificadorEquipado = null;

        if (inicial != null)
            Equipar(inicial);
    }

    public void Equipar(ModificadorStats nuevoModificador)
    {
        if (!esPlayer && !esEnemy)
            return;

        if (nuevoModificador == modificadorEquipado)
            return;

        // Primero retirar el anterior.
        Desequipar();

        if (nuevoModificador == null)
            return;

        modificadorEquipado = nuevoModificador;

        foreach (EfectoModificador efecto in nuevoModificador.efectos)
        {
            if (AplicarEfecto(efecto.atributo, efecto.cantidad))
            {
                efectosAplicados.Add(
                    new EfectoAplicado(efecto.atributo, efecto.cantidad)
                );
            }
        }

        ActualizarVidaUI();
    }

    public void Desequipar()
    {
        if (modificadorEquipado == null &&
            efectosAplicados.Count == 0)
            return;

        // Retirar exactamente los efectos que se aplicaron.
        foreach (EfectoAplicado efecto in efectosAplicados)
        {
            AplicarEfecto(efecto.atributo, -efecto.cantidad);
        }

        efectosAplicados.Clear();
        modificadorEquipado = null;

        ActualizarVidaUI();
    }

    private bool AplicarEfecto(AtributoModificable atributo, float cantidad)
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
                        0f, Mathf.Max(0f, playerStats.vidaMax)
                    );
                    return true;
                }

                if (esEnemy && enemyStats != null)
                {
                    enemyStats.vida = Mathf.Clamp(
                        enemyStats.vida + cantidad,
                        0f, Mathf.Max(0f, enemyStats.vidaMax)
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
            playerStats.ActualizarTexto();

        if (esEnemy && enemyStats != null)
            enemyStats.ActualizarTexto();
    }
}
