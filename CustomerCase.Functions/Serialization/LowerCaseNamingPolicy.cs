using Microsoft.Extensions.Logging;

namespace CustomerCase.Functions;

public class LowerCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
            return name;


        var lowerInvariant = char.ToLowerInvariant(name[0]) + name[1..];

        return lowerInvariant;
    }
}