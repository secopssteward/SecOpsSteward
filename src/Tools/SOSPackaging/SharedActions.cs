﻿using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Azure.Storage.Blobs;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Cryptography.Extensions;
using SecOpsSteward.Shared.Packaging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SOSPackaging
{
    public static class SharedActions
    {
        public static string Create(string path)
        {
            ChimeraContainer package;
            if (Directory.Exists(path))
            {
                package = ChimeraContainer.CreateFromFolder(path);
            }
            else
            {
                var fs = new FileStream(path, FileMode.Open);
                package = ChimeraContainer.CreateFromArchiveStream(fs);
            }

            var metadata = package.GetMetadata();
            var tree = new Tree($"📦 [green]{metadata.ContainerId.ShortId}[/] - {metadata.Version}");
            foreach (var service in metadata.ServicesMetadata)
            {
                var svcNode = tree.AddNode($"🛠 [blue]{service.ServiceId.ServiceId}[/] - {service.Name} ({service.Description}) " + (service.Image == null ? "(No image)" : "(With image)"));
                var pkgNode = svcNode.AddNode("[yellow]Plugins[/]");
                foreach (var pkg in service.PluginIds)
                {
                    var plugin = metadata.PluginsMetadata.First(p => p.PluginId == pkg);
                    pkgNode.AddNode($"🔌 [yellow]{plugin.PluginId.PluginId}[/] - {plugin.Name} (by {plugin.Author}) - {plugin.Description}");
                }
                var templateNode = svcNode.AddNode("[fuchsia]Templates[/]");
                foreach (var template in service.Templates)
                    templateNode.AddNode($"📃 [fuchsia]{template.WorkflowTemplateId.ShortIdEnd()}[/] - {template.Name} ({template.Participants.Count} steps)");
            }
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();
            AnsiConsole.Render(tree);
            AnsiConsole.WriteLine();

            AnsiConsole.Markup("Checking Package Integrity and Metadata ... ");
            metadata.CheckIntegrity();
            AnsiConsole.MarkupLine("[green]OK![/]");

            package.WritePackageFile(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "pkg-" + metadata.ContainerId.ContainerId + ".zip");

            return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "pkg-" +
                   metadata.ContainerId.ContainerId + ".zip";
        }

        public static void Sign(string path, string privateKeyBase64, string signer)
        {
            var ms = new MemoryStream();
            using (var fs = new FileStream(path, FileMode.Open))
            {
                fs.CopyTo(ms);
            }

            ms.Seek(0, SeekOrigin.Begin);
            var package = new ChimeraContainer(ms);
            var metadata = package.GetMetadata();
            metadata.PubliclySign(signer, Convert.FromBase64String(privateKeyBase64));
            package.WriteMetadata(metadata);

            ms.Seek(0, SeekOrigin.Begin);

            using (var fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                ms.CopyTo(fs);
            }
        }

        public static int Verify(string path, string publicKeyBase64)
        {
            using (var fs = new FileStream(path, FileMode.Open))
            {
                var package = new ChimeraContainer(fs);
                var metadata = package.GetMetadata();
                if (metadata.PubliclyVerify(Convert.FromBase64String(publicKeyBase64)))
                    return 0;
                return 1;
            }
        }

        public static void Post(string packagePath, string sasKey)
        {
            var blobServiceClient = new BlobServiceClient(new Uri(sasKey));
            var container = blobServiceClient.GetBlobContainerClient("packages");

            using (var fs = new FileStream(packagePath, FileMode.Open))
            {
                var blobName = Path.GetFileName(packagePath);
                var pkg = new ChimeraContainer(fs, false);
                var metadata = pkg.GetMetadata();
                fs.Seek(0, SeekOrigin.Begin);
                container.GetBlobClient(blobName).Upload(fs, true);

                var metadataEncoded = ChimeraSharedHelpers.SerializeToBytes(metadata);
                var mdBlobName = Path.GetFileNameWithoutExtension(packagePath) + ".meta";
                container.GetBlobClient(mdBlobName).Upload(new MemoryStream(metadataEncoded), true);
            }
        }

        public static void ApplyConfiguration(CommandSettings settings)
        {
            var config = settings.GetType().GetProperty("Configuration").GetValue(settings) as string;
            if (string.IsNullOrEmpty(config))
                return;

            byte[] configBytes;
            var span = new Span<byte>(new byte[config.Length]);
            if (Convert.TryFromBase64String(
                config.PadRight(config.Length / 4 * 4 + (config.Length % 4 == 0 ? 0 : 4), '='), span, out _))
                configBytes = Convert.FromBase64String(config);
            else
                configBytes = File.ReadAllBytes(config);

            var configDeserialized = JsonSerializer.Deserialize<ConfigFile>(configBytes);
            foreach (var property in settings.GetType().GetProperties())
            {
                var propObj = typeof(ConfigFile).GetProperty(property.Name);
                if (propObj != null)
                    property.SetValue(settings, propObj.GetValue(configDeserialized));
            }
        }
    }

    public class ConfigFile
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string Signer { get; set; }
        public string SasKey { get; set; }
    }
}