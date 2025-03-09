using System;
using System.Collections.Generic;

namespace homeowner.Models
{
    public class PollModel
    {
        public int PollID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedByUsername { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<PollOptionModel> Options { get; set; }
        public bool HasVoted { get; set; }
        public int TotalVotes { get; set; }
        public int ResponseCount { get; set; }
    }

    public class PollOptionModel
    {
        public int OptionID { get; set; }
        public int PollID { get; set; }
        public string OptionText { get; set; }
        public int VoteCount { get; set; }
        public double Percentage { get; set; }
    }
} 