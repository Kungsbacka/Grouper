using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using System.Data;

namespace GrouperLib.Database;

internal class NvarcharTableParam
{
    public string Name { get; }

    private readonly string[] _list;

    private static readonly DataTable emptyNvarcharTableParam = InitializeEmptyDataTable();

    private static DataTable InitializeEmptyDataTable()
    {
        DataTable dt = new();
        dt.Columns.Add("value", typeof(string));
        return dt;
    }

    public NvarcharTableParam(string name, string[] list)
    {
        Name = name;
        _list = list;
    }

    public void AddAsParameter(SqlCommand cmd)
    {
        SqlParameter sqlParam = cmd.CreateParameter();
        sqlParam.ParameterName = Name;
        sqlParam.SqlDbType = SqlDbType.Structured;
        sqlParam.TypeName = "dbo.NvarcharTable";
        if (_list.Length == 0)
        {
            sqlParam.Value = emptyNvarcharTableParam;
        }
        else
        {
            sqlParam.Value = _list.Select(t =>
            {
                SqlMetaData meta = new("value", SqlDbType.NVarChar, 1000);
                SqlDataRecord record = new(meta);
                record.SetValue(0, t);
                return record;
            });
        }
        cmd.Parameters.Add(sqlParam);
    }
}