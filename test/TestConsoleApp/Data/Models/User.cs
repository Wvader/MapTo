using System;
using TestConsoleApp.ViewModels;
using MapTo;
using System.Collections.Generic;

namespace TestConsoleApp.Data.Models
{
    [MapFrom(typeof(UserViewModel))]
    public partial struct User
    {
        public int Id { get; set; }

        public List<List<string>> ListOfListOfString {get; }

        public List<string> StringList { get; }
        public DateTimeOffset RegisteredAt { get; set; }

        public User( int id, List<List<string>>  listOfListOfString, List<string> stringList, DateTimeOffset registeredAt)
        {
            this.StringList = stringList;
            this.Id = id;
            ListOfListOfString = listOfListOfString;
            RegisteredAt = registeredAt;
        }



    }
}