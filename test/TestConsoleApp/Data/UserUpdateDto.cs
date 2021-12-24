using MapTo;

namespace BlueWest.Data
{
    [MapFrom(typeof(User))]

    public partial class UserUpdateDto
    {
        public string Name;
        public string Address;

        public string BTCAddress;
        public string LTCAddress;

        public double BTCAmount;
        public double LTCAmount;

    }
}