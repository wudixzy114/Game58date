#nullable enable
using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Game58date.Terrain;

public sealed class FirstPersonCharacterController : SyncScript
{
    private const float MaximumPitch = MathUtil.PiOverTwo * 0.49f;
    private const float SkinWidth = 0.02f;
    private const float DefaultMouseSensitivity = 0.0022f;

    public const float DefaultRadius = 0.35f;
    public const float DefaultEyeHeight = 1.62f;
    public const float DefaultHeadHeightAboveEye = 0.18f;
    public const float DefaultStepHeight = 0.45f;

    private VoxelTerrainWorldRuntime? worldRuntime;
    private float yaw;
    private float pitch;
    private bool isActiveMode;

    public float Radius { get; set; } = DefaultRadius;

    public float EyeHeightFromFeet { get; set; } = DefaultEyeHeight;

    public float HeadHeightAboveEye { get; set; } = DefaultHeadHeightAboveEye;

    public float WalkSpeed { get; set; } = 5.6f;

    public float RunSpeed { get; set; } = 8.4f;

    public float JumpSpeed { get; set; } = 7.2f;

    public float Gravity { get; set; } = -24f;

    public float StepHeight { get; set; } = DefaultStepHeight;

    public float MouseSensitivity { get; set; } = DefaultMouseSensitivity;

    public bool IsActiveMode => isActiveMode;

    public bool IsGrounded { get; private set; }

    public Vector3 LinearVelocity { get; private set; } = Vector3.Zero;

    public Vector3 EyePosition => Entity.Transform.Position;

    public void Initialize(VoxelTerrainWorldRuntime runtime)
    {
        worldRuntime = runtime;
    }

    public override void Update()
    {
        if (!isActiveMode || worldRuntime is null)
        {
            ReleaseMouseCapture();
            return;
        }

        EnsureMouseCapture();
        if (!Game.IsActive || !Game.Window.Focused)
        {
            LinearVelocity = Vector3.Zero;
            return;
        }

        HandleLook();
        HandleMovement();
        ApplyMovement((float)Game.UpdateTime.Elapsed.TotalSeconds);
        UpdateTransform();
    }

    public void SetActiveMode(bool active)
    {
        isActiveMode = active;
        if (!active)
        {
            LinearVelocity = Vector3.Zero;
            ReleaseMouseCapture();
            return;
        }

        EnsureMouseCapture(forceCenter: true);
        UpdateTransform();
    }

    public override void Cancel()
    {
        ReleaseMouseCapture();
        base.Cancel();
    }

    public void SnapToEyePosition(Vector3 eyePosition)
    {
        Entity.Transform.Position = eyePosition;
        LinearVelocity = Vector3.Zero;
        IsGrounded = false;
        UpdateTransform();
    }

    public void SetYawPitch(float newYaw, float newPitch)
    {
        yaw = newYaw;
        pitch = MathUtil.Clamp(newPitch, -MaximumPitch, MaximumPitch);
        UpdateTransform();
    }

    public void MatchPose(Vector3 eyePosition, Quaternion cameraRotation)
    {
        Matrix rotation = Matrix.RotationQuaternion(cameraRotation);
        Vector3 forward = rotation.Forward;
        float nextPitch = MathF.Asin(MathUtil.Clamp(Vector3.Dot(forward, Vector3.UnitY), -1f, 1f));
        float nextYaw = MathF.Atan2(-forward.X, -forward.Z);

        SnapToEyePosition(eyePosition);
        yaw = nextYaw;
        pitch = MathUtil.Clamp(nextPitch, -MaximumPitch, MaximumPitch);
        UpdateTransform();
    }

    private void HandleLook()
    {
        if (!Input.HasMouse || !Input.IsMousePositionLocked)
        {
            return;
        }

        yaw -= Input.AbsoluteMouseDelta.X * MouseSensitivity;
        pitch -= Input.AbsoluteMouseDelta.Y * MouseSensitivity;
        pitch = MathUtil.Clamp(pitch, -MaximumPitch, MaximumPitch);
    }

    private void EnsureMouseCapture(bool forceCenter = false)
    {
        if (!Input.HasMouse)
        {
            Game.IsMouseVisible = false;
            return;
        }

        if (!Game.IsActive || !Game.Window.Visible || !Game.Window.Focused)
        {
            ReleaseMouseCapture();
            return;
        }

        if (!Input.IsMousePositionLocked)
        {
            Input.LockMousePosition(forceCenter);
        }

        Game.IsMouseVisible = false;
    }

    private void ReleaseMouseCapture()
    {
        if (Input.HasMouse && Input.IsMousePositionLocked)
        {
            Input.UnlockMousePosition();
        }

        Game.IsMouseVisible = true;
    }

