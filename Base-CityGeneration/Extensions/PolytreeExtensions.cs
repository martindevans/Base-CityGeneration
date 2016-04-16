using System.Diagnostics.Contracts;
using ClipperLib;

namespace Base_CityGeneration.Extensions
{
    public static class PolytreeExtensions
    {
        public static void RemoveHoles(this PolyTree tree)
        {
            Contract.Requires(tree != null);

            RemoveHoles((PolyNode)tree);
        }

        private static void RemoveHoles(PolyNode node)
        {
            for (int i = node.ChildCount - 1; i >= 0; i--)
            {
                if (node.Childs[i].IsHole)
                    node.Childs.RemoveAt(i);
                else
                    RemoveHoles(node.Childs[i]);
            }
        }
    }
}
