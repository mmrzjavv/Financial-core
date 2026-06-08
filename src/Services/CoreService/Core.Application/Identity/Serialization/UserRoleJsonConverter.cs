using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Domain.Identity;

namespace Core.Application.Identity.Serialization;

public sealed class NullableUserRoleJsonConverter : JsonConverter<UserRole?>
{
    public override UserRole? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.Number)
        {
            var roleNumber = reader.GetInt32();
            if (Enum.IsDefined(typeof(UserRole), roleNumber))
                return (UserRole)roleNumber;

            throw new JsonException($"Invalid role number: {roleNumber}");
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var roleText = reader.GetString();
            if (UserRoleClaims.TryParse(roleText, out var role))
                return role;

            throw new JsonException($"Invalid role: {roleText}");
        }

        throw new JsonException("Role must be a number or string.");
    }

    public override void Write(Utf8JsonWriter writer, UserRole? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(UserRoleClaims.From(value.Value));
    }
}
