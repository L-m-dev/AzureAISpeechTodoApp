using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Web;



namespace DatabaseLayer

{
    public class DatabaseLayer
    {
        private NpgsqlConnection conn;
        private string connectionString;
        public DatabaseLayer()
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

             connectionString = config.GetConnectionString("DefaultConnection");
         }

        public async Task<IEnumerable<TodoTask>> GetAll()
        {
            var tasks = new List<TodoTask>();
            using var conn = new NpgsqlConnection(connectionString);
            using var command = conn.CreateCommand();

            await conn.OpenAsync();

            command.CommandText = "SELECT * FROM repo";

            using var reader = await command.ExecuteReaderAsync();

            if (reader is not null)
            {
                while (await reader.ReadAsync())
                {
                    int id;
                    string description;
                    DateTime date;

                    if (reader["id"] != DBNull.Value)
                    {
                        id = Convert.ToInt32(reader["id"]);
                    }
                    else
                    {
                        id = -1;
                    }

                    if (reader["description"] != DBNull.Value)
                    {
                        description = Convert.ToString(reader["description"]);
                    }
                    else
                    {
                        description = "No description";
                    }

                    if (reader["date"] != DBNull.Value)
                    {
                        date = Convert.ToDateTime(reader["date"]);
                    }
                    else
                    {
                        date = DateTime.MinValue;
                    }

                    tasks.Add(new TodoTask(description, date, id)); 

                }

            }
            await conn.CloseAsync();
            return tasks;

        }

        public async Task<IEnumerable<TodoTask>> GetResult(TodoTask task)   
        {
            var tasks = new List<TodoTask>();

            using var conn = new NpgsqlConnection(connectionString);
        
            string query = "SELECT * FROM repo WHERE 1=1 ";

            if(task.Id != 0)
            {
                query += "AND id = @Id ";
            }
            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                query += "AND description = @Description ";
            }
            if (task.Date.HasValue)
            {
                query += "AND \"date\" = @Date ";
            }
            using var command = conn.CreateCommand();
            command.CommandText = query;

            if (task.Id != 0)
            {
                command.Parameters.AddWithValue("@Id", task.Id);
            }
            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                command.Parameters.AddWithValue("@Description", task.Description);
            }
            if (task.Date.HasValue)
            {
                command.Parameters.AddWithValue("@Date", task.Date.Value);
            }
            await conn.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if(reader is not null)
            {
                while (await reader.ReadAsync())
                {
                    int id;
                    string description;
                    DateTime date;

                    if (reader["id"] != DBNull.Value)
                    {
                        id = Convert.ToInt32(reader["id"]);
                    }
                    else
                    {
                        id = -1;
                    }

                    if (reader["description"] != DBNull.Value)
                    {
                        description = Convert.ToString(reader["description"]);
                    }
                    else
                    {
                        description = "No description";
                    }

                    if (reader["date"] != DBNull.Value)
                    {
                        date = Convert.ToDateTime(reader["date"]);
                    }
                    else
                    {
                        date = DateTime.MinValue;
                    }

                    tasks.Add(new TodoTask(description, date,id));

                }
            }
            await conn.CloseAsync();
            return tasks;

        }


        public async Task<bool> InsertTodoTask(TodoTask task)
        {

            using var conn = new NpgsqlConnection(connectionString);
            string query = "INSERT INTO repo (description, date) VALUES (@Description, @Date)";
            await conn.OpenAsync();
            using var command = conn.CreateCommand();
            command.CommandText = query;
           
            command.Parameters.AddWithValue("@Description", task.Description);
            command.Parameters.AddWithValue("@Date", task.Date);

            try
            {
                var rowAffected = await command.ExecuteNonQueryAsync();
            }catch(Exception ex) { Console.WriteLine(ex.Message); }
             
            await conn.CloseAsync();

            return false; ;

        }

        public async Task<bool> DeleteTodoTask(TodoTask task)
        {
            if(task.Id == 0)
            {
                return false;
            }
            using var conn = new NpgsqlConnection(connectionString);
            string query = "DELETE FROM repo WHERE id = @Id";
 
            using var command = conn.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@Id", task.Id);

            await conn.OpenAsync();

            var rowAffected = await command.ExecuteNonQueryAsync();
            await conn.CloseAsync();
            return rowAffected > 0;

        }

        public async Task<bool> DeleteAllTask()
        {
            try {
                using var conn = new NpgsqlConnection(connectionString);
                string query = "TRUNCATE TABLE repo";
                using var command = conn.CreateCommand();
                command.CommandText = query;
                await conn.OpenAsync();
                var rowAffected = await command.ExecuteNonQueryAsync();

                await conn.CloseAsync();
                return true;
            } catch(NpgsqlException ex)
            {
                return false;
            } catch(Exception ex)
            {
                return false;
            }
          }

        public async Task<bool> UpdateTodoTask(TodoTask task)
        {
            string query = "UPDATE repo SET description = @Description, date = @Date WHERE id = @Id";

            string updatedDescription;
            DateTime? updatedDate;

            IEnumerable<TodoTask> tasksFromDatabase = await this.GetResult(new TodoTask(null,null,task.Id));
            TodoTask taskFromDatabase = tasksFromDatabase.FirstOrDefault();

            int? updatedId = taskFromDatabase.Id;

            if (string.IsNullOrWhiteSpace(task.Description))
            {
                updatedDescription = taskFromDatabase.Description;

            }
            else
            {
                updatedDescription = task.Description;
            }
            if (!task.Date.HasValue)
            {
                updatedDate = taskFromDatabase.Date;
            }
            else
            {
                updatedDate = task.Date;
            }

            TodoTask updatedTask = new TodoTask( updatedDescription, updatedDate,updatedId);

            using var conn = new NpgsqlConnection(connectionString);
            using var command = conn.CreateCommand();
            command.CommandText = query;

            command.Parameters.AddWithValue("@Id", updatedTask.Id);
            command.Parameters.AddWithValue("@Description", updatedTask.Description);
            command.Parameters.AddWithValue("@Date", updatedTask.Date);

            await conn.OpenAsync();

            var rowAffected = await command.ExecuteNonQueryAsync();
            await conn.CloseAsync();
            return rowAffected > 0;

        }
    }
}
