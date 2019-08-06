using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDeployer.App
{
    public abstract class BaseTask<T> where T: BaseOptions
    {

        public abstract string TaskName {  get; }
        public T Options { get; set; }
        public OrganizationServiceProxy Service { get; set; }

        protected EntityReference Website { get; private set; }
        
        public void Run()
        {
            Website = GetWebsite();
            RunTask();
        }

        private EntityReference GetWebsite()
        {
            var fetchXml = $@"
<fetch>
  <entity name='adx_website'>
    <attribute name='adx_websiteid' />
    <attribute name='adx_name' />
  </entity>
</fetch>";
            FetchExpression query = new FetchExpression(fetchXml);
            var result = Service.RetrieveMultiple(query);
            if (result.Entities.Count == 1)
            {
                var e = result.Entities[0];
                return new EntityReference()
                {
                    Id = e.Id,
                    LogicalName = e.LogicalName,
                    Name = (string)e["adx_name"]
                };
            } else
            {
                throw new ApplicationException("Website is not unique.");
            }
        }

        protected abstract void RunTask();
    }
}
