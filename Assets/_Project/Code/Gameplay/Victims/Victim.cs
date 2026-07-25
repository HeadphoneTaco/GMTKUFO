using UnityEngine;

public class Victim : MonoBehaviour 
{
    [SerializeField] private float _startBloodPoints;
    private float _currentBloodPoints;
    private bool _isDead = false;
    private Collider _collider;
    /// <summary>
    /// put the victim's collider slightly in the positive z direction of the line the player runs along
    /// make sure they have the "Victims tag"
    /// </summary>
    void Start()
    {
        _collider = GetComponent<Collider>();
        _currentBloodPoints = _startBloodPoints;
    }
    public bool GetBit()
    {
        return _isDead;
    }
    public (bool, float) DrainBlood(float drainRate)
    {
        if (_currentBloodPoints - drainRate <= 0)
        {
            // TODO death animation
            _collider.enabled = false;
            _isDead = true;
            return (true, _currentBloodPoints);
        }
        else
        {
            _currentBloodPoints -= drainRate;
            return (false, drainRate);
        }
    }
}
