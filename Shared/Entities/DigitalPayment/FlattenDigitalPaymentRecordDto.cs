using System;
using System.Runtime.Serialization;

namespace Entities.DigitalPayment
{
    [Serializable]
    [DataContract]
    public class FlattenMakeOrReceivedDigitalPaymentRecord
    {
        [DataMember]
        public string OBS_VALUE { get; set; }

        [DataMember]
        public string TIME_FORMAT { get; set; }

        [DataMember]
        public int? UNIT_MULT { get; set; }

        [DataMember]
        public string COMMENT_OBS { get; set; }

        [DataMember]
        public string OBS_STATUS { get; set; }

        [DataMember]
        public string OBS_CONF { get; set; }

        [DataMember]
        public string AGG_METHOD { get; set; }

        [DataMember]
        public int? DECIMALS { get; set; }

        [DataMember]
        public string COMMENT_TS { get; set; }

        [DataMember]
        public string DATA_SOURCE { get; set; }

        [DataMember]
        public bool LATEST_DATA { get; set; }

        [DataMember]
        public string DATABASE_ID { get; set; }

        [DataMember]
        public string INDICATOR { get; set; }

        [DataMember]
        public string REF_AREA { get; set; }

        [DataMember]
        public string SEX { get; set; }

        [DataMember]
        public string AGE { get; set; }

        [DataMember]
        public string URBANISATION { get; set; }

        [DataMember]
        public string COMP_BREAKDOWN_1 { get; set; }

        [DataMember]
        public string COMP_BREAKDOWN_2 { get; set; }

        [DataMember]
        public string COMP_BREAKDOWN_3 { get; set; }

        [DataMember]
        public string TIME_PERIOD { get; set; }

        [DataMember]
        public string FREQ { get; set; }

        [DataMember]
        public string UNIT_MEASURE { get; set; }

        [DataMember]
        public string UNIT_TYPE { get; set; }
    }
}
