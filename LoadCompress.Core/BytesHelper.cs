namespace LoadCompress.Core
{
    public static class BytesHelper
    {
        public static int Mb(this int mb)
        {
            return mb.Kb() * 1024;
        }

        public static int Kb(this int kb)
        {
            return kb * 1024;
        }
    }
}
