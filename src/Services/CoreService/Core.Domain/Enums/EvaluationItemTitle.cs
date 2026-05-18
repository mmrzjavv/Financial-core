namespace Core.Domain.Enums;

public static class EvaluationItemTitle
{
    public static IReadOnlyList<string> DataEntry1Checklist { get; } =
    [
        "Pitch Deck",
        "Financial Statements / Tax Documents",
        "Input Market Size Check",
        "Fund Value Chain Validation",
        "Company Registration Documents",
        "Shareholder & Manager Validation",
        "Sales Documents Validation"
    ];

    public static IReadOnlyList<string> DataEntry2Checklist { get; } =
    [
        "Investment Attraction Plans",
        "Technical Evaluation",
        "Financial Evaluation",
        "Team Evaluation",
        "Market Evaluation",
        "Business Plan Evaluation"
    ];
}

