using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerStateMachine MyStateMachine;
    

    public void Awake()
    {
        MyStateMachine = new PlayerStateMachine(this);
    }

    public void Update()
    {
        MyStateMachine.Execute();
    }
}
