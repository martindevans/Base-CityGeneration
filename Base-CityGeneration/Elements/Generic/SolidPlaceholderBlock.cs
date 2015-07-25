using Base_CityGeneration.Parcelling;
using Base_CityGeneration.Parcelling.Rules;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;
using Myre.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Generic
{
    [Script("AEDE0791-79DF-4936-BD3A-56642105D494", "Solid Block")]
    public class SolidPlaceholderBlock
        : BaseBlock
    {
        private readonly ObbParceller _parceller;

        public SolidPlaceholderBlock()
        {
            _parceller = new ObbParceller(Random);
            _parceller.AddTerminationRule(new AreaRule(100, 500, 0.25f));
            _parceller.AddTerminationRule(new FrontageRule(25, 50, 0.45f, "road"));
            _parceller.AddTerminationRule(new AccessRule("road", 0.75f));
        }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        protected override IEnumerable<Parcel> GenerateParcels(Vector2[] footprint)
        {
            return _parceller.GenerateParcels(new Parcel(footprint, new[] { "road" }));
        }

        protected override IEnumerable<KeyValuePair<Parcel, ISubdivisionContext>> CreateParcelNodes(Parcel[] parcels, float height)
        {
            var roadAccess = new HashSet<Parcel>(parcels.Where(p => p.HasAccess("road")));
            var noRoadAccess = new HashSet<Parcel>(parcels.Where(p => !p.HasAccess("road")));

            //Fill plots with no road access with flat grass
            foreach (var parcel in noRoadAccess)
            {
                var c = CreateChild(new Prism(height, parcel.Points()), Quaternion.Identity, Vector3.Zero, new ScriptReference(typeof(BigFlatPlane)));
                yield return new KeyValuePair<Parcel, ISubdivisionContext>(parcel, c);
            }

            //Fill plots with road access with placeholder blocks (and one safe house)
            foreach (var parcel in roadAccess)
            {
                var script = ScriptReference.Find<SolidPlaceholderBuilding>();

                var c = CreateChild(new Prism(height, parcel.Points()), Quaternion.Identity, Vector3.Zero, script);
                yield return new KeyValuePair<Parcel, ISubdivisionContext>(parcel, c);
            }
        }
    }
}
