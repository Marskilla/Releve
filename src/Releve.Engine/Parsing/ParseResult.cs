using Releve.Engine.Core;

namespace Releve.Engine.Parsing;

/// <summary>Erreurs de l'analyseur (§6.7).</summary>
public enum ParseError
{
    None,
    Empty,
    UnknownVerb,
    MissingObject,
    UnknownObject,
    Ambiguous,
}

/// <summary>Issue d'une analyse : commande, ou erreur avec candidats en cas d'ambiguite.</summary>
public sealed record ParseResult(Command? Command, ParseError Error = ParseError.None, IReadOnlyList<string>? CandidateNames = null)
{
    public static ParseResult Ok(Command command) => new(command);

    public static ParseResult Fail(ParseError error, IReadOnlyList<string>? candidates = null) => new(null, error, candidates);

    /// <summary>Message neutre correspondant a l'erreur.</summary>
    public string ErrorMessage => Error switch
    {
        ParseError.UnknownVerb => Core.Messages.UnknownVerb,
        ParseError.MissingObject => Core.Messages.MissingObject,
        ParseError.UnknownObject => Core.Messages.UnknownObject,
        ParseError.Ambiguous => Core.Messages.AmbiguousPrefix
            + (CandidateNames is { Count: > 0 } ? string.Join(" ou ", CandidateNames) + "." : string.Empty),
        _ => string.Empty,
    };
}
