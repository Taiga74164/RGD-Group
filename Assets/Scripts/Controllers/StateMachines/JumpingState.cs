﻿using Managers;
using UnityEngine;

namespace Controllers.StateMachines
{
    public class JumpingState : Airborne
    {
        public JumpingState(PlayerController player) : base("JumpingState", player)
        {
        }
        
        public override void EnterState()
        {
            if (GameManager.IsPaused) return;
            
            input.IsJumping = true;
            
            player.CanParachute = true;

            // Set the jumping animation.
            player.animator.SetBool(Jumping, true);
            
            Jump();
            
            // Configure the audio source and play the jump sound.
            player.audioSource.Configure(player.playerSettings.jumpAudioData);
            player.audioSource.Play();
        }

        private void Jump()
        {
            if (player.IsKnockback) return;
            
            if (!player.IsGrounded() && player.CoyoteTimeCounter <= 0.0f) return;
            
            var jumpForce = player.playerSettings.jumpHeight;
            
            player.rb.velocity = new Vector2(player.rb.velocity.x, jumpForce);
            player.ResetCoyoteTimeAndJumpBufferCounter();
        }
        
        public override void HandleInput()
        {
            base.HandleInput();
            
            if (player.rb.velocity.y < Mathf.Epsilon)
                player.ChangeState(player.fallingState);
            
            if (input.IsParachuting)
                player.ChangeState(player.parachutingState);
            
            if (player.rb.velocity.magnitude > player.playerSettings.maxVelocity)
            {
                player.rb.velocity *= player.playerSettings.maxVelocity;
                Debug.Log("done");
            }
        }
        
        public override void ExitState()
        {
            // Set the jumping animation to false.
            player.animator.SetBool(Jumping, false);
            
            // Stop the jump sound.
            player.audioSource.Stop();
            
            input.IsJumping = false;
        }
    }
}