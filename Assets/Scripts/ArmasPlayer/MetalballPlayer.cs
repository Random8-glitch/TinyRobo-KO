
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MetalballPlayer : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float velocidad = 15f;
    [SerializeField] private float danioNormal = 10f;

    [Header("Rebotes")]
    [SerializeField] private int rebotesMaximos = 3;
    [SerializeField] private int rebotesRealizados = 0;

    [SerializeField] private float anguloMinimo = 160f;
    [SerializeField] private float anguloMaximo = 200f;

    [Header("Tiempo de Vida")]
    [SerializeField] private float tiempoVida = 60f;

    private int enemyLayer;
    private int wallLayer;

    private Rigidbody rb;

    private bool destruyendo = false;

    // Referencia a los chips del jugador
    private EquiparChips equiparChips;

    public int RebotesRealizados => rebotesRealizados;

    private void Awake()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");
        wallLayer = LayerMask.NameToLayer("Wall");

        rb = GetComponent<Rigidbody>();

        // Movimiento controlado por el script
        rb.useGravity = false;
        rb.isKinematic = true;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        // Busca EquiparChips en los objetos padre
        equiparChips = GetComponentInParent<EquiparChips>();

        rebotesRealizados = 0;
    }

    private void Start()
    {
        // Desaparece después de 60 segundos
        Destroy(gameObject, tiempoVida);
    }

    private void FixedUpdate()
    {
        if (destruyendo)
            return;

        // Movimiento continuo hacia donde apunta
        Vector3 movimiento =
            transform.forward * velocidad * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + movimiento);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (destruyendo)
            return;

        int layer = other.gameObject.layer;

        // Ignorar objetos que no sean Wall o Enemy
        if (layer != wallLayer && layer != enemyLayer)
            return;

        // IMPACTO CONTRA ENEMIGO
        if (layer == enemyLayer)
        {
            EnemyStats stats =
                other.GetComponentInParent<EnemyStats>();

            if (stats != null)
            {
                // Daño base
                float danio = danioNormal;

                // Aplicar multiplicador del chip
                float multiplicadorChip =
                    ObtenerMultiplicadorChip();

                danio *= multiplicadorChip;

                // Enviar daño al enemigo
                stats.RecibirDanio(danio);

                Debug.Log("Enemigo golpeado por Metalball");
            }
        }

        // Comprobar si ya realizó los 3 rebotes
        if (rebotesRealizados >= rebotesMaximos)
        {
            destruyendo = true;

            Debug.Log("Metalball alcanzó su límite de rebotes");

            Destroy(gameObject);

            return;
        }

        // REBOTAR
        Rebotar();
    }

    private void Rebotar()
    {
        // Incrementar contador
        rebotesRealizados++;

        // Elegir ángulo aleatorio entre 160 y 200
        float angulo = Random.Range(
            anguloMinimo,
            anguloMaximo
        );

        // Rotar instantáneamente sobre el eje Y
        transform.Rotate(
            0f,
            angulo,
            0f,
            Space.World
        );

        Debug.Log(
            $"Metalball rebotó: {rebotesRealizados}/{rebotesMaximos}"
        );
    }

    // Permite asignar los chips del jugador
    // cuando la esfera se instancia como proyectil
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
