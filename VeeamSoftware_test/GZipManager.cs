using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace VeeamSoftware_test.Gzip
{
    public interface IGZipManager
    {
        void Compress(string input, string output);
        void Decompress(string input, string output);
    }

    public class GZipManager : IGZipManager
    {
        public void  Compress(string input, string output)
        {

            using (FileStream outFile = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (GZipStream gZipStream = new GZipStream(outFile, CompressionMode.Compress))
                {
                    string dir = "";
                    foreach (string sFilePath in getPathFiles(input, out dir))
                    {
                        GzipDriver.CompressFile(dir, sFilePath, gZipStream);
                    }
                }
            }
        }

        public void  Decompress(string input, string output)
        {
            using (FileStream inFile = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (GZipStream zipStream = new GZipStream(inFile, CompressionMode.Decompress, true))
                {
                    while (GzipDriver.DecompressFile(output, zipStream));
                }
            }
        }

        private List<string> getPathFiles(string inputDir, out string sDir)
        {

            if (File.Exists(inputDir))
            {
                sDir = Path.GetDirectoryName(inputDir);
                return new List<string> {Path.GetFileName(inputDir)};
            }

            List<string> result = new List<string>();

            string[] sFiles = Directory.GetFiles(inputDir, "*.*", SearchOption.AllDirectories);
            int iDirLen = inputDir.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? inputDir.Length
                : inputDir.Length + 1;
            foreach (string sFilePath in sFiles)
            {
                result.Add(sFilePath.Substring(iDirLen));
            }
            sDir = inputDir;

            return result;
        }
    }
}
