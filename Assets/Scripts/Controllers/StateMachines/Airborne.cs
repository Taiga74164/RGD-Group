﻿using Managers;
using UnityEngine;

namespace Controllers.StateMachines
{
    /// <summary>
    /// Hierarchical class for airborne player states.
    /// </summary>
    public class Airborne : BaseState
    {
        protected Airborne(string name, PlayerController player) : base(name, player)
        {
        }

        public override void HandleInput()
        {
            if (InputManager.Attack.triggered)
                ChangeSubState(new AttackingSubState(this));
            
            if (!input.IsAttacking)
                ChangeSubState(null);
        }

        public override void UpdateState()
        {
            base.UpdateState();

            Move();
            HandleClampFallSpeed();
        }
        
        private void Move()
        {
            if (player.IsKnockback) return;
            
            // Move the player while in the air.
            if (!player.BeenWarped)
            {
                player.rb.velocity = new Vector2(
                input.MoveInput.x *
                player.playerSettings.movementSpeed *
                (input.IsRunning ? player.playerSettings.runSpeedMultiplier : 1.0f),
                player.rb.velocity.y);
            }
            else
            {
                var playerVelocity =  new Vector2(player.rb.velocity.x +
                input.MoveInput.x *
                player.playerSettings.movementSpeed *
                (input.IsRunning ? player.playerSettings.runSpeedMultiplier : 1.0f),
                player.rb.velocity.y);
                if(playerVelocity.magnitude <= player.rb.velocity.magnitude)
                    player.rb.velocity = playerVelocity;
            }
        }
        
        protected virtual void HandleClampFallSpeed()
        {
            switch (player.rb.velocity.y)
            {
                // If the player is falling, increase the fall speed.
                case < 0:
                    // Apply the fall multiplier.
                    player.rb.velocity += Vector2.up * 
                                          (Physics2D.gravity.y * (player.playerSettings.fallMultiplier - 1) * 
                                           Time.deltaTime);
                    input.IsJumping = false;
                    break;
                // If the player is jumping and the jump button is released, decrease the jump speed.
                case > 0 when !InputManager.Jump.IsPressed():
                    // Apply the low jump multiplier.
                    player.rb.velocity += Vector2.up * 
                                          (Physics2D.gravity.y * (player.playerSettings.lowJumpMultiplier - 1) * 
                                           Time.deltaTime);
                    break;
            }
        }
    }
}