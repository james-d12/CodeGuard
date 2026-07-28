using System.Xml.Linq;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Analyzers;

namespace RulesEngine.Evaluation.Analyzers;

/// <summary>
/// Flags projects matching a configured name pattern that don't follow a configured bootstrap
/// convention: (a) no call-site matching a configured pattern exists in the project, and/or (b)
/// the project file doesn't have a `&lt;Content Include=&gt;` entry referencing a configured folder
/// name (docs/RULE_COVERAGE_PLAN.md skill.reporting.dbup-bootstrap is one example configuration of
/// this, using DbUp's <c>DeployChanges</c> call and a <c>Scripts</c> folder). This is an
/// approximation of that rule's full original description - the filename-pattern half of that
/// description is already covered declaratively by Stage A's
/// golden-persistence-dbup-script-naming-001.yml. All three coordinates (which projects, which
/// call, which folder) are rule YAML parameters, not hardcoded here, so this analyzer works for
/// any "matching projects must call X and package folder Y as content" convention, not just DbUp.
/// </summary>
public sealed class ProjectConventionAnalyzer(
    string projectPattern,
    string requiredCallPattern = "*DeployChanges*",
    string requiredContentFolder = "Scripts") : ICustomAnalyzer
{
    public string Name => "project-convention";

    public IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model)
    {
        var projects = model.Solutions.SelectMany(s => s.Projects)
            .Where(p => GlobMatcher.IsMatch(p.Name, projectPattern));

        foreach (var project in projects)
        {
            var hasRequiredCallSite = model.CallSites.Any(cs =>
                cs.ProjectName == project.Name && GlobMatcher.IsMatch(cs.InvokedMember, requiredCallPattern));
            var hasRequiredContentEntry = HasRequiredContentEntry(project.Path);

            if (!hasRequiredCallSite || !hasRequiredContentEntry)
            {
                var missing = string.Join(" and ", new[]
                {
                    !hasRequiredCallSite ? $"a call-site matching '{requiredCallPattern}'" : null,
                    !hasRequiredContentEntry ? $"a <Content Include=> entry referencing '{requiredContentFolder}'" : null
                }.Where(m => m is not null));

                yield return new AnalyzerViolation(
                    Message: $"Project '{project.Name}' is missing {missing} expected by this project's bootstrap convention.",
                    FilePath: project.Path,
                    ProjectName: project.Name);
            }
        }
    }

    private bool HasRequiredContentEntry(string projectPath)
    {
        if (!File.Exists(projectPath))
        {
            return false;
        }

        var document = XDocument.Load(projectPath);
        return document.Descendants()
            .Where(e => e.Name.LocalName == "Content")
            .Select(e => e.Attribute("Include")?.Value)
            .Any(include => include is not null && include.Contains(requiredContentFolder, StringComparison.OrdinalIgnoreCase));
    }
}
