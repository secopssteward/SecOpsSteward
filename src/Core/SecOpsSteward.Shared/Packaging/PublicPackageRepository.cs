using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using SecOpsSteward.Shared.Packaging.Metadata;

namespace SecOpsSteward.Shared.Packaging
{
    public class PublicPackageRepository
    {
        private const string KEY_CONTAINER = "key";
        private const string KEY_FILE = "package-index.public.key";
        private const string PACKAGE_CONTAINER = "packages";

        private readonly string _connectionString;

        public PublicPackageRepository(string connectionString)
        {
            _connectionString = connectionString;
        }


        /// <summary>
        ///     List packages from a public repository source
        /// </summary>
        /// <returns>List of Package metadata keyed by Package filename</returns>
        public async Task<Dictionary<string, ContainerMetadata>> ListPackages()
        {
            var target = $"{_connectionString}/{PACKAGE_CONTAINER}?restype=container&comp=list";

            using (var client = new HttpClient())
            {
                var xml = await client.GetStringAsync(target);
                var xmlDocument = XDocument.Parse(xml);

                var blobElements = xmlDocument.Descendants("Blob");
                var pkgName = blobElements.Select(b => b.Element("Name").Value).ToList();

                var allMetadata = await Task.WhenAll(pkgName.Where(p => p.EndsWith(".meta")).Select(async p =>
                {
                    var metadata = await GetMetadata(p);
                    return new KeyValuePair<string, ContainerMetadata>(Path.GetFileNameWithoutExtension(p) + ".zip",
                        metadata);
                }));

                return new Dictionary<string, ContainerMetadata>(allMetadata);
            }
        }


        /// <summary>
        ///     Get the public key for the configured public repository
        /// </summary>
        /// <returns></returns>
        public async Task<byte[]> GetIndexKey()
        {
            var target = $"{_connectionString}/{KEY_CONTAINER}/{KEY_FILE}";

            using (var client = new HttpClient())
            {
                var key = await client.GetStringAsync(target);
                return Convert.FromBase64String(key);
            }
        }


        /// <summary>
        ///     Get the package by its filename
        /// </summary>
        /// <param name="packageFileName">Package filename</param>
        /// <returns>Loaded container</returns>
        public async Task<ChimeraContainer> GetPackage(string packageFileName)
        {
            var target = $"{_connectionString}/{PACKAGE_CONTAINER}/{packageFileName}";

            using (var client = new HttpClient())
            {
                var ms = new MemoryStream();
                var data = await client.GetStreamAsync(target);
                data.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                return new ChimeraContainer(ms);
            }
        }

        private async Task<ContainerMetadata> GetMetadata(string file)
        {
            var target = $"{_connectionString}/{PACKAGE_CONTAINER}/{file}";
            using (var client = new HttpClient())
            {
                var ms = new MemoryStream();
                var data = await client.GetStreamAsync(target);
                data.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                return ChimeraSharedHelpers.GetFromSerializedBytes<ContainerMetadata>(ms.ToArray());
            }
        }
    }
}