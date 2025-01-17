using Managers;
using UnityEngine;

namespace Controllers.StateMachines
{
    public class ParachutingState : Airborne
    {
        public ParachutingState(PlayerController player) : base("ParachutingState", player)
        {
        }

        public override void EnterState()
        {
            // Set the gliding animation.
            player.animator.SetBool(Gliding, true);
            
            player.CanParachute = false;
            
            // Play the parachute audio.
            player.audioSource.PlayOneShot(player.playerSettings.umbrellaAudioData.clip);

            // Prevents player velocity from exceeding the slowdown of the glide.
            player.rb.velocity = Vector2.zero;
        }
        
        public override void HandleInput()
        {
            base.HandleInput();

            if (player.IsGrounded())
                player.ChangeState(input.IsMoving ? player.walkingState : player.idleState);
            
            if (!InputManager.Jump.IsPressed())
                player.ChangeState(player.fallingState);
        }
        
        protected override void HandleClampFallSpeed()
        {
            // Apply gravity diminisher 
            player.rb.velocity += Vector2.up * (Physics2D.gravity.y *
                                                (player.playerSettings.umbrellaGravityDiminisher - 1) * Time.deltaTime);
            input.IsJumping = false;   
        }
        
        public override void ExitState()
        {
            player.CanParachute = true;
            
            // Set the gliding animation to false.
            player.animator.SetBool(Gliding, false);
        }
    }
}