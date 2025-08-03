using System;
using System.Runtime.Serialization;

namespace Entities.Tariff
{
    [Serializable]
    [DataContract]
    public class FlatIndicatorRecord
    {
        [DataMember]
        public string CountryId { get; set; }      // "MY"

        [DataMember]
        public string CountryName { get; set; }    // "Malaysia"

        [DataMember]
        public string CountryIso3Code { get; set; } // "MYS"

        [DataMember]
        public int Year { get; set; }              // 2024

        [DataMember]
        public string IndicatorId { get; set; }    // "TX.VAL.MANF.ZS.UN"

        [DataMember]
        public string IndicatorName { get; set; }  // "Manufactures exports (% of merchandise exports)"

        [DataMember]
        public double? Value { get; set; }         // 69.78

        [DataMember]
        public string Unit { get; set; }

        [DataMember]
        public string ObsStatus { get; set; }

        [DataMember]
        public int Decimal { get; set; }
    }
}
