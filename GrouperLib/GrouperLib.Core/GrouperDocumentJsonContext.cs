using System.Text.Json.Serialization;

namespace GrouperLib.Core;

[JsonSerializable(typeof(GrouperDocument))]
[JsonSerializable(typeof(GrouperDocumentMember))]
[JsonSerializable(typeof(GrouperDocumentRule))]
[JsonSerializable(typeof(ValidationError))]
public partial class GrouperDocumentJsonContext : JsonSerializerContext
{

}
