using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using System.Data;

namespace GrouperLib.Database;

internal static class SqlExtensions
{
    private static readonly DataTable emptyNvarcharTableParam = InitializeEmptyDataTable();

    private static DataTable InitializeEmptyDataTable()
    {
        DataTable dt = new();
        dt.Columns.Add("value", typeof(string));
        return dt;
    }

    public static void AddParameters(this SqlCommand command, IDictionary<string, object?>? parameterDictionary)
    {
        if (parameterDictionary == null)
        {
            return;
        }
        foreach (KeyValuePair<string, object?> param in parameterDictionary)
        {
            if (param.Value == null)
            {
                continue;
            } 
            
            if (param.Value is string[] stringArray)
            {
                SqlParameter sqlParam = command.CreateParameter();
                sqlParam.ParameterName = param.Key;
                sqlParam.SqlDbType = SqlDbType.Structured;
                sqlParam.TypeName = "dbo.NvarcharTable";
                if (stringArray.Length == 0)
                {
                    sqlParam.Value = emptyNvarcharTableParam;
                }
                else
                {
                    sqlParam.Value = stringArray.Select(str =>
                    {
                        SqlMetaData meta = new("value", SqlDbType.NVarChar, 1000);
                        SqlDataRecord record = new(meta);
                        record.SetValue(0, str);
                        return record;
                    });
                }
                command.Parameters.Add(sqlParam);
            }
            else
            {
                command.Parameters.AddWithValue(param.Key, param.Value);
            }
        }
    }

    public static T? GetNullable<T>(this SqlDataReader dataReader, int i)
    {
        if (dataReader.IsDBNull(i))
        {
            return default;
        }
        return (T)dataReader.GetValue(i);
    }
}