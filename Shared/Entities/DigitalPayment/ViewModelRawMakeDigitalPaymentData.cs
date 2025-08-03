using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Entities.DigitalPayment
{
    [DataContract]
    public class ViewModelRawMakeDigitalPaymentData
    {
        [DataMember]
        public int count { get; set; }

        [DataMember]
        public List<FlattenMakeOrReceivedDigitalPaymentRecord> value { get; set; }
    }
}
