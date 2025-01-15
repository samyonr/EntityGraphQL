using Argon;

namespace EntityGraphQL.AspNet.Extra.Tests.Infra;

public class OrderedContractResolver : DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(
        Type type,
        MemberSerialization memberSerialization)
    {
        // Create properties normally
        IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);

        // Order properties alphabetically by their JSON names
        return properties.OrderBy(p => p.PropertyName).ToList();
    }
}