using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsoleApp.Data.Models
{
   
    public class Employee
    {
        public int Id { get;  }

        public string EmployeeCode { get; }

        public Employee(int id, string employeeCode)
        {
            Id = id;
            EmployeeCode = employeeCode;
        }

    }
}
