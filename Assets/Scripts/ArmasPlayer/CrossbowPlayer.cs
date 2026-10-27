
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CrossbowPlayer : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float velocidad = 15f;
    [SerializeField] private float danioNormal = 10f;
    [SerializeField] private bool hit = false;

    private int enemyLayer;
    private int wallLayer;

    private Rigidbody rb;
    private Collider[] hitboxes;

    // Referencia a los chips del jugador
    private EquiparChips equiparChips;

    public bool Hit => hit;

    private void Awake()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");
        wallLayer = LayerMask.NameToLayer("Wall");

        rb = GetComponent<Rigidbody>();

        // Movimiento controlado por el script
        rb.useGravity = false;
        rb.isKinematic = true;

        // Obtiene todas las hitboxes de la flecha
        hitboxes = GetComponentsInChildren<Collider>();

        // Busca EquiparChips en los objetos padre
        equiparChips = GetComponentInParent<EquiparChips>();

        hit = false;
    }

    private void Start()
    {
        // Si pasan 20 segundos sin impactar, desaparece
        Invoke(nameof(DespawnSinHit), 20f);
    }

    private void FixedUpdate()
    {
        // Si ya impactó, no puede moverse
        if (hit)
            return;

        // Avanza constantemente hacia donde apunta
        Vector3 movimiento =
            transform.forward * velocidad * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + movimiento);
    }

    private void OnTriggerEnter(Collider other)
    {
        // No puede volver a impactar
        if (hit)
            return;

        // IMPACTO CONTRA PARED
        if (other.gameObject.layer == wallLayer)
        {
            RegistrarHit();

            Debug.Log("Flecha impactó contra una pared");

            return;
        }

        // IMPACTO CONTRA ENEMIGO
        if (other.gameObject.layer == enemyLayer)
        {
            EnemyStats stats =
                other.GetComponentInParent<EnemyStats>();

            if (stats != null)
            {
                // Daño base de la flecha
                float danio = danioNormal;

                // Aplicar multiplicador del chip
                float multiplicadorChip =
                    ObtenerMultiplicadorChip();

                danio *= multiplicadorChip;

                // Enviar daño al enemigo
                stats.RecibirDanio(danio);

                Debug.Log("Enemigo golpeado por flecha");
            }

            // Registrar impacto y detener movimiento
            RegistrarHit();

            // Convertir la flecha en hija del enemigo
            Transform enemigo = stats != null
                ? stats.transform
                : other.transform;

            transform.SetParent(enemigo, true);
        }
    }

    private void RegistrarHit()
    {
        // Evitar múltiples impactos
        if (hit)
            return;

        // Activar estado de impacto
        hit = true;

        // Desactivar permanentemente las hitboxes
        foreach (Collider col in hitboxes)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }

        // Cancelar despawn de 20 segundos
        CancelInvoke(nameof(DespawnSinHit));

        // Retirar el Rigidbody para que deje
        // de intervenir en el movimiento
        if (rb != null)
        {
            Destroy(rb);
        }

        // Destruir únicamente esta flecha
        // después de 3 segundos
        Destroy(gameObject, 3f);
    }

    private void DespawnSinHit()
    {
        if (!hit)
        {
            Destroy(gameObject);
        }
    }

    // Permite asignar los chips del jugador
    // cuando la flecha se instancia como proyectil
    public void AsignarEquiparChips(EquiparChips chips)
    {
        equiparChips = chips;
    }

    // Busca únicamente efectos Weapon -> DañoPlus
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
