using System.Collections.Generic;

namespace BlueWest.Data
{
    public class UserList
    {
        public List<User> Users;

        public UserList(List<User> users)
        {
            Users = users;
        }

        public int Length => Users.Count;
    }
}