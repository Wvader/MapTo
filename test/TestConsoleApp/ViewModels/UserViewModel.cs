using System;
using MapTo;
using TestConsoleApp.Data.Models;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(User))]
    public partial class UserViewModel
    {
        public int Id { get; set; }

        public DateTimeOffset RegisteredAt { get;  }
    }
}