using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using SeltUploader.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SeltUploader.Extension
{
    internal static class ApartmentExtension
    {
        internal static void PreparingJson(this Apartment apartment)
        {
            apartment.SquareCommon = apartment.SquareCommon.Replace(".", ",");
            apartment.SquareMetrPrice = apartment.SquareMetrPrice.Replace(".", ",");
            apartment.Sum = apartment.Sum.Replace(".", ",");
        }

        internal static bool TrySetStatus(this Apartment apartment, Entity entity)
        {
            if (string.IsNullOrEmpty(apartment.Status))
            {
                throw new ArgumentNullException("Transferred status from selt is empty");
            }

            int statusCode = entity.GetAttributeValue<OptionSetValue>("statuscode").Value;
            if (MappingState.mapCollection.TryGetValue(statusCode, out OptionSetValue value))
            {
                entity.Attributes["statuscode"] = value;
                return true;
            }
            return false;
        }
    }
    public static class IOrganizationServiceExtensions
    {
        public static void BulkUpdate(this IOrganizationService svc, List<Entity> entities)
        {
            ExecuteMultipleRequest multipleRequest = new ExecuteMultipleRequest()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection()
            };
            foreach (Entity entity in entities)
            {
                UpdateRequest updateRequest = new UpdateRequest { Target = entity };
                multipleRequest.Requests.Add(updateRequest);
            }

            ExecuteMultipleResponse multipleResponse = (ExecuteMultipleResponse)svc.Execute(multipleRequest);
        }
    }

    public static class ListExtension
    {
        public static IEnumerable<List<T>> Partition<T>(this IList<T> source, Int32 size)
        {
            for (int i = 0; i < Math.Ceiling(source.Count / (Double)size); i++)
                yield return new List<T>(source.Skip(size * i).Take(size));
        }
    }
}

