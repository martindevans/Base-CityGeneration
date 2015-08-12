using System;
using Base_CityGeneration.Elements.Blocks.Spec.Adjustment;
using Base_CityGeneration.Elements.Blocks.Spec.Lots;
using Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints;
using Base_CityGeneration.Elements.Blocks.Spec.Subdivision;
using Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing;
using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Utilities;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Scripts;
using SharpYaml.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Base_CityGeneration.Elements.Blocks.Spec
{
    public class BlockSpec
    {
        private readonly BaseSubdivideSpec _subdivide;
        public BaseSubdivideSpec Subdivider
        {
            get { return _subdivide; }
        }

        private readonly BaseAdjustmentSpec[] _adjustments;
        public IEnumerable<BaseAdjustmentSpec> Adjustments
        {
            get { return _adjustments; }
        }

        private readonly LotSpec[] _lots;
        public IEnumerable<LotSpec> Lots
        {
            get { return _lots; }
        }

        public BlockSpec(BaseSubdivideSpec subdivide, IEnumerable<BaseAdjustmentSpec> adjustments, IEnumerable<LotSpec> lots)
        {
            _subdivide = subdivide;
            _adjustments = adjustments.ToArray();
            _lots = lots.ToArray();
        }

        public IEnumerable<Parcel> CreateParcels(Parcel shape, Func<double> random)
        {
            //Generate parcels from shape
            var parcels = Subdivider.GenerateParcels(shape, random);

            //Adjust parcels
            foreach (var adjustment in _adjustments)
                parcels = adjustment.Adjust(shape, parcels, random);

            return parcels;
        }

        public ScriptReference SelectLot(Parcel parcel, Func<double> random, Func<string[], ScriptReference> scriptFinder)
        {
            foreach (var lotSpec in _lots)
            {
                if (lotSpec.Check(parcel, random))
                {
                    string[] selected;
                    return lotSpec.Tags.SelectScript(random, scriptFinder, out selected);
                }
            }

            return null;
        }

        #region serialization
        private static Serializer CreateSerializer()
        {
            var serializer = new Serializer(new SerializerSettings
            {
                EmitTags = true,
            });


            //Utility types
            serializer.Settings.RegisterTagMapping("NormalValue", typeof(NormallyDistributedValue.Container));
            serializer.Settings.RegisterTagMapping("UniformValue", typeof(UniformlyDistributedValue.Container));

            //Block Spec Types
            serializer.Settings.RegisterTagMapping("Block", typeof(Container));

            //Parcelling types
            serializer.Settings.RegisterTagMapping("ObbParceller", typeof(ObbParcellerSpec.Container));

            //Parcelling rules
            serializer.Settings.RegisterTagMapping("Area", typeof(AreaRuleSpec.Container));
            serializer.Settings.RegisterTagMapping("Frontage", typeof(FrontageRuleSpec.Container));
            serializer.Settings.RegisterTagMapping("Access", typeof(AccessRuleSpec.Container));

            //Parcelling adjusters
            serializer.Settings.RegisterTagMapping("MergeAdjacent", typeof(MergeAdjacentSpec.Container));

            //Lot constraints
            serializer.Settings.RegisterTagMapping("RequireArea", typeof(RequireAreaSpec.Container));
            serializer.Settings.RegisterTagMapping("RequireAccess", typeof(RequireAccessSpec.Container));

            return serializer;
        }

        public static BlockSpec Deserialize(TextReader reader)
        {
            var s = CreateSerializer();

            return s.Deserialize<Container>(reader).Unwrap();
        }

        internal class Container
        {
            public List<object> Aliases { get; set; }

            public BaseSubdivideSpec.BaseContainer Subdivide { get; set; }

            public BaseAdjustmentSpec.BaseContainer[] Adjustments { get; set; }

            public LotSpec.BaseContainer[] Lots { get; set; }

            public BlockSpec Unwrap()
            {
                return new BlockSpec(
                    Subdivide.Unwrap(),
                    (Adjustments ?? new BaseAdjustmentSpec.BaseContainer[0]).Select(a => a.Unwrap()).ToArray(),
                    (Lots ?? new LotSpec.BaseContainer[0]).Select(a => a.Unwrap()).ToArray()
                );
            }
        }
        #endregion
    }
}
