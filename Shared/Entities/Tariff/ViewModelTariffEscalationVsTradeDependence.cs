using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Entities.Tariff
{
    [Serializable]
    [DataContract]
    public class ViewModelTariffEscalationVsTradeDependence
    {
        [DataMember]
        public string Country { get; set; }

        [DataMember]
        public List<ViewModelManufacturesExports> ManufacturesExports { get; set; }

        [DataMember]
        public List<ViewModelForeignDirectInvestmentNetInflows> ForeignDirectInvestmentNetInflows { get; set; }

        [DataMember]
        public List<ViewModelTariffRateAppliedWeightedMeanAllProducts> TariffRateAppliedWeightedMeanAllProducts { get; set; }

        [DataMember]
        public List<ViewModelTradePercentageOfGDP> TradePercentageOfGDP { get; set; }
    }
}
