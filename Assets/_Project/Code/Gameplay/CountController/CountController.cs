using UnityEngine;

namespace _Project.Code.Gameplay.CountController
{
    [RequireComponent(typeof(CountInputManager))]
    public class CountController : MonoBehaviour
    {
        # region Setup code
        [field: SerializeField] public GameObject CountModel{ get; private set; }
        [field: SerializeField] public GameObject BatModel{ get; private set; }
        [field: SerializeField] public CountData CountData { get; private set; }
        [field: SerializeField] public GameObject BatLocation { get; private set; }
        [HideInInspector] public GameObject CurrentModel;
        [HideInInspector] public CountInputManager CountInputs { get; private set; }
        public CountStateMachine StateMachine { get; private set; }
        public CountAnimator Animator { get; private set; }
        public Rigidbody RB { get; private set; }
        #endregion
        #region Data variables

        public Vector2 MoveInput { get; set; }
        #endregion
        
        void Start()
        {
            Debug.Log($"{nameof(CountController)} starts up");
            Animator = GetComponent<CountAnimator>() ;
            CountInputs = GetComponent<CountInputManager>();
            RB = GetComponent<Rigidbody>();
            StateMachine = new CountStateMachine(this);
            if(RB == null)
                Debug.LogError($"{nameof(CountController)} requires a {nameof(Rigidbody)}");
            if(CountModel == null)
                Debug.LogError($"{nameof(CountController)} requires a {nameof(CountModel)}");
            if(Animator == null)
                Debug.Log("You didn't add an animator to the count, you dummy");
        }
        void Update()=> StateMachine.Update();
        void FixedUpdate()=> StateMachine.FixedUpdate();
    }

}