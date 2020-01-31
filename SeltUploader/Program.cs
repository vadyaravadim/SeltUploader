using Microsoft.Xrm.Sdk;
using SeltUploader.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Helpers = SeltUploader.Helper.Helper;
using Requests = SeltUploader.Transaction.Request;

namespace SeltUploader
{
    public class Program
    {
        static ILogger logger = new LoggerConfiguration().WriteTo.File($@"{Environment.CurrentDirectory}\Logs\Log_{DateTime.Now.ToString("d")}.txt").CreateLogger();

        static void Main(string[] args)
        {
            logger.Information("running app");
            IOrganizationService orgSvc = Requests.ConnectionCrm();
            HttpClient client = new HttpClient();
            UpdateApartmentsAggregate(orgSvc, client);
        }

        private static void UpdateApartmentsAggregate(IOrganizationService orgSvc, HttpClient client)
        {
            Task<ApartmentMain> apartmentsList = Task.Run(() => Requests.GetApartmentsSeltAsync(client));
            Task<List<SeltMapping>> seltMapping = Task.Run(() => Requests.GetSeltMapAsync(orgSvc));
            try
            {
                Task.WaitAll(apartmentsList, seltMapping);
            }
            catch (AggregateException ex)
            {
                logger.Error(ex, ex.Message);
            }

            List<SeltMapping> selts = seltMapping.GetAwaiter().GetResult();
            List<Apartment> apartments = apartmentsList.GetAwaiter().GetResult()?.AppartmentsList?.Apartment;
            
            if (apartments.Count <= 0 || selts.Count <= 0)
            {
                logger.Warning("Collection appartments or selt mapping is empty");
                return;
            }

            List<SeltMapping> validateCollectionApartments = Helpers.AggregateCollectionEstate(apartments, selts, logger);

            Helpers.ProcessingApartmentsUpdate(orgSvc, validateCollectionApartments, logger);
            InstanceEntityUpdate.PushEntity(orgSvc, logger);
            logger.Information($"Successfully updated {InstanceEntityUpdate.Entities.Count} entities");
        }
    }
}
