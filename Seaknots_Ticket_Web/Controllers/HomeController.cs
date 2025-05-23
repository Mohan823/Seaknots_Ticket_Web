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
                            Category = category ?? "Name",
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
            StatusDetailVM statusDetail = new StatusDetailVM();
            statusDetail.JiraCount = new List<JiraCount>();
            statusDetail.DailyChart = new List<JiraCount>();
            statusDetail.Jiras = new List<Jira>();
            string searchParam = GetParam(category);

            // Get Jiras based on filter
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand cmd = new($"SELECT * FROM Jiras WHERE IsActive = 1 AND  Status IN ('TO DO', 'RESOLVED', 'IN REVIEW', 'Sent for Approval', 'REOPENED', 'IN PROGRESS', 'Testing', 'NICE TO HAVE') AND {searchParam} = '{filterby}'", connection);
                connection.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.HasRows)
                    {
                        statusDetail.Jiras.Add(new Jira
                        {
                            JiraId = Convert.ToInt32(reader["JiraId"]),
                            Aging = DateTime.Now.Subtract(Convert.ToDateTime(reader["CreatedDate"])).Days,
                            PlannedDate = reader["estOfRep"] != DBNull.Value ? Convert.ToDateTime(reader["estOfRep"]).ToString("dd/MM/yyyy") : "",
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

            // Get status count
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand cmd = new($"SELECT Status, COUNT(*) Count FROM Jiras WHERE IsActive = 1 AND  Status IN ('TO DO', 'RESOLVED', 'IN REVIEW', 'Sent for Approval', 'REOPENED', 'IN PROGRESS', 'Testing', 'NICE TO HAVE') AND {searchParam} = '{filterby}' GROUP BY Status", connection);
                connection.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.HasRows)
                    {
                        statusDetail.JiraCount.Add(new JiraCount
                        {
                            Status = Convert.ToString(reader["Status"]),
                            Count = Convert.ToInt32(reader["Count"]),
                            Category = searchParam,
                            SubCategory = filterby
                        });
                    }
                }
            }

            // Insert into StsTransationHistory if not exists for today
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand cmd = new($"IF NOT EXISTS(SELECT 1 FROM StsTransationHistory WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)) " +
                    $"BEGIN " +
                    $"INSERT INTO StsTransationHistory " +
                    $"SELECT AssignedEmployee , COUNT(*) Count, DATEADD(MINUTE,330, GETDATE()) , 1 " +
                    $"FROM Jiras WHERE IsActive = 1 " +
                    $"AND Status IN ('TO DO', 'RESOLVED', 'IN REVIEW', 'Sent for Approval', 'REOPENED', 'IN PROGRESS', 'Testing', 'NICE TO HAVE') " +
                    $"group by AssignedEmployee " +
                    $"END", connection);
                connection.Open();
                cmd.ExecuteNonQuery();
            }

            // Get daily chart data
            if (searchParam == "AssignedEmployee")
            {
                using (SqlConnection connection = new(connectionString))
                {
                    SqlCommand cmd = new($"SELECT DAY(CreatedAt) AS [Day], SUM(COUNT) AS [Count] FROM StsTransationHistory " +
                        $"WHERE {searchParam} = '{filterby}' " +
                        $"AND CreatedAt >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) " +
                        $"AND CreatedAt < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) " +
                        $"GROUP BY DAY(CreatedAt) ORDER BY [Day];", connection);
                    connection.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader.HasRows)
                        {
                            statusDetail.DailyChart.Add(new JiraCount
                            {
                                Status = Convert.ToString(reader["Day"]),
                                Count = Convert.ToInt32(reader["Count"]),
                                Category = searchParam,
                                SubCategory = filterby
                            });
                        }
                    }
                }
            }

            return View(statusDetail);
        }


        public IActionResult StatusCardDetail(string category, string filterby, string subCategory)
        {
            StatusDetailVM statusDetail = new StatusDetailVM();
            statusDetail.JiraCount = new List<JiraCount>();
            statusDetail.Jiras = new List<Jira>();

            using (SqlConnection connection = new(connectionString))
            {
                string searchParam = GetParam(category);
                SqlCommand cmd = new($"SELECT * FROM Jiras WHERE IsActive = 1 AND  Status = '{filterby}' AND {category} = '{subCategory}'", connection);
                connection.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.HasRows)
                    {
                        statusDetail.Jiras.Add(new Jira
                        {
                            JiraId = Convert.ToInt32(reader["JiraId"]),
                            Aging = DateTime.Now.Subtract(Convert.ToDateTime(reader["CreatedDate"])).Days,
                            PlannedDate = reader["estOfRep"] != DBNull.Value ? Convert.ToDateTime(reader["estOfRep"]).ToString("dd/MM/yyyy") : "",
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

            using (SqlConnection connection = new(connectionString))
            {
                string searchParam = GetParam(category);
                SqlCommand cmd = new($"SELECT Status, COUNT(*) Count FROM Jiras WHERE IsActive = 1 AND  Status IN ('TO DO', 'RESOLVED', 'IN REVIEW', 'Sent for Approval', 'REOPENED', 'IN PROGRESS', 'Testing', 'NICE TO HAVE') AND {searchParam} = '{subCategory}' GROUP BY Status", connection);
                connection.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.HasRows)
                    {
                        statusDetail.JiraCount.Add(new JiraCount
                        {
                            Status = reader["Status"].ToString(),
                            Count = Convert.ToInt32(reader["Count"]),
                            Category = category,
                            SubCategory = subCategory
                        });
                    }
                }
            }

            return View("StatusDetail", statusDetail);
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
                "Name" => "AssignedEmployee",
                "Status" => "Status",
                "Project" => "Project",
                "Module" => "ModuleBranch",
                _ => "AssignedEmployee",
            };
        }
    }
}