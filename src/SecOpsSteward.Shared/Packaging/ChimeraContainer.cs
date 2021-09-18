using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using SecOpsSteward.Shared.Packaging.Metadata;
using SecOpsSteward.Shared.Packaging.Wrappers;

namespace SecOpsSteward.Shared.Packaging
{
    public class ChimeraContainer : IDisposable
    {
        private static readonly byte[] CHIMERA_MAGIC =
        {
            Convert.ToByte('L'), // Lion
            Convert.ToByte('G'), // Goat
            Convert.ToByte('S'), // Serpent
            0x80 // AT
        };

        private readonly bool _disposeStreamOnClose;
        private bool disposedValue;

        /// <summary>
        ///     Wrap an existing Container Stream
        /// </summary>
        /// <param name="stream">Existing stream</param>
        /// <param name="disposeStreamOnClose">If the stream should be closed when this is disposed</param>
        public ChimeraContainer(Stream stream, bool disposeStreamOnClose = true)
        {
            ContainerStream = stream;
            _disposeStreamOnClose = disposeStreamOnClose;

            // calculate file hash for duplicate-checking
            stream.Seek(0, SeekOrigin.Begin);
            var hash = ChimeraSharedHelpers.GetStreamHash(stream);
            var hashName = BitConverter.ToString(hash).Replace("-", "");
            stream.Seek(0, SeekOrigin.Begin);

            if (Environment.GetEnvironmentVariable("TEMPDIR") != null)
                ExtractedPath = Path.Combine(Environment.GetEnvironmentVariable("TEMPDIR"), hashName);
            else
                ExtractedPath = Path.Combine(Path.GetTempPath(), hashName);

            if (!Directory.Exists(ExtractedPath) || Directory.EnumerateFiles(ExtractedPath).Count() == 0)
            {
                Directory.CreateDirectory(ExtractedPath);
                new ZipArchive(stream).ExtractToDirectory(ExtractedPath, true);
            }

            foreach (var candidate in Directory.GetFiles(ExtractedPath, "*.dll"))
            {
                ContainerWrapper wrapper;
                try
                {
                    wrapper = new ContainerWrapper(candidate);
                }
                catch
                {
                    continue;
                }

                if (!wrapper.IsValid()) continue;

                Wrapper = wrapper;
                return;
            }
        }

        /// <summary>
        ///     Stream of the entire Container, including metadata
        /// </summary>
        public Stream ContainerStream { get; set; }

        /// <summary>
        ///     Path which the archive has been extracted to
        /// </summary>
        public string ExtractedPath { get; }

