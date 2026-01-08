using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Content.Shared.Contraband;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Paper;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._Coyote.Commands;

internal sealed class GetAllContraband : LocalizedEntityCommands
{
    [Dependency] private readonly IMapManager            _map = default!;
    [Dependency] private readonly IEntityManager         _entityManager = default!;
    [Dependency] private readonly IPrototypeManager      _prototypeManager = default!;
    [Dependency] private readonly SharedTransformSystem  _transform = default!;
    [Dependency] private readonly SharedMapSystem        _mapSystem = default!;
    [Dependency] private readonly PaperSystem            _paperSystem = default!;
    [Dependency] private readonly SharedHandsSystem      _heandsSystem = default!;

    public override string Command => "allcontraband";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { AttachedEntity: { } entity })
            return;

        var transform = _entityManager.GetComponent<TransformComponent>(entity);


        var finalText   = GetAllContrabandText();
        var coords      = _transform.GetMapCoordinates(entity);
        var paperEntity = _entityManager.Spawn("Paper", coords);
        _entityManager.TryGetComponent<PaperComponent>(paperEntity, out var paperComp);
        _paperSystem.SetContent((paperEntity, paperComp!), finalText);
        // then, THEN, we give it to the reader.
        _heandsSystem.TryPickupAnyHand(entity, paperEntity);
        //whatever

        shell.WriteLine($"Wish granted, heres a paper with all the contraband on it. Enjoy!");
    }

    /// <summary>
    /// All prototypes with ContrabandComponent, formatted as a long bulleted list.
    /// Goes through the yaml prototypes, not the spawned entities.
    /// </summary>
    /// <param name="entityManager"></param>
    /// <returns></returns>
    private string GetAllContrabandText()
    {
        List<EntityPrototype> contrabandProtos =
            _prototypeManager.EnumeratePrototypes<EntityPrototype>()
                .Where(proto => proto.Components.ContainsKey("Contraband"))
                .ToList();

        // lets make it pretty, and organized by severity

        Dictionary<string, List<string>> sorted = new();
        foreach (var proto in contrabandProtos)
        {
            if(!proto.TryGetComponent<ContrabandComponent>(out var contrabandComp))
                continue;
            // got the component, now format info
            List<string> lines = new();
            lines.Add($"• {proto.Name}");
            lines.Add($"    - ID: {proto.ID}");
            lines.Add($"    - Severity: {contrabandComp.Severity.ToString()}");
            if (contrabandComp.AllowedDepartments.Count > 0)
            {
                lines.Add($"    - Allowed Departments:");
                lines.Add($"        + {string.Join("\n        + ", contrabandComp.AllowedDepartments)}");
            }
            if (contrabandComp.AllowedJobs.Count > 0)
            {
                lines.Add($"    - Allowed Jobs:");
                lines.Add($"        + {string.Join("\n        + ", contrabandComp.AllowedJobs)}");
            }
            if (contrabandComp.TurnInValues.Count > 0)
            {
                lines.Add($"    - Turn-In Values:");
                foreach (var (faction, value) in contrabandComp.TurnInValues)
                {
                    lines.Add($"        > {faction}: {value.ToString(CultureInfo.InvariantCulture)}");
                }
            }
            lines.Add(""); // extra newline for spacing
            string entry = string.Join("\n", lines);
            if (!sorted.ContainsKey(contrabandComp.Severity.ToString()))
            {
                sorted[contrabandComp.Severity.ToString()] = new List<string>();
            }
            sorted[contrabandComp.Severity.ToString()].Add(entry);
        }
        // sort by severity key alphabetically
        var orderedKeys = sorted.Keys.OrderBy(k => k).ToList();
        List<string> finalLines = new();
        finalLines.Add("All Contraband Prototypes:");
        finalLines.Add("");
        foreach (var key in orderedKeys)
        {
            // alphabetize the entries within severity
            sorted[key] = sorted[key].OrderBy(s => s).ToList();
            finalLines.Add($"=== Severity: {key} ===");
            finalLines.Add("");
            finalLines.AddRange(sorted[key]);
        }
        return string.Join("\n", finalLines);
    }


    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}
