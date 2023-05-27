using Moon.Classes.Drawings.SDKs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Moon.Classes.Drawings.Drawings;

namespace Moon.Classes.Drawings
{
    public static class Drawings
    {
        #region Variables
        public static Dictionary<GraphicsAPIs, string[]> APIs = new Dictionary<GraphicsAPIs, string[]>()
        {
            { GraphicsAPIs.OpenGL, new string[] { "opengl32.dll" } },
            { GraphicsAPIs.DirectX, new string[] { "d3d9.dll", "d3d11.dll" } },
        };
        public static string GraphicsLibrary { get; private set; }
        private static GraphicsAPIs graphicsApi = GraphicsAPIs.None;
        public static GraphicsAPIs Graphics {
            get
            {
                if(graphicsApi == GraphicsAPIs.None)
                    graphicsApi = GetGraphicsAPI();

                return graphicsApi;
            }
         }
        #endregion

        public static GraphicsAPIs GetGraphicsAPI()
        {
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                foreach (var apiData in APIs)
                    foreach (string library in apiData.Value)
                        if (module.ModuleName.ToLower() == library.ToLower())
                        {
                            GraphicsLibrary = module.ModuleName;
                            return apiData.Key;
                        }
            }

            return GraphicsAPIs.Software;
        }

        public static Size Viewport()
        {
            int[] viewport = new int[4];
            OpenGL.GetInteger(Target.VIEWPORT, viewport);
            return new Size(viewport[2], viewport[3]);
        }

        public static void Line(this GraphicsAPIs api, Vector2 src, Vector2 dst, Color color)
        {
            api.Line((int)src.X, (int)src.X, (int)dst.X, (int)dst.Y, color);
        }

        public static void Line(this GraphicsAPIs api, int x1, int y1, int x2, int y2, Color color)
        {
            if(api == GraphicsAPIs.OpenGL)
            {
                OpenGL.Begin(BeginMode.Lines);
                OpenGL.Color(color.R, color.G, color.B, color.A);
                OpenGL.Vertex(x1, y1);
                OpenGL.Vertex(x2, y2);
                OpenGL.End();
                OpenGL.Color(255f, 255f, 255f, 255f);
            }
            else if (api == GraphicsAPIs.DirectX)
            {
                // I will add later
            }
        }

        public static void Rectangle(this GraphicsAPIs api, int x1, int y1, int width, int height, Color color, bool filled = false)
        {
            if (api == GraphicsAPIs.OpenGL)
            {
                OpenGL.Begin(filled ? BeginMode.Quads : BeginMode.LineLoop);
                OpenGL.Color(color.R, color.G, color.B, color.A);
                OpenGL.Vertex(x1, y1 + height);
                OpenGL.Vertex(x1 + width, y1 + height);
                OpenGL.Vertex(x1 + width, y1);
                OpenGL.Vertex(x1, y1);
                OpenGL.End();
                OpenGL.Color(255f, 255f, 255f, 255f);
            }
            else if (api == GraphicsAPIs.DirectX)
            {
                // I will add later
            }
        }

        public static bool WorldToScreen(this GraphicsAPIs api, Vector3 src, out Vector2 dst, float[] viewMatrix)
        {
            dst = Vector2.Zero;

            Vector4 ClipCoords = new Vector4();
            ClipCoords.X = src.X * viewMatrix[0] + src.Y * viewMatrix[4] + src.Z * viewMatrix[8] + viewMatrix[12];
            ClipCoords.Y = src.X * viewMatrix[1] + src.Y * viewMatrix[5] + src.Z * viewMatrix[9] + viewMatrix[13];
            ClipCoords.Z = src.X * viewMatrix[2] + src.Y * viewMatrix[6] + src.Z * viewMatrix[10] + viewMatrix[14];
            ClipCoords.W = src.X * viewMatrix[3] + src.Y * viewMatrix[7] + src.Z * viewMatrix[11] + viewMatrix[15];

            if (ClipCoords.W < 0.1f)
                return false;

            Vector3 NDC;
            NDC.X = ClipCoords.X / ClipCoords.W;
            NDC.Y = ClipCoords.Y / ClipCoords.W;
            NDC.Z = ClipCoords.Z / ClipCoords.W;

            Size viewport = Viewport();
            dst.X = (viewport.Width / 2 * NDC.X) + (NDC.X + viewport.Width / 2);
            dst.Y = -(viewport.Height / 2 * NDC.Y) + (NDC.Y + viewport.Height / 2);
            return true;
        }

        #region Structs

        public enum GraphicsAPIs
        {
            None,
            Software,
            DirectX,
            OpenGL
        }

        #endregion
    }
}
