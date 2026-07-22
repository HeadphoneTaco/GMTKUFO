using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerStateMachine MyStateMachine;
    [HideInInspector] public Vector2 DirectionalInput;
    [HideInInspector] public bool BatInputHeld;

    [Header("GroundCheck")]
    [SerializeField] private float _groundCheckOffset;
    [SerializeField] private float _groundCheckDistance;
    [SerializeField] private string _groundLayerName;
    private int _groundLayerIndex;
    
    

    private void Awake()
    {
        MyStateMachine = new PlayerStateMachine(this);
        _groundLayerIndex = LayerMask.GetMask(_groundLayerName);
    }
    private void OnEnable()
    {
        MyStateMachine.Initialize(MyStateMachine.StateIdle);
    }
    private void OnDisable()
    {
        MyStateMachine.Disable();
    }

    public void Update()
    {
        MyStateMachine.Execute();
    }

    public bool IsGrounded()
    {
        return Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y) + _groundCheckOffset * Vector2.down, Vector2.down, _groundCheckDistance, _groundLayerIndex);
    }
}
