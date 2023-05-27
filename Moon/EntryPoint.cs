using Detours;
using Moon.Classes;
using Moon.Classes.Drawings;
using Moon.Classes.Drawings.SDKs;
using Moon.Classes.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace Moon
{
    public class EntryPoint
    {
        private static HookManager hooks = new HookManager();
        [DllExport("DllMain", CallingConvention.Cdecl)]
        public static void DllMain()
        {
            Imports.AllocConsole();
            Console.WriteLine("Moon->EntryPoint->DllMain called!");
            IntPtr openGl32 = Imports.GetModuleHandle("opengl32.dll");
            Console.WriteLine($"opengl32.dll 0x{openGl32.ToString("X")}!");
            IntPtr wglGetSwapBuffersAddress = Imports.GetProcAddress(openGl32, "wglSwapBuffers");
            Console.WriteLine($"wglGetSwapBuffersAddress (0x{wglGetSwapBuffersAddress.ToString("X")}) function !");

            // Hook SwapBuffers
            hooks.Add(Marshal.GetDelegateForFunctionPointer<Delegate_wglSwapBuffers>(wglGetSwapBuffersAddress), hook_SwapBuffers, "SwapBuffers");
            hooks.InstallAll();
        }

        // WndProc
        public delegate IntPtr Delegate_WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        public static Delegate_WndProc original_WndProc;
        public static Delegate_WndProc hook_WndProc = h_WndProc;

        // SwapBuffers
        public delegate bool Delegate_wglSwapBuffers([In] IntPtr hDc);
        public static Delegate_wglSwapBuffers original_SwapBuffers;
        public static Delegate_wglSwapBuffers hook_SwapBuffers = h_wglSwapBuffers;

        private static IntPtr Context;
        private static bool IsContextCreated = false;
        private static int[] Viewport = new int[4];
        public static bool h_wglSwapBuffers([In] IntPtr hDc)
        {
            // Get Current Context
            IntPtr OldContext = OpenGL.GetCurrentContext();

            // Get Old Viewport
            int[] CurrentViewport = new int[4];
            OpenGL.GetInteger(Target.VIEWPORT, CurrentViewport);

            // Initalize OpenGL Context
            if (!IsContextCreated)
            {
                // Create Context
                Context = OpenGL.wglCreateContext(hDc);
                OpenGL.wglMakeCurrent(hDc, Context);

                // Setup Context for 2D Drawings
                OpenGL.MatrixMode(MatrixMode.Projection);
                OpenGL.LoadIdentity();

                OpenGL.GetInteger(Target.VIEWPORT, Viewport);
                OpenGL.glOrtho(0.0, Viewport[2], Viewport[3], 0.0, 1.0, -1.0);

                OpenGL.MatrixMode(MatrixMode.ModelView);
                OpenGL.ClearColor(0, 0, 0, 1.0f);

                IsContextCreated = true;
            }
            else
                OpenGL.wglMakeCurrent(hDc, Context);

            // Reset Context when viewport changed
            if (Viewport[2] != CurrentViewport[2] && Viewport[3] != CurrentViewport[3])
            {
                Viewport = CurrentViewport;
                IsContextCreated = false;
            }

            Drawings.Graphics.Rectangle(50, 50, 60, 60, Color.Red);
            Drawings.Graphics.Rectangle(60, 60, 40, 40, Color.Blue, true);

            // Set Current Context to Old One
            OpenGL.wglMakeCurrent(hDc, OldContext);
            return (bool)hooks["SwapBuffers"].CallOriginal(hDc);
        }

        public static IntPtr h_WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            Console.WriteLine($"msg : 0x{msg.ToString("X")}");
            return original_WndProc(hWnd, msg, wParam, lParam);
        }
    }
}
