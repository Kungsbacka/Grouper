﻿using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace GrouperLib.Database
{
    public static class SqlExtensions
    {
        private static readonly DataTable _emptyNvarcharTableParam = InitializeEmptyDataTable();

        private static DataTable InitializeEmptyDataTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("value", typeof(string));
            return dt;
        }

        public static void AddParameters(this SqlCommand command, IDictionary<string, object> parameterDictionary)
        {
            if (parameterDictionary != null)
            {
                foreach (var param in parameterDictionary)
                {
                    if (param.Value is string[])
                    {
                        string[] stringArray = (string[])param.Value;
                        SqlParameter sqlParam = command.CreateParameter();
                        sqlParam.ParameterName = param.Key;
                        sqlParam.SqlDbType = SqlDbType.Structured;
                        sqlParam.TypeName = "dbo.NvarcharTable";
                        if (stringArray.Length == 0)
                        {
                            sqlParam.Value = _emptyNvarcharTableParam;
                        }
                        else
                        {
                            sqlParam.Value = stringArray.Select(str =>
                            {
                                SqlMetaData meta = new SqlMetaData("value", SqlDbType.NVarChar, 1000);
                                SqlDataRecord record = new SqlDataRecord(meta);
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
        }

        public static T GetNullable<T>(this SqlDataReader dataReader, int i)
        {
            if (dataReader.IsDBNull(i))
            {
                return default(T);
            }
            return (T)dataReader.GetValue(i);
        }
    }
}
