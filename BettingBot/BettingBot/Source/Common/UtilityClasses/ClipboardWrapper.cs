using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace BettingBot.Source.Common.UtilityClasses
{
    public static class ClipboardWrapper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetOpenClipboardWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(int hwnd, StringBuilder text, int count);

        private static string GetOpenClipboardWindowText()
        {
            var hwnd = GetOpenClipboardWindow();
            var sb = new StringBuilder(501);
            GetWindowText(hwnd.ToInt32(), sb, 500);
            return sb.ToString();
        }

        public static ActionStatus TrySetText(string text)
        {
            Exception lastEx = null;

            for (var i = 0; i < 10; i++)
            {
                try
                {
                    Clipboard.Clear();
                    Clipboard.SetDataObject(text);
                    return ActionStatus.Success();
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                }
            }

            var message = lastEx?.Message;
            message += Environment.NewLine;
            message += Environment.NewLine;
            message += "Problem:";
            message += Environment.NewLine;
            message += GetOpenClipboardWindowText();

            return new ActionStatus(ErrorCode.CannotSetClipboardText, message);
        }
    }
}
