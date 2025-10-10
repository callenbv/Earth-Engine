/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Camera3D.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Default 3D camera used by runtime for MeshRenderer.          
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Engine.Core.Game;

namespace Engine.Core.Systems
{
    public class Camera3D
    {
        private static Camera3D? _main;
        public static Camera3D Main => _main ??= new Camera3D();

        public Vector3 Position { get; set; } = new Vector3(0f, 0f, 5f);
        public Vector3 Target { get; set; } = Vector3.Zero;
        public Vector3 Up { get; set; } = Vector3.Up;
        public float FieldOfViewDegrees { get; set; } = 60f;
        public float NearPlane { get; set; } = 0.01f;
        public float FarPlane { get; set; } = 1000f;
        
        /// <summary>
        /// 3D view matrix
        /// </summary>
        /// <returns></returns>
        public Matrix GetViewMatrix()
        {
            return Matrix.CreateLookAt(Position, Target, Up);
        }

        /// <summary>
        /// Projection matrix in 3D
        /// </summary>
        /// <param name="viewportWidth"></param>
        /// <param name="viewportHeight"></param>
        /// <returns></returns>
        public Matrix GetProjectionMatrix(int viewportWidth, int viewportHeight)
        {
            float aspect = (viewportWidth <= 0 || viewportHeight <= 0) ? 16f / 9f : (float)viewportWidth / viewportHeight;
            return Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(FieldOfViewDegrees), aspect, NearPlane, FarPlane);
        }
    }
}


