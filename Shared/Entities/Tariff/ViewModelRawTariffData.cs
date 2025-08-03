namespace Entities.Tariff
{
    public class ViewModelRawTariffData
    {
        public Indicator Indicator { get; set; }
        public Country Country { get; set; }
        public string CountryIso3Code { get; set; }
        public string Date { get; set; }
        public double? Value { get; set; }
        public string Unit { get; set; }
        public string ObsStatus { get; set; }
        public int Decimal { get; set; }
    }

    public class Indicator
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }

    public class Country
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }
}
