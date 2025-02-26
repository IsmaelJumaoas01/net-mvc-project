using System;

namespace homeowner.Models
{
    public class ForumCommentVoteModel
    {
        public int VoteID { get; set; }
        public int UserID { get; set; }
        public int CommentID { get; set; }
        public sbyte VoteValue { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
