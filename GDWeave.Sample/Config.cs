using System.Text.Json.Serialization;

namespace GDWeave.Sample;

public class Config {
    [JsonInclude] public bool SomeSetting = true;
}
