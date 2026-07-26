using _Project.Code.Gameplay.PlayerController;
using UnityEngine;

// Anything that hurts the player on contact: the gator, the kangaroo, the spike fence.
// Solid by default, so contact arrives through OnCollisionEnter. OnTriggerEnter is kept as well,
// so a hazard can be switched to a trigger in the Inspector without touching this script.
public class Obstacle : MonoBehaviour
{
    [Tooltip("Health removed per hit. The player dies when this meets or exceeds current health.")]
    [SerializeField] private float _damage = 25f;

    [Tooltip("Speed of the knockback away from the contact point.")]
    [SerializeField] private float _pushStrength = 6f;

    [Tooltip("Extra scaling on the upward part of the knockback. 1 is uniform.")]
    [SerializeField] private float _yMultiplier = 1f;

    [Tooltip("Minimum upward component of the knockback, before push strength is applied. Keeps " +
             "a hit readable when the player and the contact point are at the same height.")]
    [SerializeField, Range(0f, 1f)] private float _minUpward = 0.4f;

    private void OnCollisionEnter(Collision collision)
    {
        // Push from where the player actually touched, not from this object's pivot. A long fence
        // has its pivot far from the contact point, so pivot-based knockback fires the wrong way.
        Vector3 contact = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;
        TryDamage(collision.collider, contact);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other, transform.position);
    }

    private void TryDamage(Collider hit, Vector3 contactPoint)
    {
        var player = hit.GetComponent<PlayerController>();
        if (player == null) return;

        player.TakeDamage(_damage, BuildKnockback(player.transform.position, contactPoint));
    }

    // Direction away from the contact, normalized and then scaled. The previous version multiplied
    // the raw position difference by push strength, so knockback grew with distance and a hazard
    // whose pivot sat far from the player launched them across the level.
    private Vector3 BuildKnockback(Vector3 playerPos, Vector3 contactPoint)
    {
        Vector3 away = playerPos - contactPoint;
        away.z = 0f;

        // Degenerate case: the player's origin sits exactly on the contact point. Pick a direction
        // rather than normalizing a zero vector, which gives NaN and freezes the rigidbody.
        if (away.sqrMagnitude < 0.0001f) away = Vector3.up;
        away.Normalize();

        away.y = Mathf.Max(away.y * _yMultiplier, _minUpward);
        return away.normalized * _pushStrength;
    }
}
