using System.Globalization;
using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

/// <summary>
/// Adds the <see cref="GeenNpcsNodig"/> flag to the NPC configuration. Supersedes
/// <see cref="PlotboekPaginaAantalNpcsAangepast"/>; the old event is kept for existing streams.
/// </summary>
public sealed record PlotboekPaginaAantalNpcsAangepastV2 : AuditTrailEvent
{
    public bool AlleNpcs { get; init; }

    public bool GeenNpcsNodig { get; init; }

    required public IReadOnlyList<NpcRolData> Rollen { get; init; }

    public override string What
    {
        get
        {
            var aantalNpcs = AantalNpcsData.Create(Rollen, AlleNpcs, GeenNpcsNodig);
            if (!aantalNpcs.HasValue)
            {
                return $"<strong>{CoreTekst.AantalNpcsAangepastLabel}</strong> {CoreTekst.Leeg}";
            }

            return $"<strong>{CoreTekst.AantalNpcsAangepast}</strong>";
        }
    }

    public override string Details
    {
        get
        {
            var aantalNpcs = AantalNpcsData.Create(Rollen, AlleNpcs, GeenNpcsNodig);
            if (!aantalNpcs.HasValue)
            {
                return null;
            }

            var parts = new List<string>();

            if (GeenNpcsNodig)
            {
                parts.Add($"<strong>{CoreTekst.AantalNpcsGeenNodig}</strong>");
            }

            if (AlleNpcs)
            {
                parts.Add($"<strong style=\"color: #dc3545;\">{CoreTekst.NpcsAlle}</strong>");
            }

            var rollenHtml = Rollen.Where(r => r.HasValue).Select(FormatRol).ToList();
            if (rollenHtml.Count > 0)
            {
                parts.AddRange(rollenHtml);
            }

            return string.Join("<br/>", parts);
        }
    }

    private static string FormatRol(NpcRolData rol)
    {
        var parts = new List<string>();

        // Details is rendered with Html.Raw, so every freely-typed value below goes through
        // PlainTextInline — the role name included, since that one is required on every role.
        var rolNaam = PlainTextInline(rol.Rol);

        string countPart;
        if (rol.MinimumAantal == 0 && !rol.MaximumAantal.HasValue)
        {
            // No count specified (for "all NPCs" scenarios)
            countPart = rolNaam;
        }
        else if (rol.IsRange)
        {
            countPart = $"{rol.MinimumAantal}-{rol.MaximumAantal} {rolNaam}";
        }
        else
        {
            countPart = $"{rol.MinimumAantal} {rolNaam}";
        }

        parts.Add($"• {countPart}");

        var annotations = new List<string>();
        if (rol.IsTerukerend) annotations.Add(CoreTekst.NpcRolTerugkerend);
        if (rol.IsEerderGespeeld)
        {
            annotations.Add(string.IsNullOrWhiteSpace(rol.GespeeldDoor)
                ? CoreTekst.NpcRolEerderGespeeld
                : string.Format(CultureInfo.CurrentCulture, CoreTekst.NpcRolEerderGespeeldDoor, PlainTextInline(rol.GespeeldDoor)));
        }
        if (rol.IsAlGekozen)
        {
            annotations.Add(string.IsNullOrWhiteSpace(rol.GekozenPersoon)
                ? CoreTekst.NpcRolAlGekozen
                : string.Format(CultureInfo.CurrentCulture, CoreTekst.NpcRolGekozen, PlainTextInline(rol.GekozenPersoon)));
        }

        if (annotations.Count > 0)
        {
            parts.Add($"<em>({string.Join(", ", annotations)})</em>");
        }

        if (!string.IsNullOrWhiteSpace(rol.KledingEnSchmink))
        {
            parts.Add($"<br/>&nbsp;&nbsp;<i class=\"fas fa-tshirt\"></i> <em>{string.Format(CultureInfo.CurrentCulture, CoreTekst.NpcRolKleding, PlainTextInline(rol.KledingEnSchmink))}</em>");
        }

        if (!string.IsNullOrWhiteSpace(rol.CastingInstructie))
        {
            parts.Add($"<br/>&nbsp;&nbsp;<i class=\"fas fa-user-check\"></i> <em>{string.Format(CultureInfo.CurrentCulture, CoreTekst.NpcRolCasting, PlainTextInline(rol.CastingInstructie))}</em>");
        }

        if (rol.VereistAfstemming)
        {
            // Set apart from the other lines: this is the one requirement that has to be discussed with a
            // human before the plot runs.
            var afstemming = string.IsNullOrWhiteSpace(rol.AfstemmingToelichting)
                ? CoreTekst.NpcRolVereistAfstemming
                : string.Format(CultureInfo.CurrentCulture, CoreTekst.NpcRolVereistAfstemmingMetToelichting, PlainTextInline(rol.AfstemmingToelichting));
            parts.Add($"<br/>&nbsp;&nbsp;<i class=\"fas fa-handshake\"></i> <strong><em>{afstemming}</em></strong>");
        }

        return string.Join(" ", parts);
    }
}
