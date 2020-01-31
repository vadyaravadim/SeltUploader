using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using SeltUploader.Extension;
using Serilog;
using System;
using System.Collections.Generic;

namespace SeltUploader.Model
{
    internal struct AppSettings
    {
        internal const string Seltapi = "http://ashx.seltpd.ru/SeltService.svc";
        internal const string ApartmentsFullJson = "ApartmentsFullJson";
        internal const string Authtoken = "мтр1614пост";
        internal const string CrmLogin = "dyn_admin@metriumgr.onmicrosoft.com";
        internal const string CrmPassword = "Sut601355";
        internal const string CrmUrlAuth = @"RequireNewInstance=True;Url=https://org0063f16e.crm4.dynamics.com; Username=dyn_admin@metriumgr.onmicrosoft.com; Password=Sut601355; authtype=Office365";
        internal static string UrlSeltRequest => $"{Seltapi}/{ApartmentsFullJson}/{Authtoken}";
    }

    internal struct MappingState
    {
        internal const string Free = "Свободно";
        internal const string Fixation = "Фиксация";
        internal const string Contract = "Контракт";
        internal static Dictionary<int, OptionSetValue> mapCollection = new Dictionary<int, OptionSetValue>()
        {
            { 962090004, new OptionSetValue(962090001) }, // с Продано не нами на Свободно
            { 962090000, new OptionSetValue(962090001) }, // с Оценка на Свободно
            { 962090005, new OptionSetValue(962090001) }, // с Стратегический резерв на Свободно
        };
    }

    internal class SeltMapping
    {
        internal string SeltId { get; private set; }
        internal Guid BuildingId { get; private set; }
        internal string CorpusNumber { get; private set; }
        internal Apartment Apartment { get; private set; }
        internal SeltMapping(string seltId, Guid buildingId, string corpusNumber)
        {
            SeltId = seltId;
            BuildingId = buildingId;
            CorpusNumber = corpusNumber;
        }
        internal SeltMapping(Apartment apartment, Guid buildingId, string corpusNumber)
        {
            Apartment = apartment;
            BuildingId = buildingId;
            CorpusNumber = corpusNumber;
        }
    }

    internal class InstanceEntityUpdate
    {
        private static List<Entity> entities { get; set; }
        internal static List<Entity> Entities
        {
            get
            {
                if (entities == null)
                {
                    return entities = new List<Entity>();
                }
                return entities;
            }
            set
            {
                entities = value;
            }
        }
        internal static void PushEntity(IOrganizationService orgSvc, ILogger logger)
        {
            IEnumerable<List<Entity>> groupPartitionListing = Entities.Partition(1000);
            foreach (List<Entity> listing in groupPartitionListing)
            {
                try
                {
                    orgSvc.BulkUpdate(listing);
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Throw exception for bulk updated entity");
                }
            }
        }
    }

    #region Structure JSON 
    public class ApartmentMain
    {
        [JsonProperty("ApartmentsList")]
        public ApartmentsList AppartmentsList { get; set; }
    }

    public class ApartmentsList
    {
        [JsonProperty("apartment")]
        public List<Apartment> Apartment { get; set; }
    }

    public class Apartment
    {
        public string ID { get; set; }
        public string AdressBuild { get; set; }
        public string ApartmentID { get; set; }
        public string Section { get; set; }
        public string Floor { get; set; }
        public string NumInPlatform { get; set; }
        public string SquareCommon { get; set; }
        public string NumOrder { get; set; }
        public string Status { get; set; }
        public string LastName { get; set; }
        public string Note { get; set; }
        public string AgentName { get; set; }
        public string SquareMetrPrice { get; set; }
        public string Sum { get; set; }
        public string Rooms { get; set; }
    }
    #endregion
}
