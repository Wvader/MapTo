using MapTo.Extensions;
using System.Text;
using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    internal static class MapClassSource
    {
        internal static SourceCode Generate(MappingModel model)
        {
            return model.GenerateStructOrClass("class");
        }
    }
}