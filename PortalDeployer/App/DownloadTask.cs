using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        }

        protected override void RunTask()
        {
            DownloadWebTemplates();
            DownloadWebFiles();
        }

        private void DownloadWebFiles()
        {
            var webPagePaths = GetWebPageTree();
            var webFiles = GetWebFiles();
            foreach (var webFile  in webFiles)
            {
                var content = GetContent(webFile.Id);
                if (content != null)
                {
                    var path = Path.Combine(Options.LocalDirectory, Options.WebFilesDirectory);
                    path = Path.Combine(path, webPagePaths[webFile.ParentPageId]);
                    CreateDirectory(path);
                    var file = Path.Combine(path, webFile.Name);
                    WriteBinaryFile(file, content);
                }
            }
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
                    ParentPageId = ((EntityReference)e["adx_parentpageid"]).Id
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
    <filter>
      <condition attribute='adx_websiteid' operator='eq' value='{fetchData.adx_websiteid}'/>
    </filter>
  </entity>
</fetch>";

            var result = Service.RetrieveMultiple(new FetchExpression(fetchXml));
            var directory = CreateDirectory(Options.WebTemplatesDirectory);
            foreach (var e in result.Entities)
            {
                var filename = e["adx_name"] + ".liquid";
                var path = Path.Combine(directory, filename);
                WriteTextFile(path, (string)e["adx_source"]);
            }

        }

        private void WriteTextFile(string path, string content)
        {
            Console.WriteLine("Saving {0} ({1})", path.ShortenLeft(30), content.ShortenRight(50));
            if (!Options.WhatIf)
            {
                using (var writer = File.CreateText(path))
                {
                    writer.Write(content);
                }
            }
        }
        private void WriteBinaryFile(string path, byte[] content)
        {
            Console.WriteLine("Saving {0}", path.ShortenLeft(80));
            if (!Options.WhatIf)
            {
                File.WriteAllBytes(path, content);
            }
        }
    }
}