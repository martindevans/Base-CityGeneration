using System;
using Base_CityGeneration.Elements.Blocks.Spec.Adjustment;
using Base_CityGeneration.Elements.Blocks.Spec.Lots;
using Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints;
using Base_CityGeneration.Elements.Blocks.Spec.Subdivision;
using Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules;
using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Utilities;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using SharpYaml.Serialization;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Base_CityGeneration.Elements.Building;
using JetBrains.Annotations;

namespace Base_CityGeneration.Elements.Blocks.Spec
{
    public class BlockSpec
    {
        public IEnumerable<KeyValuePair<string, string>> Tags { get; private set; }
        public Guid Id { get; private set; }
        public string Description { get; private set; }

        private readonly BaseSubdivideSpec _subdivide;
        public BaseSubdivideSpec Subdivider
        {
            get
            {
                Contract.Ensures(Contract.Result<BaseSubdivideSpec>() != null);
                return _subdivide;
            }
        }

        private readonly BaseAdjustmentSpec[] _adjustments;
        public IEnumerable<BaseAdjustmentSpec> Adjustments
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<BaseAdjustmentSpec>>() != null);
                return _adjustments;
            }
        }

        private readonly LotSpec[] _lots;

        public IEnumerable<LotSpec> Lots
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<LotSpec>>() != null);
                return _lots;
            }
        }

        public BlockSpec(IEnumerable<KeyValuePair<string, string>> tags, Guid id, string description, BaseSubdivideSpec subdivide, IEnumerable<BaseAdjustmentSpec> adjustments, IEnumerable<LotSpec> lots)
        {
            Contract.Requires(tags != null);
            Contract.Requires(subdivide != null);
            Contract.Requires(adjustments != null);
            Contract.Requires(lots != null);

            Tags = tags;
            Id = id;
            Description = description;

            _subdivide = subdivide;
            _adjustments = adjustments.ToArray();
            _lots = lots.ToArray();
        }

        public IEnumerable<Parcel> CreateParcels(Parcel shape, Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(shape != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Ensures(Contract.Result<IEnumerable<Parcel>>() != null);

            //Generate parcels from shape
            var parcels = Subdivider.GenerateParcels(shape, random, metadata);

            //Adjust parcels
            foreach (var adjustment in _adjustments)
                parcels = adjustment.Adjust(shape, parcels, random);

            return parcels;
        }

        public ScriptReference SelectLot(Parcel parcel, Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> scriptFinder)
        {
            Contract.Requires(parcel != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(scriptFinder != null);

            return (from lotSpec in _lots
                    where lotSpec.Check(parcel, random, metadata)
                    let result = lotSpec.Tags.SelectScript(random, scriptFinder, typeof(IBuildingContainer))
                    select result == null ? null : result.Script
            ).FirstOrDefault();
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
            serializer.Settings.RegisterTagMapping("AspectRatio", typeof(AspectRatioRuleSpec.Container));

            //Parcelling adjusters
            // None!

            //Lot constraints
            serializer.Settings.RegisterTagMapping("InvertRequire", typeof(Invert.Container));
            serializer.Settings.RegisterTagMapping("RequireArea", typeof(RequireAreaSpec.Container));
            serializer.Settings.RegisterTagMapping("RequireAccess", typeof(RequireAccessSpec.Container));

            return serializer;
        }

        public static BlockSpec Deserialize(TextReader reader)
        {
            var s = CreateSerializer().Deserialize<Container>(reader);
            Contract.Assume(s != null);
            return s.Unwrap();
        }

        internal class Container
        {
            public Dictionary<string, string> Tags { get; [UsedImplicitly]set; }
            public string Id { get; [UsedImplicitly]set; }
            public string Description { get; [UsedImplicitly]set; }

            public List<object> Aliases { get; [UsedImplicitly]set; }

            public BaseSubdivideSpec.BaseContainer Subdivide { get; [UsedImplicitly]set; }

            public BaseAdjustmentSpec.BaseContainer[] Adjustments { get; [UsedImplicitly]set; }

            public LotSpec.BaseContainer[] Lots { get; [UsedImplicitly]set; }

            public BlockSpec Unwrap()
            {
                Contract.Requires(Subdivide != null);

                return new BlockSpec(
                    Tags ?? (IEnumerable<KeyValuePair<string, string>>)new List<KeyValuePair<string, string>>(),
                    Guid.Parse(Id ?? Guid.NewGuid().ToString()),
                    Description,
                    Subdivide.Unwrap(),
                    (Adjustments ?? new BaseAdjustmentSpec.BaseContainer[0]).Select(a => a.Unwrap()).ToArray(),
                    (Lots ?? new LotSpec.BaseContainer[0]).Select(a => a.Unwrap()).ToArray()
                );
            }
        }
        #endregion
    }
}
