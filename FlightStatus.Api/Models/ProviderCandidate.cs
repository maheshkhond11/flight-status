namespace FlightStatus.Api.Models
{
    public class ProviderCandidate
    {
        public ProviderCandidate(string providerName, FlightStatusResult result)
        {
            ProviderName = providerName;
            Result = result;
        }
        public string ProviderName { get; set; }

        public FlightStatusResult Result { get; set; }
    }
}
