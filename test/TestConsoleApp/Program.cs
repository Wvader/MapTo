using System;
using MapTo;
using TestConsoleApp.Data.Models;
using TestConsoleApp.ViewModels;

namespace TestConsoleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //UserTest();
             
            // EmployeeManagerTest();
            Console.WriteLine("done");
        }

        private static void EmployeeManagerTest()
        {
            
            var employee1 = new Employee
            {
                Id = 101,
                EmployeeCode = "E101",
            };

            var employee2 = new Employee
            {
                Id = 102,
                EmployeeCode = "E102",
            };


        }


 
    }
}