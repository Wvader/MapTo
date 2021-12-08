using System;
using BlueWest.Data;
using MapTo;
using TestConsoleApp.Data.Models;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(User))]
    public partial struct UserViewModel
    {
        public int Id { get; set; }

        public DateTimeOffset RegisteredAt { get;  }
    }
}