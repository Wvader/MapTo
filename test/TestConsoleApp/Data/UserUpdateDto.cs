using MapTo;

namespace BlueWest.Data
{
    [MapFrom(typeof(User))]

    public partial struct UserUpdateDto
    {
        public string Name { get; set; } 
        public string Address { get; set; }
        
        public string BTCAddress { get; set; }
        public string LTCAddress { get; set; }

        public double BTCAmount { get; set; }
        public double LTCAmount { get; set; }

       

    }
}