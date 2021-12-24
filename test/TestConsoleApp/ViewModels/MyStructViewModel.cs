using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestConsoleApp.Data.Models;
using MapTo;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(MyStruct))]

    public partial struct MyStructViewModel
    {
        public int SomeInt { get; set; }

        public MyStructViewModel(int someInt)
        {
            SomeInt = someInt;
        }
    }
}
