using DataBaseAPI.Models;
using DotNetEnv;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Internal.DataProvider.PostgreSQL;

namespace DataBaseAPI
{
    public class AppDataConnection : DataConnection
    {
        public AppDataConnection() : base(
            new DataOptions()
                .UsePostgreSQL(Environment.GetEnvironmentVariable("CONNECTION_STRING"))
        )
        { }

        public ITable<TaskTableModel> MailTasks => this.GetTable<TaskTableModel>();
        public ITable<UserTableModel> Users => this.GetTable<UserTableModel>();
    }
}