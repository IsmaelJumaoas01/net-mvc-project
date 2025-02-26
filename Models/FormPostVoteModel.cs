using System;

namespace homeowner.Models
{
    public class ForumPostVoteModel
    {
        public int VoteID { get; set; }
        public int UserID { get; set; }
        public int PostID { get; set; }
        // Use sbyte for +1 (upvote) and -1 (downvote)
        public sbyte VoteValue { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
