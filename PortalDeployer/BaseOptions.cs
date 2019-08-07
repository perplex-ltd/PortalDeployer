using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDeployer
{
    public class BaseOptions
    {
        string localDirectory = null;
        [Option('d', "directory", Required = false, HelpText = "The local root directory of the portal configuation.")]
        public string LocalDirectory
        {
            get
            {
                return string.IsNullOrEmpty(localDirectory) ? "." : localDirectory;
            }
            set { localDirectory = value; }
        }

        [Option('w', "whatIf", Required = false, HelpText = "Indicate if no actual action should be taken.")]
        public bool WhatIf { get; set; }

        [Option('t', "webTemplatesDirectory", Default = "WebTemplates", HelpText = "The directory for the web templates (default: WebTemplates).",
            Required = false)]
        public string WebTemplatesDirectory { get; set; }

        [Option('f', "webFilesDirectory", Default = "WebFiles", HelpText = "The directory for the web pages (default: WebFiles).",
            Required = false)]
        public string WebFilesDirectory { get; set; }

        [Option('o', "overwriteAll", Default = false, HelpText = "Overwrite all files. Without this option, only changed files will be downloaded or deployed.")]
        public bool Overwrite { get; set; }
    }
}
