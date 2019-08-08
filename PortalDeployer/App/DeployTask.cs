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
    class DeployTask : BaseTask<DeployOptions>
    {
        public override string TaskName => "Deploy Portal configuration";

        protected override void RunTask()
        {
            DeployWebTemplates();
            DeployWebFiles();
        }

        private void DeployWebFiles()
        {
            Console.WriteLine("Web Files");
            var path = Options.LocalDirectory;
            path = Path.Combine(path, "WebFiles");
            var metaData = new ConfigurationMetaData(Path.Combine(path, "WebFiles.json"));
            var directories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);


            foreach (var dir in directories)
            {
                var relativeDir = dir.Substring(path.Length + 1);
                Console.WriteLine(relativeDir);
                var hierarchy = relativeDir.Split('\\');
                string pageFilter = "";
                foreach (var page in relativeDir.Split('\\'))
                {
                    pageFilter = string.Format(@"
<link-entity name='adx_webpage' from='adx_webpageid' to='adx_parentpageid'>
<filter>
  <condition attribute='adx_name' operator='eq' value='{0}'/>
</filter>
{1}
</link-entity>
", page, pageFilter);

                }
                foreach (var file in Directory.GetFiles(dir))
                {
                    var fileName = file.Substring(dir.Length + 1);
                    var relativeFileName = file.Substring(path.Length + 1);
                    if (relativeFileName.ToLower() == "webfiles.json") continue;
                    var metaDataElement = metaData.GetElementByFilename(relativeFileName);
                    if (metaDataElement.CheckSum == CheckSum.CalculateHashFromFile(file))
                    {
                        continue; // skip loop
                    }
                    var fetchData = new
                    {
                        adx_websiteid = Website.Id,
                        adx_name = fileName,
                        pageFilter
                    };
                    var fetchXml = $@"
<fetch top='1'>
  <entity name='annotation'>
    <attribute name='annotationid' />
    <attribute name='documentbody' />
    <attribute name='modifiedon' />
    <order attribute='modifiedon' descending='true' />
    <link-entity name='adx_webfile' from='adx_webfileid' to='objectid'>
      <attribute name='adx_name' />
      <filter>
        <condition attribute='adx_websiteid' operator='eq' value='{fetchData.adx_websiteid/*2AB10DAB-D681-4911-B881-CC99413F07B6*/}'/>
        <condition attribute='adx_name' operator='eq' value='{fetchData.adx_name/*logo.jpg*/}'/>
      </filter>
	  {fetchData.pageFilter}
    </link-entity>
  </entity>
</fetch>";
                    var results = Service.RetrieveMultiple(new FetchExpression(fetchXml));

                    if (results.Entities.Count == 0)
                    {
                        throw new ApplicationException(string.Format("No file found for {0}. Create one first.", fileName));

                    }
                    else if (results.Entities.Count > 1)
                    {
                        throw new ApplicationException(string.Format("More than one file found ({0}). I really didn't expect this.", fileName));

                    }
                    else
                    {
                        var target = results.Entities[0];
                        var modifiedOn = (DateTime)target["modifiedon"];
                        bool shouldDeploy = true;
                        if (metaDataElement.ModifiedOn < modifiedOn)
                        {
                            shouldDeploy = AskOverwrite(string.Format("The remote file has been changed since the {0} was downloaded. Overwrite anyway? (Yes/No/All)", fileName));
                        }

                        var content = Convert.ToBase64String(File.ReadAllBytes(file));
                        if (content == (string)target["documentbody"])
                        {
                            shouldDeploy = false;
                        }
                        
                        if (shouldDeploy)
                        {
                            Entity entity = new Entity(target.LogicalName, target.Id);
                            entity["documentbody"] = content;
                            Console.WriteLine("Updating {0}", relativeFileName.ShortenLeft(50));
                            if (!Options.WhatIf)
                            {
                                Service.Update(entity);
                            }
                            entity = Service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(new string[] { "documentbody", "modifiedon" }));
                            metaDataElement.CheckSum = CheckSum.CalculateHash((string)entity["documentbody"]);
                            metaDataElement.ModifiedOn = (DateTime)entity["modifiedon"];
                        }
                    }

                }
            }
            metaData.Update();
        }

        private void DeployWebTemplates()
        {
            Console.WriteLine("Deploying Web Templates");
            var path = Path.Combine(Options.LocalDirectory, Options.WebTemplatesDirectory);
            var metaData = new ConfigurationMetaData(Path.Combine(path, "WebTemplates.json"));
            var files = Directory.GetFiles(path);

            foreach (var file in files)
            {
                var fileName = file.Substring(path.Length + 1);
                if (fileName.ToLower() == "webtemplates.json") continue;
                var webTemplateName = fileName.Replace(".liquid", "");
                var fetchData = new
                {
                    adx_websiteid = Website.Id,
                    adx_name = webTemplateName
                };
                var fetchXml = $@"
<fetch>
  <entity name='adx_webtemplate'>
    <attribute name='adx_webtemplateid' />
    <attribute name='adx_source' />
    <attribute name='modifiedon' />
    <filter>
      <condition attribute='adx_websiteid' operator='eq' value='{fetchData.adx_websiteid/*2AB10DAB-D681-4911-B881-CC99413F07B6*/}'/>
      <condition attribute='adx_name' operator='eq' value='{fetchData.adx_name/*Breadcrumbs*/}'/>
    </filter>
  </entity>
</fetch>";
                var results = Service.RetrieveMultiple(new FetchExpression(fetchXml));
                if (results.Entities.Count == 0)
                {
                    throw new ApplicationException(string.Format("Web Template {0} does not exist. Please create it first.", webTemplateName));
                }
                else if (results.Entities.Count > 1)
                {
                    throw new ApplicationException(string.Format("Web Template {0} is not a unique name.", webTemplateName));
                }
                var template = results.Entities[0];
                var modifiedOn = (DateTime)template["modifiedon"];
                var metaDataElement = metaData.GetElementByFilename(fileName);
                var shouldDeploy = true;
                if (metaDataElement.ModifiedOn < modifiedOn)
                {
                    shouldDeploy = AskOverwrite(string.Format("The remote file has been changed since the {0} was downloaded. Overwrite anyway? (Yes/No/All)", webTemplateName));
                }
                var content = File.ReadAllText(file);


                if ((string)template["adx_source"] == content)
                {
                    shouldDeploy = false;
                }
                else
                {
                    Console.WriteLine("updating");
                    var entity = new Entity("adx_webtemplate", template.Id);
                    entity["adx_source"] = content;
                    if (!Options.WhatIf)
                    {
                        Console.WriteLine("Updating {0}", webTemplateName.ShortenRight(50));
                        Service.Update(entity);
                    }
                    entity = Service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(new string[] { "adx_source", "modifiedon" }));
                    metaDataElement.CheckSum = CheckSum.CalculateHash((string)entity["adx_source"]);
                    metaDataElement.ModifiedOn = (DateTime)entity["modifiedon"];
                }

            }
            metaData.Update();

        }
    }
}
