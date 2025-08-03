using System.Runtime.Serialization;

namespace Entities
{
    [Serializable]
    [DataContract]
    public abstract class WorldBankIndicatorBase
    {
        [DataMember]
        public string IndicatorId { get; set; }

        [DataMember]
        public string IndicatorName { get; set; }

        [DataMember]
        public string CountryId { get; set; }

        [DataMember]
        public string CountryName { get; set; }

        [DataMember]
        public string CountryIso3Code { get; set; }

        [DataMember]
        public int Year { get; set; }

        [DataMember]
        public double? Value { get; set; }

        [DataMember]
        public string Unit { get; set; }

        [DataMember]
        public string ObsStatus { get; set; }

        [DataMember]
        public int Decimal { get; set; }
    }
}
