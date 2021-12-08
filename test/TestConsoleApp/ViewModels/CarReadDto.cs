using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapTo;
using TestConsoleApp.Data.Models;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(Car))]
    partial class CarReadDto
    {
        public int Size { get; }
        public string Brand { get; }

        public CarReadDto(int size, string brand)
        {
            Size = size;
            Brand = brand;
        }
    }
}
