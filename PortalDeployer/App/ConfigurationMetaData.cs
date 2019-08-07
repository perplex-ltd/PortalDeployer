using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDeployer.App
{
    class ConfigurationMetaData
    {

        private List<ConfigurationElement> elements;
        private string fileName;

        /// <summary>
        /// Create a new ConfigurationMetaData object by loading the data from the specified file.
        /// If the file does not exists, an empty configuration is created.
        /// </summary>
        /// <param name="fileName"></param>
        public ConfigurationMetaData(string fileName)
        {
            this.fileName = fileName;
            elements = LoadMetaData();
        }

        /// <summary>
        /// Saves the metadata object to the same file it was loaded from.
        /// </summary>
        public void Update()
        {
            Console.WriteLine("Saving meta data {0}", fileName.ShortenLeft(30));
            string content = JsonConvert.SerializeObject(elements);
            using (var writer = File.CreateText(fileName))
            {
                writer.Write(content);
            }
        }

        private List<ConfigurationElement> LoadMetaData()
        {
            if (!File.Exists(fileName))
            {
                return new List<ConfigurationElement>();
            }
            else
            {
                string content = File.ReadAllText(fileName);
                return JsonConvert.DeserializeObject<List<ConfigurationElement>>(content);
            }
        }

        /// <summary>
        /// Gets an element from the meta data configuration by its id. 
        /// If the element doesn't exist, it's added to the metadata collection.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>A configuration element with the same id as specified. Any other property may be uninitialised.</returns>
        public ConfigurationElement GetElementById(Guid id)
        {
            if (id == Guid.Empty) throw new ArgumentException("id cannot be an empty guid.");
            var element = elements.Find(el => el.RecordId == id);
            if (element == null)
            {
                element = new ConfigurationElement()
                {
                    RecordId = id,
                    CheckSum = CheckSum.Empty,
                    ModifiedOn = DateTime.MinValue
                };
                elements.Add(element);
            }
            return element;
        }

        /// <summary>
        /// Gets an element from the meta data configuration by its filename. 
        /// If the element doesn't exist, it's added to the metadata collection.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ConfigurationElement GetElementByFilename(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentException("id cannot be null or empty.");
            var element = elements.Find(el => fileName == el.FileName);
            if (element == null)
            {
                element = new ConfigurationElement()
                {
                    FileName = fileName,
                    CheckSum = CheckSum.Empty,
                    ModifiedOn = DateTime.MinValue
                };
                elements.Add(element);
            }
            return element;
        }

    }
}
