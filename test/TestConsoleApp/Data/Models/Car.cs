using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapTo;
using TestConsoleApp.ViewModels;

namespace TestConsoleApp.Data.Models
{
    [MapFrom(typeof(CarReadDto))]
    [UseUpdate]
    partial class Car
    {
        public int Size { get; }
        public int Id { get; }

        public string Brand { get; }

        public Car(int size, int id, string brand)
        {
            Size = size;
            Id = id;
            Brand = brand;
        }
    }
}
