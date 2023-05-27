using Injector.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Injector
{
    internal class Program
    {
        public static string ApplicationPath => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public static string DllFile => ApplicationPath + "\\Moon.dll";
        public static string Function => "DllMain";
        static void Main(string[] args)
        {
            Process[] processes = Process.GetProcessesByName("ac_client");
            if(processes.Length > 0)
            {
                // load dll
                Process process = processes.FirstOrDefault();
                Console.WriteLine($"Process : {process.ProcessName}, Id : {process.Id}");

                IntPtr handle = Imports.OpenProcess((int)(Imports.ProcessAccessFlags.CreateThread | Imports.ProcessAccessFlags.QueryInformation | Imports.ProcessAccessFlags.VirtualMemoryOperation | Imports.ProcessAccessFlags.VirtualMemoryWrite | Imports.ProcessAccessFlags.VirtualMemoryRead), false, process.Id);
                if (handle == IntPtr.Zero) { throw new Exception("Handle cannot created!"); }
                Console.WriteLine($"Handle created! (0x{handle.ToString("X")})");

                IntPtr loadLibrary = Imports.GetProcAddress(Imports.GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                if (loadLibrary == IntPtr.Zero) { throw new Exception("LoadLibrary function can't taken from memory!"); }
                Console.WriteLine($"LoadLibrary function address => 0x{loadLibrary.ToString("X")}");

                IntPtr allocatedMemory = Imports.VirtualAllocEx(handle, IntPtr.Zero, (uint)((DllFile.Length + 1) * Marshal.SizeOf(typeof(char))), Imports.MEM_COMMIT | Imports.MEM_RESERVE, Imports.PAGE_READWRITE);
                if(allocatedMemory == IntPtr.Zero) { throw new Exception("Memory cannot allocated!"); }
                Console.WriteLine($"Memory Allocated created! (0x{allocatedMemory.ToString("X")})");

                if (!Imports.WriteProcessMemory(handle, allocatedMemory, Encoding.Default.GetBytes(DllFile), (uint)((DllFile.Length + 1) * Marshal.SizeOf(typeof(char))), out UIntPtr bytesWritten))
                    throw new Exception("Cannot write at the memory!");
                Console.WriteLine($"Parameter has been writed into memory!");

                IntPtr threadAddress = Imports.CreateRemoteThread(handle, IntPtr.Zero, 0, loadLibrary, allocatedMemory, 0, IntPtr.Zero);
                Console.WriteLine($"LoadLibrary({DllFile}) (0x{threadAddress.ToString("X")}) has been calling...");

                Imports.WaitForSingleObject(threadAddress, (uint)Imports.INFINITE);
                Console.WriteLine($"LoadLibrary({DllFile}) (0x{threadAddress.ToString("X")}) has been called!");

                // List all modules
                IntPtr moduleAddress = IntPtr.Zero;
                foreach(ProcessModule module in process.Modules)
                {
                    if (module.FileName == DllFile)
                    {
                        Console.WriteLine($"Module : {module.ModuleName} 0x{module.BaseAddress.ToString("X")}, EntryPoint 0x{module.EntryPointAddress.ToString("X")} {module.FileName}");
                        moduleAddress = module.BaseAddress;
                    }
                }

                // Call function
                IntPtr loadedDll = Imports.LoadLibrary(DllFile);
                if (handle == IntPtr.Zero) { throw new Exception("Dll cannot loaded into own process"); }
                Console.WriteLine($"Dll loaded into own process! (0x{loadedDll.ToString("X")})");

                IntPtr functionAddress = Imports.GetProcAddress(loadedDll, Function);
                if (handle == IntPtr.Zero) { throw new Exception("Function has not been exported!"); }
                Console.WriteLine($"Function ({Function}) address => (0x{functionAddress.ToString("X")})");

                IntPtr difference = IntPtr.Subtract(functionAddress, (int)loadedDll);
                IntPtr functionThreadAddress = Imports.CreateRemoteThread(handle, IntPtr.Zero, 0, IntPtr.Add(moduleAddress, (int)difference), IntPtr.Zero, 0, IntPtr.Zero);
                Console.WriteLine($"Function address => 0x{IntPtr.Add(moduleAddress, (int)difference).ToString("X")}");
                //Console.WriteLine($"Function ({Function}) called! (0x{functionThreadAddress.ToString("X")})");
                //Imports.WaitForSingleObject(threadAddress, (uint)Imports.INFINITE);
            }
            else
            {
                Console.WriteLine($"Process not found!");
                Console.ReadLine();
            } 
        }
    }
}
