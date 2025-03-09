using homeowner.Models;
using System.Collections.Generic;

namespace homeowner.ViewModels
{
    public class ManageUsersViewModel
    {
        public List<UserModel> Users { get; set; } = new();
        public UserModel AddUser { get; set; } = new();
        public UserModel EditUser { get; set; } = new();
    }
}