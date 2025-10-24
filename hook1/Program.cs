using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using System.Xml;
using System.Runtime.CompilerServices;

namespace hook1
{
    internal class Program
    {
        struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }
        [DllImport("user32.dll")]
        static extern IntPtr DispatchMessage(ref MSG lpMsg);
        [DllImport("user32.dll")]
        static extern bool TranslateMessage(ref MSG lpMsg);
        [DllImport("user32.dll")]
        static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;
        }
        static void Main(string[] args)
        {
            
            KeyBoardHook hook = new KeyBoardHook();
            hook.installHook();
            MSG msg;
            while (GetMessage(out msg, IntPtr.Zero, 0, 0))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
            hook.uninstallHook();
        }
    }
    public class Bond
    {
        public int vcode;
        public char target,Starget;
    }

    public class KeyBoardHook
    {
        public int KEY_LL = 13;
        public int WM_KEYDOWN = 0x0100;
        public int WM_KEYUP = 0x0101;
        public int protector = 0;
        public IntPtr _hookID;
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        public LowLevelKeyboardProc proc;
        public InputSimulator simulator = new InputSimulator();
        static String xmlDoc="OPT.xml";
        static XmlDocument xmlDocument = new XmlDocument();
        static Dictionary<int, Bond> translate;
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
        int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        public bool Shift()
        {
            return (GetAsyncKeyState(0xA0) & 0x8000) != 0 ||
              (GetAsyncKeyState(0xA1) & 0x8000) != 0;

        }
        public void initialXml()
        {
            Console.WriteLine("Reading");
            translate = new Dictionary<int, Bond>();
            try
            {
                xmlDocument.Load(xmlDoc);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in reading");
            }
            XmlNode root= xmlDocument.DocumentElement;
            if (root.SelectNodes("Bond").Count == 0) Console.WriteLine("bonds not found");
            foreach(XmlNode bond in root.SelectNodes("Bond"))
            {
                Bond b=new Bond();
                b.vcode =Convert.ToInt32( bond.SelectSingleNode("vcode")?.InnerText);
                b.target = bond.SelectSingleNode("target").InnerText[0];
                b.Starget = bond.SelectSingleNode("Starget").InnerText[0];
                Console.WriteLine(b.vcode.ToString() + "|" + b.target + "|" + b.Starget);
                translate.Add(b.vcode,b);
            }
            Console.WriteLine("Finished");
        }
        public void installHook() {
            initialXml();
            proc = Ehandler;
            using (var curModule = Process.GetCurrentProcess().MainModule)
            {
                IntPtr moduleHandle = GetModuleHandle(curModule.ModuleName);
                _hookID = SetWindowsHookEx(KEY_LL, proc, moduleHandle, 0);
            }
        }
        public void uninstallHook() {
            UnhookWindowsHookEx(_hookID);
        }
        public IntPtr Ehandler(int nCode, IntPtr wParam, IntPtr lParam)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Console.WriteLine("Get:"+vkCode.ToString());
            Bond b;
            bool succTyping = false;
            translate.TryGetValue(vkCode,out b);
            if (b!=null&&protector<=1000&& (int)wParam == 256)
            {
                if (Shift() && b.Starget != '\0')
                {
                    simulator.Keyboard.TextEntry(b.Starget);
                    succTyping = true;
                }
                else
                {
                    if (b.target != '\0')
                    {
                        simulator.Keyboard.TextEntry(b.target);
                        succTyping = true;
                    }
                }
                if (succTyping)
                {
                    protector++;
                    return (IntPtr)1;
                }
                else
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        public void sendKey(byte key)
        {
            keybd_event((byte)key, 0, 0, UIntPtr.Zero);
            keybd_event((byte)key, 0, 0x0002, UIntPtr.Zero);
        }

    }

}
