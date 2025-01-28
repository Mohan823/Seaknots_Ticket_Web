namespace Seaknots_Ticket_Web.Models
{
    public class Jira
    {
        public int? JiraId { get; set; }
        public int? Aging { get; set; }
        public string? Status { get; set; }
        public string? IssueType { get; set; }
        public string? Project { get; set; }
        public string? ModuleName { get; set; }
        public string? ModuleBranch { get; set; }
        public string? AssignedEmployee { get; set; }
        public string? Assignee { get; set; }
        public string? Priority { get; set; }
        public string? Description { get; set; }
        public string? RootCause { get; set; }
        public string? Summary { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? Commands { get; set; }
        //public string? ReopenedBy { get; set; }
        //public DateTime? ReopenedDate { get; set; }
        //public string? TestCompletedBy { get; set; }
        //public DateTime? TestCompletedDate { get; set; }
    }
}
