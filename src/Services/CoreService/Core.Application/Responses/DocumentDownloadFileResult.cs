namespace Core.Application.Responses;

public sealed record DocumentDownloadFileResult(Stream Content, string ContentType, string FileName);