        /// <summary>
        ///     Wrapper to access functions inside the container
        /// </summary>
        public ContainerWrapper Wrapper { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Create a new Container from a folder containing a library of plugins
        /// </summary>
        /// <param name="folder">Folder containing plugin</param>
        /// <returns>New container</returns>
        public static ChimeraContainer CreateFromFolder(string folder)
        {
            var tempFile = Path.GetTempFileName();
            File.Delete(tempFile);
            ZipFile.CreateFromDirectory(folder, tempFile);

            var ms = new MemoryStream();
            using (var fs = new FileStream(tempFile, FileMode.Open))
            {
                fs.CopyTo(ms);
            }

            File.Delete(tempFile);
            ms.Seek(0, SeekOrigin.Begin);
            return CreateFromArchiveStream(ms);
        }

        /// <summary>
        ///     Create a new Container from a ZIP archive Stream
        /// </summary>
        /// <param name="p">ZIP archive Stream containing plugin</param>
        /// <returns>New package</returns>
        public static ChimeraContainer CreateFromArchiveStream(Stream p)
        {
            var container = new ChimeraContainer(p);

            var containerMetadata = new ContainerMetadata(container);
            containerMetadata.PluginsMetadata.AddRange(
                container.Wrapper.Plugins.Select(p => new PluginMetadata(p.Value.Emit())));
            containerMetadata.ServicesMetadata.AddRange(
                container.Wrapper.Services.Select(s => new ServiceMetadata(s.Value.Emit())));

            if (containerMetadata.PluginsMetadata.Select(p => p.PluginId).Union(
                    containerMetadata.ServicesMetadata.Select(s => s.ServiceId))
                .Select(id => id.ContainerId).Distinct().Count() > 1)
                throw new Exception("Container naming mismatch!");

            containerMetadata.ContainerId = containerMetadata.PluginsMetadata.First().PluginId
                .GetComponents(PackageIdentifierComponents.Container);

            container.WriteMetadata(containerMetadata); // pre-hash
            containerMetadata.PackageContentHash = container.GetPackageContentHash();
            container.WriteMetadata(containerMetadata); // post-hash
            return container;
        }

        /// <summary>
        ///     Create a new Package from a path
        /// </summary>
        /// <param name="path">Path to plugin</param>
        /// <returns>New package</returns>
        public static ChimeraContainer CreateFromPath(string path)
        {
            if (Directory.Exists(path))
            {
                return CreateFromFolder(path);
            }

            var fs = new FileStream(path, FileMode.Open);
            return CreateFromArchiveStream(fs);
        }

        /// <summary>
        ///     Write the package to a file
        /// </summary>
        /// <param name="filename">Path to file on disk</param>
        /// <returns>Path to written file</returns>
        public string WritePackageFile(string filename = null)
        {
            using (var fs = new FileStream(filename, FileMode.Create))
            {
                ContainerStream.Seek(0, SeekOrigin.Begin);
                ContainerStream.CopyTo(fs);
            }

            return filename;
        }

        /// <summary>
        ///     Get just the Package's content stream from this Stream
        /// </summary>
        /// <returns>Package content in a Stream</returns>
        public Stream GetPackageContentStream()
        {
            var blockSize = GetMetadataBlockSize(ContainerStream);
            var ms = new MemoryStream();
            ContainerStream.CopyTo(ms);
            if (blockSize > 0)
                ms.SetLength(ContainerStream.Length - blockSize - 8);
            return ms;
        }

        /// <summary>
        ///     Extract the metadata for this Package from the Stream
        /// </summary>
        /// <returns></returns>
        public ContainerMetadata GetMetadata()
        {
            var blockSize = GetMetadataBlockSize(ContainerStream);
            ContainerStream.Seek(-8 - blockSize, SeekOrigin.End);
            var metadataBlock = new byte[blockSize];
            ContainerStream.Read(metadataBlock, 0, metadataBlock.Length);
            return ChimeraSharedHelpers.GetFromSerializedBytes<ContainerMetadata>(metadataBlock);
        }

        /// <summary>
        ///     Write a given block of metadata back to the Package's Stream
        /// </summary>
        /// <param name="metadata">Metadata to persist</param>
        public void WriteMetadata(ContainerMetadata metadata)
        {
            var existingBlockSize = GetMetadataBlockSize(ContainerStream);
            if (existingBlockSize > 0)
                // overwrite -- reduce length by metadata block size and magic
                ContainerStream.SetLength(ContainerStream.Length - existingBlockSize - 8);

            var jsonBytes = ChimeraSharedHelpers.SerializeToBytes(metadata);
            ContainerStream.Write(jsonBytes);
            ContainerStream.Write(BitConverter.GetBytes(jsonBytes.Length));

            // append magic
            ContainerStream.Write(CHIMERA_MAGIC);
        }

        /// <summary>
        ///     Get a hash of the Package's content
        /// </summary>
        /// <returns></returns>
        public byte[] GetPackageContentHash()
        {
            return ChimeraSharedHelpers.GetStreamHash(GetPackageContentStream());
        }

        private int GetMetadataBlockSize(Stream packageDataStream)
        {
            packageDataStream.Seek(-8, SeekOrigin.End);
            var blockSize = new byte[4];
            var magic = new byte[4];
            packageDataStream.Read(blockSize, 0, 4);
            packageDataStream.Read(magic, 0, 4);

            var blockSizeInt = BitConverter.ToInt32(blockSize);
            if (!magic.SequenceEqual(CHIMERA_MAGIC) || blockSizeInt < 0)
                return 0;
            return blockSizeInt;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Wrapper.Dispose();
                    Wrapper = null;
                }

                if (_disposeStreamOnClose)
                    ContainerStream.Dispose();

                try
                {
                    Directory.Delete(ExtractedPath, true);
                }
                catch (Exception)
                {
                }

                disposedValue = true;
            }
        }
    }
}