#if (ENABLE_MONO || ENABLE_IL2CPP)
namespace MyProject
{
#endif
    public class ExportContentData
    {
        public static void Export(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            {
                Console.WriteLine($"ExportContentData from : {path}");
                stream.Close();
            }
        }
    }
#if (ENABLE_MONO || ENABLE_IL2CPP)
}
#endif