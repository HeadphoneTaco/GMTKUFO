namespace _Project.Code.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>How a run finished. Decides which end screen loads.</summary>
    public enum RunOutcome
    {
        /// <summary>Blood hit zero, from hazards or from burning in the daylight. EndScreenA.</summary>
        Died,

        /// <summary>Made it home to the coffin with blood banked. EndScreenB.</summary>
        ReachedCoffin
    }
}
