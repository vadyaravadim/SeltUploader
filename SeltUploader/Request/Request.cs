using DataLoaderTools;
using DataLoaderTools.Connector;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using SeltUploader.Extension;
using SeltUploader.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SeltUploader.Transaction
{
    public class Request
    {
        internal static IOrganizationService ConnectionCrm()
        {
            ConnectorFactory connector = new ConnectorFactory();
            ConnectionData connection = new ConnectionData
            {
                Login = AppSettings.CrmLogin,
                Password = AppSettings.CrmPassword,
                Url = AppSettings.CrmUrlAuth
            };
            IOrganizationService orgSvc = connector.Create(connection, ConnectorFactory.Developer.Metrium);
            return orgSvc;
        }

        internal static async Task<ApartmentMain> GetApartmentsSeltAsync(HttpClient client)
        {
            string resultDataSelt = await client.GetStringAsync($"{AppSettings.UrlSeltRequest}");
            ApartmentMain apartmentsList = JsonConvert.DeserializeObject<ApartmentMain>(resultDataSelt);
            return apartmentsList;
        }

        internal static List<SeltMapping> GetSeltMapAsync(IOrganizationService orgSvc)
        {
            string fetchToSeltMap = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='mtr_selt_address'>
                                            <attribute name='mtr_selt_addressid' />
                                            <attribute name='mtr_name' />
                                            <attribute name='mtr_seltid' />
                                            <attribute name='mtr_buildingid' />
                                            <attribute name='mtr_korpus_number' />
                                            <order attribute='mtr_name' descending='false' />
                                            <filter type='and'>
                                                <condition attribute='mtr_seltid' operator='not-null' />
                                            </filter>
                                        </entity>
                                      </fetch>";
            IEnumerable<Entity> entities = orgSvc.RetrieveMultiple(new FetchExpression(fetchToSeltMap)).Entities;
            List<SeltMapping> seltMap = new List<SeltMapping>();
            foreach (Entity entity in entities)
            {
                EntityReference reference = entity.GetAttributeValue<EntityReference>("mtr_buildingid");
                if (reference != null)
                {
                    seltMap.Add(new SeltMapping(entity.GetAttributeValue<string>("mtr_seltid"), reference.Id, entity.GetAttributeValue<string>("mtr_korpus_number")));
                }
            }
            return seltMap;
        }

        internal static Entity GetEstateObject(IOrganizationService orgSvc, Apartment apartment, Guid buildingId, string corpusNumber)
        {
            string fetchToEstateObject = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                <entity name='mtr_property'>
                                                <attribute name='mtr_propertyid' />
                                                <attribute name='mtr_name' />
                                                <attribute name='mtr_spacedesign' />
                                                <attribute name='mtr_quantity' />
                                                <attribute name='mtr_seltid' />
                                                <attribute name='mtr_cost' />
                                                <attribute name='mtr_price' />
                                                <attribute name='statuscode' />
                                                <order attribute='mtr_name' descending='false' />
                                                <filter type='and'>
                                                    <condition attribute='mtr_buildingid' operator='eq' value='{buildingId}' />
                                                    <condition attribute='mtr_floor' operator='eq' value='{apartment.Floor}' />
                                                    <condition attribute='mtr_before_bti_number' operator='eq' value='{apartment.NumOrder}' />
                                                    <condition attribute='mtr_sectionnumber' operator='eq' value='{apartment.Section.Trim()}' />
                                                    <condition attribute='mtr_platform_number' operator='eq' value='{apartment.NumInPlatform}' />
                                                    <condition attribute='mtr_korpus_number' operator='eq' value='{corpusNumber}' />
                                                </filter>
                                                </entity>
                                            </fetch>";

            IEnumerable<Entity> entities = orgSvc.RetrieveMultiple(new FetchExpression(fetchToEstateObject)).Entities;
            foreach (Entity entity in entities)
            {
                string spaceDesign = entity.GetAttributeValue<decimal>("mtr_spacedesign").ToString("#.##").Replace(",", ".");
                string quantity = entity.GetAttributeValue<decimal>("mtr_quantity").ToString("#.##").Replace(",", ".");
                if (apartment.SquareCommon == spaceDesign || apartment.SquareCommon == quantity)
                {
                    return entity;
                }
            }
            return null;
        }

        internal static void SettingEstateObject(IOrganizationService orgSvc, Entity entity, Apartment apartment)
        {
            apartment.PreparingJson();
            decimal metrPrice = Convert.ToDecimal(apartment.SquareMetrPrice);
            decimal sum = Convert.ToDecimal(apartment.Sum);

            entity["mtr_seltid"] = apartment.ApartmentID;
            entity["mtr_cost"] = new Money(metrPrice);
            entity["mtr_price"] = new Money(sum);
            apartment.TrySetStatus(entity);
        }
    }
}
