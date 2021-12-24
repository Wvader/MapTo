using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestConsoleApp.ViewModels;
using MapTo;

namespace TestConsoleApp.Data.Models
{
    [MapFrom(typeof(MyStructViewModel))]
    [UseUpdate]
    public partial struct MyStruct
    {
        public int SomeInt { get; set; }

        public string ReadOnlyString { get; }

        public MyStruct(int someInt, string readOnlyString)
        {
            SomeInt = someInt;
            ReadOnlyString = readOnlyString;
        }

    }
}
