using System;
using TestConsoleApp.ViewModels;
using MapTo;

namespace TestConsoleApp.Data.Models
{
    [MapFrom(typeof(UserViewModel))]
    public partial class User
    {
        public int Id { get; set; }

        public DateTimeOffset RegisteredAt { get; set; }

    }
}