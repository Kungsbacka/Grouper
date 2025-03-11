using System.Runtime.Versioning;
using GrouperLib.Core;

namespace CompileTarget;

public static class CompileTarget
{
    [SupportedOSPlatform("windows")]
    public static async Task<int> Main()
    {
        const string json = """
                            {
                             "id" : "72834f7c-f286-4207-9b5b-013648ba98cf",
                             "groupId" : "f0a05cfe-5e6f-4eca-abc5-8d0bdfc4c266",
                             "GROUPName" : "DL VO Björkris Vård & Omsorgsboende  Hus 4",
                             "store" : "Exo",
                             "interval": 1,
                             "members" : [ {
                               "source" : "Personalsystem",
                               "action" : "Include",
                               "rules" : [ {
                                 "name" : "Organisation",
                                 "value" : "011J0000I006"
                               }, {
                                 "name" : "IncludeManager",
                                 "value" : "true"
                               } ]
                             } ]
                            }
                            """;
        
        var doc = GrouperDocument.FromJson(json);
        Console.WriteLine(doc.ToJson(true));
        
        return await Task.FromResult(0);
    }
}