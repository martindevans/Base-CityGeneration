using System;
using System.Diagnostics.Contracts;
using System.Linq;
using EpimetheusPlugins.Procedural;

namespace Base_CityGeneration
{
    public static class TreeSearch
    {
        /// <summary>
        /// Search up a tree of nodes looking for a node which can answer a given question.
        /// </summary>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <typeparam name="TNode">The type of the intermediate node which can answer the question</typeparam>
        /// <param name="start">The node to start at</param>
        /// <param name="queryNode">Take an intermediate node and produce a result</param>
        /// <param name="stopTypes">A set of types to stop at if encountered and cancel the search</param>
        /// <returns></returns>
        public static TResult SearchUp<TResult, TNode>(this ISubdivisionContext start, Func<TNode, TResult> queryNode, params Type[] stopTypes)
            where TResult : class
            where TNode : class
        {
            Contract.Requires(start != null);
            Contract.Requires(queryNode != null);
            Contract.Requires(stopTypes != null);

            ISubdivisionContext node = start.Parent;

            while (node != null)
            {
                //Once we find a provider take suggestions from it
                var p = node as TNode;
                if (p != null)
                {
                    var f = queryNode(p);
                    return f;
                }

// ReSharper disable AccessToModifiedClosure
                if (stopTypes.Any(t => t.IsInstanceOfType(node)))
// ReSharper restore AccessToModifiedClosure
                    break;

                //Search further up
                node = node.Parent;
            }

            //Found nothing
            return null;
        }
    }
}
