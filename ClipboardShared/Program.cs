using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Text;

namespace ClipboardShared
{
    // https://stackoverflow.com/questions/2226920/how-do-i-monitor-clipboard-content-changes-in-c
    internal static class NativeMethods
    {
        // See http://msdn.microsoft.com/en-us/library/ms649021%28v=vs.85%29.aspx
        public const int WM_CLIPBOARDUPDATE = 0x031D;

        public static IntPtr HWND_MESSAGE = new IntPtr(-3);

        // See http://msdn.microsoft.com/en-us/library/ms632599%28VS.85%29.aspx#message_only
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        // See http://msdn.microsoft.com/en-us/library/ms633541%28v=vs.85%29.aspx
        // See http://msdn.microsoft.com/en-us/library/ms649033%28VS.85%29.aspx
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
    }

    internal class Program
    {
        private class NotificationForm : Form
        {
            public NotificationForm()
            {
                NativeMethods.SetParent(Handle, NativeMethods.HWND_MESSAGE);
                NativeMethods.AddClipboardFormatListener(Handle);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
                {
                    OnClipboardUpdate();
                }
                base.WndProc(ref m);
            }
        }

        private const string c_TextFile = "text.txt";
        private const string c_ImageFile = "img.png";

        private static event Action ClipboardUpdate;

        private static string directory;
        private static string textPath;
        private static string imgPath;

        private static FileSystemWatcher watcher = new FileSystemWatcher();
        private static int watcherCounter = 0;
        private static int clipboardImgCounter = 0;

        private static void OnClipboardUpdate()
        {
            ClipboardUpdate?.Invoke();
        }

        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: ClipShared can't run with no specified path");
                return;
            }

            directory = args[0];
            textPath = Path.Combine(directory, c_TextFile);
            imgPath = Path.Combine(directory, c_ImageFile);
            if (!File.Exists(textPath))
            {
                File.Create(textPath).Close();
            }

            ClipboardUpdate += ClipboardChangeHandler;

            watcher.Path = directory;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += FileChangeHandler;
            watcher.EnableRaisingEvents = true;

            Application.Run(new NotificationForm());
        }

        private static void ClipboardChangeHandler()
        {
            watcher.EnableRaisingEvents = false; // Don't invoke file changed events for our own change
            Console.ForegroundColor = ConsoleColor.Green;
            var data = Clipboard.GetDataObject();
            if (data.GetDataPresent(DataFormats.UnicodeText))
            {
                var contents = data.GetData(DataFormats.UnicodeText) as string;
                File.WriteAllText(textPath, contents);
                Console.WriteLine("[Out] text: " + contents);
            }
            else if (data.GetDataPresent(DataFormats.Bitmap))
            {
                clipboardImgCounter++;
                if (clipboardImgCounter == 2)
                {
                    using (MemoryStream pngStream = data.GetData("PNG") as MemoryStream)
                    {
                        if (pngStream != null)
                        {
                            if (File.Exists(imgPath))
                            {
                                File.Delete(imgPath);
                            }
                            using (FileStream fileStream = new FileStream(imgPath, FileMode.Create))
                            {
                                pngStream.WriteTo(fileStream);
                            }
                        }
                    }
                    Console.WriteLine("[Out] screenshot: " + imgPath);
                    clipboardImgCounter = 0;
                }
            }
            watcher.EnableRaisingEvents = true;
        }

        private static void FileChangeHandler(Object src, FileSystemEventArgs e)
        {
            switch (e.Name.ToLower())
            {
                case c_TextFile:
                    // This event gets fired twice, on start modifying and on finish modifying
                    watcherCounter++;
                    if (watcherCounter == 2)
                    {
                        string text = File.ReadAllText(e.FullPath);
                        if (!string.IsNullOrEmpty(text))
                        {
                            CallClipboardFun(
                                "[In] text: " + text,
                                () => Clipboard.SetText(text)
                                );
                        }
                        watcherCounter = 0;
                    }
                    break;

                case c_ImageFile:
                    using (Bitmap bm = new Bitmap(e.FullPath))
                    {
                        CallClipboardFun(
                            "[In] screenshot: " + e.FullPath,
                            () => Clipboard.SetImage(bm));
                    }
                    break;
            }
        }

        private static void CallClipboardFun(string msg, Action action)
        {
            // unlisten first, so we don't get invoked by our own change
            ClipboardUpdate = null;

            Thread thread = new Thread(action.Invoke);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(msg);

            ClipboardUpdate += ClipboardChangeHandler;
        }
    }
}