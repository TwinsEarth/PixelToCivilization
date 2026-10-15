using System.Text;

namespace PxC.Infra.Save
{
    /// <summary>FNV-1a 64 位校验（存档完整性校验）。</summary>
    public static class FnvChecksum
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        public static ulong Hash64(string text)
        {
            ulong hash = OffsetBasis;
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= Prime;
            }
            return hash;
        }

        public static string Hash64Hex(string text) => Hash64(text).ToString("X16");
    }
}
