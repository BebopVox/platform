﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using CEFInjector.DirectXHook;
using CEFInjector.DirectXHook.Hook;
using CEFInjector.DirectXHook.Interface;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace CEFInjector
{
    class Program
    {
        static unsafe void Main(string[] args)
        {
            Console.WriteLine("Starting...");

            Console.WriteLine("Attaching to process...");

            AttachProcess("GTA5.exe", Direct3DVersion.Direct3D11);
            
            Console.WriteLine("Starting main loop...");

            byte[] bitmapBytes = new byte[0];
            
            Mutex mutex;

            try
            {
                mutex = Mutex.OpenExisting("GTANETWORKCEFMUTEX");
            }
            catch
            {
                mutex = new Mutex(false, "GTANETWORKCEFMUTEX");
            }

            
            const int FPS = 1;
            const int waitTime = 1000/FPS;
            /*
            var cirno = new Bitmap(@"A:\Dropbox\stuff\Reaction Images\cirno.png");
            Bitmap testBitmap = new Bitmap(cirno.Width, cirno.Height, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(testBitmap))
            {
                graphics.DrawImage(cirno, new Point(0, 0));
            }

            bitmapBytes = ScreenshotExtensions.BitmapToByteArray(testBitmap);
            */
            bool continueReading = true;
            
            Thread t = new Thread((ThreadStart) delegate
            {
                while (continueReading)
                {
                    int width = 0, height = 0;

                    //width = testBitmap.Width;
                    //height = testBitmap.Height;

                    //_captureProcess.CaptureInterface.UpdateMainBitmap(bitmapBytes, width, height);

                    
                    if (mutex.WaitOne())
                    {
                        using (var mmf = MemoryMappedFile.OpenExisting("GTANETWORKBITMAPSCREEN",
                                MemoryMappedFileRights.FullControl))
                        {
                            using (var accessor = mmf.CreateViewStream())
                            using (var binReader = new BinaryReader(accessor))
                            {
                                var bitmapLen = binReader.ReadInt32();
                                width = binReader.ReadInt32();
                                height = binReader.ReadInt32();

                                bitmapBytes = new byte[bitmapLen];
                                accessor.SafeMemoryMappedViewHandle.ReadArray(0, bitmapBytes, 0, bitmapLen);
                            }
                        }

                        mutex.ReleaseMutex();
                    }


                    if (bitmapBytes.Length > 0)
                        _captureProcess.CaptureInterface.UpdateMainBitmap(bitmapBytes, width, height);
                    Thread.Sleep(waitTime);
                }
            });
            t.IsBackground = true;
            t.Start();

            Console.ReadLine();

            continueReading = false;

            exit:
            Console.WriteLine("Detaching...");
            HookManager.RemoveHookedProcess(_captureProcess.Process.Id);
            _captureProcess.CaptureInterface.Disconnect();
            _captureProcess = null;
        }

        static int processId = 0;
        static Process _process;
        static CaptureProcess _captureProcess;
        static void AttachProcess(string exe, Direct3DVersion direct3DVersion)
        {
            string exeName = Path.GetFileNameWithoutExtension(exe);

            Process[] processes = Process.GetProcessesByName(exeName);
            foreach (Process process in processes)
            {
                // Simply attach to the first one found.

                // If the process doesn't have a mainwindowhandle yet, skip it (we need to be able to get the hwnd to set foreground etc)
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                // Skip if the process is already hooked (and we want to hook multiple applications)
                if (HookManager.IsHooked(process.Id))
                {
                    continue;
                }

                

                CaptureConfig cc = new CaptureConfig()
                {
                    Direct3DVersion = direct3DVersion,
                    ShowOverlay = true,
                };

                processId = process.Id;
                _process = process;

                var captureInterface = new CaptureInterface();
                captureInterface.RemoteMessage += new MessageReceivedEvent(CaptureInterface_RemoteMessage);
                _captureProcess = new CaptureProcess(process, cc, captureInterface);

                break;
            }
        }

        static void CaptureInterface_RemoteMessage(MessageReceivedEventArgs message)
        {
            Console.WriteLine(message);
        }
    }
}