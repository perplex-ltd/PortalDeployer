using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDeployer.App
{
    public class ConfigurationElement
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ElementType
        {
            WebFile, WebTemplate
        }

        public ElementType Type { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public Guid RecordId { get; set; }

        public DateTime ModifiedOn { get; set; }
        public string CheckSum { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: {1} ({2})", Type, Name, RecordId);
        }
    }
}
