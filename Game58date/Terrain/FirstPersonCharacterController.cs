#nullable enable
using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;

namespace Game58date.Terrain;

public sealed class FirstPersonCharacterController : SyncScript
{
    private const float MaximumPitch = MathUtil.PiOverTwo * 0.49f;

    private CharacterComponent? character;
    private float yaw;
    private float pitch;
    private bool isActiveMode;

    public Entity? CameraEntity { get; set; }

    public float EyeHeight { get; set; } = 1.62f;

    public float WalkSpeed { get; set; } = 5.6f;

    public float RunSpeed { get; set; } = 8.4f;

    public float MouseSensitivity { get; set; } = 0.0035f;

    public bool IsActiveMode => isActiveMode;

    public override void Start()
    {
        character = Entity.Get<CharacterComponent>();
        if (character is null)
        {
            throw new InvalidOperationException("FirstPersonCharacterController requires a CharacterComponent.");
        }
    }

    public override void Update()
    {
        if (!isActiveMode || character is null || CameraEntity is null)
        {
            return;
        }

        HandleLook();
        HandleMovement();
        UpdateCameraTransform();
    }

    public void SetActiveMode(bool active)
    {
        isActiveMode = active;

        if (active)
        {
            if (Input.HasMouse)
            {
                Input.LockMousePosition(true);
            }

            Game.IsMouseVisible = false;
            UpdateCameraTransform();
        }
        else
        {
            if (character is not null)
            {
                character.SetVelocity(Vector3.Zero);
            }

            if (Input.HasMouse)
            {
                Input.UnlockMousePosition();
            }

            Game.IsMouseVisible = true;
        }
    }

    public void SnapTo(Vector3 position)
    {
        Entity.Transform.Position = position;
        character?.Teleport(position);
        UpdateCameraTransform();
    }

    public void SetYawPitch(float newYaw, float newPitch)
    {
        yaw = newYaw;
        pitch = MathUtil.Clamp(newPitch, -MaximumPitch, MaximumPitch);
        UpdateCameraTransform();
    }

    public (float Yaw, float Pitch) GetYawPitch()
    {
        return (yaw, pitch);
    }

    public void MatchCameraPose(Vector3 cameraPosition, Quaternion cameraRotation)
    {
        Matrix rotation = Matrix.RotationQuaternion(cameraRotation);
        Vector3 forward = rotation.Forward;
        float nextPitch = MathF.Asin(MathUtil.Clamp(Vector3.Dot(forward, Vector3.UnitY), -1f, 1f));
        float nextYaw = MathF.Atan2(-forward.X, -forward.Z);

        Vector3 bodyPosition = cameraPosition - Vector3.UnitY * EyeHeight;
        SnapTo(bodyPosition);
        SetYawPitch(nextYaw, nextPitch);
    }

    public Vector3 GetCameraPosition()
    {
        return Entity.Transform.Position + Vector3.UnitY * EyeHeight;
    }

    private void HandleLook()
    {
        if (!Input.HasMouse)
        {
            return;
        }

        yaw -= Input.MouseDelta.X * MouseSensitivity;
        pitch -= Input.MouseDelta.Y * MouseSensitivity;
        pitch = MathUtil.Clamp(pitch, -MaximumPitch, MaximumPitch);
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = Vector3.Zero;

        if (Input.IsKeyDown(Keys.W))
        {
            moveDirection.Z -= 1f;
        }
        if (Input.IsKeyDown(Keys.S))
        {
            moveDirection.Z += 1f;
        }
        if (Input.IsKeyDown(Keys.A))
        {
            moveDirection.X -= 1f;
        }
        if (Input.IsKeyDown(Keys.D))
        {
            moveDirection.X += 1f;
        }

        if (moveDirection.LengthSquared() > 1f)
        {
            moveDirection.Normalize();
        }

        Matrix yawMatrix = Matrix.RotationY(yaw);
        Vector3 worldMove = Vector3.TransformNormal(moveDirection, yawMatrix);
        float speed = Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift) ? RunSpeed : WalkSpeed;
        character!.SetVelocity(worldMove * speed);

        if (Input.IsKeyPressed(Keys.Space) && character.IsGrounded)
        {
            character.Jump();
        }
    }

    private void UpdateCameraTransform()
    {
        if (CameraEntity is null)
        {
            return;
        }

        CameraEntity.Transform.Position = GetCameraPosition();
        CameraEntity.Transform.Rotation = Quaternion.RotationYawPitchRoll(yaw, pitch, 0f);
        Entity.Transform.Rotation = Quaternion.RotationY(yaw);
    }
}
