using System.Diagnostics;
using System.Security.Cryptography;

namespace SCPlus
{
    internal class ResourceHandler
    {
        public static string? LoadEmbeddedJSON(string name)
        {
            name = resourcesFolder + name;

            string? result = null;

            Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (stream != null)
            {
                StreamReader reader = new StreamReader(stream);
                result = reader.ReadToEnd();
            }

            return result;
        }

        public static void ExtractFolderFromResources(string targetFolder, string resourceFolder, bool overwrite = false)
        {

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (Directory.Exists(targetFolder) && !overwrite)
                return;

            Directory.CreateDirectory(targetFolder);

            var assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();

            foreach (var resourceName in resources)
            {
                // Only extract resources that are inside the given folder
                if (!resourceName.Contains(resourceFolder + ".")) continue;

                // Strip the namespace and resource folder prefix
                string relativePath = resourceName.Substring(resourceName.IndexOf(resourceFolder + ".") + resourceFolder.Length + 1);

                // Find the last '.' which separates the filename from its extension
                int lastDot = relativePath.LastIndexOf('.');
                if (lastDot == -1) continue;

                string nameWithoutExtension = relativePath.Substring(0, lastDot);
                string extension = relativePath.Substring(lastDot + 1);

                // Convert only the name portion (not extension) into directories
                string filePath = Path.Combine(targetFolder, nameWithoutExtension.Replace('.', Path.DirectorySeparatorChar) + "." + extension);

                string dir = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (File.Exists(filePath) && AreFilesIdentical(resourceStream, filePath))
                        continue;

                    resourceStream.Position = 0; // Reset stream if it was read

                    using (FileStream outFile = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        resourceStream.CopyTo(outFile);
                    }
                }
            }

            stopwatch.Stop();
            Log(CC.Blue, $"SC+ Data extraction pass: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");
        }

        private static bool AreFilesIdentical(Stream resourceStream, string filePath)
        {
            /*
            if (!File.Exists(filePath)) return false;

            using (FileStream existingFile = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (resourceStream.Length != existingFile.Length)
                    return false;

                const int bufferSize = 8192;
                byte[] buffer1 = new byte[bufferSize];
                byte[] buffer2 = new byte[bufferSize];

                int bytesRead1, bytesRead2;
                do
                {
                    bytesRead1 = resourceStream.Read(buffer1, 0, bufferSize);
                    bytesRead2 = existingFile.Read(buffer2, 0, bufferSize);

                    if (bytesRead1 != bytesRead2 || !buffer1.Take(bytesRead1).SequenceEqual(buffer2.Take(bytesRead2)))
                        return false;

                } while (bytesRead1 > 0);
            }

            return true;
            */
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash1, hash2;

                // Compute hash for the resource
                hash1 = sha.ComputeHash(resourceStream);
                resourceStream.Position = 0;

                // Compute hash for the file
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    hash2 = sha.ComputeHash(fileStream);
                }

                return hash1.SequenceEqual(hash2);
            }
        }
    }
}
