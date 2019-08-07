using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PortalDeployer.App
{
    class DownloadTask : BaseTask<DownloadOptions>
    {
        public override string TaskName => "Deploy Portal configuration";

        private class WebFile
        {
            public Guid Id;
            public string Name;
            public Guid ParentPageId;
            public DateTime ModifiedOn;
        }
        private class WebFileAttachment
        {
            public byte[] Content;
            public DateTime ModifiedOn;
        }

        protected override void RunTask()
        {
            DownloadWebTemplates();
            DownloadWebFiles();
        }

        /// <summary>
        /// Downloads all web files, saves them in a folder strucure according to their parent pages and saves a metadata JSON file.
        /// </summary>
        private void DownloadWebFiles()
        {
            var webPagePaths = GetWebPageTree();
            var webFiles = GetWebFiles();
            var directory = Path.Combine(Options.LocalDirectory, Options.WebFilesDirectory);
            var metaData = new ConfigurationMetaData(Path.Combine(directory, "WebFiles.json"));
            foreach (var webFile  in webFiles)
            {
                var content = GetContent(webFile.Id);
                if (content != null)
                {
                    var path = Path.Combine(directory, webPagePaths[webFile.ParentPageId]);
                    CreateDirectory(path);
                    var filePath = Path.Combine(path, webFile.Name);
                    var metaDataElement = metaData.GetElementById(webFile.Id);
                    if (ShouldOverwriteFile(metaDataElement, filePath, webFile.ModifiedOn))
                    {
                        metaDataElement.Type = ConfigurationElement.ElementType.WebFile;
                        metaDataElement.Name = webFile.Name;
                        metaDataElement.FileName = filePath.Substring(directory.Length + 1);
                        metaDataElement.ModifiedOn = webFile.ModifiedOn;
                        metaDataElement.CheckSum = CheckSum.CalculateHash(content);
                        Console.WriteLine("Saving {0}", filePath.ShortenLeft(80));
                        WriteBinaryFile(filePath, content);
                    }
                }
            }
            metaData.Update();
        }

        

        private byte[] GetContent(Guid id)
        {
            var fetchData = new
            {
                objectid = id.ToString("D")
            };
            var fetchXml = $@"
<fetch top='1'>
  <entity name='annotation'>
    <attribute name='documentbody' />
    <attribute name='modifiedon' />
    <filter>
      <condition attribute='objectid' operator='eq' value='{fetchData.objectid/*338733d5-31ad-e911-a97e-002248014773*/}'/>
    </filter>
    <order attribute='modifiedon' descending='true' />
  </entity>
</fetch>";
            var results = Service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (results.Entities.Count == 1)
            {
                var e = results.Entities[0];
                return Convert.FromBase64String((string)e["documentbody"]);
            }
            return null;
        }

        /// <summary>
        /// Returns web files
        /// </summary>
        /// <returns></returns>
        private IEnumerable<WebFile> GetWebFiles()
        {
            var fetchData = new
            {
                adx_websiteid = Website.Id,
                statecode = "0"
            };
            var fetchXml = $@"
<fetch>
  <entity name='adx_webfile'>
    <attribute name='adx_name' />
    <attribute name='adx_parentpageid' />
    <attribute name='adx_webfileid' />
    <attribute name='modifiedon' />
    <filter>
      <condition attribute='adx_websiteid' operator='eq' value='{fetchData.adx_websiteid/*2AB10DAB-D681-4911-B881-CC99413F07B6*/}'/>
      <condition attribute='statecode' operator='eq' value='{fetchData.statecode/*0*/}'/>
      <condition attribute='adx_parentpageid' operator='not-null' />
    </filter>
  </entity>
</fetch>";
            var results = Service.RetrieveMultiple(new FetchExpression(fetchXml));
            var webFiles = new Dictionary<string, Guid>();
            foreach (var e in results.Entities)
            {
                yield return new WebFile()
                {
                    Id = (Guid)e["adx_webfileid"],
                    Name = (string)e["adx_name"],
                    ParentPageId = ((EntityReference)e["adx_parentpageid"]).Id,
                    ModifiedOn = (DateTime)e["modifiedon"]
                };
            }
        }

        /// <summary>
        /// Returns a dictionary with the page id as a key and the combined relative path as value.
        /// </summary>
        /// <returns></returns>
        private IDictionary<Guid, string> GetWebPageTree()
        {
            var fetchData = new
            {
                adx_websiteid = Website.Id,
                adx_isroot = "1",
                statecode = "0"
            };
            var fetchXml = $@"
<fetch>
  <entity name='adx_webpage'>
    <attribute name='adx_name' />
    <attribute name='adx_parentpageid' />
    <attribute name='adx_webpageid' />
    <filter>
      <condition attribute='adx_websiteid' operator='eq' value='{fetchData.adx_websiteid/*2AB10DAB-D681-4911-B881-CC99413F07B6*/}'/>
      <condition attribute='adx_isroot' operator='eq' value='{fetchData.adx_isroot/*1*/}'/>
      <condition attribute='statecode' operator='eq' value='{fetchData.statecode/*0*/}'/>
    </filter>
  </entity>
</fetch>";

            var results = Service.RetrieveMultiple(new FetchExpression(fetchXml));
            Dictionary<Guid, string> paths = new Dictionary<Guid, string>();
            foreach (var entity in results.Entities)
            {
                var item = entity;
                var root = !item.Attributes.Contains("adx_parentpageid");
                var path = (string)item["adx_name"];

                while (!root)
                {

                    item = results.Entities.First(e => ((Microsoft.Xrm.Sdk.EntityReference)item["adx_parentpageid"]).Id == (Guid)e["adx_webpageid"]);
                    path = System.IO.Path.Combine((string)item["adx_name"], path);

                    root = !item.Attributes.Contains("adx_parentpageid");
                }
                paths.Add((Guid)entity["adx_webpageid"], path);
            }
            return paths;
        }

        private string CreateDirectory(string subdir)
        {
            string path = Path.Combine(Options.LocalDirectory, subdir);
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Creating directory {0}", path.ShortenRight(50));
                if (!Options.WhatIf)
                {
                    Directory.CreateDirectory(path);
                }
            }
            return path;
        }

        private void DownloadWebTemplates()
        {
            Console.WriteLine("Downloading Web Templates");
            var directory = CreateDirectory(Options.WebTemplatesDirectory);
            var metaData = new ConfigurationMetaData(Path.Combine(directory, "WebTemplates.json"));
            var fetchData = new
            {
                adx_websiteid = Website.Id
            };
            var fetchXml = $@"
<fetch>
  <entity name='adx_webtemplate'>
    <attribute name='adx_websiteid' />
    <attribute name='adx_name' />
    <attribute name='adx_source' />
    <attribute name='modifiedon' />
    <filter>
      <condition attribute='adx_websiteid' operator='eq' value='{fetchData.adx_websiteid}'/>
    </filter>
  </entity>
</fetch>";

            var result = Service.RetrieveMultiple(new FetchExpression(fetchXml));
            foreach (var e in result.Entities)
            {
                var name = (string)e["adx_name"];
                var filename = name + ".liquid";
                var path = Path.Combine(directory, filename);
                var modifiedOn = (DateTime)e["modifiedon"];
                string content = (string)e["adx_source"];
                var metaDataElement = metaData.GetElementById(e.Id);
                if (ShouldOverwriteFile(metaDataElement, path, modifiedOn))
                {
                    metaDataElement.Type = ConfigurationElement.ElementType.WebTemplate;
                    metaDataElement.Name = name;
                    metaDataElement.FileName = filename;
                    metaDataElement.RecordId = e.Id;
                    metaDataElement.ModifiedOn = modifiedOn;
                    metaDataElement.CheckSum = CheckSum.CalculateHash(content);
                    Console.WriteLine("Saving {0} ({1})", path.ShortenLeft(30), content.ShortenRight(50));
                    WriteTextFile(path, content);
                }
            }
            metaData.Update();
        }

        private bool ShouldOverwriteFile(ConfigurationElement originalElement, string localPath, DateTime remoteTimestamp)
        {
            var localFileHash = CheckSum.CalculateHashFromFile(localPath);
            if (localFileHash != originalElement.CheckSum)
            {
                return AskOverwrite(string.Format("{0} was locally modified. Overwrite? (Yes/No/All)", Path.GetFileName(localPath)));
            }
            return (remoteTimestamp != originalElement.ModifiedOn);
        }



        private void WriteBinaryFile(string path, byte[] content)
        {
            if (!Options.WhatIf)
            {
                File.WriteAllBytes(path, content);
            }
        }
    }
}