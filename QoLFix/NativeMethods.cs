using System;
using System.Runtime.InteropServices;

namespace QoLFix
{
    public static class NativeMethods
    {
        public const long MB_OK = 0x00000000L;
        public const long MB_ICONWARNING = 0x00000030L;
        public const long MB_SYSTEMMODAL = 0x00001000L;

        [DllImport("user32.dll")]
        public static extern int MessageBox(IntPtr hWnd, string text, string caption, int options);
    }
}