    private void HandleMovement()
    {
        Vector3 planarInput = Vector3.Zero;

        if (Input.IsKeyDown(Keys.W))
        {
            planarInput.Z -= 1f;
        }
        if (Input.IsKeyDown(Keys.S))
        {
            planarInput.Z += 1f;
        }
        if (Input.IsKeyDown(Keys.A))
        {
            planarInput.X -= 1f;
        }
        if (Input.IsKeyDown(Keys.D))
        {
            planarInput.X += 1f;
        }

        if (planarInput.LengthSquared() > 1f)
        {
            planarInput.Normalize();
        }

        float moveSpeed = Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift) ? RunSpeed : WalkSpeed;
        Matrix yawMatrix = Matrix.RotationY(yaw);
        Vector3 worldMove = Vector3.TransformNormal(planarInput, yawMatrix);

        Vector3 velocity = LinearVelocity;
        velocity.X = worldMove.X * moveSpeed;
        velocity.Z = worldMove.Z * moveSpeed;

        if (IsGrounded)
        {
            velocity.Y = -2f;
            if (Input.IsKeyPressed(Keys.Space))
            {
                velocity.Y = JumpSpeed;
                IsGrounded = false;
            }
        }

        LinearVelocity = velocity;
    }

    private void ApplyMovement(float deltaTime)
    {
        Vector3 velocity = LinearVelocity;
        velocity.Y += Gravity * deltaTime;
        LinearVelocity = velocity;

        Vector3 position = EyePosition;
        position = MoveHorizontal(position, Vector3.UnitX * (LinearVelocity.X * deltaTime));
        position = MoveHorizontal(position, Vector3.UnitZ * (LinearVelocity.Z * deltaTime));
        position = MoveVertical(position, LinearVelocity.Y * deltaTime);
        Entity.Transform.Position = position;
    }

    private Vector3 MoveHorizontal(Vector3 current, Vector3 delta)
    {
        if (delta == Vector3.Zero || worldRuntime is null)
        {
            return current;
        }

        Vector3 candidate = current + delta;
        if (!IntersectsSolid(candidate))
        {
            return candidate;
        }

        if (IsGrounded)
        {
            Vector3 stepped = current + Vector3.UnitY * StepHeight + delta;
            if (!IntersectsSolid(stepped))
            {
                return stepped;
            }
        }

        return current;
    }

    private Vector3 MoveVertical(Vector3 current, float deltaY)
    {
        if (worldRuntime is null)
        {
            return current;
        }

        Vector3 candidate = current;
        candidate.Y += deltaY;
        if (!IntersectsSolid(candidate))
        {
            IsGrounded = false;
            return candidate;
        }

        if (deltaY <= 0f)
        {
            current.Y = ResolveGroundedEyeY(current);
            Vector3 velocity = LinearVelocity;
            velocity.Y = 0f;
            LinearVelocity = velocity;
            IsGrounded = true;
            return current;
        }

        Vector3 upwardBlockedVelocity = LinearVelocity;
        upwardBlockedVelocity.Y = 0f;
        LinearVelocity = upwardBlockedVelocity;
        return current;
    }

    private float ResolveGroundedEyeY(Vector3 current)
    {
        float minX = current.X - Radius + SkinWidth;
        float maxX = current.X + Radius - SkinWidth;
        float minZ = current.Z - Radius + SkinWidth;
        float maxZ = current.Z + Radius - SkinWidth;

        int startX = (int)MathF.Floor(minX);
        int endX = (int)MathF.Floor(maxX);
        int startZ = (int)MathF.Floor(minZ);
        int endZ = (int)MathF.Floor(maxZ);

        float feetY = current.Y - EyeHeightFromFeet;
        int sampleY = (int)MathF.Floor(feetY);
        float highestTop = float.MinValue;

        for (int z = startZ; z <= endZ; z++)
        {
            for (int x = startX; x <= endX; x++)
            {
                if (worldRuntime!.SampleBlockWorld(x, sampleY, z) is not BlockKind.Air and not BlockKind.Water)
                {
                    highestTop = MathF.Max(highestTop, sampleY + 1f);
                }
            }
        }

        return highestTop == float.MinValue
            ? current.Y
            : highestTop + EyeHeightFromFeet + SkinWidth;
    }

    private bool IntersectsSolid(Vector3 eyePosition)
    {
        if (worldRuntime is null)
        {
            return false;
        }

        float minX = eyePosition.X - Radius + SkinWidth;
        float maxX = eyePosition.X + Radius - SkinWidth;
        float minY = eyePosition.Y - EyeHeightFromFeet + SkinWidth;
        float maxY = eyePosition.Y + HeadHeightAboveEye - SkinWidth;
        float minZ = eyePosition.Z - Radius + SkinWidth;
        float maxZ = eyePosition.Z + Radius - SkinWidth;

        int startX = (int)MathF.Floor(minX);
        int endX = (int)MathF.Floor(maxX);
        int startY = (int)MathF.Floor(minY);
        int endY = (int)MathF.Floor(maxY);
        int startZ = (int)MathF.Floor(minZ);
        int endZ = (int)MathF.Floor(maxZ);

        for (int z = startZ; z <= endZ; z++)
        {
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    if (worldRuntime.SampleBlockWorld(x, y, z) is not BlockKind.Air and not BlockKind.Water)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private void UpdateTransform()
    {
        Entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(yaw, pitch, 0f);
    }
}
