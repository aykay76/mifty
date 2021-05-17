using System.Collections.Generic;

namespace mifty
{
    public class Catalogue
    {
        public string Label { get; set; }
        public List<MasterFileEntry> Entries { get; set; }
        public List<Catalogue> Children { get; set; }

        public Catalogue FindChild(string label)
        {
            if (Children == null) return null;

            foreach (Catalogue child in Children)
            {
                if (child.Label == label)
                {
                    return child;
                }
            }

            return null;
        }
    }
}