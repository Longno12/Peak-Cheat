using UnityEngine;

namespace MyCoolMod
{
    public enum FlyMode { Standard, Horizontal, Superman }

    public static class FlyMod
    {
        public static bool IsFlying { get; private set; } = false;
        public static FlyMode CurrentMode { get; set; } = FlyMode.Standard;
        public static bool IsHovering { get; set; } = false;
        public static bool NoClipEnabled { get; set; } = true;
        public static float FlySpeed { get; set; } = 50f;
        public static float BoostSpeed { get; set; } = 150f;

        public static void ToggleFly()
        {
            IsFlying = !IsFlying;
            var character = Character.localCharacter;
            if (character == null) return;

            foreach (var bodyPart in character.refs.ragdoll.partList)
            {
                if (bodyPart?.Rig != null)
                {
                    bodyPart.Rig.useGravity = !IsFlying;
                }
                if (bodyPart?.GetComponent<Collider>() != null)
                {
                    bodyPart.GetComponent<Collider>().isTrigger = IsFlying && NoClipEnabled;
                }
            }

            if (!IsFlying)
            {
                character.refs.ragdoll.HaltBodyVelocity();
            }

            Plugin.Log.LogInfo($"Fly Mod {(IsFlying ? "Enabled" : "Disabled")}");
        }

        public static void HandleFly()
        {
            if (!IsFlying || Character.localCharacter == null || Camera.main == null) return;

            if (IsHovering)
            {
                Character.localCharacter.refs.ragdoll.HaltBodyVelocity();
                return;
            }

            switch (CurrentMode)
            {
                case FlyMode.Standard:
                    HandleStandardFly();
                    break;
                case FlyMode.Horizontal:
                    HandleHorizontalFly();
                    break;
                case FlyMode.Superman:
                    HandleSupermanFly();
                    break;
            }
        }

        private static void HandleStandardFly()
        {
            var character = Character.localCharacter;
            var input = character.input;
            var cameraTransform = Camera.main.transform;
            Vector3 moveDirection = GetBaseMoveDirection(cameraTransform, input);
            ApplyVelocity(character, moveDirection);
        }

        private static void HandleHorizontalFly()
        {
            var character = Character.localCharacter;
            var input = character.input;
            var cameraTransform = Camera.main.transform;
            Vector3 forward = cameraTransform.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 right = cameraTransform.right;
            Vector3 moveDirection = (forward * input.movementInput.y) + (right * input.movementInput.x);
            if (input.jumpIsPressed) moveDirection += Vector3.up;
            if (Input.GetKey(KeyCode.LeftShift)) moveDirection -= Vector3.up;
            ApplyVelocity(character, moveDirection);
        }

        private static void HandleSupermanFly()
        {
            var character = Character.localCharacter;
            var input = character.input;
            var cameraTransform = Camera.main.transform;
            Vector3 moveDirection = GetBaseMoveDirection(cameraTransform, input);
            ApplyVelocity(character, moveDirection);

            if (moveDirection.sqrMagnitude > 0.1f)
            {
                var hip = character.refs.ragdoll.partDict[BodypartType.Hip].Rig;
                if (hip != null)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
                    hip.MoveRotation(Quaternion.Slerp(hip.rotation, targetRotation, Time.fixedDeltaTime * 5f));
                }
            }
        }

        private static Vector3 GetBaseMoveDirection(Transform cameraTransform, CharacterInput input)
        {
            Vector3 moveDirection = (cameraTransform.forward * input.movementInput.y) + (cameraTransform.right * input.movementInput.x);
            if (input.jumpIsPressed) moveDirection += Vector3.up;
            if (Input.GetKey(KeyCode.LeftShift)) moveDirection -= Vector3.up;
            return moveDirection;
        }

        private static void ApplyVelocity(Character character, Vector3 moveDirection)
        {
            float currentSpeed = Input.GetKey(KeyCode.LeftControl) ? BoostSpeed : FlySpeed;
            Vector3 targetVelocity = moveDirection.normalized * currentSpeed;
            foreach (var bodyPart in character.refs.ragdoll.partList)
            {
                if (bodyPart?.Rig != null)
                {
                    bodyPart.Rig.velocity = targetVelocity;
                }
            }
        }
    }
}