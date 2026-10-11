using Releve.Engine.Parsing;
using Xunit;

namespace Releve.Engine.Tests;

public class NormalizationTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("Bonjour", "bonjour")]
    [InlineData("Élève à l'école", "eleve a ecole")]
    [InlineData("Élève à l’école", "eleve a ecole")]
    [InlineData("Prends la clé.", "prends cle")]
    [InlineData("Prends la CLEF !", "prends clef")]
    [InlineData("L’enfant où est-il ?", "enfant ou estil")]
    [InlineData("un œuf et une clé", "oeuf et cle")]
    [InlineData("cæcum", "caecum")]
    [InlineData("vont-ils", "vontils")]
    [InlineData("parle à l'androïde", "parle a androide")]
    [InlineData("utilise le badge sur le lecteur", "utilise badge sur lecteur")]
    [InlineData("utilise le badge au lecteur", "utilise badge a lecteur")]
    [InlineData("« Porte » … fermée", "porte fermee")]
    [InlineData("Ça — non", "ca non")]
    [InlineData("Ouvrez  l'armoire", "ouvrez armoire")]
    public void Normalize_Removes_Accents_Articles_And_Punctuation(string input, string expected)
    {
        var words = TextNormalizer.Normalize(input);
        Assert.Equal(expected, string.Join(" ", words));
    }

    [Fact]
    public void Normalize_Keeps_Prepositions_In_Canonical_Form()
    {
        var words = TextNormalizer.Normalize("avec à au aux sur dans vers contre");
        Assert.Equal(new[] { "avec", "a", "a", "a", "sur", "dans", "vers", "contre" }, words);
    }

    [Fact]
    public void Normalize_Aliases_Are_Normalized_The_Same_Way()
    {
        var alias = TextNormalizer.Normalize("lecteur de badge");
        var input = TextNormalizer.Normalize("utilise le lecteur de badge");
        Assert.Equal("lecteur badge", string.Join(" ", alias));
        Assert.DoesNotContain("de", input);
    }
}
