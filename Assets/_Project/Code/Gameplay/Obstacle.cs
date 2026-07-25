using _Project.Code.Gameplay.PlayerController;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private float _damage;
    [SerializeField] private float _pushStrength;
    [SerializeField] private float _yMultiplier;

    private void OnTriggerEnter(Collider other)
    {
        other.GetComponent<PlayerController>()?.TakeDamage(_damage,new Vector3((-transform.position.x + other.transform.position.x) * _pushStrength, (-transform.position.y + other.transform.position.y) * _pushStrength * _yMultiplier,0));
    }
}
