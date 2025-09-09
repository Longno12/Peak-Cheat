using UnityEngine;

namespace MyCoolMod
{
    public enum FlyMode { Standard, Horizontal, Superman, Orbit, Jetpack, Spectator }

    public static class FlyMod
    {
        public static bool IsFlying { get; private set; } = false;
        public static FlyMode CurrentMode { get; set; } = FlyMode.Standard;
        public static bool IsHovering { get; set; } = false;
        public static bool NoClipEnabled { get; set; } = true;
        public static float FlySpeed { get; set; } = 50f;
        public static float BoostSpeed { get; set; } = 150f;
        public static float Acceleration { get; set; } = 10f;
        public static float Deceleration { get; set; } = 8f;
        public static float MaxAltitude { get; set; } = 2000f;
        public static float MinAltitude { get; set; } = 0.5f;
        public static float JetpackForce { get; set; } = 15f;
        public static bool UseStamina { get; set; } = false;
        public static float MaxStamina { get; set; } = 100f;
        public static float StaminaDrain { get; set; } = 5f;
        public static float StaminaRegen { get; set; } = 10f;

        private static float currentStamina = 100f;
        private static Vector3 smoothVelocity;
        private static float defaultFOV;

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

            if (Camera.main != null)
            {
                defaultFOV = Camera.main.fieldOfView;
            }

            Plugin.Log.LogInfo($"Fly Mod {(IsFlying ? "Enabled" : "Disabled")}");
        }

        public static void HandleFly()
        {
            if (!IsFlying || Character.localCharacter == null || Camera.main == null) return;

            var character = Character.localCharacter;
            if (UseStamina)
            {
                if (currentStamina <= 0f)
                {
                    Plugin.Log.LogInfo("Out of stamina! Fly disabled.");
                    ToggleFly();
                    return;
                }
                else if (IsGrounded(character))
                {
                    currentStamina = Mathf.Min(MaxStamina, currentStamina + StaminaRegen * Time.deltaTime);
                }
            }

            if (IsHovering)
            {
                character.refs.ragdoll.HaltBodyVelocity();
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
                case FlyMode.Orbit:
                    HandleOrbitFly();
                    break;
                case FlyMode.Jetpack:
                    HandleJetpackFly();
                    break;
                case FlyMode.Spectator:
                    HandleSpectatorFly();
                    break;
            }
        }

        private static void HandleStandardFly()
        {
            var character = Character.localCharacter;
            var moveDirection = GetBaseMoveDirection(Camera.main.transform, character.input);
            ApplyVelocity(character, moveDirection);
        }
        private static bool IsGrounded(Character character)
        {
            if (character == null) return false;
            Vector3 origin = character.transform.position + Vector3.up * 0.2f;
            float checkDistance = 0.4f;
            return Physics.Raycast(origin, Vector3.down, checkDistance);
        }

        private static void HandleHorizontalFly()
        {
            var character = Character.localCharacter;
            var input = character.input;
            var cam = Camera.main.transform;
            Vector3 forward = cam.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 right = cam.right;
            Vector3 moveDirection = (forward * input.movementInput.y) + (right * input.movementInput.x);
            if (input.jumpIsPressed) moveDirection += Vector3.up;
            if (Input.GetKey(KeyCode.LeftShift)) moveDirection -= Vector3.up;
            ApplyVelocity(character, moveDirection);
        }

        private static void HandleSupermanFly()
        {
            var character = Character.localCharacter;
            var moveDirection = GetBaseMoveDirection(Camera.main.transform, character.input);
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

        private static void HandleOrbitFly()
        {
            var character = Character.localCharacter;
            var cam = Camera.main.transform;
            float orbitRadius = 10f;
            float orbitSpeed = 30f;
            Vector3 orbitPosition = character.transform.position + new Vector3(Mathf.Sin(Time.time * orbitSpeed), 0, Mathf.Cos(Time.time * orbitSpeed)) * orbitRadius;
            Vector3 moveDirection = (orbitPosition - character.transform.position).normalized;
            ApplyVelocity(character, moveDirection);
        }

        private static void HandleJetpackFly()
        {
            var character = Character.localCharacter;
            if (Input.GetKey(KeyCode.Space))
            {
                foreach (var bodyPart in character.refs.ragdoll.partList)
                {
                    if (bodyPart?.Rig != null)
                    {
                        bodyPart.Rig.AddForce(Vector3.up * JetpackForce, ForceMode.Acceleration);
                    }
                }

                if (UseStamina)
                    currentStamina = Mathf.Max(0, currentStamina - StaminaDrain * Time.deltaTime);
            }
        }

        private static void HandleSpectatorFly()
        {
            var character = Character.localCharacter;
            var moveDirection = GetBaseMoveDirection(Camera.main.transform, character.input);
            ApplyVelocity(character, moveDirection, ignorePhysics: true);
        }

        private static Vector3 GetBaseMoveDirection(Transform cameraTransform, CharacterInput input)
        {
            Vector3 moveDirection = (cameraTransform.forward * input.movementInput.y) + (cameraTransform.right * input.movementInput.x);
            if (input.jumpIsPressed) moveDirection += Vector3.up;
            if (Input.GetKey(KeyCode.LeftShift)) moveDirection -= Vector3.up;
            return moveDirection;
        }

        private static void ApplyVelocity(Character character, Vector3 moveDirection, bool ignorePhysics = false)
        {
            float targetSpeed = Input.GetKey(KeyCode.LeftControl) ? BoostSpeed : FlySpeed;
            Vector3 targetVelocity = moveDirection.normalized * targetSpeed;
            smoothVelocity = Vector3.Lerp(smoothVelocity, targetVelocity, Time.fixedDeltaTime * Acceleration);

            if (ignorePhysics)
            {
                character.transform.position += smoothVelocity * Time.fixedDeltaTime;
                return;
            }

            foreach (var bodyPart in character.refs.ragdoll.partList)
            {
                if (bodyPart?.Rig != null)
                {
                    bodyPart.Rig.velocity = smoothVelocity;
                }
            }
            if (Camera.main != null)
            {
                float targetFOV = Input.GetKey(KeyCode.LeftControl) ? defaultFOV + 15f : defaultFOV;
                Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, targetFOV, Time.deltaTime * 5f);
            }
            if (character.transform.position.y > MaxAltitude)
            {
                character.transform.position = new Vector3(character.transform.position.x, MaxAltitude, character.transform.position.z);
            }
            else if (character.transform.position.y < MinAltitude)
            {
                character.transform.position = new Vector3(character.transform.position.x, MinAltitude, character.transform.position.z);
            }
            if (UseStamina && moveDirection.sqrMagnitude > 0.1f)
            {
                currentStamina = Mathf.Max(0, currentStamina - StaminaDrain * Time.deltaTime);
            }
        }
    }
}
