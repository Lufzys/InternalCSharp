using Moon.Classes.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Moon.Classes
{
    internal class Memory
    {
        public unsafe static T Read<T>(IntPtr address) where T : struct
        {
            //return Marshal.PtrToStructure<T>(address);
            return *(T*)(address);
        }

        public unsafe static void Write<T>(IntPtr address, T value) where T : struct
        {
            *(T*)(address) = value;
        }

        public static byte[] Read(IntPtr address, int size)
        {
            byte[] destination = new byte[size];
            Marshal.Copy(address, destination, 0, destination.Length);
            return destination;
        }

        public static float[] ReadMatrix<T>(IntPtr address, int matrixSize)
        {
            byte[] buffer = Read(address, Marshal.SizeOf(typeof(T)) * matrixSize);
            return ConvertToFloatArray(buffer);
        }

        public static void Write(IntPtr address, byte[] data)
        {
            Imports.VirtualProtect(address, data.Length, 0x40, out uint old);
            Marshal.Copy(data, 0, address, data.Length);
            Imports.VirtualProtect(address, data.Length, old, out uint _);
        }

        public static float[] ConvertToFloatArray(byte[] bytes)
        {
            if (bytes.Length % 4 != 0) throw new ArgumentException();

            float[] floats = new float[bytes.Length / 4];

            for (int i = 0; i < floats.Length; i++) floats[i] = BitConverter.ToSingle(bytes, i * 4);

            return floats;
        }
    }
}
