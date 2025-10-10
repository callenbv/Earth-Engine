

using Engine.Core.Systems;
using Microsoft.Xna.Framework;

/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Camera.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------


namespace Engine.Core
{
    /// <summary>
    /// Define if camera is in 3D or 2D
    /// </summary>
    public enum CameraMode
    {
        Camera2D,
        Camera3D
    }

    /// <summary>
    /// Represents the camera in the game, handling position, zoom, rotation, and target tracking.
    /// </summary>
    public static class GameCamera
    {
        public static CameraMode Mode = CameraMode.Camera2D;

        public static Matrix GetViewMatrix()
        {
            switch (Mode)
            {
                case CameraMode.Camera3D:
                    return Camera3D.Main.GetViewMatrix();

                default:
                    return Camera.Main.GetViewMatrix(EngineContext.InternalWidth, EngineContext.InternalHeight);
            }
        }
        public static Matrix GetProjectionMatrix(int width, int height)
        {
            switch (Mode)
            {
                case CameraMode.Camera3D:
                    return Camera3D.Main.GetProjectionMatrix(width,height);

                default:
                    return Matrix.Identity;
            }
        }
    }
} 
