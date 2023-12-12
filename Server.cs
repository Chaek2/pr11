using SYEL.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace SYEL
{
    public class Server
    {
        public enum Function { select, insert, update, delete };
        public static DataSet dataSet = new DataSet();
        private static string connText = "Data Source=HOME-PC\\MYSERV;Initial Catalog=SYEL;Integrated Security=false; user id=sa; password=123"; // строка подключения
        private static string return_TableName = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES"; // вывод таблиц
        private static string return_ColumnName = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '"; // вывод столбцов таблицы
        private static string return_ColumnID = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE ORDINAL_POSITION = 1 and TABLE_NAME = '"; // вывод 1-ого столбца 
        private static SqlConnection sqlConn = new SqlConnection(connText);
        public string Execution(string table, Function function = Function.select, ArrayList valueList = null, int id = 0)
        {
            try
            {
                
                string query = "";
                SqlDataAdapter adapter = new SqlDataAdapter("", sqlConn);
                DataTable datatable = new DataTable();
                SqlCommand command = new SqlCommand("", sqlConn);
                dataSet.Tables[table].Columns.Clear();
                dataSet.Tables[table].Rows.Clear();
                switch (function)
                {
                    case Function.select:
                        adapter.SelectCommand = new SqlCommand($"SELECT * FROM {table}", sqlConn);
                        adapter.Fill(dataSet.Tables[table]);
                        break;
                    case Function.insert:
                        command.CommandText = return_ColumnName + $"{table}'";
                        datatable.Load(command.ExecuteReader());
                        query = $"INSERT {table} (";
                        for (int i = 1; i < datatable.Rows.Count; i++)
                        {
                            query += $" {datatable.Rows[i][0]}";
                            if (i < datatable.Rows.Count - 1)
                                query += ",";
                        }
                        query += ") VALUES (";
                        for (int i = 1; i <= datatable.Rows.Count - 1; i++)
                        {
                            query += $" @{datatable.Rows[i][0]}";
                            if (i < datatable.Rows.Count - 1)
                                query += ",";
                        }
                        query += ")";
                        adapter.InsertCommand = new SqlCommand(query, sqlConn);
                        adapter.InsertCommand.Parameters.Clear();
                        for (int i = 1; i < datatable.Rows.Count; i++)
                        {
                            adapter.InsertCommand.Parameters.AddWithValue($"@{datatable.Rows[i][0]}", valueList[i - 1]);
                        }
                        adapter.InsertCommand.ExecuteNonQuery();
                        adapter.SelectCommand = new SqlCommand($"SELECT * FROM {table}", sqlConn);
                        adapter.Fill(dataSet.Tables[table]);
                        break;
                    case Function.update:
                        if ((id > 0))
                        {
                            command.CommandText = return_ColumnName + $"{table}'";
                            datatable.Load(command.ExecuteReader());
                            query = $"UPDATE {table} SET ";
                            for (int i = 1; i < datatable.Rows.Count; i++)
                            {
                                query += $" {datatable.Rows[i][0]} = @{datatable.Rows[i][0]}";
                                if (i < datatable.Rows.Count - 1)
                                    query += ",";
                            }
                            query += $" WHERE {datatable.Rows[0][0]} = {id}";
                            adapter.UpdateCommand = new SqlCommand(query, sqlConn);
                            adapter.UpdateCommand.Parameters.Clear();
                            for (int i = 1; i <= datatable.Rows.Count - 1; i++)
                            {
                                adapter.UpdateCommand.Parameters.AddWithValue($"@{datatable.Rows[i][0]}", valueList[i - 1]);
                            }
                            adapter.UpdateCommand.ExecuteNonQuery();
                            adapter.SelectCommand = new SqlCommand($"SELECT * FROM {table}", sqlConn);
                            adapter.Fill(dataSet.Tables[table]);
                        }
                        break;
                    case Function.delete:
                        if ((id > 0))
                        {
                            command.CommandText = return_ColumnID + $"{table}'";
                            datatable.Load(command.ExecuteReader());
                            adapter.DeleteCommand = new SqlCommand($"DELETE FROM {table} WHERE {datatable.Rows[0][0]} = {id}", sqlConn);
                            adapter.DeleteCommand.ExecuteNonQuery();
                            adapter.SelectCommand = new SqlCommand($"SELECT * FROM {table}", sqlConn);
                            adapter.Fill(dataSet.Tables[table]);
                        }
                        break;
                }
                return "OK";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return ex.Message;
            }
            finally { }
        }
        public void Loginig(string login, string password)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("", sqlConn);
                DataTable client = new DataTable();
                adapter.SelectCommand = new SqlCommand($"Select * from [dbo].[Logining]('{login}','{password}')", sqlConn);
                adapter.Fill(client);
                if (client.Rows.Count == 1)
                {
                    Settings.Default.id_client = Int32.Parse(client.Rows[0][0].ToString());
                    Settings.Default.role_client = Int32.Parse(client.Rows[0][1].ToString());
                    Settings.Default.ln_client = login;
                    Settings.Default.pwd_client = password;
                }
                else
                {
                    Settings.Default.id_client = 0;
                    Settings.Default.role_client = 0;
                    Settings.Default.ln_client = "";
                    Settings.Default.pwd_client = "";
                }
            }
            catch { }
        }
        public string Registration(string login, string password)
        {
            try
            {

                SqlDataAdapter adapter = new SqlDataAdapter("", sqlConn);
                DataTable client = new DataTable();
                adapter.SelectCommand = new SqlCommand($"Select * from [dbo].[Client]", sqlConn);
                adapter.Fill(client);
                for (int i = 0; i < client.Rows.Count; i++)
                {
                    if (client.Rows[i][2].ToString() == login)
                    {
                        return "Такой логин уже есть";
                    }
                }
                ArrayList array = new ArrayList();
                array.Add(3);
                array.Add(login);
                array.Add(password);
                string error = Execution("Client", Function.insert, array);
                if (error == "OK")
                {
                    Loginig(login, password);
                    return "OK";
                }
                else
                {
                    return error;
                }
            }
            catch { return "BAD"; }
        }
        public string UpdateClient(string login, string password, int id)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("", sqlConn);
                string query = $"UPDATE Client set [Login] = '{login}', [Password] = (Select [dbo].[Hashing]('{password}')) where [ID_Client] = {id}";
                adapter.UpdateCommand = new SqlCommand(query, sqlConn);
                adapter.UpdateCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return ex.Message;
            }
            finally {  }
            return "OK";
        }
        public DataTable BooksFull(int id)
        {
            SqlDataAdapter adapter = new SqlDataAdapter("", sqlConn);
            DataTable books = new DataTable();
            adapter.SelectCommand = new SqlCommand($"select * from [dbo].[Bookes]({id})", sqlConn);
            adapter.Fill(books);
            return books;
        }
    }
}
