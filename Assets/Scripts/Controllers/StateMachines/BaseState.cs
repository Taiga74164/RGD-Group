using UnityEngine;

namespace Controllers.StateMachines
{
    /// <summary>
    /// Base class for player states.
    /// </summary>
    public abstract class BaseState
    {
        public string name;
        protected internal PlayerController player;
        protected BaseSubState currentSubState;
        protected InputController input;
        protected BaseState(string name, PlayerController player)
        {
            this.name = name;
            this.player = player;
            input = player.inputController;
        }
        
        #region Cached Properties

        protected static readonly int Walking = Animator.StringToHash("Walking");
        protected static readonly int Running = Animator.StringToHash("Running");
        protected static readonly int Jumping = Animator.StringToHash("Jumping");
        protected static readonly int Falling = Animator.StringToHash("Falling");
        protected internal static readonly int Attacking = Animator.StringToHash("Attacking");

        #endregion
        
        public virtual void EnterState() { }

        public virtual void HandleInput() { }

        public virtual void UpdateState() { }
        
        public virtual void ExitState() { }

        protected void ChangeSubState(BaseSubState subState)
        {
            currentSubState?.ExitSubState();
            currentSubState = subState;
            currentSubState.EnterSubState();
        }
    }
}