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

        [Option('w', "whatif", Required = false, HelpText = "Indicate if no actual action should be taken.")]
        public bool WhatIf { get; set; }
    }
}
