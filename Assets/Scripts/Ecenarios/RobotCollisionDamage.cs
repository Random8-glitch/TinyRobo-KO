using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class RobotCollisionDamage : MonoBehaviour
{
    [Header("Prefab del daño")]
    [SerializeField] private DecalProjector decalPrefab;

    [Header("Configuracion")]
    [SerializeField] private int maxDecals = 30;
    [SerializeField] private float collisionCooldown = 0.12f;
    [SerializeField] private float surfaceOffset = 0.015f;
    [SerializeField] private float randomRotation = 180f;

    [Header("Tamano aleatorio")]
    [SerializeField]
    private Vector2 widthRange =
        new Vector2(0.12f, 0.24f);

    [SerializeField]
    private Vector2 heightRange =
        new Vector2(0.07f, 0.15f);

    private readonly Queue<GameObject> activeDecals =
        new Queue<GameObject>();

    private float nextDamageTime;

    private void OnCollisionEnter(Collision collision)
    {
        if (decalPrefab == null)
            return;

        if (Time.time < nextDamageTime)
            return;

        // Ignorar colisiones contra objetos sin Rigidbody
        // o sin una configuracion de colision apropiada.
        if (collision.contactCount == 0)
            return;

        ContactPoint contact = collision.GetContact(0);

        // Identificar el collider de ESTE robot.
        Collider ownCollider = contact.thisCollider;

        if (ownCollider == null)
            ownCollider = contact.otherCollider; // Cambiar esta línea

        if (ownCollider == null)
            return;

        Vector3 point = contact.point;

        // Calcular una normal que apunte hacia fuera
        // de la superficie de nuestro robot.
        Vector3 normal = contact.normal;

        Vector3 towardCenter =
            ownCollider.bounds.center - point;

        if (Vector3.Dot(normal, towardCenter) > 0f)
            normal = -normal;

        if (normal.sqrMagnitude < 0.001f)
            return;

        normal.Normalize();

        SpawnDamage(point, normal, ownCollider.transform);

        nextDamageTime = Time.time + collisionCooldown;
    }

    private void SpawnDamage(
        Vector3 point,
        Vector3 normal,
        Transform surface)
    {
        // Alejar ligeramente el proyector de la superficie.
        Vector3 spawnPosition =
            point + normal * surfaceOffset;

        // El proyector apunta hacia la superficie.
        Quaternion rotation =
            Quaternion.LookRotation(-normal);

        // Girar aleatoriamente el rayón alrededor de la normal.
        rotation *= Quaternion.AngleAxis(
            Random.Range(-randomRotation, randomRotation),
            Vector3.forward
        );

        DecalProjector decal = Instantiate(
            decalPrefab,
            spawnPosition,
            rotation,
            surface
        );

        // Variar las dimensiones para evitar marcas repetidas.
        decal.size = new Vector3(
            Random.Range(widthRange.x, widthRange.y),
            Random.Range(heightRange.x, heightRange.y),
            decal.size.z
        );

        // Conservar un maximo de marcas.
        activeDecals.Enqueue(decal.gameObject);

        while (activeDecals.Count > maxDecals)
        {
            GameObject oldest = activeDecals.Dequeue();

            if (oldest != null)
                Destroy(oldest);
        }
    }

    public void ClearDamage()
    {
        while (activeDecals.Count > 0)
        {
            GameObject decal = activeDecals.Dequeue();

            if (decal != null)
                Destroy(decal);
        }
    }
}
