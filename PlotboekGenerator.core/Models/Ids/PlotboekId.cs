using Qowaiv.Customization;
using Qowaiv.OpenApi;
using System.Security.Cryptography;
using System.Text;

namespace PlotboekGenerator.core.Models;

[OpenApiDataType(
    description: "Plotboek Id",
    type: "Uuid",
    example: "a3c01573-7941-4d1d-a6f7-6f35d003595a")]
[Id<UuidBehavior, Uuid>]
public readonly partial struct PlotboekId
{
    // Namespace UUID for plotboek ids (generated once, used as constant: every id depends on it)
    private static readonly Guid Namespace = new ("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

    [Obsolete("Use Create(Year jaar, string eventId) to avoid legacy enum coupling.")]
    public static PlotboekId Create(Year jaar, LegacyEvent @event)
    {
        var name = $"{jaar}-{@event}";
        return new PlotboekId(CreateDeterministicUuid(Namespace, name));
    }

    public static PlotboekId Create(Year jaar, string eventId)
    {
        var name = $"{jaar}-{eventId}";
        return new PlotboekId(CreateDeterministicUuid(Namespace, name));
    }

    public static PlotboekId CreateEvergreen() => Create(Year.Create(1), "Evergreen");

    private static Uuid CreateDeterministicUuid(Guid namespaceId, string name)
    {
        // UUID version 5 (SHA-1 based) - deterministic UUID generation
        var namespaceBytes = namespaceId.ToByteArray();
        SwapByteOrder(namespaceBytes);

        var nameBytes = Encoding.UTF8.GetBytes(name);
        var data = new byte[namespaceBytes.Length + nameBytes.Length];
        namespaceBytes.CopyTo(data, 0);
        nameBytes.CopyTo(data, namespaceBytes.Length);

        var hash = SHA1.HashData(data);

        var result = new byte[16];
        Array.Copy(hash, 0, result, 0, 16);

        // Set version to 5 (SHA-1 based)
        result[6] = (byte)((result[6] & 0x0F) | 0x50);
        // Set variant to RFC 4122
        result[8] = (byte)((result[8] & 0x3F) | 0x80);

        SwapByteOrder(result);
        return Uuid.Parse(new Guid(result).ToString());
    }

    private static void SwapByteOrder(byte[] guid)
    {
        (guid[0], guid[3]) = (guid[3], guid[0]);
        (guid[1], guid[2]) = (guid[2], guid[1]);
        (guid[4], guid[5]) = (guid[5], guid[4]);
        (guid[6], guid[7]) = (guid[7], guid[6]);
    }
}
