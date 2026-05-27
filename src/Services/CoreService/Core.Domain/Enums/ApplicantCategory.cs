namespace Core.Domain.Enums;

[Flags]
public enum ApplicantCategory
{
    None = 0,
    KnowledgeBased = 1,
    Creative = 2,
    Technologist = 4,
    Other = 8
}
