namespace Seaknots_Ticket_Web.Models
{
    public class StatusDetailVM
    {
        public List<Jira> Jiras { get; set; }
        public List<JiraCount> JiraCount { get; set; }
        public List<JiraCount> DailyChart { get; set; }
    }
}
