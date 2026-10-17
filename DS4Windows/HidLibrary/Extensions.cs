using System.Text;

namespace DS4Windows
{
    public static class Extensions
    {
        public static string ToUTF8String(this byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return string.Empty;
            var value = Encoding.UTF8.GetString(buffer);
            int nullIndex = value.IndexOf((char)0);
            return nullIndex >= 0 ? value.Remove(nullIndex) : value;
        }

        public static string ToUTF16String(this byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return string.Empty;
            var value = Encoding.Unicode.GetString(buffer);
            int nullIndex = value.IndexOf((char)0);
            return nullIndex >= 0 ? value.Remove(nullIndex) : value;
        }
    }
}