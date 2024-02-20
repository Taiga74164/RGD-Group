﻿using Managers;
using UnityEngine;

namespace Controllers.StateMachines
{
    /// <summary>
    /// Hierarchy class for grounded player states.
    /// </summary>
    public class Grounded : BaseState
    {
        private bool _isInvincible;
        private float _invincibilityFrameCounter;
        
        protected Grounded(string name, PlayerController player) : base(name, player)
        {
        }
        
        public override void HandleInput()
        {
            base.HandleInput();

            if (input.IsCrouching)
                player.ChangeState(player.crouchingState);
            
            if (InputManager.Jump.IsInProgress())
                player.ChangeState(player.jumpingState);

            if (input.IsFalling)
                player.ChangeState(player.fallingState);
            
            if (InputManager.Attack.triggered)
                ChangeSubState(new AttackingSubState(this));
        }
        
        public override void UpdateState()
        {
            base.UpdateState();
            currentSubState?.UpdateSubState();
            
            if (player.CanJump())
                player.ChangeState(player.jumpingState);
        }
    }
}