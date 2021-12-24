using MapTo.Extensions;
using static MapTo.Sources.Constants;
using System.Collections.Generic;
using System.Text;

namespace MapTo.Sources
{
    internal static class MapStructSource
    {
        internal static SourceCode Generate(MappingModel model)
        {
            return model.GenerateStructOrClass("struct");
        }
    }
}