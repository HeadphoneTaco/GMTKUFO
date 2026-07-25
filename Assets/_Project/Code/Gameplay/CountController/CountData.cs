using UnityEngine;

namespace _Project.Code.Gameplay.CountController
{
    [CreateAssetMenu(fileName = "CountData", menuName = "ScriptableObjects/Count Data", order = 0)]
    public class CountData : ScriptableObject
    {
        [Header("Count Stats")]
        [field: SerializeField]
        public float WalkSpeed { get; private set; } = 5f;

        [field: SerializeField] public float FlySpeed { get; private set; } = 10f;
        [field: SerializeField] public float MistSpeed { get; private set; } = 6f;
        [field: SerializeField] public float MistTime { get; private set; } = 4f;
        [field: SerializeField] public float MaxBatTime { get; private set; } = 10f;
        [field: SerializeField] public float BatTimeDrainRate { get; private set; } = 100f;
        [field:SerializeField] public float BatTimeFillRate { get; private set; }
        [HideInInspector] public float LastBatBreakTime { get; private set; }
        [field:SerializeField] public float TimeAfterBreakToTransform { get; private set; }
        [field:SerializeField] public float TimeBetweenMist { get; private set; }
        [field:SerializeField] public float DefaultGravity { get; private set; }
        [field: SerializeField] public LayerMask GroundLayer { get; private set; }
        [Header("Jump")]
        [Tooltip("Upward velocity applied when jumping from the ground.")]
        [field:SerializeField] public float JumpForce { get; private set; } = 8f;
    }
}