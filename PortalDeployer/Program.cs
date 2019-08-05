using CommandLine;
using Microsoft.Xrm.Sdk.Client;
using PortalDeployer.App;
using PortalDeployer.Crm;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;

namespace PortalDeployer
{
    class Program
    {

        static int Main(string[] args)
        {
            Console.WriteLine("PortalDeployer (c) Perplex Ltd");
            int result = Parser.Default.ParseArguments<DownloadOptions, DeployOptions>(args)
              .MapResult(
                (DownloadOptions opts) => Run(new DownloadTask(), opts),
                (DeployOptions opts) => Run(new DeployTask(), opts),
                errs => 1);
#if DEBUG
            Console.WriteLine("Press <Enter> to exit.");
            Console.ReadLine();
#endif
            return result;
        }

        private static int Run<T>(BaseTask<T> task, T opts) where T: BaseOptions
        {
            task.Options = opts;
            OrganizationServiceProxy serviceProxy = null;
            try
            {
                // Obtain the target organization's web address and client logon credentials
                // from the user by using a helper class.
                ServerConnection serverConnect = new ServerConnection();
                ServerConnection.Configuration config = serverConnect.GetServerConfiguration();

                // Establish an authenticated connection to the Organization web service. 
                serviceProxy = new OrganizationServiceProxy(config.OrganizationUri, config.HomeRealmUri,
                                                            config.Credentials, null);
                task.Service = serviceProxy;
                task.Run();

            }
            catch (Exception e)
            {
                HandleException(e);
                return 1;
            }
            finally
            {
                // Always dispose the service object to close the service connection and free resources.
                if (serviceProxy != null) serviceProxy.Dispose();
            }
            return 0;
        }


        /// Handle a thrown exception.
        /// </summary>
        /// <param name="ex">An exception.</param>
        private static void HandleException(Exception e)
        {
            // Display the details of the exception.
            Console.WriteLine("\n" + e.Message);
            Console.WriteLine(e.StackTrace);

            if (e.InnerException != null) HandleException(e.InnerException);
        }
    }
}
