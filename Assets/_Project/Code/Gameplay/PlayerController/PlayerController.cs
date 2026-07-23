using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerStateMachine MyStateMachine;
    [HideInInspector] public Vector2 DirectionalInput = Vector2.zero;
    [HideInInspector] public bool BatInputHeld;
    [HideInInspector] public Rigidbody2D RB;
    [HideInInspector] public bool CanTransform = true;

    [Header("Player Stats")]
    [SerializeField] public float WalkSpeed;
    [SerializeField] public float FlySpeed;
    [SerializeField] public float MistSpeed;
    [SerializeField] public float MistTime;
    [SerializeField] public float MaxBatTime;
    [SerializeField] public float TimeBetweenMist;
    private float _lastTransformationTime;


    [Header("GroundCheck")]
    [SerializeField] private float _groundCheckOffset;
    [SerializeField] private float _groundCheckDistance;
    [SerializeField] private string _groundLayerName;
    private int _groundLayerIndex;

    [Header("EatStats")]
    [SerializeField] private Vector2 BoxCastHalf;

    
    

    private void Awake()
    {
        MyStateMachine = new PlayerStateMachine(this);
        _groundLayerIndex = LayerMask.GetMask(_groundLayerName);
        RB = GetComponent<Rigidbody2D>();
    }
    private void OnEnable()
    {
        MyStateMachine.Initialize(MyStateMachine.StateIdle);
        EventManager.TransformationChanged += ChangeBatInput;
    }
    private void OnDisable()
    {
        MyStateMachine.Disable();
        EventManager.TransformationChanged -= ChangeBatInput;
    }

    public void Update()
    {
        MyStateMachine.Execute();
    }

    public bool IsGrounded()
    {
        return Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y) + _groundCheckOffset * Vector2.down, Vector2.down, _groundCheckDistance, _groundLayerIndex);
    }
    public void ChangeDI(Vector2 directionalInput)
    {
        DirectionalInput = directionalInput;
    }
    public void ChangeBatInput(bool batInputHeld)
    {
        BatInputHeld = batInputHeld;
        if (CanTransform && _lastTransformationTime - Time.time < TimeBetweenMist)
        {
            MyStateMachine.ChangeState(MyStateMachine.StateMist);
        }
    }
}
