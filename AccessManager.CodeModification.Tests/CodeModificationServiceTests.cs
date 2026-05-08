using System.Text.Json;
using AccessManager.UI.Services.CodeModification;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AccessManager.CodeModification.Tests;

public class CodeModificationServiceTests
{
    [Fact]
    public async Task ApplyDiff_matches_context_when_whitespace_differs()
    {
        var temp = Path.Combine(Path.GetTempPath(), "am-diff-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            const string rel = "sample.txt";
            var body = "line1\n  spaced    word\nline3\n";
            await File.WriteAllTextAsync(Path.Combine(temp, rel), body);

            var cfgPath = Path.Combine(temp, "test.settings.json");
            var cfgJson = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["Git"] = new Dictionary<string, string> { ["RepoPath"] = temp }
            });
            await File.WriteAllTextAsync(cfgPath, cfgJson);

            var config = new ConfigurationBuilder()
                .AddJsonFile(cfgPath, optional: false)
                .Build();

            var svc = new CodeModificationService(config, new StubGitService());

            var unifiedDiff = @"--- a/sample.txt
+++ b/sample.txt
@@ -1,3 +1,3 @@
 line1
-  spaced    word
+replaced
 line3
";

            var result = await svc.ApplyDiffAsync(rel, unifiedDiff);

            Assert.True(result.Success, result.Message);
            var after = await File.ReadAllTextAsync(Path.Combine(temp, rel));
            Assert.Contains("replaced", after);
            Assert.Contains("line1", after);
            Assert.Contains("line3", after);
        }
        finally
        {
            try
            {
                Directory.Delete(temp, recursive: true);
            }
            catch
            {
                /* temp cleanup best-effort */
            }
        }
    }

    [Fact]
    public async Task ApplyDiff_matches_context_with_tabs_and_spaces_normalized()
    {
        var temp = Path.Combine(Path.GetTempPath(), "am-diff-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            const string rel = "sample-tabs.txt";
            var body = "line1\n\tspaced\tword\nline3\n";
            await File.WriteAllTextAsync(Path.Combine(temp, rel), body);

            var cfgPath = Path.Combine(temp, "test.settings.json");
            var cfgJson = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["Git"] = new Dictionary<string, string> { ["RepoPath"] = temp }
            });
            await File.WriteAllTextAsync(cfgPath, cfgJson);

            var config = new ConfigurationBuilder()
                .AddJsonFile(cfgPath, optional: false)
                .Build();

            var svc = new CodeModificationService(config, new StubGitService());

            var unifiedDiff = @"--- a/sample-tabs.txt
+++ b/sample-tabs.txt
@@ -1,3 +1,3 @@
 line1
-  spaced   word
+replaced-tabs
 line3
";

            var result = await svc.ApplyDiffAsync(rel, unifiedDiff);

            Assert.True(result.Success, result.Message);
            var after = await File.ReadAllTextAsync(Path.Combine(temp, rel));
            Assert.Contains("replaced-tabs", after);
        }
        finally
        {
            try
            {
                Directory.Delete(temp, recursive: true);
            }
            catch
            {
                /* temp cleanup best-effort */
            }
        }
    }
}
