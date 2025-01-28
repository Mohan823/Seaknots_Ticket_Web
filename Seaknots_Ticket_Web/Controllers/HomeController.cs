using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Seaknots_Ticket_Web.Models;
using System.Data;
using System.Diagnostics;

namespace Seaknots_Ticket_Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string connectionString;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection").ToString();
            _logger = logger;
        }

        public IActionResult Index(string? category)
        {
            List<JiraCount> jiraStatuses = new();
            using (SqlConnection connection = new(connectionString))
            {
                string searchParam = GetParam(category);
                string query = string.Empty;
                SqlCommand cmd = new("sp_GetSTSDashboardCount", connection);
                cmd.Parameters.AddWithValue("@Type", searchParam);
                cmd.CommandType = CommandType.StoredProcedure;
                connection.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.HasRows)
                    {
                        jiraStatuses.Add(new JiraCount
                        {
                            Category = category ?? "Status",
                            Status = reader[searchParam] == DBNull.Value ? "NA" : (string)reader[searchParam],
                            Count = (int)reader["Count"]
                        });
                    }
                }
            }
            return View(jiraStatuses);
        }


        public IActionResult StatusDetail(string category, string filterby)
        {
            List<Jira> jira = new();
            using (SqlConnection connection = new(connectionString))
            {
                string searchParam = GetParam(category);
                SqlCommand cmd = new($"SELECT * FROM Jiras WHERE IsActive = 1 AND  Status IN ('TO DO', 'RESOLVED', 'IN REVIEW', 'Sent for Approval', 'REOPENED', 'IN PROGRESS') AND {searchParam} = '{filterby}'", connection);
                connection.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.HasRows)
                    {
                        jira.Add(new Jira
                        {
                            JiraId = Convert.ToInt32(reader["JiraId"]),
                            Aging = DateTime.Now.Subtract(Convert.ToDateTime(reader["CreatedDate"])).Days,
                            IssueType = Convert.ToString(reader["IssueType"]),
                            AssignedEmployee = Convert.ToString(reader["AssignedEmployee"]),
                            Assignee = Convert.ToString(reader["Assignee"]),
                            Description = Convert.ToString(reader["Description"]),
                            Priority = Convert.ToString(reader["Priority"]),
                            RootCause = Convert.ToString(reader["RootCause"]),
                            Summary = Convert.ToString(reader["Summary"]),
                            IsActive = Convert.ToBoolean(reader["IsActive"]),
                            ModuleName = Convert.ToString(reader["ModuleName"]),
                            Status = Convert.ToString(reader["Status"]),
                            CreatedBy = Convert.ToString(reader["CreatedBy"]),
                            CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                            UpdatedBy = Convert.ToString(reader["UpdatedBy"]),
                            UpdatedDate = Convert.ToDateTime(reader["UpdatedDate"]),
                            ModuleBranch = Convert.ToString(reader["ModuleBranch"]),
                            Project = Convert.ToString(reader["Project"]),
                            //ReopenedBy = Convert.ToString(reader["reopenedby"]),
                            //ReopenedDate = reader["reopeneddate"] == DBNull.Value ? null : (DateTime?)reader["reopeneddate"],
                            //TestCompletedBy = Convert.ToString(reader["testcompletedby"]),
                            //TestCompletedDate = reader["testcompleteddate"] == DBNull.Value ? null : (DateTime?)reader["testcompleteddate"],
                        });
                    }
                }
            }
            return View(jira);
        }


        public IActionResult Privacy()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static string GetParam(string? filterby)
        {
            return filterby switch
            {
                "name" => "AssignedEmployee",
                "status" => "Status",
                "project" => "Project",
                _ => "Status",
            };
        }
    }
}