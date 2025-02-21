using homeowner.Models;
using System.Collections.Generic;

namespace homeowner.ViewModels
{
    public class ManageUsersViewModel
    {
        public IEnumerable<UserModel> Users { get; set; }
        public UserModel AddUser { get; set; }
        public UserModel EditUser { get; set; }
    }
}