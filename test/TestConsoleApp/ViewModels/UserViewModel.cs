﻿using MapTo;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(Data.Models.User))]
    public partial class UserViewModel
    {
        public string FirstName { get; }

        // [IgnoreProerty]
        public string LastName { get; }
    }
}