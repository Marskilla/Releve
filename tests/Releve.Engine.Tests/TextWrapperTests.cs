using Releve.Engine.Core;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>
/// Tests pour TextWrapper (J2-3).
/// Mesure simulee : 1 unite par caractere.
/// </summary>
public class TextWrapperTests
{
    private static float Measure(string s) => s.Length;

    [Fact]
    public void Text_Width_Fits_Returns_Single_Line()
    {
        var result = TextWrapper.Wrap("court", 10f, Measure);
        Assert.Single(result);
        Assert.Equal("court", result[0]);
    }

    [Fact]
    public void Text_Wraps_At_Spaces()
    {
        var result = TextWrapper.Wrap("un texte plus long", 7f, Measure);
        Assert.All(result, line => Assert.True(line.Length <= 7));
        Assert.Equal("un texte plus long", string.Join(" ", result));
    }

    [Fact]
    public void Word_Longer_Than_Width_Is_Cut()
    {
        var result = TextWrapper.Wrap("treslongmot", 5f, Measure);
        Assert.All(result, line => Assert.True(line.Length <= 5));
        Assert.Equal("treslongmot", string.Concat(result));
    }

    [Fact]
    public void Empty_Lines_Are_Preserved()
    {
        var result = TextWrapper.Wrap("a\n\nb", 10f, Measure);
        Assert.Equal(3, result.Count);
        Assert.Equal("a", result[0]);
        Assert.Equal(string.Empty, result[1]);
        Assert.Equal("b", result[2]);
    }

    [Fact]
    public void CRLF_Is_Normalized_To_LF()
    {
        var result = TextWrapper.Wrap("a\r\nb", 10f, Measure);
        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0]);
        Assert.Equal("b", result[1]);
    }

    [Fact]
    public void Empty_String_Returns_One_Empty_Line()
    {
        var result = TextWrapper.Wrap(string.Empty, 10f, Measure);
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0]);
    }
}