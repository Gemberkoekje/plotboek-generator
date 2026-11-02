using System.Collections.Immutable;

namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// Replaces all references to a source SL user with a target SL user.
    /// This applies proper events for VerantwoordelijkeSl/ExtraSlIds and PropsEnLoot.
    /// Used when merging two user accounts.
    /// </summary>
    public Result<PlotboekPagina> VervangenSlUser(
        string sourceUserId,
        string targetUserId,
        string targetUserName,
        IReadOnlyDictionary<string, string> userNameLookup,
        string who)
    {
        var result = Result.For(this);

        if (ReferencesSlUser(sourceUserId))
        {
            result = result | (p => p.ReplaceSlInVerantwoordelijke(sourceUserId, targetUserId, userNameLookup, who));
        }

        if (ReferencesSlUserInPropsEnLoot(sourceUserId))
        {
            result = result | (p => p.ReplaceSlInPropsEnLoot(sourceUserId, targetUserId, targetUserName, who));
        }

        return result;
    }

    /// <summary>
    /// Returns true if the source user ID is referenced as VerantwoordelijkeSl or in ExtraSlIds.
    /// </summary>
    public bool ReferencesSlUser(string sourceUserId) =>
        VerantwoordelijkeSl == sourceUserId
        || (ExtraSlIds?.Contains(sourceUserId) == true);

    /// <summary>
    /// Returns true if the source user ID is referenced in any PropLootItemData.
    /// </summary>
    public bool ReferencesSlUserInPropsEnLoot(string sourceUserId) =>
        PropsEnLoot.Props?.Any(p => p.VerantwoordelijkeSlId == sourceUserId) == true
        || PropsEnLoot.Loot?.Any(l => l.VerantwoordelijkeSlId == sourceUserId) == true;

    private Result<PlotboekPagina> ReplaceSlInVerantwoordelijke(
        string sourceUserId,
        string targetUserId,
        IReadOnlyDictionary<string, string> userNameLookup,
        string who)
    {
        var newSlId = VerantwoordelijkeSl == sourceUserId ? targetUserId : VerantwoordelijkeSl;
        var newSlName = userNameLookup.GetValueOrDefault(newSlId, newSlId);

        var newExtraSlIds = (ExtraSlIds ?? [])
            .Select(id => id == sourceUserId ? targetUserId : id)
            .ToList();
        var newExtraSlNames = newExtraSlIds
            .Select(id => userNameLookup.GetValueOrDefault(id, id))
            .ToList();

        return AanpassenVerantwoordelijkeSl(newSlId, newSlName, newExtraSlIds, newExtraSlNames, who);
    }

    private Result<PlotboekPagina> ReplaceSlInPropsEnLoot(
        string sourceUserId,
        string targetUserId,
        string targetUserName,
        string who)
    {
        var newProps = PropsEnLoot.Props?
            .Select(p => p.VerantwoordelijkeSlId == sourceUserId
                ? p with { VerantwoordelijkeSlId = targetUserId, VerantwoordelijkeSlName = targetUserName }
                : p)
            .ToImmutableList();

        var newLoot = PropsEnLoot.Loot?
            .Select(l => l.VerantwoordelijkeSlId == sourceUserId
                ? l with { VerantwoordelijkeSlId = targetUserId, VerantwoordelijkeSlName = targetUserName }
                : l)
            .ToImmutableList();

        return AanpassenPropsEnLoot(PropsEnLoot with { Props = newProps, Loot = newLoot }, who);
    }
}
