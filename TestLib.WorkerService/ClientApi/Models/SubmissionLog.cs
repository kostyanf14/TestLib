using Newtonsoft.Json;

namespace TestLib.Worker.ClientApi.Models
{
    public enum SubmissionLogType : byte
    {
        Source = 0,
        Checker = 1,
    }
    
    public class SubmissionLog
    {
        [JsonIgnore]
        public ulong SubmissionId;
        [JsonProperty(PropertyName = "type")]
        public SubmissionLogType Type;
        [JsonProperty(PropertyName = "data")]
        public string Data;
    }
}