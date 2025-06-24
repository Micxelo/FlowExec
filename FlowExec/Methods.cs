using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FlowExec
{
    public static class IconHelper
    {
        // 导入 Windows API
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbSizeFileInfo,
            uint uFlags
        );

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // 标志常量
        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        // 文件属性常量
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        // 结构定义
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        /// <summary>
        /// 获取文件或文件夹的图标。
        /// </summary>
        /// <param name="path">文件/文件夹路径。</param>
        /// <param name="isSmall">是否小图标。</param>
        /// <param name="handleInexistent">对于不存在的对象，是否根据扩展名获取图标。</param>
        /// <returns>ImageSource 对象。</returns>
        public static (bool, ImageSource) GetIcon(string path, bool isSmall = false, bool handleInexistent = false)
        {
            try
            {
                uint flags = SHGFI_ICON | (isSmall ? SHGFI_SMALLICON : SHGFI_LARGEICON);
                uint attributes = File.Exists(path)
                    ? FILE_ATTRIBUTE_NORMAL
                    : (Directory.Exists(path) ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL);

                // 处理不存在的路径
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    if (!handleInexistent)
                        return (false, CreateDefaultIcon());

                    flags |= SHGFI_USEFILEATTRIBUTES;
                    attributes = Path.HasExtension(path)
                        ? FILE_ATTRIBUTE_NORMAL
                        : FILE_ATTRIBUTE_DIRECTORY;
                }

                SHFILEINFO shfi = new SHFILEINFO();
                IntPtr result = SHGetFileInfo(path, attributes, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

                if (shfi.hIcon == IntPtr.Zero)
                    throw new FileNotFoundException("无法获取图标", path);

                // 转换为 WPF 图像源
                ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                    shfi.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                DestroyIcon(shfi.hIcon); // 释放图标资源
                return (true, imageSource);
            }
            catch
            {
                // 返回空图标或默认图标
                return (false, CreateDefaultIcon());
            }
        }

        // 创建默认图标（错误时使用）
        private static ImageSource CreateDefaultIcon()
        {
            // 创建 1x1 透明位图作为默认图标
            var wb = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
            wb.Lock();
            wb.Unlock();
            return wb;
        }
    }

    public static class Dwm
    {
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern long DwmSetWindowAttribute(
            IntPtr hwnd, 
            DWMWINDOWATTRIBUTE attribute,
            ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
            uint cbAttribute
            );

        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }

        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }
    }

    public static class CommandLine
    {
        public static List<string> Parse(string input)
        {
            List<string> tokens = new List<string>();
            StringBuilder currentToken = new StringBuilder();
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool escapeNext = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (escapeNext)
                {
                    // 转义字符：直接添加下一个字符（无论是什么）
                    currentToken.Append(c);
                    escapeNext = false;
                    continue;
                }

                switch (c)
                {
                    case '\\':
                        // 设置转义标志（下一个字符将被直接添加）
                        escapeNext = true;
                        break;

                    case '\'':
                        if (!inDoubleQuote)
                        {
                            inSingleQuote = !inSingleQuote; // 切换单引号状态
                        }
                        else
                        {
                            currentToken.Append(c); // 双引号内的单引号作为普通字符
                        }
                        break;

                    case '"':
                        if (!inSingleQuote)
                        {
                            inDoubleQuote = !inDoubleQuote; // 切换双引号状态
                        }
                        else
                        {
                            currentToken.Append(c); // 单引号内的双引号作为普通字符
                        }
                        break;

                    case ' ':
                        if (inSingleQuote || inDoubleQuote)
                        {
                            // 引号内的空格保留
                            currentToken.Append(c);
                        }
                        else if (currentToken.Length > 0)
                        {
                            // 结束当前令牌
                            tokens.Add(currentToken.ToString());
                            currentToken.Clear();
                        }
                        break;

                    default:
                        // 所有其他字符直接添加到当前令牌
                        currentToken.Append(c);
                        break;
                }
            }

            // 处理最后一个令牌
            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString());
            }

            return tokens;
        }
    }
}
