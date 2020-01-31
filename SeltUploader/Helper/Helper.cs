using Microsoft.Xrm.Sdk;
using SeltUploader.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Requests = SeltUploader.Transaction.Request;

namespace SeltUploader.Helper
{
    internal static class Helper
    {
        internal static List<SeltMapping> AggregateCollectionEstate(List<Apartment> apartmentsList, List<SeltMapping> seltMapping, ILogger logger)
        {
            List<SeltMapping> validateCollectionApartments = new List<SeltMapping>();

            try
            {
                Parallel.ForEach(apartmentsList, (apartment) =>
                {
                    seltMapping.ToList().ForEach((item) =>
                    {
                        if (item.SeltId == apartment.ID)
                        {
                            validateCollectionApartments.Add(new SeltMapping(apartment, item.BuildingId, item.CorpusNumber));
                        }
                    });
                });
            }
            catch (AggregateException ex)
            {
                logger.Error(ex, "Exception in parallel foreach, the iteration is stopped");
            }

            return validateCollectionApartments;
        }
        internal static void ProcessingApartmentsUpdate(IOrganizationService orgSvc, List<SeltMapping> validateCollectionApartments, ILogger logger)
        {
            Parallel.ForEach(validateCollectionApartments, (apartment) =>
            {
                try
                {
                    Entity entity = Requests.GetEstateObject(orgSvc, apartment.Apartment, apartment.BuildingId, apartment.CorpusNumber);
                    if (entity != null)
                    {
                        Requests.SettingEstateObject(orgSvc, entity, apartment.Apartment);
                        InstanceEntityUpdate.Entities.Add(entity);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "the element is not processed");
                }
            });
        }
    }
}
