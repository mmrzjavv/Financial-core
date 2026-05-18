using Core.Domain.Enums;

namespace Core.Application.Common;

public static class DocumentVersionScopes
{
    public const string DataEntry = "data-entry";
    public const string Preliminary = "preliminary";
    public const string Contracts = "contracts";
    public const string All = "all";

    public static IReadOnlyList<DocumentType> ResolveTypes(string? scope)
    {
        return scope?.Trim().ToLowerInvariant() switch
        {
            DataEntry => ApplicantDataEntryTypes,
            Preliminary => [DocumentType.PreContract],
            Contracts => ContractTypes,
            All or null or "" => [],
            _ => []
        };
    }

    public static readonly DocumentType[] ApplicantDataEntryTypes =
    [
        DocumentType.PitchDeck,
        DocumentType.FinancialStatements,
        DocumentType.TaxDocuments,
        DocumentType.CompanyRegistration,
        DocumentType.ShareholderManager,
        DocumentType.SalesDocuments,
        DocumentType.Other
    ];

    public static readonly DocumentType[] ContractTypes =
    [
        DocumentType.PreContract,
        DocumentType.FinalContract,
        DocumentType.SignedContract
    ];
}
